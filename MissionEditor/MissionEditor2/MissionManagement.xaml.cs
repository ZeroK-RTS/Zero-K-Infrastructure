using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MissionEditor2.ServiceReference;
using ZkData;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for MissionManagement.xaml
	/// </summary>
	public partial class MissionManagement: Window
	{
		public MissionManagement()
		{
			InitializeComponent();
		}

		void RefreshList()
		{
			var loading = new LoadingDialog { Text = "Getting Mission List" };
			loading.ShowDialog();
			Utils.InvokeInNewThread(delegate
			{
				using (var client = new MissionServiceClient())
				{
					var list = client.ListMissionInfos();
					this.Invoke(delegate
					{
						DataGrid.ItemsSource = list;
						loading.Close();
					});
				}
			});
		}

		void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			var selectedMission = (Mission)DataGrid.SelectedItem;
			var dialog = new StringRequest { Title = "Insert Password" };
			if (dialog.ShowDialog() == true)
			{
				var password = dialog.TextBox.Text;

				Utils.InvokeInNewThread(delegate
					{
						using (var client = new MissionServiceClient()) client.DeleteMission(selectedMission.MissionID, selectedMission.Account.Name, password);
						RefreshList();
					});
			}
		}

		void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new LoadingDialog { Text = "Opening Mission" };
			var selectedMission = (Mission)DataGrid.SelectedItem;

			Utils.InvokeInNewThread(delegate
				{
					var client = new MissionServiceClient();
					var missionData = client.GetMission(selectedMission.Name);
					dialog.Invoke(delegate
						{
							dialog.Close();
							var filter = "Spring Mod Archive (*.sdz)|*.sdz|All files (*.*)|*.*";
							var saveFileDialog = new SaveFileDialog { DefaultExt = "sdz", Filter = filter, RestoreDirectory = true };
							if (saveFileDialog.ShowDialog() == true) File.WriteAllBytes(saveFileDialog.FileName, missionData.Mutator.ToArray());
							WelcomeDialog.LoadExistingMission(saveFileDialog.FileName);
						});
				});
		}

		void UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			var selectedMission = (Mission)DataGrid.SelectedItem;
			var mission = MainWindow.Instance.Mission;
			var dialog = new PublishDialog { DataContext = mission };
			if (dialog.ShowDialog() == true)
			{
				if (selectedMission.Name == mission.Name) MessageBox.Show(String.Format("The mission needs to have a new name. For example, rename it to \"{0} v2\"", mission.Name));
				Publishing.SendMissionWithDialog(mission, dialog.PasswordBox.Password, selectedMission.MissionID);
				RefreshList();
				UpdateButton.IsEnabled = true;
			}
		}

		void Window_Loaded(object sender, RoutedEventArgs e)
		{
			RefreshList();
		}

		void searchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var text = searchBox.Text;
			var item =
				DataGrid.ItemsSource.Cast<Mission>().FirstOrDefault(
					mission =>
					mission.Name.ToLower().Contains(text.ToLower()) || 
					mission.Description.ToLower().Contains(text.ToLower()) ||
					mission.Account.Name.ToLower().Contains(text.ToLower()));
			if (item == null) return;
			DataGrid.SelectedItem = item;
			DataGrid.ScrollIntoView(item);
		}
	}
}