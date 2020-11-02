using System.Collections.Generic;
using DistributedVisionRunner.Interface;

namespace DistributedVisionRunner.Tests.IntegrationTests.CanProcessRGBData
{
    public class Adapter_CanProcessRGBData : IVisionAdapter<byte>
    {
        public string Name { get; } = "CanProcessRGBData";
        public string ZeroMQAddress { get; } = "tcp://localhost:6001";
        public string ProjectName { get; }
        public bool EnableWeighting { get; }
        public int WeightSetCount { get; }
        public (string[] extensions, string fileTypePrompt)? ImageFileFilter { get; }

        public (string[] floatNames, string[] integerNames, string[] textNames) OutputNames { get; } =
            (null, null, new[] {"Result"});
        public GraphicMetaData GraphicMetaData { get; } = new GraphicMetaData()
        {
            Dimensions = new []{(1419, 1001)},
            SampleType = DataSampleType.TwoDimensionRGB,
            ShouldDisplay = true
        };
        public List<byte[]> ConvertInput(byte[] input)
        {
            return new List<byte[]>(){input};
        }

        public ResultType GetResultType(Statistics statistics)
        {
            statistics.FloatResults = new Dictionary<string, float>();
            statistics.TextResults["Result"] = "OK";

            return ResultType.OK;
        }

        public void SaveImage(List<byte[]> imageData, string mainFolder, string subFolder, string fileNameWithoutExtension,
            string exceptionDetail)
        {
        }

        public List<byte[]> ReadFile(string path)
        {
            throw new System.NotImplementedException();
        }
    }
}