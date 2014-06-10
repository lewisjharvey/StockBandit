using StockBandit.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Server.Analysis
{
    public interface IModel
    {
        bool Evaluate(Quote quote, List<ClosingPrice> historicPrices, decimal currentPrice, out string emailBody, out string emailSubject);
    }
}
