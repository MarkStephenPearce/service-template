// Copyright (c) 2014-16 Mark Pearce
// http://opensource.org/licenses/MIT

using System;
using System.Configuration.Install;
using System.ComponentModel;
using System.Configuration;
using System.ServiceProcess;
using System.Diagnostics;

namespace SleekSoftware
{
	[RunInstaller(true)]
	public partial class ServiceInstall : Installer
	{
		// Config file settings.
		private const string CONFIG_SERVICE_NAME = "ServiceName";
		private const string CONFIG_DISPLAY_NAME = "ServiceDisplayName";
		private const string CONFIG_DESCRIPTION = "ServiceDescription";
		private const string CONFIG_SERVICE_ACCOUNT = "ServiceAccount";
		private const string CONFIG_USER_NAME = "ServiceUserName";
		private const string CONFIG_USER_PASSWORD = "ServiceUserPassword";
		private const string CONFIG_EVENT_SOURCE_NAME = "EventSource";
		private const string CONFIG_EVENT_LOG_NAME = "EventLog";

		// Constants for evaluating the account under which the service will run.
		// These correspond to the ServiceAccount enumeration.
		private const string ACCOUNT_LOCAL_SERVICE = "LocalService";
		private const string ACCOUNT_NETWORK_SERVICE = "NetworkService";
		private const string ACCOUNT_LOCAL_SYSTEM = "LocalSystem";
		private const string ACCOUNT_USER = "User";

		public ServiceInstall()
		{
			// Service installer - this defines the service's primary properties.
			var serviceInstaller = new ServiceInstaller();
			serviceInstaller = new ServiceInstaller();
			serviceInstaller.ServiceName = ConfigurationManager.AppSettings[CONFIG_SERVICE_NAME];
			serviceInstaller.DisplayName = ConfigurationManager.AppSettings[CONFIG_DISPLAY_NAME];
			serviceInstaller.Description = ConfigurationManager.AppSettings[CONFIG_DESCRIPTION];
			serviceInstaller.StartType = ServiceStartMode.Automatic;

			// Service process installer - this defines the service's credentials.
			var serviceProcessInstaller = new ServiceProcessInstaller();
			var serviceAccount = (ConfigurationManager.AppSettings[CONFIG_SERVICE_ACCOUNT]);
			switch (serviceAccount)
			{
				case ACCOUNT_NETWORK_SERVICE:
					serviceProcessInstaller.Account = ServiceAccount.NetworkService;
					serviceProcessInstaller.Username = null;
					serviceProcessInstaller.Password = null;
					break;
				case ACCOUNT_LOCAL_SERVICE:
					serviceProcessInstaller.Account = ServiceAccount.LocalService;
					serviceProcessInstaller.Username = null;
					serviceProcessInstaller.Password = null;
					break;
				case ACCOUNT_LOCAL_SYSTEM:
					serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
					serviceProcessInstaller.Username = null;
					serviceProcessInstaller.Password = null;
					break;
				case ACCOUNT_USER:
					serviceProcessInstaller.Account = ServiceAccount.User;
					serviceProcessInstaller.Username = ConfigurationManager.AppSettings[CONFIG_USER_NAME];
					serviceProcessInstaller.Password = ConfigurationManager.AppSettings[CONFIG_USER_PASSWORD];
					break;
				default:
					serviceProcessInstaller.Account = ServiceAccount.User;
					serviceProcessInstaller.Username = ConfigurationManager.AppSettings[CONFIG_USER_NAME];
					serviceProcessInstaller.Password = ConfigurationManager.AppSettings[CONFIG_USER_PASSWORD];
				break;
			}

			// Event log installer - this defines where service activity will be logged.
			var eventLogInstaller = new EventLogInstaller();
			eventLogInstaller.Source = ConfigurationManager.AppSettings[CONFIG_EVENT_SOURCE_NAME];
			eventLogInstaller.Log = ConfigurationManager.AppSettings[CONFIG_EVENT_LOG_NAME];

			// Remove the rather poor default event log installer and use our own installers instead.
			serviceInstaller.Installers.Clear();
			this.Installers.AddRange(new Installer[] { serviceProcessInstaller, serviceInstaller, eventLogInstaller });

			InitializeComponent();
		}
	}
}