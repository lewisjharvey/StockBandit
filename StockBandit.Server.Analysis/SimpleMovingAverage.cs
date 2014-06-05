using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Server.Analysis
{
    public class SimpleMovingAverage : MovingAverage
    {
        public SimpleMovingAverage(int period) : base(period) { }

        public override decimal Calculate()
        {
            if (values.Count == 0) 
                return 0; 
            return values.Average();
        }
    }
}
