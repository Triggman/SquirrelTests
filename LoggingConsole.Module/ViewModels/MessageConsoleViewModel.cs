using LoggingConsole.Module.RollingFileAppender;
using Prism.Events;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace LoggingConsole.Module.ViewModels
{
    public class MessageConsoleViewModel : BindableBase
    {
        #region private fields

        private readonly Dictionary<string, RollingFileLogger> _textLoggers;
        private readonly HashSet<string> _loggerNameInUi;

        #endregion private fields

        #region props

        public Dictionary<string, MessageQueueViewModel> MessageQueues { get; }

        public ICommand OpenLogDirCommand { get; }

        public int MessageQueueOnUiCount { get; }

        #endregion props

        #region ctor

        public MessageConsoleViewModel(IEventAggregator ea, MessageConsoleInitParam initParams)
        {
            ea.GetEvent<LogEvent>().Subscribe(Log);

            MessageQueues = initParams.Loggers.Where(t => t.showInUi).ToDictionary(t => t.logger.Name, t => new MessageQueueViewModel(t.logger));
            _textLoggers = initParams.Loggers.Where(t => !t.showInUi).ToDictionary(t => t.logger.Name, t => t.logger);

            _loggerNameInUi = new HashSet<string>(MessageQueues.Select(q => q.Key));
            MessageQueueOnUiCount = MessageQueues.Count;
        }

        #endregion ctor

        #region impl

        private void Log((string loggerName, LoggingMessageItem messageItem) obj)
        {
            if (_loggerNameInUi.Contains(obj.loggerName)) LogToMessageQueue(obj);
            else LogToTextFile(obj);
        }

        private void LogToMessageQueue((string, LoggingMessageItem) messageItem)
        {
            var queueToLog = messageItem.Item1;
            var message = messageItem.Item2;

            MessageQueues[queueToLog].EnqueueMessage(message);
        }

        private void LogToTextFile((string, LoggingMessageItem) nameAndContent)
        {
            _textLoggers[nameAndContent.Item1].Log(nameAndContent.Item2.SaveMessage ?? nameAndContent.Item2.DisplayMessage, nameAndContent.Item2.LogLevel);
        }

        #endregion impl
    }
}