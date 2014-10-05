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
        private const string BASE_URL = "http://www.google.com/finance/getprices?q={0}&x={1}&i=86400&p={2}&f=d,c,v";
        private DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private const string MATCH_LINE_REGEX = "^[a]?[0-9]";
        private const string DEFAULT_PERIOD = "1Y";

        public List<DailyPrice> Fetch(Quote quote, DateTime lastRetrieveTime)
        {
            List<DailyPrice> historicPrices = new List<DailyPrice>();

            string[] quoteParts = quote.Symbol.Split(new char[1] { ':' });

            // Don't get old prices if retrieved yesterday
            if(!ShouldCollectHistory(lastRetrieveTime))
                return new List<DailyPrice>();

            // TODO Validation of the quote
            string url = string.Format(BASE_URL, quoteParts[1], quoteParts[0], GetCollectionPeriod(lastRetrieveTime));

            // Read URL and parse
            HttpWebRequest oReq = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse resp = (HttpWebResponse)oReq.GetResponse();
            if (resp.ContentType.StartsWith("text/plain", StringComparison.InvariantCultureIgnoreCase))
            {
                Regex matchRegex = new Regex(MATCH_LINE_REGEX); 
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
                        historicPrices.Add(new DailyPrice() { Date = date.Date, StockCode = quote.Symbol, Price = decimal.Parse(parts[1]), Volume = int.Parse(parts[2]) });
                    }
                }
            }

            return historicPrices;
        }

        private bool ShouldCollectHistory(DateTime lastRetrieveTime)
        {
            DateTime date = DateTime.Today;

            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                // If it's a weekend, the lastRetrieve must be the Friday before.
                do
                {
                    date = date.AddDays(-1);
                }
                while (date.DayOfWeek != DayOfWeek.Friday);

                // Now we have the Friday, check lastRetrieve against this
                return date.Date != lastRetrieveTime.Date;
            }

            // Now we have determines we are not on a weekend we can check if the lastretrieve is older than one day
            return lastRetrieveTime.Date < DateTime.Today.AddDays(-1);
        }

        private string GetCollectionPeriod(DateTime lastRetrieveTime)
        {
            string period = DEFAULT_PERIOD;
            if(lastRetrieveTime > DateTime.MinValue)
            {
                if(lastRetrieveTime > DateTime.Today.AddDays(-40))
                {
                    period = "40d";
                }
            }
            return period;
        }

        public DateTime UnixTimeToDateTime(string text)
        {
            double seconds = double.Parse(text, CultureInfo.InvariantCulture);
            return epoch.AddSeconds(seconds);
        }
    }
}
