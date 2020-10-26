using RemoteVisionConsole.Data;
using RemoteVisionConsole.Interface;
using RemoteVisionConsole.Module.Helper;
using System;
using System.Collections.Generic;

namespace RemoteVisionModule.Tests.Mocks
{
    public class VisionAdapterMock : IVisionAdapter<byte>
    {
        private readonly Random _random = new Random();

        public string Name { get; } = "VisionAdapterMock";
        public GraphicMetaData GraphicMetaData { get; } = new GraphicMetaData { SampleType = DataSampleType.TwoDimension, Width = 1419, Height = 1001, ShouldDisplay = true };
        public string ZeroMQAddress { get; } = "tcp://localhost:6001";
        public (string[] extensions, string fileTypePrompt)? ImageFileFilter { get; } = (new[] { "tif", "tiff" }, "Tif Files");
        public string ProjectName { get; } = "TestProject";
        public (string[] floatNames, string[] integerNames, string[] textNames) OutputNames { get; } = (new[] { "Value1" }, new[] { "Value2" }, new[] { "Value1Result", "Value2Result" });

        public byte[] ConvertInput(byte[] input)
        {
            return input;
        }

        public string GetResultType(Statistics statistics)
        {
            if (statistics.FloatResults["Value1"] > 0) return "OK";
            return "NG";
        }

        public bool IsInterestingData(string sourceId)
        {
            return true;
        }

        public (byte[] data, int cavity) ReadFile(string path)
        {
            var (data, samplesPerPixel, width) = ImageHelper.ReadByteTiff(path);
            return (data, 1);
        }


        public void SaveImage(byte[] imageData, string mainFolder, string subFolder, string fileName, string exceptionDetail)
        {
            Log("Saved image");
        }



        public Statistics Weight(Statistics statistics)
        {
            return new Statistics
            {
                FloatResults = new Dictionary<string, float> { ["Value1"] = (float)_random.NextDouble() },
                IntegerResults = new Dictionary<string, int> { ["Value2"] = _random.Next() },
                TextResults = new Dictionary<string, string> { ["Value1Result"] = "OK", ["Value2Result"] = "NG" },
            };
        }

        ResultType IVisionAdapter<byte>.GetResultType(Statistics statistics)
        {
            return ResultType.OK;
        }

        private void Log(string message)
        {
            Console.WriteLine($"Vision adapter mock: {message}");
        }

        (byte[] data, int cavity, string sn) IVisionAdapter<byte>.ReadFile(string path)
        {
            throw new NotImplementedException();
        }
    }
}
