using RemoteVisionConsole.Interface;
using System.Collections.Generic;

namespace RemoteVisionModule.Tests.Mocks
{
    public class VisionProcessorMock : IVisionProcessor<byte>
    {
        public ProcessResult<byte> Process(byte[] data, int cavityIndex)
        {
            return new ProcessResult<byte>()
            {
                DisplayData = data,
                Statistics = new Statistics
                {
                    FloatResults = new Dictionary<string, float>
                    {
                        ["Value1"] = 1
                    }
                }
            };
        }
    }
}
