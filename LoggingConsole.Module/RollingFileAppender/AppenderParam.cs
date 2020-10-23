namespace LoggingConsole.Module.RollingFileAppender
{
    public class AppenderParam
    {
        #region ctor

        public AppenderParam(string appenderName, string dir)
        {
            AppenderName = appenderName;
            Dir = dir;
        }

        #endregion

        #region props

        public string AppenderName { get; }
        public string Dir { get; }

        public bool LogByDate { get; set; } = true;

        public string Extension { get; set; } = "log";

        public string LineFormat { get; set; } = LineFormat_DateLevelMessage;

        public string MaximumFileSize { get; set; } = "1MB";
        public int MaxSizeRollBackups { get; set; } = 10;
        public bool ShowInUI { get; set; } = true;

        #endregion

        const string LineFormat_DateLevelMessage = "[%date] -- %level -- %message%newline";
        const string LineFormat_MessageOnly = "%message%newline";
    }
}