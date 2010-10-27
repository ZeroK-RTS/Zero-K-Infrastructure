using System;
using System.IO;
using System.Linq;
using System.Threading;
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

		void Window_Loaded(object sender, RoutedEventArgs e)
		{
			RefreshList();
		}

		void RefreshList()
		{
			if (Dispatcher.Thread != Thread.CurrentThread)
			{
				this.Invoke(RefreshList);
				return;
			}
			var loadingDialog = new LoadingDialog { Text = "Getting Mission List", Owner = MainWindow.Instance };
			Utils.InvokeInNewThread(delegate
			{
				using (var client = new MissionServiceClient())
				{
					var list = client.ListMissionInfos();
					this.Invoke(delegate
					{
						DataGrid.ItemsSource = list;
						loadingDialog.Close();
					});
				}
			});
			loadingDialog.ShowDialog();
		}

		void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			var selectedMission = (Mission)DataGrid.SelectedItem;
			var dialog = new PasswordRequest { Owner = MainWindow.Instance };
			if (dialog.ShowDialog() == true)
			{
				var password = dialog.PasswordBox.Password;

				Utils.InvokeInNewThread(delegate
					{
#pragma warning disable 612,618
						using (var client = new MissionServiceClient()) client.DeleteMission(selectedMission.MissionID, selectedMission.AuthorName, password);
#pragma warning restore 612,618
						RefreshList();
					});
			}
		}

		void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new LoadingDialog { Text = "Opening Mission" };
			var selectedMission = (Mission)DataGrid.SelectedItem;
			if (selectedMission == null) return;
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
			if (selectedMission == null) return;
			var mission = MainWindow.Instance.Mission;
			var dialog = new PublishDialog { DataContext = mission, Owner = this };
			dialog.OKButton.Click += delegate
			{
				var error = mission.VerifyCanPublish();
				if (error == null)
				{
					Publishing.SendMissionWithDialog(mission, dialog.PasswordBox.Password, selectedMission.MissionID);
					dialog.Close();
					RefreshList();
				}
				else MessageBox.Show(error);
			};
			dialog.ShowDialog();
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