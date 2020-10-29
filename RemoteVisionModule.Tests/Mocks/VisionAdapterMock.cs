using RemoteVisionConsole.Data;
using RemoteVisionConsole.Interface;
using RemoteVisionConsole.Module.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace RemoteVisionModule.Tests.Mocks
{
    public class VisionAdapterMock : IVisionAdapter<byte>
    {
        private readonly Random _random = new Random();

        public string Name { get; } = "VisionAdapterMock";
        public GraphicMetaData GraphicMetaData { get; } = new GraphicMetaData
        {
            SampleType = DataSampleType.TwoDimension,
            Dimensions = new (int, int)[] { (1419, 1001), (320, 240) },
            ShouldDisplay = true
        };
        public string ZeroMQAddress { get; } = "tcp://localhost:6001";
        public (string[] extensions, string fileTypePrompt)? ImageFileFilter { get; } = (new[] { "tif", "tiff" }, "Tif Files");
        public string ProjectName { get; } = "TestProject";
        public (string[] floatNames, string[] integerNames, string[] textNames) OutputNames { get; } = (new[] { "OutputFloatValue" }, new[] { "OutputIntValue" }, new[] { "OutputText1", "OutputText2" });
        public (float min, float max) DataRange { get; }
        public bool EnableWeighting { get; } = true;
        public int WeightSetCount { get; } = 3;

        public List<byte[]> ConvertInput(byte[] input)
        {
            return new List<byte[]> { input };
        }

        public ResultType GetResultType(Statistics statistics)
        {
            statistics.IntegerResults["OutputIntValue"] = DateTime.Now.Millisecond;
            statistics.TextResults["OutputText1"] = DateTime.Now.Millisecond.ToString() + "MS";
            statistics.TextResults["OutputText2"] = DateTime.Now.Second.ToString() + "S";

            if (statistics.FloatResults["OutputFloatValue"] > 0) return ResultType.OK;
            return ResultType.NG;
        }



        public List<byte[]> ReadFile(string path)
        {
            var dir = Directory.GetParent(path).FullName;
            var fileName = Path.GetFileNameWithoutExtension(path);
            var pattern = new Regex(@"^(\w+)(\d+)");
            var index = int.Parse(pattern.Match(fileName).Groups[2].Value) + 1;
            var fileNameBase = pattern.Match(fileName).Groups[1].Value;
            var nextFile = Path.Combine(dir, $"{fileNameBase}{index}.tif");

            var firstImage = ImageHelper.ReadByteTiff(path);
            var secondImage = ImageHelper.ReadByteTiff(nextFile);

            return new List<byte[]> { firstImage.data, secondImage.data };
        }


        public void SaveImage(List<byte[]> imageData, string mainFolder, string subFolder, string fileName, string exceptionDetail)
        {
            Log("Saved image");
        }




        private void Log(string message)
        {
            Console.WriteLine($"Vision adapter mock: {message}");
        }


    }
}
