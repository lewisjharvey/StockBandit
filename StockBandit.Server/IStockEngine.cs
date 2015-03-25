#region © Copyright
// <copyright file="IStockEngine.cs" company="Lewis Harvey">
//      Copyright (c) Lewis Harvey. All rights reserved.
//      This software is provided "as is" without warranty of any kind, express or implied, 
//      including but not limited to warranties of merchantability and fitness for a particular 
//      purpose. The authors do not support the Software, nor do they warrant
//      that the Software will meet your requirements or that the operation of the Software will
//      be uninterrupted or error free or that any defects will be corrected.
// </copyright>
#endregion

using System;
using System.Collections.Generic;
using StockBandit.Model;

namespace StockBandit.Server
{
    /// <summary>
    /// The stock engine for collecting prices
    /// </summary>
    public interface IStockEngine
    {
        /// <summary>
        /// Fetches latest prices for a stock
        /// </summary>
        /// <param name="stock">The stock to check prices for</param>
        /// <param name="lastRetrieveTime">The date the last retrieve was made</param>
        /// <returns>A list of retrieved prices</returns>
        List<DailyPrice> Fetch(Stock stock, DateTime lastRetrieveTime);
    }
}