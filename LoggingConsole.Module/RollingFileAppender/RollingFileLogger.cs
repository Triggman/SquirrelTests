using log4net;
using LoggingConsole.Interface;

namespace LoggingConsole.Module.RollingFileAppender
{
    public class RollingFileLogger
    {
        #region private fields

        private readonly ILog _log;

        #endregion

        public string Name { get; }

        #region ctor

        internal RollingFileLogger(ILog log, string name)
        {
            _log = log;
            Name = name;
        }

        #endregion

        #region api

        public void Log(string message, LogLevel logLevel = LogLevel.Debug)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                    _log.Debug(message);
                    break;
                case LogLevel.Info:
                    _log.Info(message);
                    break;
                case LogLevel.Warn:
                    _log.Warn(message);
                    break;
                case LogLevel.Fatal:
                    _log.Fatal(message);
                    break;
            }
        }

        #endregion


    }
}