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

        public BollingerBandsModel(int bandPeriod)
        {
            this.bandPeriod = bandPeriod;
        }

        public bool Evaluate(List<decimal> historicPrices, decimal currentPrice, out string emailBody, out string emailSubject)
        {
            List<decimal> bandedHistoricPrices = historicPrices.Take(this.bandPeriod).ToList();

            decimal middleBand = bandedHistoricPrices.Average();
            decimal standardDeviation = StandardDeviation.CalculateStdDev(bandedHistoricPrices);
            decimal upperBand = middleBand + (standardDeviation * 2);
            decimal lowerBand = middleBand - (standardDeviation * 2);

            if (currentPrice >= upperBand)
            {
                emailBody = string.Format("POSSIBLE BOLLINGER SELL ACTION\r\n\r\nCurrent Price: {0}\r\nUpper Band: {1}", currentPrice, upperBand);
                emailSubject = "POSSIBLE BOLLINGER SELL ACTION ({0})";
                return true;
            }

            if (currentPrice <= lowerBand)
            {
                emailBody = string.Format("POSSIBLE BOLLINGER BUY ACTION\r\n\r\nCurrent Price: {0}\r\nLower Band: {1}", currentPrice, lowerBand);
                emailSubject = "POSSIBLE BOLLINGER BUY ACTION ({0})";
                return true;
            }

            emailBody = null;
            emailSubject = null;
            return false;
        }
    }
}
