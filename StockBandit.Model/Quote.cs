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
using System.ComponentModel;

namespace StockBandit.Model
{
    public class Quote
    {
        public DateTime LastUpdate { get; set; }
        public string Symbol { get; set; }
        public decimal? LastTradePrice { get; set; }

        public Quote(string symbol)
        {
            this.Symbol = symbol;
        }
    }
}