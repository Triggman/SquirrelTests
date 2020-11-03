using Afterbunny.Windows.Helpers;
using CygiaSqliteAccess.Proxy;
using CygiaSqliteAccess.Proxy.Proxy;
using DistributedVisionRunner.Interface;
using DistributedVisionRunner.Module.Helper;
using DistributedVisionRunner.Module.Models;
using LoggingConsole.Interface;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using UniversalWeightSystem.Framework.SDK;

namespace DistributedVisionRunner.Module.ViewModels
{
    public class VisionProcessUnitViewModel<TData> : BindableBase
    {
        #region private fields
        private readonly IEventAggregator _ea;
        private readonly IDialogService _dialogService;
        private readonly TypeSource _processorTypeSource;
        private readonly TypeSource _adapterTypeSource;

        private readonly ResponseSocket _serverSocket;
        private ProcessUnitUserSetting _userSetting;
        private string _userSettingPath;
        private string _tableNameRawOnline;
        private string _tableNameWeightedOnline;
        private string _tableNameRawOffline;
        private string _tableNameWeightedOffline;
        private bool _databaseServiceInstalled = true;
        private readonly Regex _namingPattern = new Regex(@"_?[a-zA-Z]+[\w_]*");

        #endregion



        #region props

        public IVisionAdapter<TData> Adapter { get; }
        public IVisionProcessor<TData> Processor { get; }

        private List<WriteableBitmap> _displayImages;
        public List<WriteableBitmap> DisplayImages
        {
            get => _displayImages;
            set => SetProperty(ref _displayImages, value);
        }

        private ObservableCollection<Dictionary<string, object>> _displayData = new ObservableCollection<Dictionary<string, object>>();


        public ObservableCollection<Dictionary<string, object>> DisplayData
        {
            get => _displayData;
            set => SetProperty(ref _displayData, value);
        }

        private bool _isIdle = true;

        public bool IsIdle
        {
            get => _isIdle;
            set => SetProperty(ref _isIdle, value);
        }

        private bool _weightsConfigured;
        private string _weightProjectDir;
        private string _weightProjectFilePath;
        private Dictionary<int, Dictionary<string, double>> _weightCollectionByCavity;
        private Dictionary<string, string> _methodsByOutputName;
        private HashSet<string> _adapterFloatNamesLookup;
        private HashSet<string> _adapterIntegerNamesLookup;
        private HashSet<string> _adapterTextNamesLookup;
        private HashSet<string> _processorOutputNamesLookup;
        private readonly bool _noNamingProblems;

        public bool WeightsConfigured
        {
            get => _weightsConfigured;
            set => SetProperty(ref _weightsConfigured, value);
        }

        public string Name { get; }


        public string ServerAddress { get; }

        public string ImageSaveFolderToday => Path.Combine(_userSetting.ImageSaveMainFolder, DateTime.Now.ToString("yyyy-MM-dd"));

        public ICommand ShowPropertiesCommand { get; }
        public ICommand RunSingleFileCommand { get; }
        public ICommand RunFolderCommand { get; }
        public ICommand OpenSettingDialogCommand { get; }
        public ICommand OpenWeightEditorDialogCommand { get; }
        #endregion

        #region ctor
        public VisionProcessUnitViewModel(IEventAggregator ea, IDialogService dialogService, TypeSource processorTypeSource, TypeSource adapterTypeSource)
        {
            _ea = ea;
            _dialogService = dialogService;
            _processorTypeSource = processorTypeSource;
            _adapterTypeSource = adapterTypeSource;
            Processor = (IVisionProcessor<TData>)Activator.CreateInstance(processorTypeSource.Type);
            Adapter = (IVisionAdapter<TData>)Activator.CreateInstance(adapterTypeSource.Type);
            Name = Adapter.Name;

            _ea.GetEvent<DataEvent>().Subscribe(ProcessDataFromDataEvent);

            ShowPropertiesCommand = new DelegateCommand(ShowProperties);
            RunSingleFileCommand = new DelegateCommand(RunSingleFile);
            RunFolderCommand = new DelegateCommand(RunFolder);
            OpenSettingDialogCommand = new DelegateCommand(OpenSettingDialog);
            OpenWeightEditorDialogCommand = new DelegateCommand(OpenWeightEditorDialog);

            _userSetting = LoadUserSetting(Adapter.Name);

            CreateDatabase();

            _noNamingProblems = CheckNamingProblems();
            if (Adapter.EnableWeighting)
            {
                WeightsConfigured = CheckIfWeightsAreConfigured(Adapter.Name);
                if (WeightsConfigured) ReloadWeights();
            }

            CreateVariableNamesCache();

            // Setup server
            var enableZeroMQText = ConfigurationManager.AppSettings["EnableZeroMQ"];
            var enableZeroMQ = !string.IsNullOrEmpty(enableZeroMQText) && bool.Parse(enableZeroMQText);

            if (enableZeroMQ)
            {
                ServerAddress = ConfigurationManager.AppSettings[$"ServerAddress-{Adapter.Name}"] ?? Adapter.ZeroMQAddress ?? "tcp://localhost:6000";
                _serverSocket = new ResponseSocket(ServerAddress);
                new Thread(ListenForProcessDataFromZeroMQ) { IsBackground = true }.Start();
            }

            GenerateEmptyDisplayImages();
        }



        #endregion

        #region api



        public void Stop()
        {
            _serverSocket?.Close();
        }

        #endregion

        #region impl

        private bool CheckNamingProblems()
        {
            // Check naming of Adapter.OutputNames
            if (Adapter.OutputNames.floatNames != null)
                foreach (var name in Adapter.OutputNames.floatNames)
                {
                    if (!VariableNameIsValid(name, "Adapter")) return false;
                }

            if (Adapter.OutputNames.integerNames != null)
            {
                foreach (var name in Adapter.OutputNames.integerNames)
                {
                    if (!VariableNameIsValid(name, "Adapter")) return false;
                }

                if (Adapter.OutputNames.integerNames.Contains("Cavity"))
                {
                    Log("Adapter.OutputNames.integerNames不能含有Cavity", "Adapter.OutputNames.integerNames can not contains cavity", LogLevel.Fatal);
                    return false;
                }
            }

            if (Adapter.OutputNames.textNames != null)
            {
                foreach (var name in Adapter.OutputNames.textNames)
                {
                    if (!VariableNameIsValid(name, "Adapter")) return false;
                }

                if (Adapter.OutputNames.textNames.Contains("SN"))
                {
                    Log("Adapter.OutputNames.textNames不能含有SN", "Adapter.OutputNames.textNames can not contains SN", LogLevel.Fatal);
                    return false;
                }
            }

            // Check process.OutputNames
            if (Processor.OutputNames == null || Processor.OutputNames.Length == 0)
            {
                Log("Processor.OutputNames未定义",
                    "Processor.OutputNames has not been properly defined", LogLevel.Fatal);
                return false;
            }

            foreach (var name in Processor.OutputNames)
            {
                if (!VariableNameIsValid(name, "Processor")) return false;
            }

            return true;
        }

        /// <summary>
        /// Create variable names cache for validation during data output
        /// </summary>
        private void CreateVariableNamesCache()
        {
            CreateNamesCache(Adapter.OutputNames.floatNames, ref _adapterFloatNamesLookup);
            CreateNamesCache(Adapter.OutputNames.integerNames, ref _adapterIntegerNamesLookup);
            CreateNamesCache(Adapter.OutputNames.textNames, ref _adapterTextNamesLookup);
            CreateNamesCache(Processor.OutputNames, ref _processorOutputNamesLookup);
        }

        private static void CreateNamesCache(string[] names, ref HashSet<string> lookup)
        {
            if (names == null || names.Length == 0) return;
            lookup = new HashSet<string>(names);
        }


        private bool CheckIfWeightsAreConfigured(string adapterName)
        {
            // Check for adapter and processor definition errors
            if (Adapter.OutputNames.floatNames == null || Adapter.OutputNames.floatNames.Length == 0)
            {
                Log("Adapter.OutputNames.floatNames未正确定义",
                    "Adapter.OutputNames.floatNames has not been properly defined", LogLevel.Fatal);
                return false;
            }


            if (Adapter.WeightSetCount < 1)
            {
                Log("Adapter.WeightSetCount未正确定义",
                    "Adapter.WeightSetCount has not been properly defined", LogLevel.Fatal);
                return false;
            }

            if (Processor.WeightNames == null || Processor.WeightNames.Length == 0)
            {
                Log("Processor.WeightNames未正确定义",
                    "Processor.WeightNames has not been properly defined", LogLevel.Fatal);
                return false;
            }



            _weightProjectDir = Path.Combine(Constants.AppDataDir, $"WeightSettings/{adapterName}");
            Directory.CreateDirectory(_weightProjectDir);
            _weightProjectFilePath = Path.Combine(_weightProjectDir, $"{adapterName}.uws");

            if (!File.Exists(_weightProjectFilePath))
            {
                Log("权重未配置", "Weights not configured", LogLevel.Fatal);
                return false;
            }

            // Check weight files
            var (loadedWeights, newlyAddedWeights) = Helpers.LoadWeights(_weightProjectDir, Processor.WeightNames, Adapter.WeightSetCount);
            if (newlyAddedWeights != null && newlyAddedWeights.Any())
            {
                var weightNamesText = string.Join(", ", newlyAddedWeights);
                Log($"存在未配置的权重:{weightNamesText}", $"Weights that are not set: {weightNamesText}", LogLevel.Fatal);
                return false;
            }



            // Check method files
            var (loadedMethods, missingMethods) = Helpers.LoadMethods(_weightProjectDir, Adapter.OutputNames.floatNames);
            if (missingMethods.Any())
            {
                var missingMethodsText = string.Join(", ", missingMethods);
                Log($"存在未配置的权重方法:{missingMethodsText}", $"Methods that are not set: {missingMethodsText}", LogLevel.Fatal);
                return false;
            }

            // Try run weights
            var firstSetOfWeights = loadedWeights[0].WeightItems.ToDictionary(item => item.Name, item => item.Weight);
            var scriptOutputAndExpressions =
                loadedMethods.ToDictionary(m => m.OutputName, m => m.MethodDefinition);
            var random = new Random(42);
            var testInputs = Processor.OutputNames.ToDictionary(name => name, name => random.NextDouble());

            var (output, exceptions) = WeightWeaver.Weight(testInputs, firstSetOfWeights, scriptOutputAndExpressions);

            if (exceptions.Count == 0) return true;

            if (exceptions.Values.All(e => e is DivideByZeroException))
            {
                Log("试运行计算时出现DivideByZeroException", "DivideByZeroException occurred while trying to run weights", LogLevel.Warn);
                return true;
            }

            // Show exception details
            var exceptionDetails = new List<string>();
            foreach (var outputName in exceptions.Keys)
            {
                var exception = exceptions[outputName];
                exceptionDetails.Add($"{outputName}: [{exception.GetType()}] {exception.Message}");
            }

            var exceptionDetailsText = string.Join("\n", exceptionDetails);
            Log($"试运行时出错: \n{exceptionDetailsText}", $"Error occurs while trying to run weights: \n{exceptionDetailsText}", LogLevel.Fatal);

            return false;
        }

        private bool VariableNameIsValid(string name, string adapterOrProcessor)
        {
            if (!_namingPattern.IsMatch(name))
            {
                Log($"{adapterOrProcessor}的变量{name}不符合命名规范",
                    $"Variable {name} from {adapterOrProcessor} does not match naming convention", LogLevel.Fatal);
                return false;
            }

            return true;
        }

        private void OpenWeightEditorDialog()
        {
            _dialogService.ShowDialog("WeightEditorDialog", new DialogParameters{{"Constraint", new WeightConfigurationConstraint()
            {
                ProjectFilePath = _weightProjectFilePath,
                InputNames = Processor.OutputNames,
                OutputNames = Adapter.OutputNames.floatNames,
                WeightNames = Processor.WeightNames,
                WeightSetCount = Adapter.WeightSetCount,
                TabName = Adapter.Name
            } },{ "Login", DistributedVisionRunnerModule.UserLogin }}, r =>
            {
                if (r.Result == ButtonResult.OK)
                {
                    WeightsConfigured = true;
                    ReloadWeights();
                }

            });
        }

        private void ReloadWeights()
        {
            var (loadedWeightCollections, missingWeightNames) =
                Helpers.LoadWeights(_weightProjectDir, Processor.WeightNames, Adapter.WeightSetCount);
            _weightCollectionByCavity = loadedWeightCollections.ToDictionary(c => c.Index,
                c => c.WeightItems.ToDictionary(i => i.Name, i => i.Weight));

            var (loadedMethods, missingMethodNames) =
                Helpers.LoadMethods(_weightProjectDir, Adapter.OutputNames.floatNames);
            _methodsByOutputName = loadedMethods.ToDictionary(m => m.OutputName, m => m.MethodDefinition);
        }


        private void GenerateEmptyDisplayImages()
        {
            var displayImages = new List<WriteableBitmap>();
            var rgb = DistributedVisionRunnerModule.DefaultImageBackground;
            var color = new[] { rgb.r, rgb.g, rgb.b };

            for (int imageIndex = 0; imageIndex < Adapter.GraphicMetaData.Dimensions.Length; imageIndex++)
            {
                var width = Adapter.GraphicMetaData.Dimensions[imageIndex].width;
                var height = Adapter.GraphicMetaData.Dimensions[imageIndex].height;
                var pixelData = new byte[width * height * 3];
                for (int i = 0; i < pixelData.Length; i++)
                {
                    var channel = i % 3;
                    pixelData[i] = color[channel];
                }


                var image = CreateDisplayImage(width, height, pixelData);
                displayImages.Add(image);
            }

            DisplayImages = displayImages;
        }

        private void OpenSettingDialog()
        {
            var settingCopied = Helpers.CopyObject(_userSetting);
            _dialogService.ShowDialog("UserSettingDialog",
                new DialogParameters { { "setting", settingCopied }, { "login", DistributedVisionRunnerModule.UserLogin } }, r =>
                {
                    if (r.Result == ButtonResult.OK)
                    {
                        _userSetting = r.Parameters.GetValue<ProcessUnitUserSetting>("setting");
                        using (var writer = new StreamWriter(_userSettingPath))
                        {
                            var serializer = new XmlSerializer(typeof(ProcessUnitUserSetting));
                            serializer.Serialize(writer, _userSetting);
                        }
                        Log("保存设置成功", "Save setting success");
                    }
                    else
                    {
                        Log("设置未保存", "Settings not save", LogLevel.Warn);
                    }
                });
        }

        private void CreateDatabase()
        {
            var projectName = Adapter.ProjectName ?? "VisionRunner";
            _tableNameRawOnline = $"{projectName}.{Adapter.Name}_Raw_Online";
            _tableNameWeightedOnline = $"{projectName}.{Adapter.Name}_Weighted_Online";
            _tableNameRawOffline = $"{projectName}.{Adapter.Name}_Raw_Offline";
            _tableNameWeightedOffline = $"{projectName}.{Adapter.Name}_Weighted_Offline";

            var integerNamesRaw = new[] { "Cavity" };
            var textNamesRaw = new[] { "SN" };

            var integerNamesWeighted = new List<string>() { "Cavity" };
            if (Adapter.OutputNames.integerNames != null) integerNamesWeighted.AddRange(Adapter.OutputNames.integerNames);
            var textNamesWeighted = new List<string>() { "SN" };
            if (Adapter.OutputNames.textNames != null) textNamesWeighted.AddRange(Adapter.OutputNames.textNames);

            try
            {
                var proxy = new CygiaSqliteAccessProxy(EndPointType.TCP);
                proxy.CreateTable(_tableNameRawOnline, Processor.OutputNames, integerNamesRaw, textNamesRaw);
                proxy.CreateTable(_tableNameRawOffline, Processor.OutputNames, integerNamesRaw, textNamesRaw);
                proxy.CreateTable(_tableNameWeightedOnline, Adapter.OutputNames.floatNames, integerNamesWeighted.ToArray(), textNamesWeighted.ToArray());
                proxy.CreateTable(_tableNameWeightedOffline, Adapter.OutputNames.floatNames, integerNamesWeighted.ToArray(), textNamesWeighted.ToArray());
            }
            catch (System.ServiceModel.EndpointNotFoundException)
            {
                _databaseServiceInstalled = false;
                Log("未安装服务CygiaSqliteAccess.Host.exe, \n请访问https://gitee.com/believingheart/cygia-sqlite-access-service/releases下载安装",
                    "CygiaSqliteAccess.Host.exe not installed", LogLevel.Warn);
            }
        }


        private ProcessUnitUserSetting LoadUserSetting(string name)
        {
            var settingDir = Path.Combine(Constants.AppDataDir, "UserSettings");
            Directory.CreateDirectory(settingDir);

            _userSettingPath = Path.Combine(settingDir, $"{name}.xml");

            if (!File.Exists(_userSettingPath))
            {
                return new ProcessUnitUserSetting() { ImageSaveMainFolder = GetDefaultImageSaveDir() };
            }

            using (var reader = new StreamReader(_userSettingPath))
            {
                var serializer = new XmlSerializer(typeof(ProcessUnitUserSetting));
                return serializer.Deserialize(reader) as ProcessUnitUserSetting;
            }
        }


        private async void RunFolder()
        {
            var dir = FileSystemHelper.GetDirFromDialog(cacheFile: Path.Combine(Constants.AppDataDir, $"Cache/{Adapter.Name}.RunFolder.RecentFolder"));
            if (!string.IsNullOrEmpty(dir))
            {
                var allFiles = Directory.GetFiles(dir);
                var filesThatMatch = allFiles;
                if (Adapter.ImageFileFilter.HasValue) filesThatMatch = allFiles.
                        Where(f => Adapter.ImageFileFilter.Value.extensions.Any(ex => Path.GetExtension(f).Replace(".", "").ToUpper() == ex.ToUpper())).ToArray();

                if (filesThatMatch.Length == 0)
                {
                    Warn("没有找到符合格式的文件");
                    return;
                }

                Log($"读取{filesThatMatch.Length}个文件", $"{filesThatMatch.Length} files are read");
                foreach (var file in filesThatMatch)
                {
                    await ProcessDataFromFile(file);
                }
            }
        }

        private async void RunSingleFile()
        {
            var selectedFile = FileSystemHelper.GetFileFromDialog(pattern: Adapter.ImageFileFilter, cacheFile: Path.Combine(Constants.AppDataDir, $"Cache/{Adapter.Name}.RunSingleFile.RecentFolder"));
            if (!string.IsNullOrEmpty(selectedFile))
            {
                await ProcessDataFromFile(selectedFile);
            }
        }


        private async Task ProcessDataFromFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            Log($"正在离线运行图片{fileName}", $"Running image offline: {fileName}");
            var (cavity, sn) = ParseCavitySN(Path.GetFileNameWithoutExtension(filePath));

            var data = Adapter.ReadFile(filePath);
            await Task.Run(() => ProcessData(data, cavity, sn, DataSourceType.DataFile));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName">
        /// Standard pattern : {sn}_Cavity{cavity}
        /// </param>
        /// <returns></returns>
        private (int cavity, string sn) ParseCavitySN(string fileName)
        {
            // Extract sn
            var snPattern = new Regex(@"^([\da-zA-Z]+)_");
            var snResult = snPattern.Match(fileName);
            var snExtracted = snResult.Success ? snResult.Groups[1].Value : "NoSN";

            // Extract cavity
            var cavityPattern = new Regex(@"Cavity(\d+)");
            var cavityResult = cavityPattern.Match(fileName);
            var cavityExtracted = cavityResult.Success ? int.Parse(cavityResult.Groups[1].Value) : 1;

            // Warn if any parse not success
            if (!snResult.Success || !cavityResult.Success)
            {
                var snWarn = snResult.Success ? string.Empty : "SN";
                var cavityWarn = cavityResult.Success ? string.Empty : "Cavity";
                var warnItems = string.Join(", ", new[] { snWarn, cavityWarn }.Where(t => !string.IsNullOrEmpty(t)));
                Log($"以下文件名信息解析失败: {warnItems}", $"Parsing failures occurs for: {warnItems}", LogLevel.Warn);
            }

            return (cavityExtracted, snExtracted);
        }

        private void ShowProperties()
        {
            var dialogParams = new DialogParameters {
                { "serverAddress", ServerAddress},
                { "adapterAssembly", _adapterTypeSource.AssemblyFilePath},
                { "adapterType", _adapterTypeSource.TypeName},
                { "processorAssembly", _processorTypeSource.AssemblyFilePath},
                { "processorType", _processorTypeSource.TypeName},
            };

            _dialogService.ShowDialog("VisionProcessUnitPropertyDialog", dialogParams, r => { });
        }

        private void ListenForProcessDataFromZeroMQ()
        {
            Log($"启动ZeroMQ服务器({ServerAddress})", $"Started ZeroMQ server at {ServerAddress}");
            while (true)
            {
                var message = _serverSocket.ReceiveMultipartMessage();
                var sn = message[0].ConvertToString();

                var cavity = int.Parse(message[1].ConvertToString());
                var data = message[2].Buffer;
                ProcessData(Adapter.ConvertInput(data), cavity, sn, DataSourceType.ZeroMQ);
            }
        }

        private void ProcessDataFromDataEvent((byte[] data, int cavity, string sn) input)
        {
            ProcessData(Adapter.ConvertInput(input.data), input.cavity, input.sn, DataSourceType.DataEvent);
        }

        private void ProcessData(List<TData[]> data, int cavity, string inputSn, DataSourceType dataSource)
        {
            var weightsNotConfiguredProperly = Adapter.EnableWeighting && !WeightsConfigured;
            if (weightsNotConfiguredProperly || !_noNamingProblems)
            {
                if (weightsNotConfiguredProperly) Log("权重未分配, 数据处理无法进行", "Weights not configured, cancel processing", LogLevel.Fatal);
                if (!_noNamingProblems) Log("存在变量命名问题, 数据处理无法进行", "Naming problems found, cancel processing", LogLevel.Fatal);
                if (dataSource != DataSourceType.DataFile) SendErrorToALC(dataSource);
                return;
            }

            IsIdle = false;
            var now = DateTime.Now;

            try
            {
                var sn = inputSn ?? string.Empty;
                Log($"正在处理来自{dataSource}, 长度为{data.Count}, 夹具编号为{cavity}的数据, sn为{sn}",
                    $"Start processing data of length({data.Count}) of cavity({cavity}), sn({sn}) from data source({dataSource})");

                var stopwatch = Stopwatch.StartNew();
                ProcessResult<TData> result = null;
                try
                {
                    result = Processor.Process(data);

                }
                catch (Exception ex)
                {
                    // Save image file
                    var exDetail = $"{ex.GetType()} \n{ex.Message}\n {ex.StackTrace}";
                    if (dataSource != DataSourceType.DataFile)
                        SaveImage(data, cavity, "ERROR", sn, exDetail, now);
                    Log($"视觉处理出现异常: {exDetail}", $"Errored during vision processing: {exDetail}", LogLevel.Fatal);

                    // Report ex to ALC
                    if (dataSource == DataSourceType.DataEvent)
                        _ea.GetEvent<VisionResultEvent>().Publish(new StatisticsResults()
                        { ResultType = ResultType.ERROR });
                    if (dataSource != DataSourceType.DataFile)
                    {
                        SendErrorToALC(dataSource);
                    }

                    return;
                }

                var statistics = result.Statistics;
                var rawData = statistics.FloatResults;
                // Check if processor give results that match what it promised
                // at the first run

                var namesAreEqual = CompareNames(_processorOutputNamesLookup, rawData.Keys.ToArray());
                if (!namesAreEqual)
                {
                    Log($"Processor的输出数据种类({ConcatStrings(rawData.Keys.ToArray())})与其定义的不相符,\n 请检查类的定义和输出类型",
                        $"The result of processor does not match what it promised", LogLevel.Fatal);
                    if (dataSource != DataSourceType.DataFile) SendErrorToALC(dataSource);
                    return;
                }


                statistics.FloatResults = Weight(statistics.FloatResults, cavity);
                var resultType = Adapter.GetResultType(statistics);
                var ms = stopwatch.ElapsedMilliseconds;
                Log($"计算耗时{ms}ms", $"Data process finished in {ms} ms");

                // Check if adapter give results that match what it promised
                // at the first run

                var promiseNamesAndActualNames = new List<(string kind, HashSet<string> promiseNames, string[] actualNames)>
                    {
                        ("AdapterFloatNames",_adapterFloatNamesLookup, statistics.FloatResults.Keys.ToArray()),
                        ("AdapterIntegerNames",_adapterIntegerNamesLookup, statistics.IntegerResults.Keys.ToArray()),
                        ("AdapterTextNames",_adapterTextNamesLookup, statistics.TextResults.Keys.ToArray())
                    };

                foreach (var (kind, promiseNames, actualNames) in promiseNamesAndActualNames)
                {
                    namesAreEqual = CompareNames(promiseNames, actualNames);
                    if (!namesAreEqual)
                    {
                        Log($"{kind} 的输出数据种类({ConcatStrings(actualNames)})与其定义的不相符,\n 请检查类的定义和输出类型",
                            $"The result of {kind} does not match what it promised", LogLevel.Fatal);
                        if (dataSource != DataSourceType.DataFile) SendErrorToALC(dataSource);
                        return;
                    }
                }




                if (dataSource != DataSourceType.DataFile) ReportResult(statistics, resultType, dataSource);

                DisplayStatisticResults(statistics, cavity, sn, now);

                if (Adapter.GraphicMetaData.ShouldDisplay)
                {
                    if (Adapter.GraphicMetaData.SampleType != DataSampleType.OneDimension)
                        ShowImages(result.DisplayData, Adapter.GraphicMetaData);
                    else ShowChart(result.DisplayData);
                }

                if (dataSource != DataSourceType.DataFile && _userSetting.ImageSaveFilter != ImageSaveFilter.ErrorOnly)
                {

                    if (!(_userSetting.ImageSaveFilter == ImageSaveFilter.ErrorAndNg && resultType == ResultType.OK))
                    {
                        var subFolder = string.Empty;
                        if (_userSetting.ImageSaveSchema == ImageSaveSchema.OkNgInOneFolder)
                        {
                            subFolder = "OkAndNg";
                        }
                        else
                        {
                            subFolder = resultType.ToString();
                        }

                        SaveImage(data, cavity, subFolder, sn, null, now);
                    }
                }

                // Write to database
                if (_databaseServiceInstalled)
                {
                    // If save any data
                    if (_userSetting.SaveRawDataOffline || _userSetting.SaveRawDataOnline ||
                        _userSetting.SaveWeightedDataOffline || _userSetting.SaveWeightedDataOnline)
                    {
                        var proxy = new CygiaSqliteAccessProxy(EndPointType.TCP);
                        // If save raw data
                        if (_userSetting.SaveRawDataOffline || _userSetting.SaveRawDataOnline)
                        {
                            var integerFields = new[] { new IntegerField() { Name = "Cavity", Value = cavity } };
                            var textFields = new[] { new TextField() { Name = "SN", Value = sn } };
                            var rowDatas = new[]
                            {
                                new RowData
                                {
                                    CreationTime = now,
                                    DoubleFields = rawData.Select(p => new DoubleField {Name = p.Key, Value = p.Value})
                                        .ToArray(),
                                    IntegerFields = integerFields,
                                    TextFields = textFields
                                }
                            };

                            if (_userSetting.SaveRawDataOffline && dataSource == DataSourceType.DataFile)
                            {
                                Log("保存离线原始数据", "Saved offline raw data");
                                proxy.Insert(_tableNameRawOffline, rowDatas);
                            }

                            if (_userSetting.SaveRawDataOnline && dataSource != DataSourceType.DataFile)
                            {
                                Log("保存在线原始数据", "Saved online raw data");
                                proxy.Insert(_tableNameRawOnline, rowDatas);
                            }
                        }

                        // If save weighted data
                        if (_userSetting.SaveWeightedDataOffline || _userSetting.SaveWeightedDataOnline)
                        {
                            var integerFields = statistics.IntegerResults
                                .Select(p => new IntegerField { Name = p.Key, Value = p.Value }).ToList();
                            integerFields.Insert(0, new IntegerField { Name = "Cavity", Value = cavity });
                            var textFields = statistics.TextResults
                                .Select(p => new TextField { Name = p.Key, Value = p.Value }).ToList();
                            textFields.Insert(0, new TextField { Name = "SN", Value = sn });

                            var rowDatas = new[]
                            {
                                new RowData
                                {
                                    CreationTime = now,
                                    DoubleFields = statistics.FloatResults
                                        .Select(p => new DoubleField {Name = p.Key, Value = p.Value}).ToArray(),
                                    IntegerFields = integerFields.ToArray(),
                                    TextFields = textFields.ToArray()
                                }
                            };

                            if (_userSetting.SaveWeightedDataOffline && dataSource == DataSourceType.DataFile)
                            {
                                Log("保存离线计算数据", "Saved offline weighted data");
                                proxy.Insert(_tableNameWeightedOffline, rowDatas);
                            }

                            if (_userSetting.SaveWeightedDataOnline && dataSource != DataSourceType.DataFile)
                            {
                                Log("保存在线计算数据", "Saved online weighted data");
                                proxy.Insert(_tableNameWeightedOnline, rowDatas);
                            }
                        }
                    }
                }
            }
            finally
            {
                IsIdle = true;
            }

        }

        private static string ConcatStrings(string[] array)
        {
            if (array == null || array.Length == 0) return string.Empty;
            return string.Join(",", array);
        }

        private void SaveImage(List<TData[]> data, int cavity, string subFolder, string sn, string exceptionDetail,
            DateTime dateTime)
        {
            var dir = Path.Combine(ImageSaveFolderToday, subFolder);
            Directory.CreateDirectory(dir);
            var snPart = string.IsNullOrEmpty(sn) ? "NoSN_" : $"{sn}_";
            Adapter.SaveImage(data, ImageSaveFolderToday, subFolder, $"{snPart}Cavity{cavity}_{dateTime:MMdd-HHmm-ss-fff}", exceptionDetail);
        }

        private void SendErrorToALC(DataSourceType dataSourceType)
        {
            if (dataSourceType == DataSourceType.ZeroMQ)
            {
                var json = JsonConvert.SerializeObject(new StatisticsResults() { ResultType = ResultType.ERROR });
                _serverSocket.SendFrame(json);
            }
            else if (dataSourceType == DataSourceType.DataEvent)
            {
                _ea.GetEvent<VisionResultEvent>().Publish(new StatisticsResults() { ResultType = ResultType.ERROR });
            }
        }

        /// <summary>
        /// Compare two array of names
        /// </summary>
        /// <param name="promiseNames"></param>
        /// <param name="actualNames"></param>
        /// <returns>
        /// true if all matched
        /// </returns>
        private bool CompareNames(HashSet<string> promiseNames, string[] actualNames)
        {
            if (promiseNames == null)
            {
                if (actualNames == null || actualNames.Length == 0)
                {
                    return true;
                }
                return false;
            }
            else
            {
                if (actualNames == null || actualNames.Length == 0) return false;
                if (actualNames.Length != promiseNames.Count) return false;

                return actualNames.All(promiseNames.Contains);
            }

        }

        private Dictionary<string, float> Weight(Dictionary<string, float> inputFloats, int cavity)
        {
            if (!Adapter.EnableWeighting) return inputFloats;

            var selectedWeightCollection = _weightCollectionByCavity[cavity];

            var testInputs = inputFloats.ToDictionary(p => p.Key, p => (double)p.Value);

            var (output, exceptions) = WeightWeaver.Weight(testInputs, selectedWeightCollection, _methodsByOutputName);

            return output.ToDictionary(p => p.Key, p => (float)p.Value);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cavity"></param>
        /// <param name="sn"></param>
        /// <returns>
        /// {sn}_Cavity{cavity}
        /// </returns>
        private string GetImageFileName(int cavity, string sn)
        {
            var snPart = string.IsNullOrEmpty(sn) ? "NoSN_" : $"{sn}_";

            return $"{snPart}Cavity{cavity}";
        }

        private static string GetDefaultImageSaveDir()
        {
            // Determine log dir
            // Log dir sits at the first drive that is larger than 100 GB behind c drive
            var drives = DriveInfo.GetDrives();

            var gb100 = 102400000000L;
            var storageDrive = drives.FirstOrDefault(d => !d.Name.StartsWith("C") && d.TotalSize > gb100)?.Name;

            return storageDrive ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "DistributedVisionRunnerImages");
        }

        private void ShowChart(List<TData[]> displayData)
        {
            throw new NotImplementedException();
        }

        private void DisplayStatisticResults(Statistics statistics, int cavity, string sn, DateTime dateTime)
        {
            var rowData = new Dictionary<string, object>()
            {
                ["Time"] = dateTime.ToString("HH:mm:ss fff"),
                ["Cavity"] = cavity,
                ["SN"] = sn
            };

            if (statistics.FloatResults != null)
            {
                foreach (var item in statistics.FloatResults)
                {
                    rowData[item.Key] = item.Value;
                }
            }
            if (statistics.IntegerResults != null)
            {
                foreach (var item in statistics.IntegerResults)
                {
                    rowData[item.Key] = item.Value;
                }
            }
            if (statistics.TextResults != null)
            {
                foreach (var item in statistics.TextResults)
                {
                    rowData[item.Key] = item.Value;
                }
            }

            DisplayData.Add(rowData);

            if (DisplayData.Count > 100) DisplayData = new ObservableCollection<Dictionary<string, object>>(DisplayData.Skip(50));
        }

        private void ShowImages(List<TData[]> displayData, GraphicMetaData graphicMetaData)
        {
            Application.Current.Dispatcher?.InvokeAsync(() =>
            {

                if (displayData[0] is byte[])
                {
                    ShowByteImages(displayData.Cast<byte[]>().ToList(), graphicMetaData);
                }
                else if (displayData[0] is float[])
                {
                    ShowFloatImages(displayData.Cast<float[]>().ToList(), graphicMetaData);
                }
                else if (displayData[0] is ushort[])
                {
                    ShowUShortImages(displayData.Cast<ushort[]>().ToList(), graphicMetaData);
                }
                else if (displayData[0] is short[])
                {
                    ShowShortImages(displayData.Cast<short[]>().ToList(), graphicMetaData);
                }

                OnPropertyChanged(new PropertyChangedEventArgs(nameof(DisplayImages)));
            });
        }

        private static byte[] ScaleToDisplayRange(short[] inputArray, float min, float max)
        {
            var output = new byte[inputArray.Length];
            var range = max - min;
            for (int index = 0; index < inputArray.Length; index++)
            {
                var absValue = inputArray[index] - min;
                var scaledValue = (byte)(absValue / range * 255);
                output[index] = scaledValue;
            }

            return output;
        }

        private static byte[] ScaleToDisplayRange(ushort[] inputArray, float min, float max)
        {
            var output = new byte[inputArray.Length];
            var range = max - min;
            for (int index = 0; index < inputArray.Length; index++)
            {
                var absValue = inputArray[index] - min;
                var scaledValue = (byte)(absValue / range * 255);
                output[index] = scaledValue;
            }

            return output;
        }

        private static byte[] ScaleToDisplayRange(float[] inputArray, float min, float max)
        {
            var output = new byte[inputArray.Length];
            var range = max - min;
            for (int index = 0; index < inputArray.Length; index++)
            {
                var absValue = inputArray[index] - min;
                var scaledValue = (byte)(absValue / range * 255);
                output[index] = scaledValue;
            }

            return output;
        }


        private void ShowShortImages(List<short[]> shortArray, GraphicMetaData graphicMetaData)
        {
            var displayData = new List<byte[]>();
            for (int imageIndex = 0; imageIndex < shortArray.Count; imageIndex++)
            {
                var scaledData = ScaleToDisplayRange(shortArray[imageIndex], Adapter.GraphicMetaData.DataRange.min, Adapter.GraphicMetaData.DataRange.max);
                displayData.Add(scaledData);
            }
            ShowByteImages(displayData, graphicMetaData);
        }



        private void ShowUShortImages(List<ushort[]> ushortArray, GraphicMetaData graphicMetaData)
        {
            var displayData = new List<byte[]>();
            for (int imageIndex = 0; imageIndex < ushortArray.Count; imageIndex++)
            {
                var scaledData = ScaleToDisplayRange(ushortArray[imageIndex], Adapter.GraphicMetaData.DataRange.min, Adapter.GraphicMetaData.DataRange.max);
                displayData.Add(scaledData);
            }
            ShowByteImages(displayData, graphicMetaData);
        }



        private void ShowFloatImages(List<float[]> floatArray, GraphicMetaData graphicMetaData)
        {
            var displayData = new List<byte[]>();
            for (int imageIndex = 0; imageIndex < floatArray.Count; imageIndex++)
            {
                var scaledData = ScaleToDisplayRange(floatArray[imageIndex], Adapter.GraphicMetaData.DataRange.min, Adapter.GraphicMetaData.DataRange.max);
                displayData.Add(scaledData);
            }
            ShowByteImages(displayData, graphicMetaData);
        }

        private void ShowByteImages(List<byte[]> displayData, GraphicMetaData graphicMetaData)
        {
            for (int imageIndex = 0; imageIndex < displayData.Count; imageIndex++)
            {
                byte[] pixelData;
                var currentImageData = displayData[imageIndex];
                if (graphicMetaData.SampleType == DataSampleType.TwoDimension)
                {
                    pixelData = new byte[currentImageData.Length * 3];
                    for (int i = 0; i < currentImageData.Length; i++)
                    {
                        var start = i * 3;
                        for (int offset = 0; offset < 3; offset++)
                        {
                            pixelData[start + offset] = currentImageData[i];
                        }
                    }
                }
                else
                {
                    pixelData = currentImageData;
                }


                UpdateDisplayImage(DisplayImages[imageIndex], pixelData, graphicMetaData.Dimensions[imageIndex].width, graphicMetaData.Dimensions[imageIndex].height);

            }


        }

        private WriteableBitmap CreateDisplayImage(int width, int height, byte[] pixelData)
        {
            var writeableBitmap = new WriteableBitmap(width, height, 96.0, 96.0, PixelFormats.Rgb24, BitmapPalettes.Halftone256Transparent);
            writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelData, width * 3, 0);
            return writeableBitmap;
        }

        private void UpdateDisplayImage(WriteableBitmap image, byte[] rgbData, int width, int height)
        {
            image.WritePixels(new Int32Rect(0, 0, width, height), rgbData, width * 3, 0);
        }

        private void ReportResult(Statistics statistics, ResultType resultType, DataSourceType dataSource)
        {
            var statisticResults = new StatisticsResults(statistics.FloatResults, statistics.IntegerResults, statistics.TextResults) { ResultType = resultType };
            if (dataSource == DataSourceType.DataEvent) _ea.GetEvent<VisionResultEvent>().Publish(statisticResults);
            else if (dataSource == DataSourceType.ZeroMQ)
            {
                // Serialize statistics 
                var json = JsonConvert.SerializeObject(statisticResults);
                _serverSocket.SendFrame(json);
            }
            Log("发送计算结果", "Reported statistic results");
        }

        private void Log(string displayMessage, string saveMessage, LogLevel logLevel = LogLevel.Info)
        {
            DistributedVisionRunnerModule.Log(new LoggingMessageItem($"({Adapter.Name}): {displayMessage}", $"VisionPage({Adapter.Name}): {saveMessage}") { LogLevel = logLevel });
        }

        private void Warn(string message)
        {
            _dialogService.ShowDialog("VisionRunnerNotificationDialog", new DialogParameters { { "message", message } }, r => { });
        }
        #endregion
    }

    public class VisionProcessUnitByte : VisionProcessUnitViewModel<byte>
    {
        public VisionProcessUnitByte(IEventAggregator ea, IDialogService dialogService, TypeSource processorTypeSource, TypeSource adapterTypeSource) : base(ea, dialogService, processorTypeSource, adapterTypeSource)
        {
        }
    }

    public class VisionProcessUnitFloat : VisionProcessUnitViewModel<float>
    {
        public VisionProcessUnitFloat(IEventAggregator ea, IDialogService dialogService, TypeSource processorTypeSource, TypeSource adapterTypeSource) : base(ea, dialogService, processorTypeSource, adapterTypeSource)
        {
        }
    }

    public class VisionProcessUnitShort : VisionProcessUnitViewModel<short>
    {
        public VisionProcessUnitShort(IEventAggregator ea, IDialogService dialogService, TypeSource processorTypeSource, TypeSource adapterTypeSource) : base(ea, dialogService, processorTypeSource, adapterTypeSource)
        {
        }
    }


    public class VisionProcessUnitUShort : VisionProcessUnitViewModel<ushort>
    {
        public VisionProcessUnitUShort(IEventAggregator ea, IDialogService dialogService, TypeSource processorTypeSource, TypeSource adapterTypeSource) : base(ea, dialogService, processorTypeSource, adapterTypeSource)
        {
        }
    }


}
