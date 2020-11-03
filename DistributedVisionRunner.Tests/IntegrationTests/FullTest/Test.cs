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
            var output = ProcessImage(imageName);
            Assert.IsTrue(output.ResultType == ResultType.OK);
        }
        [Test]
        public void FeedNgInputAndGetOutput()
        {
            var imageName = "byte2_underExposed.tif";
            var output = ProcessImage(imageName);
            Assert.IsTrue(output.ResultType == ResultType.NG);
        }

        [Test]
        public void FeedErrorInputAndGetOutput()
        {
            var imageName = "byte2_overExposed.tif";
            var output = ProcessImage(imageName);
            Assert.IsTrue(output.ResultType == ResultType.ERROR);
        }

        private static StatisticsResults ProcessImage(string imageName)
        {
            var (data, width, samplesPerPixel) = ImageHelper.ReadByteTiff($"Sample Data/{imageName}");
            using (var client = new DistributedVisionClient("tcp://localhost:6000"))
            {
                var output = client.RequestProcess("unknownSn", 1, data);
                return output;
            }
        }
    }
}