using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Model
{
    public class LogEntry
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="LogEntry" /> class.
        /// </summary>
        /// <param name="eventTime">The time the logging event occurred.</param>
        /// <param name="logType">The type of log entry for the logging event.</param>
        /// <param name="message">The message of the logging event.</param>
        public LogEntry(DateTime eventTime, LogType logType, string message)
        {
            this.EventTime = eventTime;
            this.LogType = logType;
            this.Message = message;
        }

        /// <summary>
        /// Gets the time the logging event occurred.
        /// </summary>
        public DateTime EventTime { get; private set; }

        /// <summary>
        /// Gets the message of the logging event.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Gets the type of log entry for the logging event.
        /// </summary>
        public LogType LogType { get; private set; }
    }
}
