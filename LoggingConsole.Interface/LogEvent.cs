using Prism.Events;

namespace LoggingConsole.Interface
{
    public class LogEvent : PubSubEvent<(string loggerName, LoggingMessageItem logItem)>
    {
    }
}
