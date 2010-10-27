using System.Threading;
using System.Windows;
using System.Windows.Controls;
using LobbyClient;
using PlasmaDownloader;
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

		void Grid_Loaded(object sender, RoutedEventArgs e) {}

		void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			label1.Content = string.Format("Downloading mission {0}", missionName);
			var down = Program.Downloader.GetResource(DownloadType.MOD, missionName);
			if (down == null)
			{
				label1.Content = string.Format("Cannot find mission {0}", missionName);
				return;
			}
			else
			{
				PlasmaShared.Utils.StartAsync(() =>
					{
						var metaWait = new EventWaitHandle(false, EventResetMode.ManualReset);
						Mod modInfo = null;
						Program.SpringScanner.MetaData.GetModAsync(missionName,
						                                           mod =>
						                                           	{
						                                           		if (!mod.IsMission) Program.MainWindow.InvokeFunc(() => label1.Content = string.Format("{0} is not a valid mission", missionName));

						                                           		else modInfo = mod;

						                                           		metaWait.Set();
						                                           	},
						                                           error =>
						                                           	{
						                                           		Program.MainWindow.InvokeFunc(
						                                           			() => label1.Content = string.Format("Download of metadata failed: {0}", error.Message));
						                                           		metaWait.Set();
						                                           	});
						WaitHandle.WaitAll(new WaitHandle[] { down.WaitHandle, metaWait });

						if (down.IsComplete == false)
						{
							Program.MainWindow.InvokeFunc(() => label1.Content = string.Format("Download of {0} failed", missionName));
						}
						
						if (modInfo != null && down.IsComplete == true)
						{
							var spring = new Spring(Program.SpringPaths);
							var name = Program.Conf.LobbyPlayerName;
							if (string.IsNullOrEmpty(name)) name = "Player";
							spring.StartGame(null, null, null, modInfo.MissionScript);
							// todo close this bar
						}
					});
			}
		}
	}
}