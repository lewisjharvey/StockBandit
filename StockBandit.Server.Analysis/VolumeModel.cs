using StockBandit.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Server.Analysis
{
    public class VolumeModel : IModel
    {
        private double alertThreshold;

        public VolumeModel(double alertThreshold)
        {
            this.alertThreshold = alertThreshold;
        }

        public bool Evaluate(Quote quote, List<ClosingPrice> historicPrices, decimal currentPrice, out string emailBody, out string emailSubject)
        {
            //ClosingPrice todayPrice = historicPrices.First();
            ClosingPrice yesterdayPrice = historicPrices.First();

            bool yesterday = yesterdayPrice.Volume > (quote.AverageVolume * this.alertThreshold);
            bool today = quote.CurrentVolume > (quote.AverageVolume * this.alertThreshold);

            if (yesterday && today)
            {
                // Volume indicates a shift in movement soon
                emailBody = string.Format("POSSIBLE VOLUME ACTION\r\n\r\nStock: {0}\r\nCurrent Price: {1}\r\nToday Volume: {2}\r\nYesterday Volume: {4}\r\nAverage Volume: {5}", quote.Symbol, currentPrice, quote.CurrentVolume, quote.AverageVolume, yesterdayPrice.Volume, quote.AverageVolume);
                emailSubject = string.Format("POSSIBLE VOLUME ACTION ({0})", quote.Symbol);
                return true;
            }

            emailBody = null;
            emailSubject = null;
            return false;
        }
    }
}
