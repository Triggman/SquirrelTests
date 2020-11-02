using DistributedVisionRunner.Interface;
using System.Collections.Generic;
using System.Linq;

namespace DistributedVisionRunner.Tests.IntegrationTests.CanProcessImagesOfDifferentSize
{
    public class Adapter_CanProcessImagesOfDifferentSize : IVisionAdapter<byte>
    {
        public string Name { get; } = "CanProcessImagesOfDifferentSize";
        public string ZeroMQAddress { get; } = "tcp://localhost:6002";
        public string ProjectName { get; }
        public bool EnableWeighting { get; }
        public int WeightSetCount { get; }
        public (string[] extensions, string fileTypePrompt)? ImageFileFilter { get; }

        public (string[] floatNames, string[] integerNames, string[] textNames) OutputNames { get; } =
            (null, null, new[] { "Result" });
        public GraphicMetaData GraphicMetaData { get; } = new GraphicMetaData()
        {
            Dimensions = new[] { (1419, 1001), (320, 240) }
        };
        public List<byte[]> ConvertInput(byte[] input)
        {
            return new List<byte[]>()
            {
                input.Take(1419 * 1001).ToArray(),
                input.Skip(1419 * 1001).Take(320 * 240).ToArray()
            };
        }

        public ResultType GetResultType(Statistics statistics)
        {
            statistics.FloatResults.Clear();
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