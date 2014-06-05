using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Server.Analysis
{
    //public class ExponentialMovingAverage : MovingAverage
    //{
    //    public ExponentialMovingAverage(int period) : base(period) { }

    //    public override decimal Calculate()
    //    {
    //        decimal alpha = (2 / ((decimal)period + 1));
    //        //return values.DefaultIfEmpty()
    //        // .Aggregate((ema, nextValue) => alpha * nextValue + (1 - alpha) * ema);
    //        // {Close - EMA(previous day)} x multiplier + EMA(previous day).
    //        if (values.Count < period)
    //            return 0;
    //        decimal emaPreviousDay = values.Skip(period - 2).First().EMA12;
    //        if (emaPreviousDay == 0)
    //        {
    //            SimpleMovingAverage sma = new SimpleMovingAverage(period);
    //            sma.Push(this.values.ToList());
    //            return sma.Calculate();
    //        }
    //        decimal ema = (values.Last().Price - emaPreviousDay) * alpha + emaPreviousDay;
    //        return ema;
    //    }
    //}

    public class ExponentialMovingAverage
    {
        private decimal alpha;
        private int period;
        private decimal? oldValue;
        private int counter = 0;
        private SimpleMovingAverage simpleMovingAverage;

        public ExponentialMovingAverage(int period)
        {
            this.period = period;
            this.alpha = (2 / ((decimal)period + 1));

            this.simpleMovingAverage = new SimpleMovingAverage(period);
        }

        public decimal Calculate(decimal value)
        {
            // Set the counter where we are
            counter++;
            if (counter < period)
            {
                this.simpleMovingAverage.Push(value);
                // TODO Need to check 0!
                return 0;
            }

            if(counter == period)
            {
                // We need a SMA
                this.simpleMovingAverage.Push(value);
                decimal sma = this.simpleMovingAverage.Calculate();
                oldValue = sma;
                return sma;
            }

            // Now we are over
            if (!oldValue.HasValue)
            {
                oldValue = value;
                return value;
            }
            decimal newValue = oldValue.Value + alpha * (value - oldValue.Value);
            oldValue = newValue;
            return newValue;
        }
    }
}
