#region © Copyright
//
// © Copyright 2012 Tradelogic Ltd
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using StockBandit.Model;
using System.Xml.Linq;
using HtmlAgilityPack;
using System.Net;

namespace StockBandit.Server
{
    public class GoogleWebStockEngine : StockEngine
    {
        private const string BASE_URL = "https://www.google.co.uk/finance?q={0}";

        public void Fetch(ObservableCollection<Quote> quotes)
        {
            foreach (Quote quote in quotes)
            {
                string url = string.Format(BASE_URL, quote.Symbol);
                
                // Read URL and parse
                HttpWebRequest oReq = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse resp = (HttpWebResponse)oReq.GetResponse();

                if (resp.ContentType.StartsWith("text/html", StringComparison.InvariantCultureIgnoreCase))
                {
                    HtmlDocument doc = new HtmlDocument();
                    var resultStream = resp.GetResponseStream();
                    doc.Load(resultStream);

                    string priceString = "";
                    string volumeString = "";
                    HtmlNode htmlNode = doc.DocumentNode.SelectSingleNode("//div[@id='price-panel']//span[@class='pr']//span");
                    if (htmlNode == null)
                    {
                        HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//div[@id='price-panel']//span[@class='pr']//span//span");
                        foreach (HtmlNode node in nodes)
                            priceString += node.InnerText;
                    }
                    else
                        priceString = htmlNode.InnerText;
                   
                    if(string.IsNullOrEmpty(priceString))
                        throw new ApplicationException("The node cannot be found for the price.");

                    HtmlNodeCollection additionalDateHtmlNodes = doc.DocumentNode.SelectNodes("//div[@class='snap-panel-and-plusone']//div[@class='snap-panel']//table//tr");
                    foreach(HtmlNode node in additionalDateHtmlNodes)
                    {
                        if(node.HasChildNodes && node.ChildNodes[1].Attributes["data-snapfield"].Value == "vol_and_avg")
                        {
                            volumeString = node.ChildNodes[3].InnerText;
                            break;
                        }
                    }

                    decimal price = -1;
                    if (decimal.TryParse(priceString, out price))
                    {
                        quote.LastTradePrice = price;
                        quote.LastUpdate = DateTime.Now;
                    }

                    string[] volumeParts = volumeString.Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    quote.CurrentVolume = (int)double.Parse(volumeParts[0]);
                    quote.AverageVolume = (int)double.Parse(volumeParts[1]);
                }
            }
        }
    }
}
