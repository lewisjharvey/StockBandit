#region © Copyright
//
// © Copyright 2012 Tradelogic Ltd
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
using System.Net.Mail;

namespace StockBandit.Server
{
    public class EmailServerConnection
    {
        private object smtpSemaphore = new object();
        private SmtpClient smtpServer;
        private string fromAddress;

        public EmailServerConnection(string server, int port, string username, string password, string fromAddress, bool useSSL)
        {
            this.fromAddress = fromAddress;

            try
            {
                smtpServer = new SmtpClient(server);

                smtpServer.Port = port;

                if (username.Length > 0)
                    smtpServer.Credentials = new System.Net.NetworkCredential(username, password);

                smtpServer.EnableSsl = useSSL;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool SendMessage(string recipient, string subject, string body)
        {
            try
            {
                MailMessage mail = new MailMessage();

                mail.From = new MailAddress(fromAddress);

                mail.To.Add(recipient);
                mail.Subject = subject;
                mail.Body = body;

                lock (smtpSemaphore)
                {
                    smtpServer.Send(mail);
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
