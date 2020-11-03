using DistributedVisionRunner.Interface;
using System.Collections.Generic;
using System.IO;

namespace DistributedVisionRunner.Tests.IntegrationTests.CanSaveImagesOnDemand
{
    public class Adapter_FullTest : IVisionAdapter<byte>
    {
        public string Name { get; } = "FullTest";
        public string ZeroMQAddress { get; } = "tcp://localhost:6000";
        public string ProjectName { get; }
        public bool EnableWeighting { get; } = true;
        public int WeightSetCount { get; } = 2;

        public (string[] extensions, string fileTypePrompt)? ImageFileFilter { get; } =
            (new[] { "tif", "tiff" }, "tif images");

        public (string[] floatNames, string[] integerNames, string[] textNames) OutputNames { get; } =
            (new[] { "Value1" }, null, new[] { "Result" });
        public GraphicMetaData GraphicMetaData { get; } = new GraphicMetaData()
        {
            Dimensions = new (int width, int height)[] { (320, 240) },
            SampleType = DataSampleType.TwoDimension,
            ShouldDisplay = true
        };
        public List<byte[]> ConvertInput(byte[] input)
        {
            return new List<byte[]>() { input };
        }

        public ResultType GetResultType(Statistics statistics)
        {
            var average = statistics.FloatResults["Value1"];
            if (average < 8)
            {
                statistics.TextResults["Result"] = "NG";
                return ResultType.NG;
            }

            statistics.TextResults["Result"] = "OK";
            return ResultType.OK;
        }

        public void SaveImage(List<byte[]> imageData, string mainFolder, string subFolder, string fileNameWithoutExtension, string exceptionDetail)
        {
            // Save image
            var data = imageData[0];
            var (width, height) = GraphicMetaData.Dimensions[0];
            var fileName = $"{mainFolder}/{subFolder}/{fileNameWithoutExtension}";
            var imagePath = $"{fileName}.tif";
            ImageHelper.SaveTiff(data, width, 1, imagePath);

            // Save exception
            if (string.IsNullOrEmpty(exceptionDetail)) return;
            var exceptionPath = $"{fileName}_error.txt";
            File.WriteAllText(exceptionPath, exceptionDetail);
        }

        public List<byte[]> ReadFile(string path)
        {
            var (data, samplesPerPixel, width) = ImageHelper.ReadByteTiff(path);
            return new List<byte[]>() { data };
        }
    }
}