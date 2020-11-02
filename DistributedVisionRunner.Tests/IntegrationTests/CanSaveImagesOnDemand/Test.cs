using DistributedVisionRunner.Client;
using DistributedVisionRunner.Interface;
using NUnit.Framework;

namespace DistributedVisionRunner.Tests.IntegrationTests.CanSaveImagesOnDemand
{
    [TestFixture]
    public class Test
    {
        [Test]
        public void FeedOkInputAndGetOutput()
        {
            var imageName = "byte2.tif";
            ProcessImage(imageName);
        }
        [Test]
        public void FeedNgInputAndGetOutput()
        {
            var imageName = "byte2_underExposed.tif";
            ProcessImage(imageName);
        }

        [Test]
        public void FeedErrorInputAndGetOutput()
        {
            var imageName = "byte2_overExposed.tif";
            ProcessImage(imageName);
        }

        private static void ProcessImage(string imageName)
        {
            var (data, width, samplesPerPixel) = ImageHelper.ReadByteTiff($"Sample Data/{imageName}");
            using (var client = new DistributedVisionClient("tcp://localhost:6000"))
            {
                var output = client.RequestProcess("unknownSn", 1, data);
            }
        }
    }
}