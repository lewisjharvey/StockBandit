using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Model
{
    public interface ILogQueue
    {
        /// <summary>
        /// Push a log entry into the queue.
        /// </summary>
        /// <param name="logEntry">The LogEntry to log.</param>
        void QueueLogEntry(LogEntry logEntry);

        /// <summary>
        /// Signal the log queue to stop processing and wait for it to complete.
        /// </summary>
        void StopProcessingQueue();
    }
}
