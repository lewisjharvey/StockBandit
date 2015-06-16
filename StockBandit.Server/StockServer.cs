#region © Copyright
// <copyright file="StockServer.cs" company="Lewis Harvey">
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using StockBandit.Model;

namespace StockBandit.Server
{
    /// <summary>
    /// Provides the server capabilities for the stock management.
    /// </summary>
    public class StockServer
    {
        /// <summary>
        /// The email queue for sending messages
        /// </summary>
        private EmailQueue emailQueue;

        /// <summary>
        /// THe engine for receiving historical prices
        /// </summary>
        private YahooHistoricWebStockEngine yahooHistoricWebStockEngine;

        /// <summary>
        /// The models that are registered for analysis
        /// </summary>
        private List<IModel> registeredModels;

        /// <summary>
        /// The timer for collecting prices and doing analysis
        /// </summary>
        private Timer priceFetchTimer;

        /// <summary>
        /// An instance of the logging engine
        /// </summary>
        private LogQueue logQueue;

        /// <summary>
        /// Initialises a new instance of the <see cref="StockServer" /> class.
        /// </summary>
        public StockServer()
        {
            this.logQueue = new LogQueue(1000);
        }

        /// <summary>
        /// Gets or sets the email server address
        /// </summary>
        public string EmailServer { get; set; }

        /// <summary>
        /// Gets or sets the from address where emails should come from
        /// </summary>
        public string EmailFromAddress { get; set; }

        /// <summary>
        /// Gets or sets the username to access the mail server
        /// </summary>
        public string EmailUsername { get; set; }

        /// <summary>
        /// Gets or sets the password to access the mail server
        /// </summary>
        public string EmailPassword { get; set; }

        /// <summary>
        /// Gets or sets the port to access the email server
        /// </summary>
        public int EmailPort { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the mail server requires SLL
        /// </summary>
        public bool EmailSSL { get; set; }

        /// <summary>
        /// Gets or sets the recipient email address of system emails
        /// </summary>
        public string EmailRecipient { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the volume model is enabled
        /// </summary>
        public bool EnableVolume { get; set; }
        
        /// <summary>
        /// Gets or sets the alerting threshold for volume
        /// </summary>
        public double AlertThreshold { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum market cap for stocks in the system
        /// </summary>
        public decimal MarketCapMax { get; set; }
        
        /// <summary>
        /// Gets or sets the minimum market cap for stocks in the system
        /// </summary>
        public decimal MarketCapMin { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum price of a stock to check
        /// </summary>
        public decimal PriceMin { get; set; }
        
        /// <summary>
        /// Gets or sets the minimum price of a stock to check
        /// </summary>
        public decimal PriceMax { get; set; }
        
        /// <summary>
        /// Gets or sets the hour the system should do the analysis
        /// </summary>
        public int HourToRun { get; set; }
        
        /// <summary>
        /// Gets or sets the number of days to silence a stock after alerting
        /// </summary>
        public int NumberOfDaysToSilence { get; set; }
        
        /// <summary>
        /// Gets or sets the list of stocks held within the system.
        /// </summary>
        public ObservableCollection<Stock> StockCodesList { get; set; }

        /// <summary>
        /// Starts the server and all classes required.
        /// </summary>
        /// <returns>The result if it could be started,</returns>
        public bool StartServer()
        {
            try
            {
                // Set up the email and log queues
                if ((this.EmailServer.Trim().Length > 0) && (this.EmailFromAddress.Trim().Length > 0))
                    this.emailQueue = new EmailQueue(this.EmailServer, this.EmailPort, this.EmailUsername, this.EmailPassword, this.EmailFromAddress, this.EmailSSL, 1000, this.logQueue);
                else
                    this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info, "Email not configured."));

                // Convert the stocks to a list
                this.StockCodesList = new ObservableCollection<Stock>();
                
                // Setup Yahoo for querying
                this.yahooHistoricWebStockEngine = new YahooHistoricWebStockEngine(this.logQueue);

                using (StockBanditDataContext dataContext = new StockBanditDataContext())
                {
                    foreach (var stock in dataContext.Stocks.Where(s => s.Active))
                    {
                        if (stock.MarketCap > this.MarketCapMin && stock.MarketCap < this.MarketCapMax)
                            this.StockCodesList.Add(stock);
                    }
                }

                // Get all the historical data and populate
                this.RegisterModels();
                this.StartPriceFetchTimer();
            }
            catch (Exception ex)
            {
                this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Fatal, string.Format("Error starting server - {0}", ex)));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Stops the server
        /// </summary>
        /// <returns>The result of the stop attempt</returns>
        public bool StopServer()
        {
            this.emailQueue.StopProcessingQueue();

            this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info, "Server Stopped."));
            this.logQueue.StopProcessingQueue();
            return true;
        }

        /// <summary>
        /// The method for the timer to fire
        /// </summary>
        /// <param name="sender">The sender object calling the method</param>
        public void PriceFetchTimerElapsed(object sender)
        {
            if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
                return;

            try
            {
                this.PopulateHistoricPrices();

                StringBuilder resultStringBuilder = new StringBuilder();

                foreach (Stock stock in this.StockCodesList)
                {
                    string resultString = null;

                    if (!stock.Silenced)
                        this.GetPricesAndEvaluate(stock, out resultString);
                    else
                    {
                        // If it is silenced, has it been 30 days?
                        if (stock.LastAlertTime.GetValueOrDefault() < DateTime.Now.AddDays(-this.NumberOfDaysToSilence))
                        {
                            this.GetPricesAndEvaluate(stock, out resultString);

                            // Now reset so it is checked again tomorrow.
                            this.ResetStockSilenceFlags(stock);
                        }
                    }

                    if (!string.IsNullOrEmpty(resultString))
                    {
                        resultStringBuilder.AppendLine(resultString);
                        this.SilenceStock(stock);
                    }
                }

                this.QueueEmail(this.EmailRecipient, "DAILY STOCK DIGEST", resultStringBuilder.ToString());
            }
            catch (Exception e)
            {
                this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Error, string.Format("Error in PriceAnalysisTimerElapsed: {0}", e)));
                this.QueueEmail(this.EmailRecipient, "System Error", e.ToString());
            }
        }

        /// <summary>
        /// Send a started email to indicate the system has started.
        /// </summary>
        public void SendStartedEmail()
        {
            if (!string.IsNullOrEmpty(this.EmailRecipient))
            {
                try
                {
                    this.QueueEmail(
                        this.EmailRecipient,
                        "Stock Bandit Server Started",
                        string.Format("Stock Bandit server started successfully at {0} on version {1}", DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy"), Assembly.GetExecutingAssembly().GetName().Version.ToString()));
                }
                catch (Exception e)
                {
                    this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Error, string.Format("Exception in StockServer.SendStartedEmail - Exception: {0}", e.ToString())));
                }
            }
        }

        /// <summary>
        /// Register all the analysis models to be checked
        /// </summary>
        private void RegisterModels()
        {
            this.registeredModels = new List<IModel>();
            if (this.EnableVolume)
                this.registeredModels.Add(new VolumeModel(this.AlertThreshold, this.logQueue));
        }

        /// <summary>
        /// Start the timer that will periodically check prices
        /// </summary>
        private void StartPriceFetchTimer()
        {
            // Create a callback for the timer 
            AutoResetEvent autoEvent = new AutoResetEvent(false);

            // Create a timer that signals the delegate to invoke 
            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            DateTime nextRun = today.AddHours(this.HourToRun).AddDays(now.Hour >= this.HourToRun ? 1 : 0);
            this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info, string.Format("Started Position Reset Timer. Next run time: {0}", nextRun)));

            this.priceFetchTimer = new Timer(this.PriceFetchTimerElapsed, autoEvent, nextRun - DateTime.Now, TimeSpan.FromHours(24));
        }
        
        /// <summary>
        /// Silence a stock when it have alerted
        /// </summary>
        /// <param name="stock">The stock to silence</param>
        private void SilenceStock(Stock stock)
        {
            using (StockBanditDataContext dataContext = new StockBanditDataContext())
            {
                Stock dalStock = dataContext.Stocks.Single(s => s.StockCode == stock.StockCode);
                dalStock.Silenced = true;
                dalStock.LastAlertTime = DateTime.Now;
                dataContext.SubmitChanges();

                stock.Silenced = true;
                stock.LastAlertTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Reset the stock silence flags so it is checked again
        /// </summary>
        /// <param name="stock">The stock to reset</param>
        private void ResetStockSilenceFlags(Stock stock)
        {
            using (StockBanditDataContext dataContext = new StockBanditDataContext())
            {
                Stock dalStock = dataContext.Stocks.Single(s => s.StockCode == stock.StockCode);
                dalStock.Silenced = false;
                dalStock.LastAlertTime = null;
                dataContext.SubmitChanges();

                stock.Silenced = false;
                stock.LastAlertTime = null;
            }
        }

        /// <summary>
        /// Collect the historical prices for a stock and evaluate against the registered models
        /// </summary>
        /// <param name="stock">The stock to check</param>
        /// <param name="resultString">The output string of the evaluation</param>
        private void GetPricesAndEvaluate(Stock stock, out string resultString)
        {
            resultString = null;

            using (StockBanditDataContext dataContext = new StockBanditDataContext())
            {
                List<DailyPrice> historicPrices =
                    dataContext.DailyPrices.Where(
                        p =>
                            p.StockCode == stock.StockCode &&
                            p.Date > DateTime.Now.Date.Subtract(new TimeSpan(1825, 0, 0, 0, 0)) &&
                            p.Date <= DateTime.Now.Date).OrderByDescending(p => p.Date).ToList();

                if (historicPrices.Count > 0 && historicPrices.First().Close > this.PriceMin &&
                    historicPrices.First().Close < this.PriceMax)
                {
                    foreach (IModel model in this.registeredModels)
                        model.Evaluate(stock, historicPrices, out resultString);
                }
            }
        }

        /// <summary>
        /// Queue an email to be sent
        /// </summary>
        /// <param name="recipient">The recipient of the message</param>
        /// <param name="subject">The subject of the message</param>
        /// <param name="body">The body of the message</param>
        private void QueueEmail(string recipient, string subject, string body)
        {
            if (this.emailQueue != null)
            {
                this.emailQueue.QueueEmail(recipient, subject, body);
            }
        }
                
        /// <summary>
        /// Populate the historic prices for all stocks
        /// </summary>
        private void PopulateHistoricPrices()
        {
            try
            {
                List<Task> updateTasks = new List<Task>();

                // Get the latest prices
                foreach (Stock stock in this.StockCodesList)
                {
                    this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info, string.Format("Collecting Historic Prices for {0}", stock.StockCode)));

                    DateTime lastRetrieveDate;
                    using (StockBanditDataContext dataContext = new StockBanditDataContext())
                    {
                        // Get the historic prices
                        lastRetrieveDate = dataContext.DailyPrices.Where(p => p.StockCode == stock.StockCode && p.Date <= DateTime.Today)
                            .OrderByDescending(p => p.Date)
                            .Select(p => p.Date)
                            .FirstOrDefault();
                    }

                    if (lastRetrieveDate == default(DateTime))
                        lastRetrieveDate = DateTime.Now.AddDays(-150);

                    Task updateTask = Task.Factory.StartNew(() => { this.GetPricesForStock(stock, lastRetrieveDate); }, TaskCreationOptions.LongRunning);
                    updateTasks.Add(updateTask);

                    this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info, string.Format("Collected Historic Prices for {0}", stock.StockCode)));
                }

                Task.WaitAll(updateTasks.ToArray());
            }
            catch (System.Net.WebException)
            {
                this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Error, "No internet access - unable to get prices."));
            }
            catch (Exception e)
            {
                this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Error, string.Format("Error in PopulateHistoricPrices: {0}", e)));
                this.QueueEmail(this.EmailRecipient, "System Error", e.ToString());
            }

            this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info, "Finished Collecting Historic Prices"));
        }

        /// <summary>
        /// Gets the historic prices for a stock
        /// </summary>
        /// <param name="stock">The stock to collect prices for</param>
        /// <param name="lastRetrieveDate">The date last retrieved</param>
        private void GetPricesForStock(Stock stock, DateTime lastRetrieveDate)
        {
            List<DailyPrice> historicPrices = this.yahooHistoricWebStockEngine.Fetch(stock, lastRetrieveDate);

            using (StockBanditDataContext dataContext = new StockBanditDataContext())
            {
                // If different add to queue.
                foreach (DailyPrice dailyPrice in historicPrices)
                {
                    // Find price for today
                    DailyPrice existingPrice =
                        dataContext.DailyPrices.FirstOrDefault(
                            p => p.Date == dailyPrice.Date && p.StockCode == stock.StockCode);
                    if (existingPrice != null)
                    {
                        existingPrice.High = dailyPrice.High;
                        existingPrice.Low = dailyPrice.Low;
                        existingPrice.Open = dailyPrice.Open;
                        existingPrice.Close = dailyPrice.Close;
                        existingPrice.Volume = dailyPrice.Volume;
                        existingPrice.AdjustedClose = dailyPrice.AdjustedClose;
                    }
                    else
                    {
                        dataContext.DailyPrices.InsertOnSubmit(dailyPrice);
                    }

                    dataContext.SubmitChanges();
                }
            }
        }
    }
}
