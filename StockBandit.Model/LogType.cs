#region © Copyright
// <copyright file="LogType.cs" company="Lewis Harvey">
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
using System.Text;

namespace StockBandit.Model
{
    /// <summary>
    /// The logging types used to log within the system.
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// The debug log.
        /// </summary>
        Debug,

        /// <summary>
        /// The error log.
        /// </summary>
        Error,

        /// <summary>
        /// The fatal log.
        /// </summary>
        Fatal,

        /// <summary>
        /// The info log.
        /// </summary>
        Info,

        /// <summary>
        /// A simple warning log.
        /// </summary>
        Warn
    }
}
