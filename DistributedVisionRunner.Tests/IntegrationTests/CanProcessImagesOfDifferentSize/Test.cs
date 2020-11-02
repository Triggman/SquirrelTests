using System;
using DistributedVisionRunner.Client;
using DistributedVisionRunner.Interface;
using NUnit.Framework;

namespace DistributedVisionRunner.Tests.IntegrationTests.CanProcessImagesOfDifferentSize
{
    [TestFixture]
    public class Test
    {
        [Test]
        public void FeedTwoImages()
        {
            var (data1, width1, samplesPerPixel1) = ImageHelper.ReadByteTiff($"Sample Data/byte1.tif");
            var (data2, width2, samplesPerPixel2) = ImageHelper.ReadByteTiff($"Sample Data/byte2.tif");
            var inputData = new byte[data1.Length + data2.Length];
            Array.Copy(data1, inputData, data1.Length);
            Array.Copy(data2, 0, inputData, data1.Length, data2.Length);
            using (var client = new DistributedVisionClient("tcp://localhost:6002"))
            {
                var output = client.RequestProcess("unknownSn", 1, inputData);
            }
        }
    }

}