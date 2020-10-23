using log4net;
using System.Collections.Generic;
using System.IO;

namespace LoggingConsole.Module.RollingFileAppender
{
    public static class RollingFileAppenderManager
    {

        public static IEnumerable<(bool showInUi, RollingFileLogger logger)> Config(List<AppenderParam> appenderParams)
        {
            var output = new List<(bool showInUi, RollingFileLogger logger)>();

            //If the directory doesn't exist then create it
            foreach (var appenderParam in appenderParams)
            {
                Directory.CreateDirectory(appenderParam.Dir);

                var loggerName = $"{appenderParam.AppenderName}Logger";

                var repoName = $"{appenderParam.AppenderName}Repository";
                var repository = LogManager.CreateRepository(repoName);


                //Add the default log appender if none exist

                var fileName = Path.Combine(appenderParam.Dir, $"{appenderParam.AppenderName}");
                var extension = appenderParam.Extension;

                //Create the rolling file appender
                var appender = new log4net.Appender.RollingFileAppender
                {
                    Name = appenderParam.AppenderName,
                    File = fileName,
                    StaticLogFileName = false,
                    AppendToFile = true,
                    RollingStyle = appenderParam.LogByDate ? log4net.Appender.RollingFileAppender.RollingMode.Date : log4net.Appender.RollingFileAppender.RollingMode.Size,
                    MaxSizeRollBackups = appenderParam.MaxSizeRollBackups,
                    MaximumFileSize = appenderParam.MaximumFileSize,
                    PreserveLogFileNameExtension = true,
                    DatePattern = $"_yyyy-MM-dd'.{extension}'"
                };

                //Configure the layout of the trace message write
                var layout = new log4net.Layout.PatternLayout()
                {
                    ConversionPattern = appenderParam.LineFormat
                };
                appender.Layout = layout;
                layout.ActivateOptions();

                //Let log4net configure itself based on the values provided
                appender.ActivateOptions();

                log4net.Config.BasicConfigurator.Configure(repository, appender);


                var iLog = LogManager.GetLogger(repoName, loggerName);
                output.Add((appenderParam.ShowInUI, new RollingFileLogger(iLog, appenderParam.AppenderName)));
            }

            return output;
        }
    }
}