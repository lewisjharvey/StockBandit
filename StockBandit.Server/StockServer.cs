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
using System.Threading;
using System.Linq;
using System.ServiceModel;

namespace StockBandit.Server
{
    public class StockServer
    {
        //log4net
        protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region ServerSettings

        public string DatabaseConnectionString { get; set; }
        public string EmailServer { get; set; }
        public string EmailFromAddress { get; set; }
        public string EmailUsername { get; set; }
        public string EmailPassword { get; set; }
        public int EmailPort { get; set; }
        public bool EmailSSL { get; set; }
        public string EmailRecipient { get; set; }
        public int BandPeriod { get; set; }
        public int PriceCheckMinutes { get; set; }

        #endregion

        public ObservableCollection<Quote> StockCodesList { get; set; }

        private EmailQueue emailQueue;
        private Timer priceFetchTimer;
        //private Dictionary<string, List<DailyPrice>> historicPrices;
        private object semaphore = new object();
        private ServiceHost serviceHost;
        private GoogleWebStockEngine googleWebStockEngine;
        private StockBanditDataContext dataContext;

        #region StartupProcedures

        public StockServer()
        {
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
                    log.Info("Email not configured.");

                // Convert the stocks to a list
                this.StockCodesList = new ObservableCollection<Quote>();
                // Setup database context
                this.dataContext = new StockBanditDataContext(this.DatabaseConnectionString);
                // Setup google for querying
                this.googleWebStockEngine = new GoogleWebStockEngine();

                foreach (string stockCode in this.dataContext.Stocks.Select(p => p.StockCode).ToList())
                {
                    this.StockCodesList.Add(new Quote(stockCode));
                }

                StartPriceFetchTimer();
            }
            catch (Exception ex)
            {
                log.FatalFormat("Error starting server - {0}", ex);
                return false;
            }
            return true;
        }

        private void StartPriceFetchTimer()
        {
            // Create a callback for the timer 
            AutoResetEvent autoEvent = new AutoResetEvent(false);
            TimerCallback timerDelegate = new TimerCallback(m => PriceFetchTimerElapsed());

            // Create a timer that signals the delegate to invoke 
            log.Info("Started Position Reset Timer");
            priceFetchTimer = new Timer(timerDelegate, autoEvent, 0, this.PriceCheckMinutes * 60 * 1000);
        }
        
        public bool StopServer()
        {
            emailQueue.StopProcessingQueue();
            return true;
        }

        #endregion

        #region Actions

        public void PriceFetchTimerElapsed()
        {
            if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
                return;

            try
            {
                lock (semaphore)
                {
                    // Get the latest prices
                    this.googleWebStockEngine.Fetch(this.StockCodesList);
                }

                // If different add to queue.
                foreach (Quote stock in this.StockCodesList)
                {
                    // Find price for today
                    DailyPrice todayPrice = this.dataContext.DailyPrices.FirstOrDefault(p => p.Date == DateTime.Now.Date && p.StockCode == stock.Symbol);
                    if (todayPrice != null)
                    {
                        // We have a price from the standard price checker. Check the value ot see if different and update if it is.
                        if (todayPrice.Price != stock.LastTradePrice && stock.LastTradePrice.HasValue)
                        {
                            todayPrice.Price = stock.LastTradePrice.Value;
                        }
                    }
                    else
                    {
                        DailyPrice dailyPrice = new DailyPrice() { Date = DateTime.Now.Date, Price = stock.LastTradePrice.Value };
                        dailyPrice.StockCode = stock.Symbol;
                        this.dataContext.DailyPrices.InsertOnSubmit(dailyPrice);
                    }

                    this.dataContext.SubmitChanges();

                    // Now we have a valid list of items, work out bands
                    List<DailyPrice> dailyPrices = this.dataContext.DailyPrices.Where(p => p.StockCode == stock.Symbol).OrderByDescending(p => p.Date).Take(this.BandPeriod).ToList();
                    if (dailyPrices.Count >= this.BandPeriod)
                    {
                        decimal middleBand = dailyPrices.Average(m => m.Price);
                        decimal standardDeviation = CalculateStdDev(dailyPrices);
                        decimal upperBand = middleBand + (standardDeviation * 2);
                        decimal lowerBand = middleBand - (standardDeviation * 2);

                        if (stock.LastTradePrice >= upperBand)
                        {
                            string body = string.Format("POSSIBLE SELL ACTION ({0})\r\n\r\nCurrent Price: {1}\r\nUpper Band: {2}", stock.Symbol, stock.LastTradePrice, upperBand);
                            QueueEmail(this.EmailRecipient, string.Format("POSSIBLE SELL ACTION ({0})", stock.Symbol), body);
                        }

                        if (stock.LastTradePrice <= lowerBand)
                        {
                            string body = string.Format("POSSIBLE BUY ACTION ({0})\r\n\r\nCurrent Price: {1}\r\nLower Band: {2}", stock.Symbol, stock.LastTradePrice, lowerBand);
                            QueueEmail(this.EmailRecipient, string.Format("POSSIBLE BUY ACTION ({0})", stock.Symbol), body);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("Error in PriceAnalysisTimerElapsed", e);
                QueueEmail(this.EmailRecipient, "System Error", e.ToString());
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
                    log.ErrorFormat("Exception in StockServer.SendStartedEmail - Exception: {0}", e.ToString());
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

        private decimal CalculateStdDev(List<DailyPrice> values)
        {
            double M = 0.0;
            double S = 0.0;
            int k = 1;
            foreach (DailyPrice value in values)
            {
                double tmpM = M;
                M += ((double)value.Price - tmpM) / k;
                S += ((double)value.Price - tmpM) * ((double)value.Price - M);
                k++;
            }
            decimal ret2 = (decimal)Math.Sqrt(S / (k - 1));
            return ret2;
        }

        #endregion

        #region CommandActions

        public string SayHello()
        {
            return "Hello, I'm the Stock Bandit service and I'm running.";
        }

        public List<string> GetLastPrices()
        {
            List<string> lastPrices = new List<string>();
            foreach (Quote quote in this.StockCodesList)
                lastPrices.Add(string.Format("{0} - {1}p", quote.Symbol, quote.LastTradePrice));
            return lastPrices;
        }

        public List<string> GetLastPriceHistories()
        {
            List<string> lastPriceHistories = new List<string>();
            foreach (var item in this.dataContext.Stocks)
            {
                foreach (var price in item.DailyPrices.OrderByDescending(p => p.Date).Take(this.BandPeriod).ToList())
                    lastPriceHistories.Add(string.Format("{0} - {1}: {2}p", item.StockCode, price.Date.ToShortDateString(), price.Price));
            }
            return lastPriceHistories;
        }

        #endregion

        #region StockAdministration

        public void AddStock(string stockCode, string stockName)
        {
            if(string.IsNullOrEmpty(stockCode))
                throw new ApplicationException("StockCode cannot be null");
            if(string.IsNullOrEmpty(stockName))
                throw new ApplicationException("StockName cannot be null");
            Stock stock = new Stock() { StockCode = stockCode, StockName = stockName };
            this.dataContext.Stocks.InsertOnSubmit(stock);
            this.dataContext.SubmitChanges();

            // Now repopulate the StockCodesList
            lock (semaphore)
            {
                this.StockCodesList = new ObservableCollection<Quote>();
                foreach (string code in this.dataContext.Stocks.Select(p => p.StockCode).ToList())
                    this.StockCodesList.Add(new Quote(code));
            }
        }

        public void DeleteStock(string stockCode)
        {
            if (string.IsNullOrEmpty(stockCode))
                throw new ApplicationException("StockCode cannot be null");
            Stock stock = this.dataContext.Stocks.FirstOrDefault(s => s.StockCode.ToUpper() == stockCode.ToUpper());
            if (stock != null)
            {
                this.dataContext.Stocks.DeleteOnSubmit(stock);
                this.dataContext.SubmitChanges();

                // Now repopulate the StockCodesList
                lock (semaphore)
                {
                    this.StockCodesList = new ObservableCollection<Quote>();
                    foreach (string code in this.dataContext.Stocks.Select(p => p.StockCode).ToList())
                        this.StockCodesList.Add(new Quote(code));
                }
            }
        }

        #endregion
    }
}
