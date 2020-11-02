using System.Collections.Generic;
using DistributedVisionRunner.Interface;

namespace DistributedVisionRunner.Tests.IntegrationTests.CanProcessRGBData
{
    public class Processor_CanProcessRGBData : IVisionProcessor<byte>
    {
        public ProcessResult<byte> Process(List<byte[]> data)
        {
            return new ProcessResult<byte>()
            {
                DisplayData = data,
                Statistics = new Statistics()
                {
                    FloatResults = new Dictionary<string, float>(){["Average"] = 100}
                }
            };
        }

        public string[] OutputNames { get; } = new[] {"Average"};
        public string[] WeightNames { get; }
    }
}