#region © Copyright
// <copyright file="VolumeModel.cs" company="Lewis Harvey">
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
using System.Linq;

using MathNet.Numerics;
using StockBandit.Model;

namespace StockBandit.Server
{
    /// <summary>
    /// An implementation of a model for checking volume anomalies.
    /// </summary>
    public class VolumeModel : IModel
    {
        /// <summary>
        /// The threshold for alerting against the volume
        /// </summary>
        private readonly double alertThreshold;

        /// <summary>
        /// An instance of the logging engine
        /// </summary>
        private readonly ILogQueue logQueue; 

        /// <summary>
        /// Initialises a new instance of the <see cref="VolumeModel" /> class.
        /// </summary>
        /// <param name="alertThreshold">the threshold at which volume above the average should alert on</param>
        /// <param name="logQueue">Instance of the logging engine</param>
        public VolumeModel(double alertThreshold, ILogQueue logQueue)
        {
            this.alertThreshold = alertThreshold;
            this.logQueue = logQueue;
        }

        /// <summary>
        /// Evaluates a stock to check against this model
        /// </summary>
        /// <param name="stock">The stock to check</param>
        /// <param name="historicPrices">The last prices to use in the model</param>
        /// <param name="emailBody">An output of the email text to append to the email</param>
        /// <returns>A flag indicating if the stock evaluated to being above volume</returns>
        public bool Evaluate(Stock stock, List<DailyPrice> historicPrices, out string emailBody)
        {
            if (historicPrices.Count > 0)
            {
                this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info, string.Format("VolumeModel Evaluation of {0} with {1} historic prices.", stock.StockCode, historicPrices.Count)));
                DailyPrice todayPrice = historicPrices.First();

                // Current average price
                var averageVolume = historicPrices
                    .Take(90)
                    .Select(p => p.Volume)
                    .Average();

                if (todayPrice.Volume > (averageVolume * this.alertThreshold))
                {
                    // Check the trend and ignore downward trends
                    if (this.CheckUpwardTrend(historicPrices.Take(90).ToList()))
                    {
                        // Volume indicates a shift in movement soon
                        emailBody =
                            string.Format(
                                "Stock: {0}\r\nPrice: {1}\r\nVolume: {2}\r\nAverage Volume: {3}\r\n\r\n",
                                stock.StockCode,
                                todayPrice.Close,
                                todayPrice.Volume,
                                Math.Round(averageVolume, 0));
                        this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info, emailBody));
                        return true;
                    }
                }

                this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info, string.Format("Finished VolumeModel Evaluation of {0} with {1} historic prices.", stock.StockCode, historicPrices.Count)));

                emailBody = null;
                return false;
            }

            this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Error, string.Format("No historic prices found for: {0}", stock.StockCode)));
            emailBody = null;
            return false;
        }

        /// <summary>
        /// Checks if the trend if upwards
        /// </summary>
        /// <param name="prices">The closing prices to check</param>
        /// <returns>A flag indicating if the trend is upwards</returns>
        private bool CheckUpwardTrend(List<DailyPrice> prices)
        {
            // Reverse as they come in in the wrong order
            prices.Reverse();

            double[] initialXAxis = Enumerable.Range(1, prices.Count()).Select(p => (double)p).ToArray();
            Tuple<double, double> result = Fit.Line(initialXAxis, prices.Select(p => (double)p.Close).ToArray());

            // Item2 is the slope
            if (result.Item2 > 0)
                return true;
            else
                return false;
        }
    }
}
