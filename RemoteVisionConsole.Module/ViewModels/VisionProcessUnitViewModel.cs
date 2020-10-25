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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace RemoteVisionConsole.Module.ViewModels
{
    public class VisionProcessUnitViewModel<TData> : BindableBase
    {
        #region private fields
        private readonly IEventAggregator _ea;
        private readonly IDialogService _dialogService;
        private readonly TypeSource _processorTypeSource;
        private readonly TypeSource _adapterTypeSource;
        private readonly IVisionAdapter<TData> _visionAdapter;
        private readonly IVisionProcessor<TData> _visionProcessor;
        private readonly ResponseSocket _serverSocket;
        private ProcessUnitUserSetting _userSetting;

        #endregion


        #region props
        private WriteableBitmap _displayImage;
        public WriteableBitmap DisplayImage
        {
            get { return _displayImage; }
            set { SetProperty(ref _displayImage, value); }
        }

        private ObservableCollection<Dictionary<string, object>> _displayData = new ObservableCollection<Dictionary<string, object>>();
        private string _userSettingPath;
        private string _tableNameRawOnline;
        private string _tableNameWeightedOnline;
        private string _tableNameRawOffline;
        private string _tableNameWeightedOffline;
        private bool _databaseServiceInstalled = true;

        public ObservableCollection<Dictionary<string, object>> DisplayData
        {
            get { return _displayData; }
            set { SetProperty(ref _displayData, value); }
        }

        public string Name { get; }

        public string ServerAddress { get; }

        public ICommand ShowPropertiesCommand { get; }
        public ICommand RunSingleFileCommand { get; }
        public ICommand RunFolderCommand { get; }
        public ICommand OpenSettingDialogCommand { get; }
        #endregion

        #region ctor
        public VisionProcessUnitViewModel(IEventAggregator ea, IDialogService dialogService, TypeSource processorTypeSource, TypeSource adapterTypeSource)
        {
            _ea = ea;
            _dialogService = dialogService;
            _processorTypeSource = processorTypeSource;
            _adapterTypeSource = adapterTypeSource;
            _visionProcessor = (IVisionProcessor<TData>)Activator.CreateInstance(processorTypeSource.Type);
            _visionAdapter = (IVisionAdapter<TData>)Activator.CreateInstance(adapterTypeSource.Type);
            Name = _visionAdapter.Name;

            _ea.GetEvent<DataEvent>().Subscribe(ProcessDataFromDataEvent);

            ShowPropertiesCommand = new DelegateCommand(ShowProperties);
            RunSingleFileCommand = new DelegateCommand(RunSingleFile);
            RunFolderCommand = new DelegateCommand(RunFolder);
            OpenSettingDialogCommand = new DelegateCommand(OpenSettingDialog);

            _userSetting = LoadUserSetting(_visionAdapter.Name);

            CreateDatabase();


            // Setup server
            var enableZeroMQText = ConfigurationManager.AppSettings["EnableZeroMQ"];
            var enableZeroMQ = !string.IsNullOrEmpty(enableZeroMQText) && bool.Parse(enableZeroMQText);

            if (enableZeroMQ)
            {
                ServerAddress = ConfigurationManager.AppSettings[$"ServerAddress-{_visionAdapter.Name}"] ?? _visionAdapter.ZeroMQAddress ?? "tcp://localhost:6000";
                _serverSocket = new ResponseSocket(ServerAddress);
                new Thread(ListenForProcessDataFromZeroMQ) { IsBackground = true }.Start();
            }
        }






        #endregion

        #region api



        public void Stop()
        {
            _serverSocket?.Close();
        }

        #endregion

        #region impl

        private void OpenSettingDialog()
        {
            var settingCopied = Helpers.CopyObject(_userSetting);
            _dialogService.ShowDialog("UserSettingDialog",
                new DialogParameters { { "setting", settingCopied } }, r =>
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
            var projectName = _visionAdapter.ProjectName ?? "VisionConsole";
            _tableNameRawOnline = $"{projectName}.{_visionAdapter.Name}_Raw_Online";
            _tableNameWeightedOnline = $"{projectName}.{_visionAdapter.Name}_Weighted_Online";
            _tableNameRawOffline = $"{projectName}.{_visionAdapter.Name}_Raw_Offline";
            _tableNameWeightedOffline = $"{projectName}.{_visionAdapter.Name}_Weighted_Offline";


            try
            {
                var proxy = new CygiaSqliteAccessProxy(EndPointType.TCP);
                proxy.CreateTable(_tableNameRawOnline, _visionProcessor.OutputNames.floatNames, _visionProcessor.OutputNames.integerNames, _visionProcessor.OutputNames.textNames);
                proxy.CreateTable(_tableNameRawOffline, _visionProcessor.OutputNames.floatNames, _visionProcessor.OutputNames.integerNames, _visionProcessor.OutputNames.textNames);
                proxy.CreateTable(_tableNameWeightedOnline, _visionAdapter.OutputNames.floatNames, _visionAdapter.OutputNames.integerNames, _visionAdapter.OutputNames.textNames);
                proxy.CreateTable(_tableNameWeightedOffline, _visionAdapter.OutputNames.floatNames, _visionAdapter.OutputNames.integerNames, _visionAdapter.OutputNames.textNames);
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

            if (!File.Exists(_userSettingPath)) return new ProcessUnitUserSetting();

            using (var reader = new StreamReader(_userSettingPath))
            {
                var serializer = new XmlSerializer(typeof(ProcessUnitUserSetting));
                return serializer.Deserialize(reader) as ProcessUnitUserSetting;
            }
        }


        private void RunFolder()
        {
            var dir = Helpers.GetDirFromDialog();
            if (!string.IsNullOrEmpty(dir))
            {
                var allFiles = Directory.GetFiles(dir);
                var filesThatMatch = allFiles;
                if (_visionAdapter.ImageFileFilter.HasValue) filesThatMatch = allFiles.
                        Where(f => _visionAdapter.ImageFileFilter.Value.extensions.Any(ex => Path.GetExtension(f).Replace(".", "").ToUpper() == ex.ToUpper())).ToArray();

                if (filesThatMatch.Length == 0)
                {
                    Warn("没有找到符合格式的文件");
                    return;
                }

                Log($"读取{filesThatMatch.Length}个文件", $"{filesThatMatch.Length} files are read");
                foreach (var file in filesThatMatch)
                {
                    ProcessDataFromFile(file);
                }
            }
        }

        private void RunSingleFile()
        {
            var selectedFile = Helpers.GetFileFromDialog(pattern: _visionAdapter.ImageFileFilter);
            if (!string.IsNullOrEmpty(selectedFile))
            {
                ProcessDataFromFile(selectedFile);
            }
        }


        private void ProcessDataFromFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            Log($"正在离线运行图片{fileName}", $"Running image offline: {fileName}");

            var (data, cavity) = _visionAdapter.ReadFile(filePath);
            ProcessData(data, cavity, DataSourceType.DataFile);
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
                var sourceId = message[0].ConvertToString();

                var shouldProcess = _visionAdapter.IsInterestingData(sourceId);
                if (!shouldProcess)
                {
                    Log($"过滤非感兴趣输入ID({sourceId})", $"source id({sourceId}) is not an interesting id");
                    return;
                }

                var cavity = int.Parse(message[1].ConvertToString());
                var data = message[2].Buffer;
                ProcessData(_visionAdapter.ConvertInput(data), cavity, DataSourceType.ZeroMQ);
            }
        }

        private void ProcessDataFromDataEvent((byte[] data, int cavity, string sourceId) input)
        {
            var shouldProcess = _visionAdapter.IsInterestingData(input.sourceId);
            if (!shouldProcess)
            {
                Log($"过滤非感兴趣输入ID({input.sourceId})", $"source id({input.sourceId}) is not an interesting id");
                return;
            }

            ProcessData(_visionAdapter.ConvertInput(input.data), input.cavity, DataSourceType.DataEvent);
        }

        private void ProcessData(TData[] data, int cavity, DataSourceType dataSource)
        {
            Log($"正在处理来自{dataSource}, 长度为{data.Length}, 夹具编号为{cavity}的数据", $"Start processing data of length({data.Length}) of cavity({cavity}) from data source({dataSource})");

            var stopwatch = Stopwatch.StartNew();
            var result = _visionProcessor.Process(data, cavity);
            var resultType = _visionAdapter.GetResultType(result.Statistics);
            var weightedStatistics = _visionAdapter.Weight(result.Statistics);
            var ms = stopwatch.ElapsedMilliseconds;
            Log($"计算耗时{ms}ms", $"Data process finished in {ms} ms");

            if (dataSource != DataSourceType.DataFile) ReportResult(weightedStatistics, resultType, dataSource);

            DisplayStatisticResults(weightedStatistics, cavity);

            if (_visionAdapter.GraphicMetaData.ShouldDisplay)
            {
                if (_visionAdapter.GraphicMetaData.SampleType != DataSampleType.OneDimension)
                    ShowImage(result.DisplayData, _visionAdapter.GraphicMetaData);
                else ShowChart(result.DisplayData);
            }

            if (_visionAdapter.ShouldSaveImage(resultType)) _visionAdapter.SaveImage(data, cavity);

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

        private void ShowChart(TData[] displayData)
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

        private void ShowImage(TData[] displayData, GraphicMetaData graphicMetaData)
        {
            Application.Current.Dispatcher?.InvokeAsync(() =>
            {

                if (displayData is byte[] byteArray)
                {
                    ShowByteImage(byteArray, graphicMetaData);
                }
                else if (displayData is float[] floatArray)
                {
                    ShowFloatImage(floatArray, graphicMetaData);
                }
                else if (displayData is ushort[] ushortArray)
                {
                    ShowUShortImage(ushortArray, graphicMetaData);
                }
                else if (displayData is short[] shortArray)
                {
                    ShowShortImage(shortArray, graphicMetaData);
                }

            });
        }

        private void ShowShortImage(short[] shortArray, GraphicMetaData graphicMetaData)
        {
            throw new NotImplementedException();
        }

        private void ShowUShortImage(ushort[] ushortArray, GraphicMetaData graphicMetaData)
        {
            throw new NotImplementedException();
        }

        private void ShowFloatImage(float[] floatArray, GraphicMetaData graphicMetaData)
        {
            throw new NotImplementedException();
        }

        private void ShowByteImage(byte[] displayData, GraphicMetaData graphicMetaData)
        {
            byte[] pixelData;
            if (graphicMetaData.SampleType == DataSampleType.TwoDimension)
            {
                pixelData = new byte[displayData.Length * 3];
                for (int i = 0; i < displayData.Length; i++)
                {
                    var start = i * 3;
                    for (int offset = 0; offset < 3; offset++)
                    {
                        pixelData[start + offset] = displayData[i];
                    }
                }
            }
            else
            {
                pixelData = displayData;
            }

            if (DisplayImage == null) DisplayImage = CreateDisplayImage(graphicMetaData, pixelData);
            else
            {
                UpdateDisplayImage(DisplayImage, pixelData, graphicMetaData.Width, graphicMetaData.Height);
            }

        }

        private WriteableBitmap CreateDisplayImage(GraphicMetaData graphicMetaData, byte[] pixelData)
        {
            var writeableBitmap = new WriteableBitmap(graphicMetaData.Width, graphicMetaData.Height, 96.0, 96.0, PixelFormats.Rgb24, BitmapPalettes.Halftone256Transparent);
            writeableBitmap.WritePixels(new Int32Rect(0, 0, graphicMetaData.Width, graphicMetaData.Height), pixelData, graphicMetaData.Width * 3, 0);
            return writeableBitmap;
        }

        private void UpdateDisplayImage(WriteableBitmap image, byte[] rgbData, int width, int height)
        {
            image.WritePixels(new Int32Rect(0, 0, width, height), rgbData, image.BackBufferStride, 0);
        }

        private void ReportResult(Statistics statistics, string resultType, DataSourceType dataSource)
        {
            if (dataSource == DataSourceType.DataEvent) _ea.GetEvent<VisionResultEvent>().Publish((statistics, resultType));
            else if (dataSource == DataSourceType.ZeroMQ)
            {
                // Serialize statistics 
                var json = JsonConvert.SerializeObject(new StatisticsResults(statistics.FloatResults, statistics.IntegerResults, statistics.TextResults));
                _serverSocket.SendMoreFrame(resultType).SendFrame(json);
            }
            Log("发送计算结果", "Reported statistic results");
        }

        private void Log(string displayMessage, string saveMessage, LogLevel logLevel = LogLevel.Info)
        {
            RemoteVisionConsoleModule.Log(new LoggingMessageItem($"视觉页面({_visionAdapter.Name}): {displayMessage}", $"VisionPage({_visionAdapter.Name}): {saveMessage}") { LogLevel = logLevel });
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
