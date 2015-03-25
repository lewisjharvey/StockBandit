#region © Copyright
// <copyright file="ProjectInstaller.cs" company="Lewis Harvey">
//      Copyright (c) Lewis Harvey. All rights reserved.
//      This software is provided "as is" without warranty of any kind, express or implied, 
//      including but not limited to warranties of merchantability and fitness for a particular 
//      purpose. The authors do not support the Software, nor do they warrant
//      that the Software will meet your requirements or that the operation of the Software will
//      be uninterrupted or error free or that any defects will be corrected.
// </copyright>
#endregion

using System.ComponentModel;

namespace StockBandit.Service
{
    /// <summary>
    /// The installer for the service project
    /// </summary>
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="ProjectInstaller" /> class.
        /// </summary>
        public ProjectInstaller()
        {
            this.InitializeComponent();
        }
    }
}
