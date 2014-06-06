using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Server.Analysis
{
    public class MovingAverageConvergenceDivergenceModel : IModel
    {
        public bool Evaluate(List<ClosingPrice> historicPrices, decimal currentPrice, out string emailBody, out string emailSubject)
        {
            // Get a complete list of data sorted with closed date at the bottom
            List<ClosingPrice> closingPrices = new List<ClosingPrice>(historicPrices);
            closingPrices.Add(new ClosingPrice() { Date = DateTime.Now.Date, Price = currentPrice });
            closingPrices.Sort(ClosingPrice.CompareByDateAscending);

            // Now we need to run through each and update the EMAs
            ExponentialMovingAverage twelveDayExponentialMovingAverage = new ExponentialMovingAverage(12);
            ExponentialMovingAverage twentySixDayExponentialMovingAverage = new ExponentialMovingAverage(26);
            ExponentialMovingAverage nineDayExponentialMovingAverage = new ExponentialMovingAverage(9);

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

            ClosingPrice todayPrice = closingPrices.First();
            ClosingPrice yesterdayPrice = closingPrices.Skip(1).First();

            bool yesterday = yesterdayPrice.MACD >= yesterdayPrice.MACDEMA9;
            bool today = todayPrice.MACD >= todayPrice.MACDEMA9;

            if (yesterday != today)
            {
                if(yesterday)
                {
                    // We have fallen below, therefore bearish/sell
                    emailBody = string.Format("POSSIBLE MACD SELL ACTION\r\n\r\nCurrent Price: {0}\r\nMACD: {1}\r\nSignal: {2}", currentPrice, todayPrice.MACD, todayPrice.MACDEMA9);
                    emailSubject = "POSSIBLE MACD SELL ACTION ({0})";
                    return true;
                }
                else
                {
                    // We have risen above, therefore bullish/buy
                    emailBody = string.Format("POSSIBLE MACD BUY ACTION\r\n\r\nCurrent Price: {0}\r\nMACD: {1}\r\nSignal: {2}", currentPrice, todayPrice.MACD, todayPrice.MACDEMA9);
                    emailSubject = "POSSIBLE MACD BUY ACTION ({0})";
                    return true;
                }
            }

            emailBody = null;
            emailSubject = null;
            return false;
        }
    }
}
