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
using System.Threading.Tasks;
using StockBandit.Server.Analysis;

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
        public bool EnableBollingerBands { get; set; }
        public bool EnableMACD { get; set; }
        public bool EnableVolume { get; set; }
        public double AlertThreshold { get; set; }

        #endregion

        public ObservableCollection<Quote> StockCodesList { get; set; }

        private EmailQueue emailQueue;
        private Timer priceFetchTimer;
        //private Dictionary<string, List<DailyPrice>> historicPrices;
        private object semaphore = new object();
        private ServiceHost serviceHost;
        private GoogleWebStockEngine googleWebStockEngine;
        private GoogleHistoricWebStockEngine googleHistoricWebStockEngine;
        private List<IModel> registeredModels;

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
                // Setup google for querying
                this.googleWebStockEngine = new GoogleWebStockEngine();
                this.googleHistoricWebStockEngine = new GoogleHistoricWebStockEngine();

                using (StockBanditDataContext dataContext = new StockBanditDataContext())
                {
                    foreach (string stockCode in dataContext.Stocks.Select(p => p.StockCode).ToList())
                    {
                        this.StockCodesList.Add(new Quote(stockCode));
                    }
                }

                // Get all the historical data and populate
                PopulateHistoricPrices();
                RegisterModels();

                StartPriceFetchTimer();
            }
            catch (Exception ex)
            {
                log.FatalFormat("Error starting server - {0}", ex);
                return false;
            }
            return true;
        }

        private void RegisterModels()
        {
            this.registeredModels = new List<IModel>();
            if(this.EnableBollingerBands)
                this.registeredModels.Add(new BollingerBandsModel(this.BandPeriod));
            if(this.EnableMACD)
                this.registeredModels.Add(new MovingAverageConvergenceDivergenceModel());
            if (this.EnableVolume)
                this.registeredModels.Add(new VolumeModel(this.AlertThreshold));
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
                // Get the google current price
                lock (semaphore)
                {
                    // Get the latest prices
                    this.googleWebStockEngine.Fetch(this.StockCodesList);
                }

                // If different add to queue.
                foreach (Quote stock in this.StockCodesList)
                {
                    DailyPrice currentPrice = InsertOrUpdateTodayPrice(stock);

                    // Pass 5 years worth of data to the models, this should be enough for any model currently.
                    using (StockBanditDataContext dataContext = new StockBanditDataContext())
                    {
                        List<DailyPrice> historicPrices =
                            dataContext.DailyPrices.Where(
                                p =>
                                    p.StockCode == stock.Symbol &&
                                    p.Date > DateTime.Now.Date.Subtract(new TimeSpan(1825, 0, 0, 0, 0)) &&
                                    p.Date < DateTime.Now.Date).OrderByDescending(p => p.Date).ToList();

                        foreach (IModel model in this.registeredModels)
                        {
                            string emailBody = null;
                            string emailSubject = null;
                            if (model.Evaluate(stock,
                                historicPrices.ConvertAll<ClosingPrice>(
                                    p => new ClosingPrice() {Date = p.Date, Price = p.Price, Volume = p.Volume}),
                                currentPrice.Price, out emailBody, out emailSubject))
                                QueueEmail(this.EmailRecipient, emailSubject, emailBody);
                        }
                    }
                }
            }
            catch (System.Net.WebException)
            {
                log.Error("No internet access - unable to get prices.");
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
        
        private DailyPrice InsertOrUpdateTodayPrice(Quote stock)
        {
            // Find price for today
            using (StockBanditDataContext dataContext = new StockBanditDataContext())
            {
                DailyPrice todayPrice =
                    dataContext.DailyPrices.FirstOrDefault(
                        p => p.Date == DateTime.Now.Date && p.StockCode == stock.Symbol);
                if (todayPrice != null)
                {
                    todayPrice.Price = stock.LastTradePrice.Value;
                    todayPrice.Volume = stock.CurrentVolume.Value;
                }
                else
                {
                    DailyPrice dailyPrice = new DailyPrice()
                    {
                        Date = DateTime.Now.Date,
                        Price = stock.LastTradePrice.Value,
                        Volume = stock.CurrentVolume.Value
                    };
                    dailyPrice.StockCode = stock.Symbol;
                    dataContext.DailyPrices.InsertOnSubmit(dailyPrice);
                    todayPrice = dailyPrice;
                }

                dataContext.SubmitChanges();

                return todayPrice;
            }
        }

        private void PopulateHistoricPrices()
        {
            try
            {
                List<Task> updateTasks = new List<Task>();

                // Get the latest prices
                foreach (Quote quote in this.StockCodesList)
                {
                    log.InfoFormat("Collecting Historic Prices for {0}", quote.Symbol);
                    
                    DateTime lastRetrieveDate = DateTime.MinValue;
                    using (StockBanditDataContext dataContext = new StockBanditDataContext())
                    {
                        // Get the historic prices
                        dataContext.DailyPrices.Where(p => p.StockCode == quote.Symbol && p.Date < DateTime.Today)
                            .OrderByDescending(p => p.Date)
                            .Select(p => p.Date)
                            .FirstOrDefault();
                    }

                    Task updateTask = Task.Factory.StartNew(() =>
                    {
                       GetPricesForStock(quote, lastRetrieveDate);
                    }, TaskCreationOptions.LongRunning);
                    updateTasks.Add(updateTask);

                    log.InfoFormat("Collected Historic Prices for {0}", quote.Symbol);
                }

                Task.WaitAll(updateTasks.ToArray());
            }
            catch (System.Net.WebException)
            {
                log.Error("No internet access - unable to get prices.");
            }
            catch (Exception e)
            {
                log.Error("Error in PopulateHistoricPrices", e);
                QueueEmail(this.EmailRecipient, "System Error", e.ToString());
            }

            log.Info("Finished Collecting Historic Prices");
        }

        private void GetPricesForStock(Quote quote, DateTime lastRetrieveDate)
        {
            List<DailyPrice> historicPrices = this.googleHistoricWebStockEngine.Fetch(quote,
                lastRetrieveDate);

            using (StockBanditDataContext dataContext = new StockBanditDataContext())
            {
                // If different add to queue.
                foreach (DailyPrice dailyPrice in historicPrices)
                {
                    // Find price for today
                    DailyPrice existingPrice =
                        dataContext.DailyPrices.FirstOrDefault(
                            p => p.Date == dailyPrice.Date && p.StockCode == quote.Symbol);
                    if (existingPrice != null)
                    {
                        existingPrice.Price = dailyPrice.Price;
                        existingPrice.Volume = dailyPrice.Volume;
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
            using (StockBanditDataContext dataContext = new StockBanditDataContext())
            {
                List<string> lastPrices = new List<string>();
                foreach (var item in dataContext.Stocks)
                {
                    foreach (var price in item.DailyPrices.OrderByDescending(p => p.Date).Take(1).ToList())
                        lastPrices.Add(string.Format("{0} - {1}: {2}p", item.StockCode, price.Date.ToShortDateString(),
                            price.Price));
                }
                return lastPrices;
            }
        }

        public List<string> GetLastPriceHistories()
        {
            using (StockBanditDataContext dataContext = new StockBanditDataContext())
            {
                List<string> lastPriceHistories = new List<string>();
                foreach (var item in dataContext.Stocks)
                {
                    foreach (var price in item.DailyPrices.OrderByDescending(p => p.Date).Take(this.BandPeriod).ToList()
                        )
                        lastPriceHistories.Add(string.Format("{0} - {1}: {2}p", item.StockCode,
                            price.Date.ToShortDateString(), price.Price));
                }
                return lastPriceHistories;
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
                Stock stock = new Stock() {StockCode = stockCode, StockName = stockName};
                dataContext.Stocks.InsertOnSubmit(stock);
                dataContext.SubmitChanges();

                // Now repopulate the StockCodesList
                lock (semaphore)
                {
                    this.StockCodesList = new ObservableCollection<Quote>();
                    foreach (string code in dataContext.Stocks.Select(p => p.StockCode).ToList())
                        this.StockCodesList.Add(new Quote(code));

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
                Stock stock = dataContext.Stocks.FirstOrDefault(s => s.StockCode.ToUpper() == stockCode.ToUpper());
                if (stock != null)
                {
                    lock (semaphore)
                    {
                        dataContext.DailyPrices.DeleteAllOnSubmit(
                            dataContext.DailyPrices.Where(p => p.StockCode == stock.StockCode));
                        dataContext.Stocks.DeleteOnSubmit(stock);
                        dataContext.SubmitChanges();

                        // Now repopulate the StockCodesList
                        this.StockCodesList = new ObservableCollection<Quote>();
                        foreach (string code in dataContext.Stocks.Select(p => p.StockCode).ToList())
                            this.StockCodesList.Add(new Quote(code));
                    }
                }
            }
        }

        #endregion
    }
}
