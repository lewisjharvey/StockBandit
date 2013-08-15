using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using log4net;
using System.Reflection;
using StockBandit.Server;
using System.Threading;

namespace StockBandit.Service
{
    public partial class BanditService : ServiceBase
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private StockServer stockServer;
        private ConfigurationController configurationController;
        private Thread mainThread;

        public BanditService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            this.configurationController = new ConfigurationController();
            this.stockServer = null;

            ThreadStart mainThreadStart = new ThreadStart(DoStart);
            mainThread = new Thread(mainThreadStart);
            mainThread.Start();
            mainThread.Join();
        }

        private void DoStart()
        {

            try
            {
                //get some properties and start the log file
                WriteLogHeader();

                this.stockServer = this.configurationController.SetupServer(log);
                if (this.stockServer == null)
                {
                    this.Stop();
                    return;
                }

                log.Info("Attempting to start server");
                if (!stockServer.StartServer())
                {
                    log.Error("Server failed to start");
                    this.Stop();
                    return;
                }
                //server is now running.
                log.Info("**************** Server Started ********************");

                // Send an email
                stockServer.SendStartedEmail();
            }
            catch (Exception ex)
            {
                log.Error("Error occurred in service", ex);
                this.Stop();
                return;
            }
        }

        private void WriteLogHeader()
        {
            log.Info("*********** Stock Bandit Server Service Started ***********");
            log.InfoFormat("Service Version: {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString(4));
            log.InfoFormat("Running on {0} ({1})", System.Environment.MachineName, System.Environment.OSVersion.VersionString);
        }

        private void WriteLogFooter()
        {
            log.Info("*********** Stock Bandit Server Service Stopped ***********");
        }

        protected override void OnStop()
        {
            try
            {
                log.Info("Attempting to stop server");
                stockServer.StopServer();
                WriteLogFooter();
            }
            catch (Exception ex)
            {
                log.Error("Error occurred in service", ex);
            }
        }

        protected override void OnShutdown()
        {
            this.Stop();
        }
    }
}
