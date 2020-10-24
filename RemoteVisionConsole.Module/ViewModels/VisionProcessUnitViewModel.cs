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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        #endregion


        #region props
        private WriteableBitmap _displayImage;
        public WriteableBitmap DisplayImage
        {
            get { return _displayImage; }
            set { SetProperty(ref _displayImage, value); }
        }

        private ObservableCollection<Dictionary<string, object>> _displayData = new ObservableCollection<Dictionary<string, object>>();
        public ObservableCollection<Dictionary<string, object>> DisplayData
        {
            get { return _displayData; }
            set { SetProperty(ref _displayData, value); }
        }

        public string Name { get; }

        public string ServerAddress { get; }

        public ICommand ShowPropertiesCommand { get; }
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

        public void ProcessDataFromFile(string filePath)
        {
            var (data, cavity) = _visionAdapter.ReadFile(filePath);
            ProcessData(data, cavity, DataSourceType.DataFile);
        }

        public void Stop()
        {
            _serverSocket?.Close();
        }

        #endregion

        #region impl


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
            Log(new LoggingMessageItem($"启动ZeroMQ服务器({ServerAddress})", $"Started ZeroMQ server at {ServerAddress}"));
            while (true)
            {
                var message = _serverSocket.ReceiveMultipartMessage();
                var sourceId = message[0].ConvertToString();

                var shouldProcess = _visionAdapter.IsInterestingData(sourceId);
                if (!shouldProcess)
                {
                    Log(new LoggingMessageItem($"过滤非感兴趣输入ID({sourceId})", $"source id({sourceId}) is not an interesting id"));
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
                Log(new LoggingMessageItem($"过滤非感兴趣输入ID({input.sourceId})", $"source id({input.sourceId}) is not an interesting id"));
                return;
            }

            ProcessData(_visionAdapter.ConvertInput(input.data), input.cavity, DataSourceType.DataEvent);
        }

        private void ProcessData(TData[] data, int cavity, DataSourceType dataSource)
        {
            Log(new LoggingMessageItem($"正在处理来自{dataSource}, 长度为{data.Length}, 夹具编号为{cavity}的数据", $"Start processing data of length({data.Length}) of cavity({cavity}) from data source({dataSource})"));

            var stopwatch = Stopwatch.StartNew();
            var result = _visionProcessor.Process(data, cavity);
            var resultType = _visionAdapter.GetResultType(result.Statistics);
            var weightedStatistics = _visionAdapter.Weight(result.Statistics);
            var ms = stopwatch.ElapsedMilliseconds;
            Log(new LoggingMessageItem($"计算耗时{ms}ms", $"Data process finished in {ms} ms"));

            if (dataSource != DataSourceType.DataFile) ReportResult(weightedStatistics, resultType, dataSource);

            DisplayStatisticResults(weightedStatistics, cavity);

            if (_visionAdapter.GraphicMetaData.ShouldDisplay)
            {
                if (_visionAdapter.GraphicMetaData.SampleType != DataSampleType.OneDimension)
                    ShowImage(result.DisplayData, _visionAdapter.GraphicMetaData);
                else ShowChart(result.DisplayData);
            }

            if (_visionAdapter.ShouldSaveImage(resultType)) _visionAdapter.SaveImage(data, cavity);
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

            if (DisplayData.Count > 10) DisplayData = new ObservableCollection<Dictionary<string, object>>(DisplayData.Skip(5));
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
            writeableBitmap.WritePixels(new Int32Rect(0, 0, graphicMetaData.Width, graphicMetaData.Height), pixelData, writeableBitmap.BackBufferStride, 0);
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
            Log(new LoggingMessageItem("发送计算结果", "Reported statistic results"));
        }

        private void Log(LoggingMessageItem messageItem)
        {
            RemoteVisionConsoleModule.Log(messageItem);
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
