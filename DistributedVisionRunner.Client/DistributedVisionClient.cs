using DistributedVisionRunner.Interface;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;

namespace DistributedVisionRunner.Client
{
    /// <summary>
    /// Use this class when vision runner is in a separate app
    /// </summary>
    public class DistributedVisionClient : IDisposable, IVisionClient
    {
        private readonly RequestSocket _requestSocket;
        private readonly object _locker = new object();


        /// <summary>
        /// 
        /// </summary>
        /// <param name="serverAddress">
        /// The ZeroMQ address to the vision runner server, for example, tcp://localhost:6001
        /// </param>
        public DistributedVisionClient(string serverAddress)
        {
            _requestSocket = new RequestSocket(serverAddress);
        }

        public StatisticsResults RequestProcess(string inputSn, int cavity, byte[] data, int? timeout = null)
        {
            var sn = string.IsNullOrEmpty(inputSn) ? string.Empty : inputSn;
            if (cavity < 1) throw new ArgumentException("Cavity can not be less than 1", nameof(cavity));
            if (data == null || !data.Any()) throw new ArgumentException("Data can not be empty or null", nameof(data));
            if (timeout != null && timeout.Value <= 0) throw new ArgumentException("Timeout must be a positive number", nameof(timeout));

            lock (_locker)
            {
                _requestSocket
                    .SendMoreFrame(sn)
                    .SendMoreFrame(cavity.ToString())
                    .SendFrame(data);

                if (timeout == null)
                {
                    var response = _requestSocket.ReceiveFrameString();
                    return JsonConvert.DeserializeObject<StatisticsResults>(response);
                }
                else
                {
                    string response = null;
                    bool? responseTaskWin = null;
                    var resetEvent = new ManualResetEvent(false);

                    // Respond task
                    new Thread(() =>
                    {
                        response = _requestSocket.ReceiveFrameString();
                        if (!responseTaskWin.HasValue) responseTaskWin = true;
                        resetEvent.Set();
                    })
                    { IsBackground = true }.Start();

                    // Time out task
                    new Thread(() =>
                        {
                            Thread.Sleep(timeout.Value);
                            if (!responseTaskWin.HasValue) responseTaskWin = false;
                            resetEvent.Set();
                        })
                    { IsBackground = true }.Start();

                    resetEvent.WaitOne();


                    if (responseTaskWin == true)
                    {
                        return JsonConvert.DeserializeObject<StatisticsResults>(response);
                    }

                    throw new TimeoutException("Process response can not return in time");
                }

            }
        }

        public void Close()
        {
            lock (_locker)
            {
                _requestSocket.Close();
            }
        }

        public void Dispose()
        {
            _requestSocket.Dispose();
        }
    }
}