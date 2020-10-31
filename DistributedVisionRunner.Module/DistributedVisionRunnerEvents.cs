using Prism.Events;
using DistributedVisionRunner.Interface;

namespace DistributedVisionRunner.Module
{
    public class DataEvent : PubSubEvent<(byte[] data, int cavity, string sn)> { }

    public class VisionResultEvent : PubSubEvent<StatisticsResults> { }
}
