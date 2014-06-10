using StockBandit.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Server.Analysis
{
    public class BollingerBandsModel : IModel
    {
        private int bandPeriod;
        private bool notificationSent = false;

        public BollingerBandsModel(int bandPeriod)
        {
            this.bandPeriod = bandPeriod;
        }

        public bool Evaluate(List<ClosingPrice> historicPrices, decimal currentPrice, out string emailBody, out string emailSubject)
        {
            List<decimal> bandedHistoricPrices = historicPrices.Take(this.bandPeriod).Select(p => p.Price).ToList();

            decimal middleBand = bandedHistoricPrices.Average();
            decimal standardDeviation = StandardDeviation.CalculateStdDev(bandedHistoricPrices);
            decimal upperBand = middleBand + (standardDeviation * 2);
            decimal lowerBand = middleBand - (standardDeviation * 2);

            if (currentPrice >= upperBand)
            {
                if (!notificationSent)
                {
                    notificationSent = true;
                    emailBody = string.Format("POSSIBLE BOLLINGER SELL ACTION\r\n\r\nCurrent Price: {0}\r\nUpper Band: {1}", currentPrice, upperBand);
                    emailSubject = "POSSIBLE BOLLINGER SELL ACTION ({0})";
                    return true;
                }
            }
            else if (currentPrice <= lowerBand)
            {
                if (!notificationSent)
                {
                    notificationSent = true;
                    emailBody = string.Format("POSSIBLE BOLLINGER BUY ACTION\r\n\r\nCurrent Price: {0}\r\nLower Band: {1}", currentPrice, lowerBand);
                    emailSubject = "POSSIBLE BOLLINGER BUY ACTION ({0})";
                    return true;
                }
            }
            else
            {
                notificationSent = false;
            }
            emailBody = null;
            emailSubject = null;

            return false;
        }
    }
}
