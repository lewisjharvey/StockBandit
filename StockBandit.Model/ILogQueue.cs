#region © Copyright
// <copyright file="ILogQueue.cs" company="Lewis Harvey">
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
    /// The logging engine for logging messages.
    /// </summary>
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
