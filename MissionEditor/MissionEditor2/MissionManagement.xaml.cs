using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using MissionEditor2.ServiceReference;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for MissionManagement.xaml
	/// </summary>
	public partial class MissionManagement : Window
	{
		public MissionManagement()
		{
			InitializeComponent();
		}

		void RefreshList()
		{
			Utils.InvokeInNewThread(delegate
				{
					using (var client = new MissionServiceClient())
					{
						var list = client.ListMissionInfos();
						this.Invoke(delegate
							{
								DataGrid.ItemsSource = list;
								DeleteButton.IsEnabled = true;
							});
					}
				});
		}

		void Window_Loaded(object sender, RoutedEventArgs e)
		{
			Utils.InvokeInNewThread(delegate
				{
					using (var client = new MissionServiceClient())

					{
						var list = client.ListMissionInfos();
						this.Invoke(() => DataGrid.ItemsSource = list);
					}
				});
		}

		void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			var selectedMission = (ZkData.Mission) DataGrid.SelectedItem;
			var dialog = new StringRequest {Title = "Insert Password"};
			if (dialog.ShowDialog() == true)
			{
				var password = dialog.TextBox.Text;

				Utils.InvokeInNewThread(delegate
					{
						using (var client = new MissionServiceClient())
						{
							client.DeleteMission(selectedMission.MissionID, selectedMission.Author, password);
						}
						RefreshList();
					});
			}
		}

		void UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			var selectedMission = (ZkData.Mission) DataGrid.SelectedItem;
			var dialog = new StringRequest {Title = "Insert Password"};
			if (dialog.ShowDialog() == true)
			{
				var password = dialog.TextBox.Text;
				var mainWindow = MainWindow.Instance;
				if (selectedMission.Name == mainWindow.Name)
				{
					throw new Exception(String.Format("The mission needs to have a new name. For example, rename it to \"{0} v2\"",
					                                  mainWindow.Mission.Name));
				}
				Utils.SendMissionWithDialog(mainWindow.Mission, password, selectedMission.MissionID);
				RefreshList();
				UpdateButton.IsEnabled = true;
			}
		}

		void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new LoadingDialog {Text = "Opening Mission"};
			var selectedMission = (ZkData.Mission) DataGrid.SelectedItem;

			Utils.InvokeInNewThread(delegate
				{
					var client = new MissionServiceClient();
					var missionData = client.GetMission(selectedMission.Name);
					dialog.Invoke(delegate
						{
							dialog.Close();
							var filter = "Spring Mod Archive (*.sdz)|*.sdz|All files (*.*)|*.*";
							var saveFileDialog = new SaveFileDialog {DefaultExt = "sdz", Filter = filter, RestoreDirectory = true};
							if (saveFileDialog.ShowDialog() == true)
							{
								File.WriteAllBytes(saveFileDialog.FileName, missionData.Mutator.ToArray());
							}
							WelcomeDialog.LoadExistingMission(saveFileDialog.FileName);
						});
				});
		}
	}
}