using StockBandit.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace StockBandit.Server
{
    public class YahooHistoricWebStockEngine : IStockEngine
    {
        private const string BASE_URL = "http://ichart.finance.yahoo.com/table.csv?{0}";

        private ILogQueue logQueue;

        public YahooHistoricWebStockEngine(ILogQueue logQueue)
        {
            this.logQueue = logQueue;
        }

        public List<DailyPrice> Fetch(Stock stock, DateTime lastRetrieveTime)
        {
            // Don't get old prices if retrieved yesterday
            if (!ShouldCollectHistory(lastRetrieveTime))
                return new List<DailyPrice>();

            return FetchPrice(stock, lastRetrieveTime);
        }

        private List<DailyPrice> FetchPrice(Stock stock, DateTime lastRetrieveTime)
        {
            this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info,
                string.Format("Collecting Prices for {0} from {1}", stock.StockCode, lastRetrieveTime)));
            var historicPrices = new List<DailyPrice>();
            var url = string.Format(BASE_URL, BuildHistoricalDataRequest(stock.StockCode, lastRetrieveTime, DateTime.Today));

            //Get page showing the table with the chosen indices

            //csv content
            var docText = string.Empty;
            string csvLine = null;
            int retryCount = 1;
            //do
            //{
                try
                {
                    var request = (HttpWebRequest)WebRequest.CreateDefault(new Uri(url));
                    request.Timeout = 300000;

                    var response = (HttpWebResponse)request.GetResponse();

                    var streamReader = new StreamReader(response.GetResponseStream(), detectEncodingFromByteOrderMarks: true);

                    streamReader.ReadLine();//skip the first (header row)
                    while ((csvLine = streamReader.ReadLine()) != null)
                    {
                        var priceParts = csvLine.Split(new char[] { ',' });

                        var date = DateTime.Parse(priceParts[0].Trim('"'));
                        var open = decimal.Parse(priceParts[1]);
                        var high = decimal.Parse(priceParts[2]);
                        var low = decimal.Parse(priceParts[3]);
                        var close = decimal.Parse(priceParts[4]);
                        var volume = int.Parse(priceParts[5]);
                        var adjClose = decimal.Parse(priceParts[6]);

                        historicPrices.Add(new DailyPrice() { Date = date.Date, StockCode = stock.StockCode, Open = open, High = high, Low = low, Close = close, Volume = volume, AdjustedClose = adjClose });
                    }
                    retryCount = 0;

                }
                catch (WebException)
                {
                    this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Error, string.Format("Web Exception (Attempt {0} of 5) for: {1}", retryCount, url)));
                    retryCount++;
                }
                catch (Exception e)
                {
                    this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Error, string.Format("Could not read prices (Attempt {0} of 5) for {1}. Error: {2}", retryCount, stock.StockCode, e)));
                    retryCount++;
                }
            //} while (retryCount < 6);

            this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info,
                string.Format("Collect {0} Prices for {1} from {2}", historicPrices.Count, stock.StockCode, lastRetrieveTime)));
            return historicPrices;
        }

        private bool ShouldCollectHistory(DateTime lastRetrieveTime)
        {
            var date = DateTime.Today;

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

        private string BuildHistoricalDataRequest(string symbol, DateTime startDate, DateTime endDate)
        {
            // We're subtracting 1 from the month because yahoo
            // counts the months from 0 to 11 not from 1 to 12.
            var request = new StringBuilder();
            request.AppendFormat("s={0}.L", symbol);
            request.AppendFormat("&a={0}", startDate.Month - 1);
            request.AppendFormat("&b={0}", startDate.Day);
            request.AppendFormat("&c={0}", startDate.Year);
            request.AppendFormat("&d={0}", endDate.Month - 1);
            request.AppendFormat("&e={0}", endDate.Day);
            request.AppendFormat("&f={0}", endDate.Year);
            request.AppendFormat("&g={0}", "d"); //daily

            return request.ToString();
        }
    }
}
