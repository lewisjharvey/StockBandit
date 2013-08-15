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

namespace StockBandit.Client.Console
{
    public class BanditService : ClientBase<IBanditService>, IBanditService
    {
        public string SayHello()
        {
            return Channel.SayHello();
        }

        public List<string> GetLastPrices()
        {
            return Channel.GetLastPrices();
        }

        public List<string> GetLastPriceHistories()
        {
            return Channel.GetLastPriceHistories();
        }

        public void AddStock(string stockCode, string stockName)
        {
            Channel.AddStock(stockCode, stockName);
        }

        public void ForcePrices()
        {
            Channel.ForcePrices();
        }

        public void DeleteStock(string stockCode)
        {
            Channel.DeleteStock(stockCode);
        }
    }
}
