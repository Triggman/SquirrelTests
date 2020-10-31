using Prism.Events;
using RemoteVisionConsole.Interface;

namespace RemoteVisionConsole.Module
{
    public class DataEvent : PubSubEvent<(byte[] data, int cavity, string sn)> { }

    public class VisionResultEvent : PubSubEvent<StatisticsResults> { }
}
