#region © Copyright
//
// © Copyright 2013 Lewis Harvey
//   All rights reserved.
//
// This software is provided "as is" without warranty of any kind, express or implied, 
// including but not limited to warranties of merchantability and fitness for a particular 
// purpose. The authors do not support the Software, nor do they warrant
// that the Software will meet your requirements or that the operation of the Software will
// be uninterrupted or error free or that any defects will be corrected.
//
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using System.Threading;

namespace StockBandit.Server
{
    class EmailQueue
    {
        //log4net
        protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Thread emailThread = null;
        private Queue<EmailQueueItem> emailMessageQueue = new Queue<EmailQueueItem>();
        private object emailQueueSemaphore = new object();
        private object emailSemaphore = new object();
        private bool continueProcessing = true;

        private EmailServerConnection emailServerConnection;
        private StockServer server;
        private string fromAddress;

        /// <summary>
        /// Create and prepare the email queue
        /// </summary>
        public EmailQueue(StockServer server, string emailServerName, int emailPort, string emailUsername, string emailPassword, string emailFromAddress, bool emailSSL, int sleepMilliseconds)
        {
            this.server = server;
            this.fromAddress = emailFromAddress;
            this.emailServerConnection = new EmailServerConnection(emailServerName, emailPort, emailUsername, emailPassword, emailFromAddress, emailSSL);

            emailThread = new Thread(delegate() { ProcessEmailQueue(sleepMilliseconds); });
            emailThread.Start();
        }

        /// <summary>
        /// Push an email into the queue
        /// </summary>
        public void QueueEmail(string recipient, string subject, string body)
        {
            EmailQueueItem emailQueueItem = new EmailQueueItem();
            emailQueueItem.Recipient = recipient;
            emailQueueItem.Subject = subject;
            emailQueueItem.Body = body;

            lock (emailQueueSemaphore)
            {
                emailMessageQueue.Enqueue(emailQueueItem);
            }
        }

        /// <summary>
        /// Signal the email queue to stop processing and wait for it to complete
        /// </summary>
        public void StopProcessingQueue()
        {
            continueProcessing = false;
            emailThread.Join();
        }

        /// <summary>
        /// Process the email queue
        /// </summary>
        /// <param name="sleep"></param>
        private void ProcessEmailQueue(int sleep)
        {
            // Process the email queue items in a separate thread
            do
            {
                if (emailMessageQueue.Count > 0)
                {
                    EmailQueueItem qi;
                    lock (emailQueueSemaphore)
                    {
                        qi = emailMessageQueue.Dequeue();
                    }
                    ProcessEmail(qi);
                }
                else
                {
                    Thread.Sleep(sleep);
                }
            } while (continueProcessing || emailMessageQueue.Count > 0);
        }

        protected bool ProcessEmail(EmailQueueItem emailQueueItem)
        {
            try
            {
                string subject = emailQueueItem.Subject;
                string body = emailQueueItem.Body;

                // Send the email
                lock (emailSemaphore)
                {
                    emailServerConnection.SendMessage(emailQueueItem.Recipient, emailQueueItem.Subject, emailQueueItem.Body);
                }

                log.InfoFormat("Email sent: {0}", subject);
            }
            catch (Exception e)
            {
                log.ErrorFormat("Exception in ProcessEmail - Exception: {0}", e.ToString());
                return (false);
            }

            return true;
        }
    }

    public struct EmailQueueItem
    {
        public string Subject;
        public string Body;
        public string Recipient;
    }
}