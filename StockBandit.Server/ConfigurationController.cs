#region © Copyright
// <copyright file="ConfigurationController.cs" company="Lewis Harvey">
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
using System.Configuration;

using log4net;

namespace StockBandit.Server
{
    /// <summary>
    /// Provides configuration access for properties of the service.
    /// </summary>
    public class ConfigurationController
    {
        #region Attributes

        /// <summary>
        /// The email server address
        /// </summary>
        private string emailServer;

        /// <summary>
        /// The address the messages should come from
        /// </summary>
        private string emailFromAddress;

        /// <summary>
        /// The username to connect to the email server
        /// </summary>
        private string emailUsername;

        /// <summary>
        /// The password to connect to the email server
        /// </summary>
        private string emailPassword;

        /// <summary>
        /// The port to connect to the email server
        /// </summary>
        private int? emailPort;

        /// <summary>
        /// Whether the email server requires SSL
        /// </summary>
        private bool? emailSSL;

        /// <summary>
        /// The recipient of the emails generated from the system
        /// </summary>
        private string emailRecipient;

        /// <summary>
        /// A flag if the volume model is enabled.
        /// </summary>
        private bool? enableVolume;

        /// <summary>
        /// The threshold to alert upon volume checks
        /// </summary>
        private double? alertThreshold;

        /// <summary>
        /// A maximum market capitalisation for stocks to check
        /// </summary>
        private decimal? marketCapMax;

        /// <summary>
        /// A minimum market capitalisation for stocks to check
        /// </summary>
        private decimal? marketCapMin;

        /// <summary>
        /// The maximum stock price to check within the system.
        /// </summary>
        private decimal? priceMax;

        /// <summary>
        /// The minimum stock price to check within the system.
        /// </summary>
        private decimal? priceMin;

        /// <summary>
        /// The hour of the day to run the stock checks
        /// </summary>
        private int? hourToRun;

        /// <summary>
        /// The number of days the system should silence a stock once it has been alerted on.
        /// </summary>
        private int? numberOfDaysToSilence;

        #endregion

        /// <summary>
        /// Initialises a new instance of the <see cref="ConfigurationController" /> class.
        /// </summary>
        public ConfigurationController()
        {
            this.ErrorMessages = new List<string>();

            // Load the configuration settings from the config file
            // Only loads settings that exist, these will be validated later.
            this.LoadConfigurationSettings();
        }

        /// <summary>
        /// Gets a list of error messages from the validation of configuration settings
        /// </summary>
        public List<string> ErrorMessages { get; private set; }

        /// <summary>
        /// Configures the server validating all configuration.
        /// If server cannot be configured, all errors are written to ILog
        /// provided and the method returns null.
        /// </summary>
        /// <param name="log">The log for logging output.</param>
        /// <returns>An instance of the server.</returns>
        public StockServer SetupServer(ILog log)
        {
            // Validate all configuration settings
            if (!this.ValidateConfiguration())
            {
                log.Error(this.ErrorMessages);
                return null;
            }

            // Now instantiate the correct site type based on the configuration.
            StockServer server = new StockServer();
            server.EmailServer = this.emailServer;
            server.EmailFromAddress = this.emailFromAddress;
            server.EmailUsername = this.emailUsername;
            server.EmailPassword = this.emailPassword;
            server.EmailPort = this.emailPort.Value;
            server.EmailSSL = this.emailSSL.Value;
            server.EmailRecipient = this.emailRecipient;
            server.EnableVolume = this.enableVolume.Value;
            server.AlertThreshold = this.alertThreshold.Value;
            server.MarketCapMin = this.marketCapMin.Value;
            server.MarketCapMax = this.marketCapMax.Value;
            server.PriceMax = this.priceMax.Value;
            server.PriceMin = this.priceMin.Value;
            server.HourToRun = this.hourToRun.Value;
            server.NumberOfDaysToSilence = this.numberOfDaysToSilence.Value;

            return server;
        }

        /// <summary>
        /// Loads the configuration settings from the configuration file
        /// </summary>
        private void LoadConfigurationSettings()
        {
            this.LoadConfigurationSetting("EmailServer", out this.emailServer);
            this.LoadConfigurationSetting("EmailFromAddress", out this.emailFromAddress);
            this.LoadConfigurationSetting("EmailUsername", out this.emailUsername);
            this.LoadConfigurationSetting("EmailPassword", out this.emailPassword);
            this.LoadConfigurationSetting("EmailPort", out this.emailPort);
            this.LoadConfigurationSetting("EmailSSL", out this.emailSSL);
            this.LoadConfigurationSetting("EmailRecipient", out this.emailRecipient);
            this.LoadConfigurationSetting("EnableVolume", out this.enableVolume);
            this.LoadConfigurationSetting("AlertThreshold", out this.alertThreshold);
            this.LoadConfigurationSetting("MarketCapMax", out this.marketCapMax);
            this.LoadConfigurationSetting("MarketCapMin", out this.marketCapMin);
            this.LoadConfigurationSetting("PriceMax", out this.priceMax);
            this.LoadConfigurationSetting("PriceMin", out this.priceMin);
            this.LoadConfigurationSetting("HourToRun", out this.hourToRun);
            this.LoadConfigurationSetting("NumberOfDaysToSilence", out this.numberOfDaysToSilence);
        }

        #region Configuration setting loaders

        /// <summary>
        /// Loads a setting from the configuration file.
        /// </summary>
        /// <param name="settingName">The setting name in the file</param>
        /// <param name="value">The output parameter to store the retrieved value</param>
        private void LoadConfigurationSetting(string settingName, out string value)
        {
            if (ConfigurationManager.AppSettings[settingName] != null)
                value = ConfigurationManager.AppSettings[settingName];
            else
                value = string.Empty;
        }

        /// <summary>
        /// Loads a setting from the configuration file.
        /// </summary>
        /// <param name="settingName">The setting name in the file</param>
        /// <param name="value">The output parameter to store the retrieved value</param>
        private void LoadConfigurationSetting(string settingName, out bool? value)
        {
            if (ConfigurationManager.AppSettings[settingName] != null)
                value = bool.Parse(ConfigurationManager.AppSettings[settingName]);
            else
                value = null;
        }

        /// <summary>
        /// Loads a setting from the configuration file.
        /// </summary>
        /// <param name="settingName">The setting name in the file</param>
        /// <param name="value">The output parameter to store the retrieved value</param>
        private void LoadConfigurationSetting(string settingName, out int? value)
        {
            if (ConfigurationManager.AppSettings[settingName] != null)
                value = int.Parse(ConfigurationManager.AppSettings[settingName]);
            else
                value = null;
        }

        /// <summary>
        /// Loads a setting from the configuration file.
        /// </summary>
        /// <param name="settingName">The setting name in the file</param>
        /// <param name="value">The output parameter to store the retrieved value</param>
        private void LoadConfigurationSetting(string settingName, out decimal? value)
        {
            if (ConfigurationManager.AppSettings[settingName] != null)
                value = decimal.Parse(ConfigurationManager.AppSettings[settingName]);
            else
                value = null;
        }

        /// <summary>
        /// Loads a setting from the configuration file.
        /// </summary>
        /// <param name="settingName">The setting name in the file</param>
        /// <param name="value">The output parameter to store the retrieved value</param>
        private void LoadConfigurationSetting(string settingName, out Guid? value)
        {
            if (ConfigurationManager.AppSettings[settingName] != null)
                value = new Guid(ConfigurationManager.AppSettings[settingName]);
            else
                value = null;
        }

        /// <summary>
        /// Loads a setting from the configuration file.
        /// </summary>
        /// <param name="settingName">The setting name in the file</param>
        /// <param name="value">The output parameter to store the retrieved value</param>
        private void LoadConfigurationSetting(string settingName, out double? value)
        {
            if (ConfigurationManager.AppSettings[settingName] != null)
                value = double.Parse(ConfigurationManager.AppSettings[settingName]);
            else
                value = null;
        }

        #endregion

        /// <summary>
        /// Validates the configuration settings required for the calling application.
        /// </summary>
        /// <returns>The result if the settings were validated</returns>
        private bool ValidateConfiguration()
        {
            // A list of messages to send back to caller
            List<string> errorMessages = new List<string>();

            // Firstly required for all types of server.
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
            if (!this.marketCapMin.HasValue)
                errorMessages.Add("The MarketCapMin configuration setting is not set.");
            if (!this.marketCapMax.HasValue)
                errorMessages.Add("The MarketCapMax configuration setting is not set.");
            if (!this.priceMin.HasValue)
                errorMessages.Add("The PriceMin configuration setting is not set.");
            if (!this.priceMax.HasValue)
                errorMessages.Add("The PriceMax configuration setting is not set.");
            if (!this.hourToRun.HasValue)
                errorMessages.Add("The HourToRun configuration setting is not set.");
            if (!this.numberOfDaysToSilence.HasValue)
                errorMessages.Add("The NumberOfDaysToSilence onfiguration setting is not set.");

            this.ErrorMessages.AddRange(errorMessages);

            return errorMessages.Count == 0;
        }
    }
}