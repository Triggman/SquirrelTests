using System.Collections.Generic;
using DistributedVisionRunner.Interface;

namespace DistributedVisionRunner.Tests.IntegrationTests.CanProcessFloatData
{
    public class Processor_CanProcessFloatData : IVisionProcessor<float>
    {
        public ProcessResult<float> Process(List<float[]> data)
        {
            return new ProcessResult<float>()
            {
                DisplayData = data,
                Statistics = new Statistics()
                {
                    FloatResults = new Dictionary<string, float>()
                    {
                        ["Average"] = 100
                    }
                }
            };
        }

        public string[] OutputNames { get; } = new[] {"Average"};
        public string[] WeightNames { get; }
    }
}