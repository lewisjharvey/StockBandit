
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using StockBandit.Model;
using log4net;
using System.Reflection;
using System.Threading;

namespace StockBandit.Server
{
    /// <summary>
    /// Handle the queuing of log entries
    /// </summary>
    public class LogQueue : ILogQueue
    {
        /// <summary>
        /// The log4net logger.
        /// </summary>
        protected static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The thread used for writing to the log file.
        /// </summary>
        private Thread logThread;

        /// <summary>
        /// The queue used for queuing log entries.
        /// </summary>
        private Queue<LogEntry> logQueue;

        /// <summary>
        /// A flag for whether the service can stop.
        /// </summary>
        private bool shouldContinueProcessing;

        /// <summary>
        /// Initialises a new instance of the <see cref="LogQueue" /> class.
        /// </summary>
        /// <param name="sleepMS">The number of milliseconds between queue checks for logging.</param>
        public LogQueue(int sleepMS)
        {
            this.logQueue = new Queue<LogEntry>();
            this.shouldContinueProcessing = true;

            this.logThread = new Thread(delegate() { this.ProcessLogQueue(sleepMS); });
            this.logThread.Start();
        }

        /// <summary>
        /// Push a log entry into the queue.
        /// </summary>
        /// <param name="logEntry">The LogEntry to log.</param>
        public void QueueLogEntry(LogEntry logEntry)
        {
            if (logEntry != null)
            {
                lock (this.logQueue)
                {
                    this.logQueue.Enqueue(logEntry);
                }
            }
        }

        /// <summary>
        /// Signal the log queue to stop processing and wait for it to complete.
        /// </summary>
        public void StopProcessingQueue()
        {
            this.shouldContinueProcessing = false;
            this.logThread.Join();
        }

        /// <summary>
        /// Process the log entry queue
        /// </summary>
        /// <param name="sleep">The number of milliseconds between log queue polling.</param>
        private void ProcessLogQueue(int sleep)
        {
            do
            {
                if (this.logQueue.Count > 0)
                {
                    LogEntry logEntry;
                    lock (this.logQueue)
                    {
                        logEntry = this.logQueue.Dequeue();
                    }

                    // Attempt to log
                    try
                    {
                        if (logEntry != null)
                        {
                            switch (logEntry.LogType)
                            {
                                case LogType.Info:
                                    Log.InfoFormat("EventTime: {0}; Message: {1};", logEntry.EventTime, logEntry.Message);
                                    break;
                                case LogType.Warn:
                                    Log.WarnFormat("EventTime: {0}; Message: {1};", logEntry.EventTime, logEntry.Message);
                                    break;
                                case LogType.Fatal:
                                    Log.FatalFormat("EventTime: {0}; Message: {1};", logEntry.EventTime,
                                        logEntry.Message);
                                    break;
                                case LogType.Error:
                                    Log.ErrorFormat("EventTime: {0}; Message: {1};", logEntry.EventTime,
                                        logEntry.Message);
                                    break;
                                case LogType.Debug:
                                    Log.DebugFormat("EventTime: {0}; Message: {1};", logEntry.EventTime,
                                        logEntry.Message);
                                    break;
                                default:
                                    throw new ApplicationException("Logging format not supported.");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.ErrorFormat("Exception in LogQueue.ProcessLogQueue: {0}", e.ToString());
                    }
                }
                else
                {
                    Thread.Sleep(sleep);
                }
            } while (this.shouldContinueProcessing || this.logQueue.Count > 0);
        }
    }
}