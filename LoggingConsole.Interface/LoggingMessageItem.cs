using System;

namespace LoggingConsole.Interface
{
    public class LoggingMessageItem
    {


        #region props

        public string DisplayMessage
        { get; }

        public string SaveMessage { get; }

        public DateTime Time
        { get; set; } = DateTime.Now;
        public LogLevel LogLevel
        { get; set; } = LogLevel.Info;

        #endregion

        #region ctor

        public LoggingMessageItem(string displayMessage, string saveMessage = null)
        {
            DisplayMessage = displayMessage;
            SaveMessage = saveMessage ?? displayMessage;
        }




        #endregion

    }
}