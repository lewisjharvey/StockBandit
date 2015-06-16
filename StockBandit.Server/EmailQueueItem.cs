#region © Copyright
// <copyright file="EmailQueueItem.cs" company="Lewis Harvey">
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

namespace StockBandit.Server
{
    /// <summary>
    /// A wrapper around an email going in the queue.
    /// </summary>
    public class EmailQueueItem
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="EmailQueueItem" /> class.
        /// </summary>
        /// <param name="subject">The subject of the message</param>
        /// <param name="body">The body of the message</param>
        /// <param name="recipient">The email address of the recipient</param>
        public EmailQueueItem(string subject, string body, string recipient)
        {
            this.Subject = subject;
            this.Body = body;
            this.Recipient = recipient;
        }

        /// <summary>
        /// Gets the subject of the message
        /// </summary>
        public string Subject { get; private set; }

        /// <summary>
        /// Gets the body of the message
        /// </summary>
        public string Body { get; private set; }

        /// <summary>
        /// Gets the email address of the recipient
        /// </summary>
        public string Recipient { get; private set; }
    }
}
