using Prism.Events;

namespace DistributedVisionRunner.Interface
{
    public class DataEvent : PubSubEvent<(byte[] data, int cavity, string sn)> { }

    public class VisionResultEvent : PubSubEvent<StatisticsResults> { }
}
