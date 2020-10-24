﻿using RemoteVisionConsole.Interface;
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

        public void SaveImage(byte[] imageData, int cavity)
        {
            Log("Saved image");
        }

        public bool ShouldSaveImage(string resultType)
        {
            return true;
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

        private void Log(string message)
        {
            Console.WriteLine($"Vision adapter mock: {message}");
        }
    }
}
