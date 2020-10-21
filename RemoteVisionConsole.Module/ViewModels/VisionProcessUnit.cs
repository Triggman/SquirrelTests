using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Prism.Events;
using Prism.Mvvm;
using RemoteVisionConsole.Data;
using RemoteVisionConsole.Interface;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading;

namespace RemoteVisionConsole.Module.ViewModels
{
    public class VisionProcessUnit<TData> : BindableBase
    {
        #region private fields
        private readonly IEventAggregator _ea;
        private readonly IVisionAdapter<TData> _visionAdapter;
        private readonly IVisionProcessor<TData> _visionProcessor;
        private readonly ResponseSocket _serverSocket;

        #endregion

        #region events
        public event Action<string> Info;

        #endregion
        #region ctor
        public VisionProcessUnit(IEventAggregator ea, TypeSource visionProcessorTypeSource, TypeSource visionAdapterTypeSource)
        {
            _ea = ea;
            _visionProcessor = visionProcessorTypeSource.CreateTypeInstance<IVisionProcessor<TData>>();
            _visionAdapter = visionAdapterTypeSource.CreateTypeInstance<IVisionAdapter<TData>>();

            _ea.GetEvent<DataEvent>().Subscribe(ProcessDataFromDataEvent);


            // Setup server
            var serverAddress = ConfigurationManager.AppSettings[$"ServerAddress-{_visionAdapter.Name}"] ?? "tcp://localhost:6000";
            _serverSocket = new ResponseSocket(serverAddress);
            new Thread(ListenForProcessDataFromZeroMQ) { IsBackground = true }.Start();
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
            _serverSocket.Close();
        }

        #endregion

        #region impl
        private void ListenForProcessDataFromZeroMQ()
        {

            while (true)
            {
                var message = _serverSocket.ReceiveMultipartMessage();
                var sourceId = message[0].ConvertToString();

                var shouldProcess = _visionAdapter.IsInterestingData(sourceId);
                if (!shouldProcess)
                {
                    LogInfo($"source id({sourceId}) is not an interesting id");
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
                LogInfo($"source id({input.sourceId}) is not an interesting id");
                return;
            }

            ProcessData(_visionAdapter.ConvertInput(input.data), input.cavity, DataSourceType.DataEvent);
        }

        private void ProcessData(TData[] data, int cavity, DataSourceType dataSource)
        {
            LogInfo($"Start processing data of length({data.Length}) of cavity({cavity}) from data source({dataSource})");
            var stopwatch = Stopwatch.StartNew();
            var result = _visionProcessor.Process(data, cavity);
            var resultType = _visionAdapter.GetResultType(result.Statistics);
            var weightedStatistics = _visionAdapter.Weight(result.Statistics);
            LogInfo($"Data process finished in {stopwatch.ElapsedMilliseconds} ms");

            if (dataSource != DataSourceType.DataFile) ReportResult(weightedStatistics, resultType, dataSource);

            DisplayStatisticResults(weightedStatistics);

            if (_visionAdapter.GraphicMetaData.ShouldDisplay) ShowImage(result.DisplayData, _visionAdapter.GraphicMetaData);

            if (_visionAdapter.ShouldSaveImage(resultType)) _visionAdapter.SaveImage(data, cavity);
        }

        private void DisplayStatisticResults(Statistics statistics)
        {
            //TODO:
        }

        private void ShowImage(TData[] displayData, GraphicMetaData graphicMetaData)
        {
            //TODO:
        }

        private void ReportResult(Statistics statistics, string resultType, DataSourceType dataSource)
        {
            if (dataSource == DataSourceType.DataEvent) _ea.GetEvent<VisionResultEvent>().Publish((statistics, resultType));
            else if (dataSource == DataSourceType.ZeroMQ)
            {
                // Serialize statistics 
                var json = JsonConvert.SerializeObject(new StatisticsResults(statistics.DoubleResults, statistics.IntegerResults, statistics.TextResults));
                _serverSocket.SendMoreFrame(resultType).SendFrame(json);
            }
            LogInfo("Reported statistic results");
        }

        private void LogInfo(string message)
        {
            Info?.Invoke($"{_visionAdapter.Name}: {message}");
        }
        #endregion
    }
}
