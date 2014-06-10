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
        private Dictionary<string, bool> notificationsSent;

        public BollingerBandsModel(int bandPeriod)
        {
            this.bandPeriod = bandPeriod;
            this.notificationsSent = new Dictionary<string, bool>();
        }

        public bool Evaluate(Quote quote, List<ClosingPrice> historicPrices, decimal currentPrice, out string emailBody, out string emailSubject)
        {
            if (!notificationsSent.ContainsKey(quote.Symbol))
                this.notificationsSent.Add(quote.Symbol, false);

            List<decimal> bandedHistoricPrices = new List<decimal>();
            bandedHistoricPrices.Add(currentPrice);
            bandedHistoricPrices.AddRange(historicPrices.Take(this.bandPeriod-1).Select(p => p.Price).ToList());

            decimal middleBand = bandedHistoricPrices.Average();
            decimal standardDeviation = StandardDeviation.CalculateStdDev(bandedHistoricPrices);
            decimal upperBand = middleBand + (standardDeviation * 2);
            decimal lowerBand = middleBand - (standardDeviation * 2);

            if (currentPrice >= upperBand)
            {
                if (!this.notificationsSent[quote.Symbol])
                {
                    this.notificationsSent[quote.Symbol] = true;
                    emailBody = string.Format("POSSIBLE BOLLINGER SELL ACTION\r\n\r\nCurrent Price: {0}\r\nUpper Band: {1}", currentPrice, upperBand);
                    emailSubject = string.Format("POSSIBLE BOLLINGER SELL ACTION ({0})", quote.Symbol);
                    return true;
                }
            }
            else if (currentPrice <= lowerBand)
            {
                if (!this.notificationsSent[quote.Symbol])
                {
                    this.notificationsSent[quote.Symbol] = true;
                    emailBody = string.Format("POSSIBLE BOLLINGER BUY ACTION\r\n\r\nCurrent Price: {0}\r\nLower Band: {1}", currentPrice, lowerBand);
                    emailSubject = string.Format("POSSIBLE BOLLINGER BUY ACTION ({0})", quote.Symbol);
                    return true;
                }
            }
            else
            {
                this.notificationsSent[quote.Symbol] = false;
            }
            emailBody = null;
            emailSubject = null;

            return false;
        }
    }
}
