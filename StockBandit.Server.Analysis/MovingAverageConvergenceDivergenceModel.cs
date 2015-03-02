using StockBandit.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Server.Analysis
{
    public class MovingAverageConvergenceDivergenceModel : IModel
    {
        private Dictionary<string, bool> notificationsSent;

        public MovingAverageConvergenceDivergenceModel()
        {
            this.notificationsSent = new Dictionary<string, bool>();
        }

        public bool Evaluate(Quote quote, List<ClosingPrice> historicPrices, decimal currentPrice, out string emailBody, out string emailSubject)
        {
            if (!notificationsSent.ContainsKey(quote.Symbol))
                this.notificationsSent.Add(quote.Symbol, false);

            // Clone the historicprices as they are passed by reference and we don't want to change the underlying data
            List<ClosingPrice> closingPrices = new List<ClosingPrice>(historicPrices);
            // For analysis we need to add the current price
            closingPrices.Add(new ClosingPrice() { Date = DateTime.Now.Date, Price = currentPrice });
            // Sort them so that the current price is at the bottom as the moving average is calculate from old to new
            closingPrices.Sort(ClosingPrice.CompareByDateAscending);

            // MACD uses three EMA - these are instantiated here.
            ExponentialMovingAverage twelveDayExponentialMovingAverage = new ExponentialMovingAverage(12);
            ExponentialMovingAverage twentySixDayExponentialMovingAverage = new ExponentialMovingAverage(26);
            ExponentialMovingAverage nineDayExponentialMovingAverage = new ExponentialMovingAverage(9);

            // Loop through the closing prices and do the calculation for MACD and the signal line.
            foreach(ClosingPrice closingPrice in closingPrices)
            {
                closingPrice.EMA12 = twelveDayExponentialMovingAverage.Calculate(closingPrice.Price);              
                closingPrice.EMA26 = twentySixDayExponentialMovingAverage.Calculate(closingPrice.Price);
                closingPrice.MACDEMA9 = nineDayExponentialMovingAverage.Calculate(closingPrice.MACD);
            }

            // Check for crossover
            // 1. Get last two items, 2. Get direction of first two, 3. Get direction of second two. 4. If different there is crossover
            // Reverse the order as it's easier to work down now.
            closingPrices.Reverse();

            if (closingPrices.Count > 1)
            {
                ClosingPrice todayPrice = closingPrices.First();
                ClosingPrice yesterdayPrice = closingPrices.Skip(1).First();

                bool yesterday = yesterdayPrice.MACD >= yesterdayPrice.MACDEMA9;
                bool today = todayPrice.MACD >= todayPrice.MACDEMA9;

                if (yesterday != today)
                {
                    if (yesterday)
                    {
                        if (!this.notificationsSent[quote.Symbol])
                        {
                            // We have fallen below, therefore bearish/sell
                            emailBody = string.Format("POSSIBLE MACD SELL ACTION\r\n\r\nStock: {0}\r\nCurrent Price: {1}\r\nMACD: {2}\r\nSignal: {3}", quote.Symbol, currentPrice, todayPrice.MACD, todayPrice.MACDEMA9);
                            emailSubject = string.Format("POSSIBLE MACD SELL ACTION ({0})", quote.Symbol);
                            this.notificationsSent[quote.Symbol] = true;
                            return true;
                        }
                    }
                    else
                    {
                        if (!this.notificationsSent[quote.Symbol])
                        {
                            // We have risen above, therefore bullish/buy
                            emailBody = string.Format("POSSIBLE MACD BUY ACTION\r\n\r\nStock: {0}\r\nCurrent Price: {1}\r\nMACD: {2}\r\nSignal: {3}", quote.Symbol, currentPrice, todayPrice.MACD, todayPrice.MACDEMA9);
                            emailSubject = string.Format("POSSIBLE MACD BUY ACTION ({0})", quote.Symbol);
                            this.notificationsSent[quote.Symbol] = true;
                            return true;
                        }
                    }
                }
                else
                {
                    this.notificationsSent[quote.Symbol] = false;
                }
            }
            emailBody = null;
            emailSubject = null;
            return false;
        }
    }
}
