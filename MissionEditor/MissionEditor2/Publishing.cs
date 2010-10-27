using System.Data.Linq;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using CMissionLib;
using CMissionLib.UnitSyncLib;
using MissionEditor2.Properties;
using MissionEditor2.ServiceReference;
using ZkData;
using Mission = CMissionLib.Mission;

namespace MissionEditor2
{
	static class Publishing
	{
		public static void Publish(Mission mission, int? missionID)
		{
			var dialog = new PublishDialog { DataContext = mission, Owner = MainWindow.Instance};
			dialog.OKButton.Click += delegate
				{
					var error = mission.VerifyCanPublish();
					if (error == null)
					{
						SendMissionWithDialog(mission, dialog.PasswordBox.Password, missionID);
						dialog.Close();
					}
					else MessageBox.Show(error);
				};
			dialog.ShowDialog();
		}

		public static void SendMission(Mission mission, string password, int? missionId)
		{
			var info = new ZkData.Mission
			           {
			           	Description = mission.Description,
			           	Map = mission.Map.Name,
			           	Mod = mission.Mod.Name,
			           	Name = mission.Name,
			           	ScoringMethod = mission.ScoringMethod,
			           	Image = new byte[0],
			           	Script = mission.GetScript(),
			           	ModRapidTag = mission.RapidTag,
			           };

			var alliances = mission.Players.Select(p => p.Alliance).Distinct().ToList();
			foreach (var player in mission.Players)
			{
				var missionSlot = new MissionSlot
				                  {
				                  	AiShortName = player.AIDll,
				                  	AiVersion = player.AIVersion,
				                  	AllyID = alliances.IndexOf(player.Alliance),
				                  	AllyName = player.Alliance,
				                  	IsHuman = player.IsHuman,
				                  	IsRequired = player.IsRequired,
				                  	TeamID = mission.Players.IndexOf(player),
				                  	TeamName = player.Name,
				                  	Color = (int)(MyCol)player.Color
				                  };
				info.MissionSlots.Add(missionSlot);
			}

			var image = File.ReadAllBytes(mission.ImagePath).ToImage(96, 96);
			var pngEncoder = new PngBitmapEncoder();
			pngEncoder.Frames.Add(BitmapFrame.Create(image));
			var imageStream = new MemoryStream();
			pngEncoder.Save(imageStream);
			imageStream.Position = 0;
			info.Image = imageStream.ToArray();

			if (ApplicationDeployment.IsNetworkDeployed) info.MissionEditorVersion = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
			using (var unitSync = new UnitSync(Settings.Default.SpringPath)) info.SpringVersion = unitSync.Version;

			if (missionId.HasValue) info.MissionID = missionId.Value;

			var tempPath = Path.GetTempFileName();
			mission.CreateArchive(tempPath);
			info.Mutator = new Binary(File.ReadAllBytes(tempPath));
			File.Delete(tempPath);
			using (var client = new MissionServiceClient())
			{
				client.SendMission(info, mission.Author, password);
				MessageBox.Show("Mission successfully uploaded.\n\rIt is now accessible from SpringDownloader.\r\nPlease make sure it works!");
			}
		}

		public static void SendMissionWithDialog(Mission mission, string password, int? missionId)
		{
			var loading = new LoadingDialog { Text = "Uploading Mission", Owner = MainWindow.Instance };
			loading.Loaded += delegate
				{
					Utils.InvokeInNewThread(delegate
						{
							SendMission(mission, password, missionId);
							loading.Invoke(loading.Close);
						});
				};
			loading.ShowDialog();
		}
	}
}