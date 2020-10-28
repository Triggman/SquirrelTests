using CygiaSqliteAccess.Proxy;
using CygiaSqliteAccess.Proxy.Proxy;
using LoggingConsole.Interface;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using RemoteVisionConsole.Data;
using RemoteVisionConsole.Interface;
using RemoteVisionConsole.Module.Helper;
using RemoteVisionConsole.Module.Models;
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

namespace RemoteVisionConsole.Module.ViewModels
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
        private string _weightProjectFilePath;

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

        private bool _isIdle;

        public bool IsIdle
        {
            get => _isIdle;
            set => SetProperty(ref _isIdle, value);
        }

        private bool _weightsConfigured;

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
            if (Adapter.EnableWeighting)
            {
                WeightsConfigured = CheckIfWeightsAreConfigured(Adapter.Name);
            }

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

        private bool CheckIfWeightsAreConfigured(string adapterName)
        {
            var dir = Path.Combine(Constants.AppDataDir, $"WeightSettings/{adapterName}");
            Directory.CreateDirectory(dir);
            _weightProjectFilePath = Path.Combine(dir, $"{adapterName}.uws");

            if (!File.Exists(_weightProjectFilePath))
            {
                var projectItem = new ProjectItem()
                {
                    InputNamesCsv = string.Join(",", Processor.OutputNames.floatNames),
                    WeightSets = Adapter.WeightSetCount,
                    WeightNamesCsv = string.Join(",", Processor.WeightNames)
                };
                using (var writer = new StreamWriter(_weightProjectFilePath))
                {
                    var serializer = new XmlSerializer(projectItem.GetType());
                    serializer.Serialize(writer, projectItem);
                }
            }

            // Check weight files
            var weightFilesDir = Path.Combine(dir, "Weights");
            Directory.CreateDirectory(weightFilesDir);
            var weightFilesCount = Directory.GetFiles(weightFilesDir).Count(f => f.EndsWith("weight"));
            if (weightFilesCount < Adapter.WeightSetCount)
            {
                Log("缺少权重文件, 请配置权重", "Weight files not enough", LogLevel.Fatal);
                return false;
            }

            // Check method files
            var methodFilesDir = Path.Combine(dir, "Methods");
            Directory.CreateDirectory(methodFilesDir);
            var methodFileNames = Directory.GetFiles(methodFilesDir).Where(f => f.EndsWith("calc")).Select(Path.GetFileNameWithoutExtension).ToArray();
            var predefinedMethodNames = Adapter.OutputNames.floatNames;
            if (methodFileNames.Length < predefinedMethodNames.Length)
            {
                Log("缺少权重计算文件, 请配置权重", "Weight method files not enough", LogLevel.Fatal);
                return false;
            }

            var allMethodsDefined = predefinedMethodNames.All(pre => methodFileNames.Contains(pre));
            return allMethodsDefined;
        }
        private void OpenWeightEditorDialog()
        {
            _dialogService.ShowDialog("WeightEditorDialog", new DialogParameters{{"Constraint", new WeightConfigurationConstraint()
            {
                ProjectFilePath = _weightProjectFilePath,
                InputNames = Processor.OutputNames.floatNames,
                OutputNames = Adapter.OutputNames.floatNames,
                WeightNames = Processor.WeightNames,
                WeightSetCount = Adapter.WeightSetCount
            } },{ "Login", RemoteVisionConsoleModule.UserLogin }}, r =>
            {
                if (r.Result == ButtonResult.OK)
                {
                    WeightsConfigured = true;
                }

            });
        }



        private void GenerateEmptyDisplayImages()
        {
            var displayImages = new List<WriteableBitmap>();
            var rgb = RemoteVisionConsoleModule.DefaultImageBackground;
            var color = new[] { rgb.R, rgb.G, rgb.B };

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
                new DialogParameters { { "setting", settingCopied }, { "login", RemoteVisionConsoleModule.UserLogin } }, r =>
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
            var projectName = Adapter.ProjectName ?? "VisionConsole";
            _tableNameRawOnline = $"{projectName}.{Adapter.Name}_Raw_Online";
            _tableNameWeightedOnline = $"{projectName}.{Adapter.Name}_Weighted_Online";
            _tableNameRawOffline = $"{projectName}.{Adapter.Name}_Raw_Offline";
            _tableNameWeightedOffline = $"{projectName}.{Adapter.Name}_Weighted_Offline";

            var integerNamesRaw = new List<string>() { "Cavity" };
            if (Processor.OutputNames.integerNames != null) integerNamesRaw.AddRange(Processor.OutputNames.integerNames);
            var integerNamesWeighted = new List<string>() { "Cavity" };
            if (Adapter.OutputNames.integerNames != null) integerNamesWeighted.AddRange(Adapter.OutputNames.integerNames);

            try
            {
                var proxy = new CygiaSqliteAccessProxy(EndPointType.TCP);
                proxy.CreateTable(_tableNameRawOnline, Processor.OutputNames.floatNames, integerNamesRaw.ToArray(), Processor.OutputNames.textNames);
                proxy.CreateTable(_tableNameRawOffline, Processor.OutputNames.floatNames, integerNamesRaw.ToArray(), Processor.OutputNames.textNames);
                proxy.CreateTable(_tableNameWeightedOnline, Adapter.OutputNames.floatNames, integerNamesWeighted.ToArray(), Adapter.OutputNames.textNames);
                proxy.CreateTable(_tableNameWeightedOffline, Adapter.OutputNames.floatNames, integerNamesWeighted.ToArray(), Adapter.OutputNames.textNames);
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
            var dir = Helpers.GetDirFromDialog();
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
            var selectedFile = Helpers.GetFileFromDialog(pattern: Adapter.ImageFileFilter);
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
            var snPattern = new Regex(@"^(\w+)_");
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
            if (Adapter.EnableWeighting && !WeightsConfigured)
            {
                Log("权重未分配, 数据处理无法进行", "Weights not configured, cancel processing");
                return;
            }

            IsIdle = false;

            try
            {
                var sn = inputSn ?? string.Empty;
                Log($"正在处理来自{dataSource}, 长度为{data.Count}, 夹具编号为{cavity}的数据, sn为{sn}", $"Start processing data of length({data.Count}) of cavity({cavity}), sn({sn}) from data source({dataSource})");

                var stopwatch = Stopwatch.StartNew();
                ProcessResult<TData> result = null;
                try
                {
                    result = Processor.Process(data, cavity);

                }
                catch (Exception ex)
                {
                    // Save image file
                    var exDetail = $"{ex.GetType()} \n{ex.Message}\n {ex.StackTrace}";
                    if (dataSource != DataSourceType.DataFile) Adapter.SaveImage(data, ImageSaveFolderToday, "ERROR", GetImageFileName(cavity, sn), exDetail);
                    else Log($"视觉处理出现异常: {exDetail}", $"Errored during vision processing: {exDetail}", LogLevel.Fatal);

                    // Report ex to ALC
                    if (dataSource == DataSourceType.DataEvent) _ea.GetEvent<VisionResultEvent>().Publish(new StatisticsResults() { ResultType = ResultType.ERROR });
                    if (dataSource == DataSourceType.ZeroMQ)
                    {
                        var json = JsonConvert.SerializeObject(new StatisticsResults() { ResultType = ResultType.ERROR });
                        _serverSocket.SendFrame(json);
                    }

                    return;
                }

                var resultType = Adapter.GetResultType(result.Statistics);
                var weightedStatistics = Adapter.Weight(result.Statistics);
                var ms = stopwatch.ElapsedMilliseconds;
                Log($"计算耗时{ms}ms", $"Data process finished in {ms} ms");

                if (dataSource != DataSourceType.DataFile) ReportResult(weightedStatistics, resultType, dataSource);

                DisplayStatisticResults(weightedStatistics, cavity);

                if (Adapter.GraphicMetaData.ShouldDisplay)
                {
                    if (Adapter.GraphicMetaData.SampleType != DataSampleType.OneDimension)
                        ShowImages(result.DisplayData, Adapter.GraphicMetaData);
                    else ShowChart(result.DisplayData);
                }

                if (dataSource != DataSourceType.DataFile && _userSetting.ImageSaveFilter != ImageSaveFilter.ErrorOnly)
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

                    Adapter.SaveImage(data, ImageSaveFolderToday, subFolder, GetImageFileName(cavity, sn), null);
                }

                // Write to database
                if (_databaseServiceInstalled)
                {
                    // If save any data
                    if (_userSetting.SaveRawDataOffline || _userSetting.SaveRawDataOnline || _userSetting.SaveWeightedDataOffline || _userSetting.SaveWeightedDataOnline)
                    {
                        var proxy = new CygiaSqliteAccessProxy(EndPointType.TCP);
                        // If save raw data
                        if (_userSetting.SaveRawDataOffline || _userSetting.SaveRawDataOnline)
                        {
                            var integerFields = result.Statistics.IntegerResults.Select(p => new IntegerField { Name = p.Key, Value = p.Value }).ToList();
                            integerFields.Insert(0, new IntegerField { Name = "Cavity", Value = cavity });
                            var rowDatas = new[] { new RowData {
                            CreationTime = DateTime.Now,
                DoubleFields = result.Statistics.FloatResults.Select(p=> new DoubleField{Name = p.Key, Value = p.Value}).ToArray(),
                IntegerFields = integerFields.ToArray(),
                TextFields = result.Statistics.TextResults.Select(p=> new TextField{Name = p.Key, Value = p.Value}).ToArray(),
                } };

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
                            var integerFields = weightedStatistics.IntegerResults.Select(p => new IntegerField { Name = p.Key, Value = p.Value }).ToList();
                            integerFields.Insert(0, new IntegerField { Name = "Cavity", Value = cavity });
                            var rowDatas = new[] { new RowData {
                            CreationTime = DateTime.Now,
                DoubleFields = weightedStatistics.FloatResults.Select(p=> new DoubleField{Name = p.Key, Value = p.Value}).ToArray(),
                IntegerFields = integerFields.ToArray(),
                TextFields = weightedStatistics.TextResults.Select(p=> new TextField{Name = p.Key, Value = p.Value}).ToArray(),
                } };

                            if (_userSetting.SaveWeightedDataOffline && dataSource == DataSourceType.DataFile)
                            {
                                Log("保存离线计算数据", "Saved offline weighted data");
                                proxy.Insert(_tableNameWeightedOffline, rowDatas);
                            }
                            if (_userSetting.SaveWeightedDataOnline && dataSource != DataSourceType.DataFile)
                            {
                                Log("保存在线计算数据", "Saved oneline weighted data");
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

            return storageDrive ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "RemoteVisionConsoleImages");
        }

        private void ShowChart(List<TData[]> displayData)
        {
            throw new NotImplementedException();
        }

        private void DisplayStatisticResults(Statistics statistics, int cavity)
        {
            var rowData = new Dictionary<string, object>()
            {
                ["Time"] = DateTime.Now.ToString("HH:mm:ss fff"),
                ["Cavity"] = cavity
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
                var scaledData = ScaleToDisplayRange(shortArray[imageIndex], Adapter.DataRange.min, Adapter.DataRange.max);
                displayData.Add(scaledData);
            }
            ShowByteImages(displayData, graphicMetaData);
        }



        private void ShowUShortImages(List<ushort[]> ushortArray, GraphicMetaData graphicMetaData)
        {
            var displayData = new List<byte[]>();
            for (int imageIndex = 0; imageIndex < ushortArray.Count; imageIndex++)
            {
                var scaledData = ScaleToDisplayRange(ushortArray[imageIndex], Adapter.DataRange.min, Adapter.DataRange.max);
                displayData.Add(scaledData);
            }
            ShowByteImages(displayData, graphicMetaData);
        }



        private void ShowFloatImages(List<float[]> floatArray, GraphicMetaData graphicMetaData)
        {
            var displayData = new List<byte[]>();
            for (int imageIndex = 0; imageIndex < floatArray.Count; imageIndex++)
            {
                var scaledData = ScaleToDisplayRange(floatArray[imageIndex], Adapter.DataRange.min, Adapter.DataRange.max);
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
            var statisticReuslts = new StatisticsResults(statistics.FloatResults, statistics.IntegerResults, statistics.TextResults) { ResultType = resultType };
            if (dataSource == DataSourceType.DataEvent) _ea.GetEvent<VisionResultEvent>().Publish(statisticReuslts);
            else if (dataSource == DataSourceType.ZeroMQ)
            {
                // Serialize statistics 
                var json = JsonConvert.SerializeObject(statisticReuslts);
                _serverSocket.SendFrame(json);
            }
            Log("发送计算结果", "Reported statistic results");
        }

        private void Log(string displayMessage, string saveMessage, LogLevel logLevel = LogLevel.Info)
        {
            RemoteVisionConsoleModule.Log(new LoggingMessageItem($"视觉页面({Adapter.Name}): {displayMessage}", $"VisionPage({Adapter.Name}): {saveMessage}") { LogLevel = logLevel });
        }

        private void Warn(string message)
        {
            _dialogService.ShowDialog("VisionConsoleNotificationDialog", new DialogParameters { { "message", message } }, r => { });
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
