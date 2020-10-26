using Prism.Events;
using RemoteVisionConsole.Data;

namespace RemoteVisionConsole.Module
{
    public class DataEvent : PubSubEvent<(byte[] data, int cavity, string sn)> { }

    public class VisionResultEvent : PubSubEvent<StatisticsResults> { }
}
