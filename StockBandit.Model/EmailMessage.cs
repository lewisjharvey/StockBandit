using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Model
{
    public class EmailMessage
    {
        public string To;
        public string Body;
        public string Subject; 

        public EmailMessage (string to, string subject, string body)
        {
            this.To = to;
            this.Subject = subject;
            this.Body = body;
        }
    }
}
