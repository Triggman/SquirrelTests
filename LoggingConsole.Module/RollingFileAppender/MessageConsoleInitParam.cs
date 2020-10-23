using System.Collections.Generic;

namespace LoggingConsole.Module.RollingFileAppender
{
    public class MessageConsoleInitParam
    {
       public IEnumerable<(bool showInUi, RollingFileLogger logger)> Loggers { get; set; }
    }
}
