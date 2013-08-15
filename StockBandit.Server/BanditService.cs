#region © Copyright
//
// © Copyright 2013 Lewis Harvey
//   All rights reserved.
//
// This software is provided "as is" without warranty of any kind, express or implied, 
// including but not limited to warranties of merchantability and fitness for a particular 
// purpose. The authors do not support the Software, nor do they warrant
// that the Software will meet your requirements or that the operation of the Software will
// be uninterrupted or error free or that any defects will be corrected.
//
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using StockBandit.Model;

namespace StockBandit.Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single, UseSynchronizationContext = false)]
    public class BanditService : IBanditService
    {
        private StockServer stockServer;

        public BanditService(StockServer stockServer)
        {
            this.stockServer = stockServer;
        }

        public string SayHello()
        {
            return "Hello, I'm the Stock Bandit service and I'm running.";
        }

        public List<string> GetLastPrices()
        {
            return this.stockServer.GetLastPrices();
        }

        public List<string> GetLastPriceHistories()
        {
            return this.stockServer.GetLastPriceHistories();
        }

        public void AddStock(string stockCode, string stockName)
        {
            this.stockServer.AddStock(stockCode, stockName);
        }

        public void DeleteStock(string stockCode)
        {
            this.stockServer.DeleteStock(stockCode);
        }

        public void ForcePrices()
        {
            this.stockServer.PriceFetchTimerElapsed();
        }

        
    }
}
