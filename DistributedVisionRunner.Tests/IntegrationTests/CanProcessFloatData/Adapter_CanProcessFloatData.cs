using DistributedVisionRunner.Interface;
using System;
using System.Collections.Generic;

namespace DistributedVisionRunner.Tests.IntegrationTests.CanProcessFloatData
{
    public class Adapter_CanProcessFloatData : IVisionAdapter<float>
    {
        public string Name { get; } = "CanProcessFloatData";
        public string ZeroMQAddress { get; } = "tcp://localhost:6003";
        public string ProjectName { get; }
        public bool EnableWeighting { get; }
        public int WeightSetCount { get; }
        public (string[] extensions, string fileTypePrompt)? ImageFileFilter { get; }

        public (string[] floatNames, string[] integerNames, string[] textNames) OutputNames { get; } =
            (null, null, new[] { "Result" });
        public GraphicMetaData GraphicMetaData { get; } = new GraphicMetaData()
        {
            Dimensions = new[] { (320, 240) },
            SampleType = DataSampleType.TwoDimension,
            ShouldDisplay = true,
            DataRange = (0, 1)

        };
        public List<float[]> ConvertInput(byte[] input)
        {
            var output = new float[320 * 240];
            Buffer.BlockCopy(input, 0, output, 0, input.Length);

            return new List<float[]>() { output };
        }

        public ResultType GetResultType(Statistics statistics)
        {
            statistics.FloatResults.Clear();
            statistics.TextResults["Result"] = "OK";

            return ResultType.OK;
        }

        public void SaveImage(List<float[]> imageData, string mainFolder, string subFolder, string fileNameWithoutExtension,
            string exceptionDetail)
        {
            var path = $"{mainFolder}/{subFolder}/{fileNameWithoutExtension}.tif";
            ImageHelper.SaveTiff(imageData[0], 320, path);
        }

        public List<float[]> ReadFile(string path)
        {
            throw new System.NotImplementedException();
        }
    }
}