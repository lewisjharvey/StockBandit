#region © Copyright
// <copyright file="IModel.cs" company="Lewis Harvey">
//      Copyright (c) Lewis Harvey. All rights reserved.
//      This software is provided "as is" without warranty of any kind, express or implied, 
//      including but not limited to warranties of merchantability and fitness for a particular 
//      purpose. The authors do not support the Software, nor do they warrant
//      that the Software will meet your requirements or that the operation of the Software will
//      be uninterrupted or error free or that any defects will be corrected.
// </copyright>
#endregion

using System.Collections.Generic;

namespace StockBandit.Server
{
    /// <summary>
    /// Represents a model for analysis of stocks
    /// </summary>
    public interface IModel
    {
        /// <summary>
        /// Called to evaluate the model and analyse a stock
        /// </summary>
        /// <param name="stock">The stock to analyse</param>
        /// <param name="historicPrices">The recent prices for analysis</param>
        /// <param name="emailBody">The email segment to append to the email</param>
        /// <returns>The result if the stock should be alerted on</returns>
        bool Evaluate(Stock stock, List<DailyPrice> historicPrices, out string emailBody);
    }
}
