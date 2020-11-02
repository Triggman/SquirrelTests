using System.Collections.Generic;
using System.IO;
using DistributedVisionRunner.Interface;

namespace DistributedVisionRunner.Tests.IntegrationTests.CanSaveImagesOnDemand
{
    public class Adapter_CanSaveImagesOnDemand : IVisionAdapter<byte>
    {
        public string Name { get; } = "CanSaveImagesOnDemand";
        public string ZeroMQAddress { get; } = "tcp://localhost:6000";
        public string ProjectName { get; }
        public bool EnableWeighting { get; } = false;
        public int WeightSetCount { get; }
        public (string[] extensions, string fileTypePrompt)? ImageFileFilter { get; }

        public (string[] floatNames, string[] integerNames, string[] textNames) OutputNames { get; } =
            (new[] {"Average"}, null, new[] {"Result"});
        public GraphicMetaData GraphicMetaData { get; } = new GraphicMetaData()
        {
            Dimensions = new (int width, int height)[]{(320, 240)},
            SampleType = DataSampleType.TwoDimension,
            ShouldDisplay = true
        };
        public List<byte[]> ConvertInput(byte[] input)
        {
            return new List<byte[]>(){input};
        }

        public ResultType GetResultType(Statistics statistics)
        {
            var average = statistics.FloatResults["Average"];
            if (average < 80)
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
            throw new System.NotImplementedException();
        }
    }
}