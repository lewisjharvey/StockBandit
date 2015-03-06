using System;
using System.Collections.Generic;
using StockBandit.Model;

namespace StockBandit.Server
{
    public interface IStockEngine
    {
        List<DailyPrice> Fetch(Stock stock, DateTime lastRetrieveTime);
    }
}