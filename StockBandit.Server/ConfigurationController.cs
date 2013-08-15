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
using log4net;
using System.Configuration;

namespace StockBandit.Server
{
    public class ConfigurationController
    {
        #region Attributes

        // Error properties
        public List<string> ErrorMessages { get; private set; }

        private string databaseConnectionString;
        private string emailServer;
        private string emailFromAddress;
        private string emailUsername;
        private string emailPassword;
        private int? emailPort;
        private bool? emailSSL;
        private string emailRecipient;
        private int? bandPeriod;
        private int? priceCheckMinutes;

        #endregion

        public ConfigurationController()
        {
            ErrorMessages = new List<string>();

            // Load the configuration settings from the config file
            // Only loads settings that exist, these will be validated later.
            LoadConfigurationSettings();
        }

        /// <summary>
        /// Configures the server validating all configuration.
        /// If server cannot be configured, all errors are written to ILog
        /// provided and the method returns null.
        /// </summary>
        public StockServer SetupServer(ILog log)
        {
            // Validate all configuration settings
            if (!ValidateConfiguration())
            {
                log.Error(ErrorMessages);
                return null;
            }

            // Now instantiate the correct site type based on the configuration.
            StockServer server = new StockServer();

            server.DatabaseConnectionString = this.databaseConnectionString;
            server.EmailServer = this.emailServer;
            server.EmailFromAddress = this.emailFromAddress;
            server.EmailUsername = this.emailUsername;
            server.EmailPassword = this.emailPassword;
            server.EmailPort = this.emailPort.Value;
            server.EmailSSL = this.emailSSL.Value;
            server.EmailRecipient = this.emailRecipient;
            server.BandPeriod = this.bandPeriod.Value;
            server.PriceCheckMinutes = this.priceCheckMinutes.Value;

            return server;
        }

        private void LoadConfigurationSettings()
        {
            this.databaseConnectionString = ConfigurationManager.ConnectionStrings["StockBandit.Server.Properties.Settings.StockBanditConnectionString"].ConnectionString;

            LoadConfigurationSetting("EmailServer", out emailServer);
            LoadConfigurationSetting("EmailFromAddress", out emailFromAddress);
            LoadConfigurationSetting("EmailUsername", out emailUsername);
            LoadConfigurationSetting("EmailPassword", out emailPassword);
            LoadConfigurationSetting("EmailPort", out emailPort);
            LoadConfigurationSetting("EmailSSL", out emailSSL);
            LoadConfigurationSetting("EmailRecipient", out emailRecipient);
            LoadConfigurationSetting("BandPeriod", out bandPeriod);
            LoadConfigurationSetting("PriceCheckMinutes", out priceCheckMinutes);
        }

        #region Configuration setting loaders

        private void LoadConfigurationSetting(string settingName, out string value)
        {
            if (ConfigurationManager.AppSettings[settingName] != null)
                value = ConfigurationManager.AppSettings[settingName];
            else
                value = "";
        }

        private void LoadConfigurationSetting(string settingName, out bool? value)
        {
            if (ConfigurationManager.AppSettings[settingName] != null)
                value = bool.Parse(ConfigurationManager.AppSettings[settingName]);
            else
                value = null;
        }

        private void LoadConfigurationSetting(string settingName, out int? value)
        {
            if (ConfigurationManager.AppSettings[settingName] != null)
                value = int.Parse(ConfigurationManager.AppSettings[settingName]);
            else
                value = null;
        }

        private void LoadConfigurationSetting(string settingName, out decimal? value)
        {
            if (ConfigurationManager.AppSettings[settingName] != null)
                value = decimal.Parse(ConfigurationManager.AppSettings[settingName]);
            else
                value = null;
        }

        private void LoadConfigurationSetting(string settingName, out Guid? value)
        {
            if (ConfigurationManager.AppSettings[settingName] != null)
                value = new Guid(ConfigurationManager.AppSettings[settingName]);
            else
                value = null;
        }

        #endregion

        private bool ValidateConfiguration()
        {
            // A list of messages to send back to caller
            List<string> errorMessages = new List<string>();

            // Firstly required for all types of server.
            if (string.IsNullOrEmpty(this.databaseConnectionString))
                errorMessages.Add("The connection string for the database is not set.");
            if (string.IsNullOrEmpty(this.emailServer))
                errorMessages.Add("The EmailServer configuration setting is not set.");
            if (string.IsNullOrEmpty(this.emailFromAddress))
                errorMessages.Add("The EmailFromAddress configuration setting is not set.");
            if (string.IsNullOrEmpty(this.emailUsername))
                errorMessages.Add("The EmailUsername configuration setting is not set.");
            if (string.IsNullOrEmpty(this.emailPassword))
                errorMessages.Add("The EmailPassword configuration setting is not set.");
            if (!this.emailPort.HasValue)
                errorMessages.Add("The EmailPort configuration setting is not set.");
            if (!this.emailSSL.HasValue)
                errorMessages.Add("The EmailSSL configuration setting is not set.");
            if (string.IsNullOrEmpty(this.emailRecipient))
                errorMessages.Add("The EmailRecipient configuration setting is not set.");
            if (!this.bandPeriod.HasValue)
                errorMessages.Add("The BandPeriod configuration setting is not set.");
            if (!this.priceCheckMinutes.HasValue)
                errorMessages.Add("The PriceCheckMinutes configuration setting is not set.");

            this.ErrorMessages.AddRange(errorMessages);

            return (errorMessages.Count == 0);
        }
    }
}