using RemoteVisionConsole.Interface;
using System.Collections.Generic;

namespace RemoteVisionModule.Tests.Mocks
{
    public class VisionProcessorMock : IVisionProcessor<byte>
    {
        public string[] OutputNames { get; } = new[] { "Value0" };
        public string[] WeightNames { get; } = new[] { "w1", "w3" };

        public ProcessResult<byte> Process(List<byte[]> data)
        {
            return new ProcessResult<byte>()
            {
                DisplayData = data,
                Statistics = new Statistics
                {
                    FloatResults = new Dictionary<string, float>
                    {
                        ["Value0"] = 1
                    }
                }
            };
        }
    }
}
