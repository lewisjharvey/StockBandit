using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net;
using System.Reflection;
using StockBandit.Server;

namespace StockBandit.Console
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            System.Console.Title = "Stock Bandit Console";

            log.Info("*********** Stock Bandit Server Console Started ***********");
            log.InfoFormat("Service Version: {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString(4));
            log.InfoFormat("Running on {0} ({1})", System.Environment.MachineName, System.Environment.OSVersion.VersionString);

            string cmd = "";

            // Create controller for instantiating the server
            ConfigurationController configurationController = new ConfigurationController();
            StockServer server = configurationController.SetupServer(log);
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
                log.Info("**************** Server Started ********************");

                // Send an email for compliance sites
                server.SendStartedEmail();

                cmd = System.Console.ReadLine();
                while (cmd.ToUpper() != "QUIT")
                {
                    switch (cmd.ToUpper())
                    {
                        case "HELLO":
                            System.Console.WriteLine(server.SayHello());
                            break;
                        case "FORCEPRICES":
                            server.PriceFetchTimerElapsed(null);
                            foreach (string priceOutput in server.GetLastPrices())
                                System.Console.WriteLine(priceOutput);
                            break;
                        case "LISTPRICES":
                            foreach (string priceOutput in server.GetLastPrices())
                                System.Console.WriteLine(priceOutput);
                            break;
                        case "LISTPRICEHISTORIES":
                            foreach (string priceOutput in server.GetLastPriceHistories())
                                System.Console.WriteLine(priceOutput);
                            break;
                        case "ADDSTOCK":
                            System.Console.WriteLine("Enter Stock Code: ");
                            string stockCode = System.Console.ReadLine();
                            System.Console.WriteLine("Enter Stock Name: ");
                            string stockName = System.Console.ReadLine();
                            server.AddStock(stockCode, stockName);
                            System.Console.WriteLine("Success");
                            break;
                        default:
                            System.Console.WriteLine(string.Format("Unrecognised command - {0}", cmd.ToUpper()));
                            break;
                    }

                    cmd = System.Console.ReadLine();
                }

                log.Info("**************** Stopping Server ********************");
                server.StopServer();
                log.Info("**************** Server Stopped ********************");
            }
            else
            {
                System.Console.WriteLine("Server failed to start - press ENTER to close");
                System.Console.ReadLine();
            }
        }
    }
}
