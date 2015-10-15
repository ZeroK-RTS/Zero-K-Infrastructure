using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using PlasmaDownloader;
using PlasmaShared;
using ZkData;

namespace ZeroKLobby.Notifications
{
	public partial class SinglePlayerBar: UserControl, INotifyBar
	{
		readonly string modInternalName;
		readonly List<Download> neededDownloads = new List<Download>();
		readonly ScriptMissionData profile;
		readonly Timer timer = new Timer();

		public SinglePlayerBar(List<Download> neededDownloads, ScriptMissionData profile, string modInternalName)
		{
			InitializeComponent();
			this.neededDownloads = neededDownloads;
			this.profile = profile;
			timer.Interval = 500;
			timer.Tick += timer_Tick;
			timer.Start();
			this.modInternalName = modInternalName;
		}

		public static void DownloadAndStartMission(ScriptMissionData profile)
		{
			var modVer = Program.Downloader.PackageDownloader.GetByTag(profile.ModTag);
			if (modVer == null)
			{
				Trace.TraceError("Cannot start mission - cannot find rapid tag: {0}", profile.ModTag);
				return;
			}

			var modName = modVer.InternalName;

			var neededDownloads = new List<Download>();

			if (!Program.SpringScanner.HasResource(modName)) neededDownloads.Add(Program.Downloader.GetResource(DownloadType.MOD, modName));
			if (!Program.SpringScanner.HasResource(profile.MapName)) neededDownloads.Add(Program.Downloader.GetResource(DownloadType.MAP, profile.MapName));
			if (profile.ManualDependencies != null) foreach (var entry in profile.ManualDependencies) if (!string.IsNullOrEmpty(entry) && !Program.SpringScanner.HasResource(entry)) neededDownloads.Add(Program.Downloader.GetResource(DownloadType.UNKNOWN, entry));
			var needEngine = Program.Downloader.GetAndSwitchEngine(Program.SpringPaths.SpringVersion);
			if (needEngine != null) neededDownloads.Add(needEngine);
				
			if (neededDownloads.Count > 0) Program.NotifySection.AddBar(new SinglePlayerBar(neededDownloads, profile, modName));
			else StartDownloadedMission(profile, modName);
		}

		public static void StartDownloadedMission(ScriptMissionData profile, string modInternalName)
		{
			var spring = new Spring(Program.SpringPaths);
			var name = Program.Conf.LobbyPlayerName;
			if (string.IsNullOrEmpty(name)) name = "Player";

			if (Utils.VerifySpringInstalled())
			{
				spring.RunLocalScriptGame(profile.StartScript.Replace("%MOD%", modInternalName).Replace("%MAP%", profile.MapName).Replace("%NAME%", name));
                var serv = GlobalConst.GetContentService();
				serv.NotifyMissionRun(Program.Conf.LobbyPlayerName, profile.Name);
			}
		}

		void CloseBar()
		{
			timer.Stop();
			timer.Tick -= timer_Tick;
			neededDownloads.Clear();
			Program.NotifySection.RemoveBar(this);
		}


		public void AddedToContainer(NotifyBarContainer container)
		{
			container.btnDetail.Enabled = false;
			container.btnDetail.Text = "SinglePlayer";
		    container.Title = "Loading a single player mission";
            container.TitleTooltip = "Please await resource download";
		}

		public void CloseClicked(NotifyBarContainer container)
		{
			CloseBar();
		}

		public void DetailClicked(NotifyBarContainer container) {}

		public Control GetControl()
		{
			return this;
		}

	    void timer_Tick(object sender, EventArgs e)
		{
			try
			{
				if (neededDownloads.All(x => x.IsComplete == true))
				{
					CloseBar();
					StartDownloadedMission(profile, modInternalName);
				}
				else if (neededDownloads.Any(x => (x.IsComplete == false || x.IsAborted)))
				{
					Trace.TraceWarning("Download failed - cannot start single player mission");
					CloseBar();
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("Error starting SP game: {0}", ex);
			}
		}
	}
}