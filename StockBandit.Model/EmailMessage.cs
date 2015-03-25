#region © Copyright
// <copyright file="EmailMessage.cs" company="Lewis Harvey">
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
    /// A class to hold details of an email message.
    /// </summary>
    public class EmailMessage
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="EmailMessage" /> class.
        /// </summary>
        /// <param name="to">The email address of who to send the message to</param>
        /// <param name="subject">The subject of the message</param>
        /// <param name="body">The body of the message</param>
        public EmailMessage(string to, string subject, string body)
        {
            this.To = to;
            this.Subject = subject;
            this.Body = body;
        }

        /// <summary>
        /// Gets the address of who the message should be sent to
        /// </summary>
        public string To { get; private set; }

        /// <summary>
        /// Gets the body of the message
        /// </summary>
        public string Body { get; private set; }
        
        /// <summary>
        /// Gets the subject of the message
        /// </summary>
        public string Subject { get; private set; }
    }
}
