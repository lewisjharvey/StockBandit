#region © Copyright
// <copyright file="Program.cs" company="Lewis Harvey">
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
using System.Reflection;
using System.Text;

using log4net;
using StockBandit.Server;

namespace StockBandit.Console
{
    /// <summary>
    /// Encapsulates the main program for the application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The log used to start the logging engine.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The main entry point for the application
        /// </summary>
        /// <param name="args">Arguments passed at start up</param>
        private static void Main(string[] args)
        {
            System.Console.Title = "Stock Bandit Console";

            Log.Info("*********** Stock Bandit Server Console Started ***********");
            Log.InfoFormat("Service Version: {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString(4));
            Log.InfoFormat("Running on {0} ({1})", System.Environment.MachineName, System.Environment.OSVersion.VersionString);

            string cmd = string.Empty;

            // Create controller for instantiating the server
            ConfigurationController configurationController = new ConfigurationController();
            StockServer server = configurationController.SetupServer(new LogQueue(1000));
            if (server == null)
            {
                System.Console.WriteLine("********** Console cannot be started due to these errors: ************");
                foreach (string errorMessage in configurationController.ErrorMessages)
                    System.Console.WriteLine("{0} - {1}", DateTime.Now.ToString("HH:mm:ss.fff"), errorMessage);
                System.Console.ReadLine();
                return;
            }

            if (server.StartServer())
            {
                // Server has started successfully
                Log.Info("**************** Server Started ********************");

                // Send an email for compliance sites
                server.SendStartedEmail();

                cmd = System.Console.ReadLine();
                while (cmd.ToUpper() != "QUIT")
                {
                    switch (cmd.ToUpper())
                    {
                        case "EVALUATE":
                            server.PriceFetchTimerElapsed(null);
                            break;
                        default:
                            System.Console.WriteLine(string.Format("Unrecognised command - {0}", cmd.ToUpper()));
                            break;
                    }

                    cmd = System.Console.ReadLine();
                }

                Log.Info("**************** Stopping Server ********************");
                server.StopServer();
                Log.Info("**************** Server Stopped ********************");
            }
            else
            {
                System.Console.WriteLine("Server failed to start - press ENTER to close");
                System.Console.ReadLine();
            }
        }
    }
}
