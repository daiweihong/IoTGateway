﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Waher.IoTGateway.WebResources.ExportFormats;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Content.Xsl;
using Waher.Events;
using Waher.IoTGateway;
using Waher.IoTGateway.Setup;
using Waher.Networking.HTTP;
using Waher.Networking.HTTP.HeaderFields;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Script;
using Waher.Runtime.Language;
using Waher.IoTGateway.WebResources;

namespace Waher.IoTGateway.Setup
{
	/// <summary>
	/// Restore Configuration
	/// </summary>
	public class BackupConfiguration : SystemConfiguration
	{
		private static BackupConfiguration instance = null;

		private HttpFolderResource exportFolder = null;
		private HttpFolderResource keyFolder = null;
		private StartExport startExport = null;
		private StartAnalyze startAnalyze = null;
		private DeleteExport deleteExport = null;
		private UpdateBackupSettings updateBackupSettings = null;
		private UpdateBackupFolderSettings updateBackupFolderSettings = null;

		/// <summary>
		/// Current instance of configuration.
		/// </summary>
		public static BackupConfiguration Instance => instance;

		/// <summary>
		/// Resource to be redirected to, to perform the configuration.
		/// </summary>
		public override string Resource => "/Settings/Backup.md";

		/// <summary>
		/// Priority of the setting. Configurations are sorted in ascending order.
		/// </summary>
		public override int Priority => 175;

		/// <summary>
		/// Gets a title for the system configuration.
		/// </summary>
		/// <param name="Language">Current language.</param>
		/// <returns>Title string</returns>
		public override string Title(Language Language)
		{
			return "Backup";
		}

		/// <summary>
		/// Is called during startup to configure the system.
		/// </summary>
		public override Task ConfigureSystem()
		{
			this.UpdateExportFolder(Export.FullExportFolder);
			this.UpdateExportKeyFolder(Export.FullKeyExportFolder);

			if (Gateway.InternalDatabase is FilesProvider FilesProvider)
				FilesProvider.AutoRepairReportFolder = Export.FullExportFolder;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Sets the static instance of the configuration.
		/// </summary>
		/// <param name="Configuration">Configuration object</param>
		public override void SetStaticInstance(ISystemConfiguration Configuration)
		{
			instance = Configuration as BackupConfiguration;
		}

		/// <summary>
		/// Initializes the setup object.
		/// </summary>
		/// <param name="WebServer">Current Web Server object.</param>
		public override Task InitSetup(HttpServer WebServer)
		{
			WebServer.Register(this.exportFolder = new HttpFolderResource("/Export", Export.FullExportFolder, false, false, false, true, Gateway.LoggedIn));
			WebServer.Register(this.keyFolder = new HttpFolderResource("/Key", Export.FullKeyExportFolder, false, false, false, true, Gateway.LoggedIn));
			WebServer.Register(this.startExport = new StartExport());
			WebServer.Register(this.startAnalyze = new StartAnalyze());
			WebServer.Register(this.deleteExport = new DeleteExport());
			WebServer.Register(this.updateBackupSettings = new UpdateBackupSettings());
			WebServer.Register(this.updateBackupFolderSettings = new UpdateBackupFolderSettings());

			return base.InitSetup(WebServer);
		}

		/// <summary>
		/// Unregisters the setup object.
		/// </summary>
		/// <param name="WebServer">Current Web Server object.</param>
		public override Task UnregisterSetup(HttpServer WebServer)
		{
			WebServer.Unregister(this.exportFolder);
			WebServer.Unregister(this.keyFolder);
			WebServer.Unregister(this.startExport);
			WebServer.Unregister(this.startAnalyze);
			WebServer.Unregister(this.deleteExport);
			WebServer.Unregister(this.updateBackupSettings = new UpdateBackupSettings());
			WebServer.Unregister(this.updateBackupFolderSettings = new UpdateBackupFolderSettings());

			return base.UnregisterSetup(WebServer);
		}

		internal void UpdateExportFolder(string Folder)
		{
			if (this.exportFolder != null)
				this.exportFolder.FolderPath = Folder;

			if (Gateway.InternalDatabase is FilesProvider FilesProvider)
				FilesProvider.AutoRepairReportFolder = Folder;
		}

		internal void UpdateExportKeyFolder(string Folder)
		{
			if (this.keyFolder != null)
				this.keyFolder.FolderPath = Folder;
		}

		/// <summary>
		/// Simplified configuration by configuring simple default values.
		/// </summary>
		/// <returns>If the configuration was changed.</returns>
		public override Task<bool> SimplifiedConfiguration()
		{
			return Task.FromResult<bool>(true);
		}

	}
}
