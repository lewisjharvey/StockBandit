using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StockBandit.Server.Analysis
{
    public interface IModel
    {
        bool Evaluate(List<decimal> historicPrices, decimal currentPrice, out string emailBody, out string emailSubject);
    }
}
