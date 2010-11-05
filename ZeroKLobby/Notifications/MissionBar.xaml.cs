using System.Threading;
using System.Windows;
using System.Windows.Controls;
using LobbyClient;
using PlasmaDownloader;
using PlasmaShared.ContentService;
using PlasmaShared.UnitSyncLib;

namespace ZeroKLobby.Notifications
{
	/// <summary>
	/// Interaction logic for MissionBar.xaml
	/// </summary>
	public partial class MissionBar: UserControl
	{
		readonly string missionName;

		public MissionBar(string missionName)
		{
			this.missionName = missionName;
			InitializeComponent();
		}


		void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			label1.Content = string.Format("Starting mission {0} - please wait", missionName);
			var down = Program.Downloader.GetResource(DownloadType.MOD, missionName);

			PlasmaShared.Utils.StartAsync(() =>
				{
					var metaWait = new EventWaitHandle(false, EventResetMode.ManualReset);
					Mod modInfo = null;
					Program.SpringScanner.MetaData.GetModAsync(missionName,
					                                           mod =>
					                                           	{
					                                           		if (!mod.IsMission)
					                                           		{
					                                           			Program.MainWindow.InvokeFunc(() =>
					                                           				{
					                                           					label1.Content = string.Format("{0} is not a valid mission", missionName);
					                                           					btnCancel.IsEnabled = true;
					                                           				});
					                                           		}

					                                           		else modInfo = mod;

					                                           		metaWait.Set();
					                                           	},
					                                           error =>
					                                           	{
					                                           		Program.MainWindow.InvokeFunc(() =>
					                                           			{
					                                           				label1.Content = string.Format("Download of metadata failed: {0}", error.Message);
					                                           				btnCancel.IsEnabled = true;
					                                           			});
					                                           		metaWait.Set();
					                                           	});
					if (down != null) WaitHandle.WaitAll(new WaitHandle[] { down.WaitHandle, metaWait });
					else metaWait.WaitOne();

					if (down != null && down.IsComplete == false)
					{
						Program.MainWindow.InvokeFunc(() =>
							{
								label1.Content = string.Format("Download of {0} failed", missionName);
								btnCancel.IsEnabled = true;
							});
					}

					if (modInfo != null && (down == null || down.IsComplete == true))
					{
						var spring = new Spring(Program.SpringPaths);
						spring.StartGame(null,
						                 null,
						                 null,
						                 modInfo.MissionScript,
						                 Program.Conf.LobbyPlayerName,
						                 PlasmaShared.Utils.HashLobbyPassword(Program.Conf.LobbyPlayerPassword));
						var cs = new ContentService() { Proxy = null };
						cs.NotifyMissionRunAsync(Program.Conf.LobbyPlayerName, missionName);
						Program.MainWindow.InvokeFunc(() => Program.NotifySection.RemoveBar(this));
					}
				});
		}

		void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			Program.NotifySection.RemoveBar(this);
		}
	}
}