#region © Copyright
// <copyright file="LogEntry.cs" company="Lewis Harvey">
//      Copyright (c) Lewis Harvey. All rights reserved.
//      This software is provided "as is" without warranty of any kind, express or implied, 
//      including but not limited to warranties of merchantability and fitness for a particular 
//      purpose. The authors do not support the Software, nor do they warrant
//      that the Software will meet your requirements or that the operation of the Software will
//      be uninterrupted or error free or that any defects will be corrected.
// </copyright>
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Model
{
    /// <summary>
    /// A wrapper class for an entry in the logging system.
    /// </summary>
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
        /// Initialises a new instance of the <see cref="LogEntry" /> class.
        /// </summary>
        /// <param name="logType">The type of log entry for the logging event.</param>
        /// <param name="message">The message of the logging event.</param>
        public LogEntry(LogType logType, string message)
            : this(DateTime.UtcNow, logType, message)
        { }

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
