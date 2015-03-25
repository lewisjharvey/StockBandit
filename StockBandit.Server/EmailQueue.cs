#region © Copyright
// <copyright file="EmailQueue.cs" company="Lewis Harvey">
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
using System.Reflection;
using System.Threading;

using StockBandit.Model;

namespace StockBandit.Server
{
    /// <summary>
    /// Provides queued and threaded email.
    /// </summary>
    public class EmailQueue
    {
        /// <summary>
        /// The thread for sending emails
        /// </summary>
        private Thread emailThread = null;

        /// <summary>
        /// The queue for messages being queued up on
        /// </summary>
        private Queue<EmailQueueItem> emailMessageQueue;

        /// <summary>
        /// A flag if the engine should continue to send messages, used for stopping.
        /// </summary>
        private bool continueProcessing = true;

        /// <summary>
        /// Accessor to the email connection
        /// </summary>
        private EmailServerConnection emailServerConnection;

        /// <summary>
        /// The address to send messages from
        /// </summary>
        private string fromAddress;

        /// <summary>
        /// The logging engine for logging messages
        /// </summary>
        private ILogQueue logQueue;

        /// <summary>
        /// Initialises a new instance of the <see cref="EmailQueue" /> class.
        /// </summary>
        /// <param name="emailServerName">The hostname of the email server</param>
        /// <param name="emailPort">The access port of the email server</param>
        /// <param name="emailUsername">The username of the email server</param>
        /// <param name="emailPassword">The password of the email server</param>
        /// <param name="emailFromAddress">The address where message will come from</param>
        /// <param name="emailSSL">A flag if the email server should use SSL</param>
        /// <param name="sleepMilliseconds">The number of milliseconds between checking for new messages in the queue.</param>
        /// <param name="logQueue">An instance of the logging engine</param>
        public EmailQueue(string emailServerName, int emailPort, string emailUsername, string emailPassword, string emailFromAddress, bool emailSSL, int sleepMilliseconds, ILogQueue logQueue)
        {
            this.logQueue = logQueue;
            this.fromAddress = emailFromAddress;
            this.emailMessageQueue = new Queue<EmailQueueItem>();
            this.emailServerConnection = new EmailServerConnection(emailServerName, emailPort, emailUsername, emailPassword, emailFromAddress, emailSSL);

            this.emailThread = new Thread(delegate() { this.ProcessEmailQueue(sleepMilliseconds); });
            this.emailThread.Start();
        }

        /// <summary>
        /// Queue a new email message to be sent
        /// </summary>
        /// <param name="recipient">The email address of the recipient.</param>
        /// <param name="subject">The subject of the message</param>
        /// <param name="body">The body of the message</param>
        public void QueueEmail(string recipient, string subject, string body)
        {
            EmailQueueItem emailQueueItem = new EmailQueueItem(subject, body, recipient);

            lock (this.emailMessageQueue)
            {
                this.emailMessageQueue.Enqueue(emailQueueItem);
            }
        }

        /// <summary>
        /// Signal the email queue to stop processing and wait for it to complete
        /// </summary>
        public void StopProcessingQueue()
        {
            this.continueProcessing = false;
            this.emailThread.Join();
        }

        /// <summary>
        /// Sends a message to the email connection from the queue
        /// </summary>
        /// <param name="emailQueueItem">The email queue item to send</param>
        /// <returns>A result if the send was successful</returns>
        protected bool ProcessEmail(EmailQueueItem emailQueueItem)
        {
            try
            {
                string subject = emailQueueItem.Subject;
                string body = emailQueueItem.Body;

                // Send the email
                lock (this.emailServerConnection)
                {
                    this.emailServerConnection.SendMessage(emailQueueItem.Recipient, emailQueueItem.Subject, emailQueueItem.Body);
                }

                this.logQueue.QueueLogEntry(new LogEntry(LogType.Info, string.Format("Email sent: {0}", subject)));
            }
            catch (Exception e)
            {
                this.logQueue.QueueLogEntry(new LogEntry(LogType.Error, string.Format("Exception in ProcessEmail - Exception: {0}", e.ToString())));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Process the email queue
        /// </summary>
        /// <param name="sleep">The milliseconds to sleep before checking the email queue</param>
        private void ProcessEmailQueue(int sleep)
        {
            // Process the email queue items in a separate thread
            do
            {
                if (this.emailMessageQueue.Count > 0)
                {
                    EmailQueueItem qi;
                    lock (this.emailMessageQueue)
                    {
                        qi = this.emailMessageQueue.Dequeue();
                    }

                    this.ProcessEmail(qi);
                }
                else
                {
                    Thread.Sleep(sleep);
                }
            } 
            while (this.continueProcessing || this.emailMessageQueue.Count > 0);
        }
    }
}