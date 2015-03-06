using StockBandit.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Server
{
    public interface IModel
    {
        bool Evaluate(Stock stock, List<DailyPrice> historicPrices, out string emailBody);
    }
}
