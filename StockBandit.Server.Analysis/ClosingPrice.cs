using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Server.Analysis
{
    public class ClosingPrice
    {
        public DateTime Date;
        public decimal Price;
        public int Volume;
        public decimal EMA12;
        public decimal EMA26;
        public decimal MACD
        {
            get
            {
                return EMA12 - EMA26;
            }
        }
        public decimal MACDEMA9;

        public static int CompareByDateAscending(ClosingPrice a, ClosingPrice b)
        {
            if (a.Date == b.Date)
                return (0);

            if (a.Date < b.Date)
                return (-1);

            return (1);
        }

        public static int CompareByDateDescending(ClosingPrice a, ClosingPrice b)
        {
            if (a.Date == b.Date)
                return (0);

            if (a.Date < b.Date)
                return (1);

            return (-1);
        }
    }
}
