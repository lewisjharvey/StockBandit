using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Server.Analysis
{
    public static class StandardDeviation
    {
        public static decimal CalculateStdDev(List<decimal> values)
        {
            double M = 0.0;
            double S = 0.0;
            int k = 1;
            foreach (decimal value in values)
            {
                double tmpM = M;
                M += ((double)value - tmpM) / k;
                S += ((double)value - tmpM) * ((double)value - M);
                k++;
            }
            decimal ret2 = (decimal)Math.Sqrt(S / (k - 1));
            return ret2;
        }
    }
}
