using Prism.Events;
using RemoteVisionConsole.Interface;

namespace RemoteVisionConsole.Module
{
    public class DataEvent : PubSubEvent<(byte[] data, int cavity, string sourceId)> { }

    public class VisionResultEvent : PubSubEvent<(Statistics statistics, string resultType)> { }
}
