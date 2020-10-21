using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Prism.Events;
using RemoteVisionConsole.Data;
using RemoteVisionConsole.Interface;
using System;
using System.Configuration;

namespace RemoteVisionConsole.Module.ViewModels
{
    public class VisionProcessSystem<TData>
    {
        private readonly IEventAggregator _ea;
        private readonly IVisionAdapter<TData> _visionAdapter;
        private readonly IVisionProcessor<TData> _visionProcessor;
        private readonly ResponseSocket _serverSocket;

        public VisionProcessSystem(IEventAggregator ea, IVisionAdapter<TData> visionAdapter, IVisionProcessor<TData> visionProcessor)
        {
            _ea = ea;
            _visionAdapter = visionAdapter;
            _visionProcessor = visionProcessor;

            _ea.GetEvent<DataEvent>().Subscribe(GetDataFromDataEvent);


            // Setup server
            var serverAddress = ConfigurationManager.AppSettings[$"ServerAddress-{visionAdapter.Name}"] ?? "tcp://localhost:6000";
            _serverSocket = new ResponseSocket(serverAddress);
            _serverSocket.ReceiveReady += GetDataFromZeroMQ;
        }

        private void GetDataFromZeroMQ(object sender, NetMQSocketEventArgs e)
        {
            var server = e.Socket;
            var message = server.ReceiveMultipartMessage();
            var sourceId = message[0].ConvertToString();
            var cavity = message[1].ConvertToInt32();
            var data = message[2].Buffer;
            ProcessData((data, cavity, sourceId), DataSourceType.ZeroMQ);
        }

        private void GetDataFromDataEvent((byte[] data, int cavity, string sourceId) input)
        {
            LogInfo($"Got data from DataEvent of size({input.data.Length}), cavity({input.cavity}), sourceId({input.sourceId})");
            ProcessData(input, DataSourceType.DataEvent);
        }

        private void ProcessData((byte[] data, int cavity, string sourceId) input, DataSourceType dataSource)
        {
            var shouldProcess = _visionAdapter.IsInterestingData(input.sourceId);
            if (!shouldProcess) return;

            var data = _visionAdapter.ConvertInput(input.data);
            var result = _visionProcessor.Process(data, input.cavity);
            var resultType = _visionAdapter.GetResultType(result.Statistics);

            if (dataSource != DataSourceType.DataFile) ReportResult(result.Statistics, resultType, dataSource);

            //TODO: show statistic results on ui

            //TODO: show image on ui

            if (_visionAdapter.ShouldSaveImage(resultType)) _visionAdapter.SaveImage(data, input.cavity);
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
        }

        private void LogInfo(string message)
        {
            throw new NotImplementedException();
        }
    }
}
