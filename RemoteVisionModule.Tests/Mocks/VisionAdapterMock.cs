using RemoteVisionConsole.Interface;
using RemoteVisionConsole.Module.Helper;
using System;

namespace RemoteVisionModule.Tests.Mocks
{
    public class VisionAdapterMock : IVisionAdapter<byte>
    {
        public string Name { get; } = "VisionAdapterMock";
        public GraphicMetaData GraphicMetaData { get; } = new GraphicMetaData { SampleType = DataSampleType.TwoDimension, Width = 100, Height = 100, ShouldDisplay = true };
        public string ZeroMQAddress { get; } = "tcp://localhost:6001";

        public byte[] ConvertInput(byte[] input)
        {
            return input;
        }

        public string GetResultType(Statistics statistics)
        {
            if (statistics.DoubleResults["Value1"] > 0) return "OK";
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
            return statistics;
        }

        private void Log(string message)
        {
            Console.WriteLine($"Vision adapter mock: {message}");
        }
    }
}
