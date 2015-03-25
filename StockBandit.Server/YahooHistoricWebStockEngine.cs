#region © Copyright
// <copyright file="YahooHistoricWebStockEngine.cs" company="Lewis Harvey">
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
using System.IO;
using System.Net;
using System.Text;

using StockBandit.Model;

namespace StockBandit.Server
{
    /// <summary>
    /// Provides functions to get historical prices from Yahoo.
    /// </summary>
    public class YahooHistoricWebStockEngine : IStockEngine
    {
        /// <summary>
        /// The url for accessing the Yahoo stock API.
        /// </summary>
        private const string BASEURL = "http://ichart.finance.yahoo.com/table.csv?{0}";

        /// <summary>
        /// An instance of the logging engine.
        /// </summary>
        private readonly ILogQueue logQueue;

        /// <summary>
        /// Initialises a new instance of the <see cref="YahooHistoricWebStockEngine" /> class.
        /// </summary>
        /// <param name="logQueue">An instance of the logging engine</param>
        public YahooHistoricWebStockEngine(ILogQueue logQueue)
        {
            this.logQueue = logQueue;
        }

        /// <summary>
        /// Fetch the prices for a stock from the given last retrieve time if required
        /// </summary>
        /// <param name="stock">The stock to collect prices</param>
        /// <param name="lastRetrieveTime">The last retrieve time</param>
        /// <returns>A list of historical daily prices</returns>
        public List<DailyPrice> Fetch(Stock stock, DateTime lastRetrieveTime)
        {
            // Don't get old prices if retrieved yesterday
            if (!this.ShouldCollectHistory(lastRetrieveTime))
                return new List<DailyPrice>();

            return this.FetchPrice(stock, lastRetrieveTime);
        }

        /// <summary>
        /// Fetch the prices for a stock from the given last retrieve time
        /// </summary>
        /// <param name="stock">The stock to collect prices</param>
        /// <param name="lastRetrieveTime">The last retrieve time</param>
        /// <returns>A list of historical daily prices</returns>
        private List<DailyPrice> FetchPrice(Stock stock, DateTime lastRetrieveTime)
        {
            this.logQueue.QueueLogEntry(
                new LogEntry(
                DateTime.Now, 
                LogType.Info,
                string.Format("Collecting Prices for {0} from {1}", stock.StockCode, lastRetrieveTime)));
            var historicPrices = new List<DailyPrice>();
            var url = string.Format(BASEURL, this.BuildHistoricalDataRequest(stock.StockCode, lastRetrieveTime, DateTime.Today));

            var docText = string.Empty;
            string csvLine = null;

            try
            {
                var request = (HttpWebRequest)WebRequest.CreateDefault(new Uri(url));
                request.Timeout = 300000;

                var response = (HttpWebResponse)request.GetResponse();

                var streamReader = new StreamReader(response.GetResponseStream(), detectEncodingFromByteOrderMarks: true);

                streamReader.ReadLine();
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
            }
            catch (WebException)
            {
                this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Error, string.Format("Web Exception for: {0}", url)));
            }
            catch (Exception e)
            {
                this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Error, string.Format("Could not read prices for {0}. Error: {1}", stock.StockCode, e)));
            }

            this.logQueue.QueueLogEntry(
                new LogEntry(
                    DateTime.Now, 
                    LogType.Info,
                    string.Format("Collect {0} Prices for {1} from {2}", historicPrices.Count, stock.StockCode, lastRetrieveTime)));
            
            return historicPrices;
        }

        /// <summary>
        /// Determines if a prices need to be collected for a stock
        /// </summary>
        /// <param name="lastRetrieveTime">The time the last retrieve was made</param>
        /// <returns>A flag indicating if the prices should be collected</returns>
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

        /// <summary>
        /// Builds the request string to add to the request url
        /// </summary>
        /// <param name="symbol">The stock symbol</param>
        /// <param name="startDate">The start date of the prices to retrieve</param>
        /// <param name="endDate">The end date of the prices to retrieve</param>
        /// <returns>The completed request string</returns>
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
            request.AppendFormat("&g={0}", "d");

            return request.ToString();
        }
    }
}
