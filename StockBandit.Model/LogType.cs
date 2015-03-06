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
