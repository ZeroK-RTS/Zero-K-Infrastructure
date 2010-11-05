using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using System.Windows.Media.Imaging;
using CMissionLib;
using MissionEditor2.MissionServiceReference;
using MissionEditor2.Properties;
using PlasmaShared.UnitSyncLib;
using ZkData;
using Binary = System.Data.Linq.Binary;
using Mission = CMissionLib.Mission;
using UnitSync = CMissionLib.UnitSyncLib.UnitSync;

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

		public static bool SendMission(Mission mission, string password, int? missionId)
		{
			try
			{
				var info = new ZkData.Mission()
				           {
				           	Description = mission.Description,
				           	Map = mission.Map.Name,
				           	Mod = mission.Mod.Name,
				           	Name = mission.Name,
				           	ScoringMethod = mission.ScoringMethod,
				           	Script = mission.GetScript(),
				           	ModRapidTag = mission.RapidTag,
				           };

				var slots = mission.GetSlots();
				

				var image = File.ReadAllBytes(mission.ImagePath).ToImage(96, 96);
				var pngEncoder = new PngBitmapEncoder();
				pngEncoder.Frames.Add(BitmapFrame.Create(image));
				var imageStream = new MemoryStream();
				pngEncoder.Save(imageStream);
				imageStream.Position = 0;
				info.Image = new Binary(imageStream.ToArray());

				if (ApplicationDeployment.IsNetworkDeployed) info.MissionEditorVersion = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
				using (var unitSync = new UnitSync(Settings.Default.SpringPath)) info.SpringVersion = unitSync.Version;

				if (missionId.HasValue) info.MissionID = missionId.Value;

				string tempPath = null;
				var missionFileName = "publish_mission_temp.sdz";
				try
				{
					using (var unitSync = new PlasmaShared.UnitSyncLib.UnitSync(Settings.Default.SpringPath))
					{
						var writeablePath = unitSync.WritableDataDirectory;
						tempPath = Path.Combine(writeablePath, missionFileName);
					}
					if (File.Exists(tempPath)) File.Delete(tempPath);
					mission.CreateArchive(tempPath);

					PlasmaShared.UnitSyncLib.Mod mod;
					using (var unitSync = new PlasmaShared.UnitSyncLib.UnitSync(Settings.Default.SpringPath))
					{
						mod = unitSync.GetModFromArchive(missionFileName);
					}
					info.Mutator = new Binary(File.ReadAllBytes(tempPath));
					File.Delete(tempPath);
					using (var client = new MissionServiceClient())
					{
						client.SendMission(info, slots, mission.Author, password, mod);
						MessageBox.Show("Mission successfully uploaded.\n\rIt is now accessible from the lobby.\r\nPlease make sure it works!");
						return true;
					}
				} 
				finally
				{
					if (tempPath != null && File.Exists(tempPath)) File.Delete(tempPath);
				}
			} 
			catch(FaultException<ExceptionDetail> e)
			{
				if (Debugger.IsAttached) throw;
				MessageBox.Show(e.Message);
				return false;
			}
			catch(FaultException e)
			{
				if (Debugger.IsAttached) throw;
				MessageBox.Show(e.Message);
				return false;
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