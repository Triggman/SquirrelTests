using DistributedVisionRunner.Interface;
using Prism.Events;
using System;
using System.Linq;
using System.Threading;

namespace DistributedVisionRunner.Client
{
    /// <summary>
    /// Use this class when vision runner is a prism module in ALC
    /// </summary>
    public class InProcessVisionClient : IVisionClient
    {
        private readonly IEventAggregator _ea;
        private readonly ManualResetEvent _resultBlocker = new ManualResetEvent(false);
        private StatisticsResults _results;

        public InProcessVisionClient(IEventAggregator ea)
        {
            _ea = ea;
            _ea.GetEvent<VisionResultEvent>().Subscribe(ReceiveResult);
        }

        private void ReceiveResult(StatisticsResults results)
        {
            _results = results;
            _resultBlocker.Set();
        }


        public StatisticsResults RequestProcess(string inputSn, int cavity, byte[] data, int? timeout = null)
        {
            var sn = string.IsNullOrEmpty(inputSn) ? string.Empty : inputSn;
            if (cavity < 1) throw new ArgumentException("Cavity can not be less than 1", nameof(cavity));
            if (data == null || !data.Any()) throw new ArgumentException("Data can not be empty or null", nameof(data));
            if (timeout != null && timeout.Value <= 0) throw new ArgumentException("Timeout must be a positive number", nameof(timeout));

            _resultBlocker.Set();
            _ea.GetEvent<DataEvent>().Publish((data, cavity, sn));
            if (timeout == null)
            {
                _resultBlocker.WaitOne();
                return _results;
            }


            bool? responseTaskWin = null;
            var resetEvent = new ManualResetEvent(false);

            // Respond task
            new Thread(() =>
                {
                    _resultBlocker.WaitOne();
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
                return _results;
            }
            else
            {
                _resultBlocker.Set();
                throw new TimeoutException("Process response can not return in time");

            }

        }
    }
}