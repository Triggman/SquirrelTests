using Prism.Events;

namespace LoggingConsole.Module
{
    public class LogEvent : PubSubEvent<(string loggerName, LoggingMessageItem logItem)>
    {
    }
}