using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Server.Analysis
{
    public abstract class MovingAverage
    {
        protected int period;
        protected Queue<decimal> values = new Queue<decimal>();

        public MovingAverage(int period)
        {
            this.period = period;
        }

        public void Push(decimal value)
        {
            if (values.Count == this.period)
                values.Dequeue();
            values.Enqueue(value);
        }

        public void Push(List<decimal> values)
        {
            foreach (decimal value in values)
                Push(value);
        }

        public void Clear()
        {
            values.Clear();
        }

        public abstract decimal Calculate();
    }
}
