using System.Collections.Generic;
using System.IO;
using System.Linq;
using DistributedVisionRunner.Interface;

namespace DistributedVisionRunner.Tests.IntegrationTests.CanSaveImagesOnDemand
{
    public class Processor_FullTest : IVisionProcessor<byte>
    {
        public ProcessResult<byte> Process(List<byte[]> data)
        {
            var imageData = data[0];
            var average = imageData.Select(b => (int) b).Average();
            if(average > 90) throw new InvalidDataException("Data average out of bound");

            return new ProcessResult<byte>()
            {
                DisplayData = data, Statistics = new Statistics(){FloatResults = new Dictionary<string, float>(){["Average"] = (float)average}}
            };
        }

        public string[] OutputNames { get; } = new[] {"Average"};
        public string[] WeightNames { get; } = new[] {"w1"};
    }
}