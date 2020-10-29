using RemoteVisionConsole.Interface;
using System.Collections.Generic;

namespace RemoteVisionModule.Tests.Mocks
{
    public class VisionProcessorMock : IVisionProcessor<byte>
    {
        public (string[] floatNames, string[] integerNames, string[] textNames) OutputNames { get; } = (new[] { "Value0" }, null, null);
        public string[] WeightNames { get; } = new[] { "w1", "w3" };

        public ProcessResult<byte> Process(List<byte[]> data, int cavityIndex)
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
