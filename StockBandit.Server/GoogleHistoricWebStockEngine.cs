using StockBandit.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace StockBandit.Server
{
    public class GoogleHistoricWebStockEngine
    {
        private const string BASE_URL = "http://www.google.com/finance/getprices?q={0}&x={1}&i=86400&p=5Y&f=d,c";
        private DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private const string matchLineRegex = "^[a]?[0-9]";

        public List<DailyPrice> Fetch(Quote quote)
        {
            List<DailyPrice> historicPrices = new List<DailyPrice>();
            string[] quoteParts = quote.Symbol.Split(new char[1] { ':' });

            // TODO Validation of the quote
            string url = string.Format(BASE_URL, quoteParts[1], quoteParts[0]);

            // Read URL and parse
            HttpWebRequest oReq = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse resp = (HttpWebResponse)oReq.GetResponse();
            if (resp.ContentType.StartsWith("text/plain", StringComparison.InvariantCultureIgnoreCase))
            {
                Regex matchRegex = new Regex(matchLineRegex); 
                TextReader textReader = new StreamReader(resp.GetResponseStream());

                // Skip the header
                string line;
                DateTime startingDate = DateTime.MinValue;

                while ((line = textReader.ReadLine()) != null)
                {
                    if(matchRegex.IsMatch(line))
                    {
                        // We know we are a date line now.
                        // If line starts with a, we need to rework the starting date.
                        // Split line into date and value
                        string[] parts = line.Split(new char[1] {','});
                        
                        DateTime date = DateTime.MinValue;
                        if(parts[0].StartsWith("a"))
                        {
                            string datePart = parts[0].Substring(1);
                            startingDate = UnixTimeToDateTime(datePart);
                            date = startingDate;
                        }
                        else
                        {
                            date = startingDate.AddDays(int.Parse(parts[0]));
                        }

                        // Now the value
                        historicPrices.Add(new DailyPrice() { Date = date.Date, StockCode = quote.Symbol, Price = decimal.Parse(parts[1]) });
                    }
                }
            }

            return historicPrices;
        }

        public DateTime UnixTimeToDateTime(string text)
        {
            double seconds = double.Parse(text, CultureInfo.InvariantCulture);
            return epoch.AddSeconds(seconds);
        }
    }
}
