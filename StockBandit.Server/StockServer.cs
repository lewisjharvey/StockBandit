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
using System.Collections.ObjectModel;
using System.Reflection;
using log4net;
using StockBandit.Model;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Threading;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using log4net.Core;

namespace StockBandit.Server
{
    public class StockServer
    {
        #region ServerSettings

        public string EmailServer { get; set; }
        public string EmailFromAddress { get; set; }
        public string EmailUsername { get; set; }
        public string EmailPassword { get; set; }
        public int EmailPort { get; set; }
        public bool EmailSSL { get; set; }
        public string EmailRecipient { get; set; }
        public bool EnableVolume { get; set; }
        public double AlertThreshold { get; set; }
        public decimal MarketCapMax { get; set; }
        public decimal MarketCapMin { get; set; }
        public decimal PriceMin { get; set; }
        public decimal PriceMax { get; set; }
        public int HourToRun { get; set; }

        #endregion

        public ObservableCollection<Stock> StockCodesList { get; set; }

        private EmailQueue emailQueue;
        //private Dictionary<string, List<DailyPrice>> historicPrices;
        private object semaphore = new object();
        private ServiceHost serviceHost;
        private YahooHistoricWebStockEngine yahooHistoricWebStockEngine;
        private List<IModel> registeredModels;
        private Timer priceFetchTimer;
        private LogQueue logQueue;

        #region StartupProcedures

        public StockServer()
        {
            this.logQueue = new LogQueue(1000);
            this.serviceHost = new ServiceHost(new BanditService(this));
        }

        public bool StartServer()
        {
            try
            {
                this.serviceHost.Open();

                // Set up the email and log queues
                if ((this.EmailServer.Trim().Length > 0) && (this.EmailFromAddress.Trim().Length > 0))
                    this.emailQueue = new EmailQueue(this, this.EmailServer, this.EmailPort, this.EmailUsername, this.EmailPassword, this.EmailFromAddress, this.EmailSSL, 1000);
                else
                    this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info, "Email not configured."));

                // Convert the stocks to a list
                this.StockCodesList = new ObservableCollection<Stock>();
                // Setup google for querying
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
                RegisterModels();
                StartPriceFetchTimer();
            }
            catch (Exception ex)
            {
                this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Fatal, string.Format("Error starting server - {0}", ex)));
                return false;
            }
            return true;
        }

        private void RegisterModels()
        {
            this.registeredModels = new List<IModel>();
            if (this.EnableVolume)
                this.registeredModels.Add(new VolumeModel(this.AlertThreshold, this.logQueue));
        }

        private void StartPriceFetchTimer()
        {
            // Create a callback for the timer 
            AutoResetEvent autoEvent = new AutoResetEvent(false);
            //TimerCallback timerDelegate = new TimerCallback(m => PriceFetchTimerElapsed());

            // Create a timer that signals the delegate to invoke 
            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            DateTime nextRun = today.AddHours(this.HourToRun).AddDays(now.Hour >= this.HourToRun ? 1 : 0);
            this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info, string.Format("Started Position Reset Timer. Next run time: {0}", nextRun)));

            this.priceFetchTimer = new Timer(PriceFetchTimerElapsed, autoEvent, nextRun - DateTime.Now, TimeSpan.FromHours(24));
        }
        
        public bool StopServer()
        {
            emailQueue.StopProcessingQueue();

            this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info, "Server Stopped."));
            this.logQueue.StopProcessingQueue();
            return true;
        }

        #endregion

        #region Actions

        public void PriceFetchTimerElapsed(object sender)
        {
            if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
                return;

            try
            {
                PopulateHistoricPrices();

                StringBuilder resultStringBuilder = new StringBuilder();

                foreach (Stock stock in this.StockCodesList)
                {
                    string resultString = null;

                    if (!stock.Silenced)
                        GetPricesAndEvaluate(stock, out resultString);
                    else
                    {
                        // If it is silenced, has it been 30 days?
                        if (stock.LastAlertTime.GetValueOrDefault() < DateTime.Now.AddDays(-30))
                        {
                            GetPricesAndEvaluate(stock, out resultString);

                            // Now reset so it is checked again tomorrow.
                            ResetStockSilenceFlags(stock);
                        }
                    }

                    if (!string.IsNullOrEmpty(resultString))
                    {
                        resultStringBuilder.AppendLine(resultString);
                        SilenceStock(stock);
                    }
                }

                QueueEmail(this.EmailRecipient, "DAILY STOCK DIGEST", resultStringBuilder.ToString());
            }
            catch (Exception e)
            {
                this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Error, string.Format("Error in PriceAnalysisTimerElapsed: {0}", e)));
                QueueEmail(this.EmailRecipient, "System Error", e.ToString());
            }
        }

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

        public void SendStartedEmail()
        {
            if (!string.IsNullOrEmpty(this.EmailRecipient))
            {
                try
                {
                    QueueEmail(this.EmailRecipient, "Stock Bandit Server Started",
                            string.Format("Stock Bandit server started successfully at {0} on version {1}", DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy"), Assembly.GetExecutingAssembly().GetName().Version.ToString()));
                }
                catch (Exception e)
                {
                    this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Error, string.Format("Exception in StockServer.SendStartedEmail - Exception: {0}", e.ToString())));
                }
            }
        }

        public void QueueEmail(string recipient, string subject, string body)
        {
            if (this.emailQueue != null)
            {
                this.emailQueue.QueueEmail(recipient, subject, body);
            }
        }

        #endregion

        #region PrivateHelpders
                
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
                        lastRetrieveDate = DateTime.Now.AddDays(-35);

                    Task updateTask = Task.Factory.StartNew(() =>
                    {
                       GetPricesForStock(stock, lastRetrieveDate);
                    }, TaskCreationOptions.LongRunning);
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
                QueueEmail(this.EmailRecipient, "System Error", e.ToString());
            }

            this.logQueue.QueueLogEntry(new LogEntry(DateTime.Now, LogType.Info, "Finished Collecting Historic Prices"));
        }

        private void GetPricesForStock(Stock stock, DateTime lastRetrieveDate)
        {
            List<DailyPrice> historicPrices = this.yahooHistoricWebStockEngine.Fetch(stock,
                lastRetrieveDate);

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

        #endregion

        #region CommandActions

        public string SayHello()
        {
            return "Hello, I'm the Stock Bandit service and I'm running.";
        }

        public List<string> GetLastPrices()
        {
            using (var dataContext = new StockBanditDataContext())
            {
                return (from item in dataContext.Stocks from price in item.DailyPrices.OrderByDescending(p => p.Date).Take(1).ToList() select string.Format("{0} - {1}: {2}p", item.StockCode, price.Date.ToShortDateString(), price.Close)).ToList();
            }
        }

        public List<string> GetLastPriceHistories()
        {
            using (var dataContext = new StockBanditDataContext())
            {
                return (from item in dataContext.Stocks from price in item.DailyPrices.OrderByDescending(p => p.Date).Take(10).ToList() select string.Format("{0} - {1}: {2}p", item.StockCode, price.Date.ToShortDateString(), price.Close)).ToList();
            }
        }

        #endregion

        #region StockAdministration

        public void AddStock(string stockCode, string stockName)
        {
            using (StockBanditDataContext dataContext = new StockBanditDataContext())
            {
                if (string.IsNullOrEmpty(stockCode))
                    throw new ApplicationException("StockCode cannot be null");
                if (string.IsNullOrEmpty(stockName))
                    throw new ApplicationException("StockName cannot be null");
                var newStock = new Stock() {StockCode = stockCode, StockName = stockName};
                dataContext.Stocks.InsertOnSubmit(newStock);
                dataContext.SubmitChanges();

                // Now repopulate the StockCodesList
                lock (semaphore)
                {
                    this.StockCodesList = new ObservableCollection<Stock>();
                    foreach (var stock in dataContext.Stocks)
                        this.StockCodesList.Add(stock);

                    PopulateHistoricPrices();
                }
            }
        }

        public void DeleteStock(string stockCode)
        {
            using (StockBanditDataContext dataContext = new StockBanditDataContext())
            {
                if (string.IsNullOrEmpty(stockCode))
                    throw new ApplicationException("StockCode cannot be null");
                Stock deletedStock = dataContext.Stocks.FirstOrDefault(s => s.StockCode.ToUpper() == stockCode.ToUpper());
                if (deletedStock != null)
                {
                    lock (semaphore)
                    {
                        dataContext.DailyPrices.DeleteAllOnSubmit(
                            dataContext.DailyPrices.Where(p => p.StockCode == deletedStock.StockCode));
                        dataContext.Stocks.DeleteOnSubmit(deletedStock);
                        dataContext.SubmitChanges();

                        // Now repopulate the StockCodesList
                        this.StockCodesList = new ObservableCollection<Stock>();
                        foreach (var stock in dataContext.Stocks)
                            this.StockCodesList.Add(stock);
                    }
                }
            }
        }

        #endregion
    }
}
