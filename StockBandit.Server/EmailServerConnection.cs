#region © Copyright
// <copyright file="EmailServerConnection.cs" company="Lewis Harvey">
//      Copyright (c) Lewis Harvey. All rights reserved.
//      This software is provided "as is" without warranty of any kind, express or implied, 
//      including but not limited to warranties of merchantability and fitness for a particular 
//      purpose. The authors do not support the Software, nor do they warrant
//      that the Software will meet your requirements or that the operation of the Software will
//      be uninterrupted or error free or that any defects will be corrected.
// </copyright>
#endregion

using System;
using System.Net.Mail;

namespace StockBandit.Server
{
    /// <summary>
    /// Encapsulates the SMTP server for sending emails.
    /// </summary>
    public class EmailServerConnection
    {
        /// <summary>
        /// The SMTP server for sending messages
        /// </summary>
        private SmtpClient smtpServer;

        /// <summary>
        /// The address all messages come from.
        /// </summary>
        private string fromAddress;

        /// <summary>
        /// Initialises a new instance of the <see cref="EmailServerConnection" /> class.
        /// </summary>
        /// <param name="server">The server address</param>
        /// <param name="port">The server access port</param>
        /// <param name="username">The username to connect</param>
        /// <param name="password">The password to connect</param>
        /// <param name="fromAddress">The email address the message should come from</param>
        /// <param name="useSSL">A flag whether SSL should be used</param>
        public EmailServerConnection(string server, int port, string username, string password, string fromAddress, bool useSSL)
        {
            this.fromAddress = fromAddress;

            try
            {
                this.smtpServer = new SmtpClient(server);
                this.smtpServer.Port = port;

                if (username.Length > 0)
                    this.smtpServer.Credentials = new System.Net.NetworkCredential(username, password);

                this.smtpServer.EnableSsl = useSSL;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Sends a message through the SMTP server.
        /// </summary>
        /// <param name="recipient">The email address of the recipient</param>
        /// <param name="subject">The subject of the message</param>
        /// <param name="body">The body of the message</param>
        /// <returns>A result whether the email was sent</returns>
        public bool SendMessage(string recipient, string subject, string body)
        {
            try
            {
                MailMessage mail = new MailMessage();

                mail.From = new MailAddress(this.fromAddress);

                mail.To.Add(recipient);
                mail.Subject = subject;
                mail.Body = body;

                lock (this.smtpServer)
                {
                    this.smtpServer.Send(mail);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Cannot send email due to the following exception: {0}", ex));
            }
        }
    }
}
