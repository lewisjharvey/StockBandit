using StockBandit.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Server
{
    public class VolumeModel : IModel
    {
        private readonly double alertThreshold;
        private readonly ILogQueue logQueue; 

        public VolumeModel(double alertThreshold, ILogQueue logQueue)
        {
            this.alertThreshold = alertThreshold;
            this.logQueue = logQueue;
        }

        public bool Evaluate(Stock stock, List<DailyPrice> historicPrices, out string emailBody)
        {
            if (historicPrices.Count > 0)
            {
                this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info, string.Format("VolumeModel Evaluation of {0} with {1} historic prices.", stock.StockCode, historicPrices.Count)));
                DailyPrice todayPrice = historicPrices.First();

                // Current average price
                var averageVolume = historicPrices
                    .Take(90)
                    .Select(p => p.Volume)
                    .Average();

                if (todayPrice.Volume > (averageVolume * this.alertThreshold))
                {
                    // Volume indicates a shift in movement soon
                    emailBody =
                        string.Format(
                            "Stock: {0}\r\nPrice: {1}\r\nVolume: {2}\r\nAverage Volume: {3}\r\n\r\n",
                            stock.StockCode, todayPrice.Close, todayPrice.Volume, Math.Round(averageVolume, 0));
                    this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info, emailBody));
                    return true;
                }

                this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info, string.Format("Finished VolumeModel Evaluation of {0} with {1} historic prices.", stock.StockCode, historicPrices.Count)));

                emailBody = null;
                return false;
            }

            this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Error, string.Format("No historic prices found for: {0}", stock.StockCode)));
            emailBody = null;
            return false;
        }
    }
}
