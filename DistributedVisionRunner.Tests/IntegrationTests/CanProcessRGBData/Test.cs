using DistributedVisionRunner.Client;
using DistributedVisionRunner.Interface;
using NUnit.Framework;

namespace DistributedVisionRunner.Tests.IntegrationTests.CanProcessRGBData
{
    [TestFixture]
    public class Test
    {
        [Test]
        public void SendRGBData()
        {
            var (data, width, samplesPerPixel) = ImageHelper.ReadByteTiff($"Sample Data/marbles.tif");
            using (var client = new DistributedVisionClient("tcp://localhost:6001"))
            {
                var output = client.RequestProcess("unknownSn", 1, data);
            }
        }
    }
}