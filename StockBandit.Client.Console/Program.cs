using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace StockBandit.Client.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.Title = "Stock Bandit Console";

            System.Console.WriteLine("*********** Stock Bandit Sevice Console ***********");
            System.Console.WriteLine("Service Version: {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString(4));
            System.Console.WriteLine("Running on {0} ({1})", System.Environment.MachineName, System.Environment.OSVersion.VersionString);

            System.Console.WriteLine("Looking for BanditService running...");
            BanditService banditService = new BanditService();
            System.Console.WriteLine("BanditService found.");

            System.Console.WriteLine("Type \"HELP\" or enter a command:");
            string cmd = System.Console.ReadLine();
            while (cmd.ToUpper() != "QUIT")
            {
                switch (cmd.ToUpper())
                {
                    case "HELP":
                        WriteHelpInfo();
                        break;
                    case "HELLO":
                        System.Console.WriteLine(banditService.SayHello());
                        break;
                    case "EXIT":
                        Environment.Exit(0);
                        break;
                    case "FORCEPRICES":
                        banditService.ForcePrices();
                        foreach (string priceOutput in banditService.GetLastPrices())
                            System.Console.WriteLine(priceOutput);
                        break;
                    case "LISTPRICES":
                        foreach (string priceOutput in banditService.GetLastPrices())
                            System.Console.WriteLine(priceOutput);
                        break;
                    case "LISTPRICEHISTORIES":
                        foreach (string priceOutput in banditService.GetLastPriceHistories())
                            System.Console.WriteLine(priceOutput);
                        break;
                    case "ADDSTOCK":
                        System.Console.WriteLine("Enter Stock Code: ");
                        string addStockCode = System.Console.ReadLine();
                        System.Console.WriteLine("Enter Stock Name: ");
                        string addStockName = System.Console.ReadLine();
                        banditService.AddStock(addStockCode, addStockName);
                        break;
                    case "DELETESTOCK":
                        System.Console.WriteLine("Enter Stock Code: ");
                        string deleteStockCode = System.Console.ReadLine();
                        banditService.DeleteStock(deleteStockCode);
                        break;
                    default:
                        System.Console.WriteLine(string.Format("Unrecognised command - {0}", cmd.ToUpper()));
                        break;
                }

                System.Console.WriteLine("Type \"HELP\" or enter a command:");
                cmd = System.Console.ReadLine();
            }
        }

        private static void WriteHelpInfo()
        {
            System.Console.WriteLine("Commands");
            System.Console.WriteLine("========");
            System.Console.WriteLine("HELP: Show this help information.");
            System.Console.WriteLine("HELLO: Say hello to the service to check it is running.");
            System.Console.WriteLine("EXIT: Close the administration console down.");
            System.Console.WriteLine("FORCEPRICES: Force a collection of the stock prices.");
            System.Console.WriteLine("LISTPRICES: List the current price the system holds for each stock.");
            System.Console.WriteLine("LISTPRICEHISTORIES: List the close prices for the the number of days in the BandPeriod.");
            System.Console.WriteLine("ADDSTOCK: Add a new stock to the system for monitoring.");
            System.Console.WriteLine("DELETESTOCK: Delete a stock from the system.");
        }
    }
}
