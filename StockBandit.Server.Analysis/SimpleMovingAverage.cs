using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Server.Analysis
{
    public class SimpleMovingAverage
    {
        protected int period;
        protected Queue<decimal> values = new Queue<decimal>();

        public SimpleMovingAverage(int period)
        {
            this.period = period;
        }

        public void Push(decimal value)
        {
            if (values.Count == this.period)
                values.Dequeue();
            values.Enqueue(value);
        }

        public void Clear()
        {
            values.Clear();
        }

        public decimal Calculate()
        {
            if (values.Count == 0) 
                return 0; 
            return values.Average();
        }
    }
}
