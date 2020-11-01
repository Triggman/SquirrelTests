using DistributedVisionRunner.Interface;

namespace DistributedVisionRunner.Client
{
    public interface IVisionClient
    {
        StatisticsResults RequestProcess(string inputSn, int cavity, byte[] data, int? timeout = null);
    }
}