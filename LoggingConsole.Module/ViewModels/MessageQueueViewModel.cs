using LoggingConsole.Interface;
using LoggingConsole.Module.RollingFileAppender;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;


namespace LoggingConsole.Module.ViewModels
{
    /// <summary>
    /// Log items to ui with queuing
    /// and log to file when items overflow
    /// </summary>
    public class MessageQueueViewModel : BindableBase
    {
        #region private filed

        private readonly uint _maxSize;
        private readonly float _preservedPercent;
        private readonly ConcurrentQueue<LoggingMessageItem> _itemQueue = new ConcurrentQueue<LoggingMessageItem>();
        private string _newestMessage;
        private ObservableCollection<LoggingMessageItem> _displayItems = new ObservableCollection<LoggingMessageItem>();
        private readonly RollingFileLogger _fileLogger;

        #endregion

        #region ctor

        public MessageQueueViewModel(RollingFileLogger fileLogger, uint maxSize = 100, float preservedPercent = 0.7f,
            double dequeueFrequency = 500)
        {
            _maxSize = maxSize;
            _preservedPercent = preservedPercent;
            _fileLogger = fileLogger;
            Name = fileLogger.Name;

            var timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(dequeueFrequency),
                IsEnabled = true
            };

            timer.Tick += DisplayAvailableItems;

            ClearMessagesCommand = new DelegateCommand(ClearMessages, CanClearMessages).ObservesProperty(() => DisplayItems.Count);
        }

        #endregion

        #region impl

        private bool CanClearMessages()
        {
            return DisplayItems.Count > 0;
        }

        private void ClearMessages()
        {
            DisplayItems.Clear();
        }


        private void DisplayAvailableItems(object sender, EventArgs e)
        {
            while (_itemQueue.TryDequeue(out var item))
            {
                DisplayItems.Add(item);
            }

            // Save dequeued items

            if (DisplayItems.Count <= _maxSize) return;

            // Remove overflowing items from the screen
            var removeSize = (int)(_maxSize * (1 - _preservedPercent));
            DisplayItems = new ObservableCollection<LoggingMessageItem>(DisplayItems.Skip(removeSize));
        }


        private (LogLevel, string) SerializeItem(LoggingMessageItem item)
        {
            return (item.LogLevel, item.SaveMessage);
        }

        #endregion


        #region props

        public ObservableCollection<LoggingMessageItem> DisplayItems
        {
            get => _displayItems;
            set
            {
                SetProperty(ref _displayItems, value);
            }
        }



        public string Name { get;  }

        public string NewestMessage
        {
            get => _newestMessage;
            private set
            {
                SetProperty(ref _newestMessage, value);
            }
        }

        public ICommand ClearMessagesCommand { get; set; }

        #endregion


        #region api

        public void EnqueueMessage(LoggingMessageItem item)
        {

            var (level, text) = SerializeItem(item);
            _fileLogger.Log(text, level);
            if (level == LogLevel.Debug) return;

            // Display
            _itemQueue.Enqueue(item);
            NewestMessage = item.DisplayMessage;
        }

        #endregion
    }
}