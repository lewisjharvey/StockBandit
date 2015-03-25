#region © Copyright
// <copyright file="BanditService.cs" company="Lewis Harvey">
//      Copyright (c) Lewis Harvey. All rights reserved.
//      This software is provided "as is" without warranty of any kind, express or implied, 
//      including but not limited to warranties of merchantability and fitness for a particular 
//      purpose. The authors do not support the Software, nor do they warrant
//      that the Software will meet your requirements or that the operation of the Software will
//      be uninterrupted or error free or that any defects will be corrected.
// </copyright>
#endregion

using System;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;

using log4net;
using StockBandit.Server;

namespace StockBandit.Service
{
    /// <summary>
    /// The service class wrapper
    /// </summary>
    public partial class BanditService : ServiceBase
    {
        /// <summary>
        /// The logging engine for initialisation
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The server for all functions against the stocks
        /// </summary>
        private StockServer stockServer;

        /// <summary>
        /// The controller for loading configuration settings
        /// </summary>
        private ConfigurationController configurationController;

        /// <summary>
        /// The thread for running the application in.
        /// </summary>
        private Thread mainThread;

        /// <summary>
        /// Initialises a new instance of the <see cref="BanditService" /> class.
        /// </summary>
        public BanditService()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// The start method of the service
        /// </summary>
        /// <param name="args">Arguments passed at start up</param>
        protected override void OnStart(string[] args)
        {
            this.configurationController = new ConfigurationController();
            this.stockServer = null;

            ThreadStart mainThreadStart = new ThreadStart(this.DoStart);
            this.mainThread = new Thread(mainThreadStart);
            this.mainThread.Start();
            this.mainThread.Join();
        }

        /// <summary>
        /// The stop method of the service
        /// </summary>
        protected override void OnStop()
        {
            try
            {
                Log.Info("Attempting to stop server");
                this.stockServer.StopServer();
                this.WriteLogFooter();
            }
            catch (Exception ex)
            {
                Log.Error("Error occurred in service", ex);
            }
        }

        /// <summary>
        /// The shutdown method of the service
        /// </summary>
        protected override void OnShutdown()
        {
            this.Stop();
        }

        /// <summary>
        /// Initiates everything required for the service
        /// </summary>
        private void DoStart()
        {
            try
            {
                this.WriteLogHeader();

                this.stockServer = this.configurationController.SetupServer(new LogQueue(1000));
                if (this.stockServer == null)
                {
                    this.Stop();
                    return;
                }

                Log.Info("Attempting to start server");
                if (!this.stockServer.StartServer())
                {
                    Log.Error("Server failed to start");
                    this.Stop();
                    return;
                }

                // Server is now running.
                Log.Info("**************** Server Started ********************");

                // Send a started email
                this.stockServer.SendStartedEmail();
            }
            catch (Exception ex)
            {
                Log.Error("Error occurred in service", ex);
                this.Stop();
                return;
            }
        }

        /// <summary>
        /// Output the log header in the logging engine
        /// </summary>
        private void WriteLogHeader()
        {
            Log.Info("*********** Stock Bandit Server Service Started ***********");
            Log.InfoFormat("Service Version: {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString(4));
            Log.InfoFormat("Running on {0} ({1})", System.Environment.MachineName, System.Environment.OSVersion.VersionString);
        }

        /// <summary>
        /// Output the log footer in the logging engine
        /// </summary>
        private void WriteLogFooter()
        {
            Log.Info("*********** Stock Bandit Server Service Stopped ***********");
        }
    }
}
