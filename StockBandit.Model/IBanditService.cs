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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace StockBandit.Model
{
    [ServiceContract]
    public interface IBanditService
    {
        [OperationContract]
        string SayHello();

        [OperationContract]
        List<string> GetLastPrices();

        [OperationContract]
        List<string> GetLastPriceHistories();

        [OperationContract]
        void AddStock(string stockCode, string stockName);

        [OperationContract]
        void ForcePrices();

        [OperationContract]
        void DeleteStock(string stockCode);
    }
}
