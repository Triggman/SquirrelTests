using System;
using DistributedVisionRunner.Client;
using DistributedVisionRunner.Interface;
using NUnit.Framework;

namespace DistributedVisionRunner.Tests.IntegrationTests.CanProcessFloatData
{
    [TestFixture]
    public class Test
    {
        [Test]
        public void FeedFloatData()
        {
            var (data, width) = ImageHelper.ReadFloatTiff($"Sample Data/float.tif");

            var byteArray = new byte[data.Length * 4];
            Buffer.BlockCopy(data, 0, byteArray, 0, byteArray.Length);

            using (var client = new DistributedVisionClient("tcp://localhost:6003"))
            {
                var output = client.RequestProcess("unknownSn", 1, byteArray);
            }
        }
    }
}