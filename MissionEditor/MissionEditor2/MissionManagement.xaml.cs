using System;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
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
					try
					{
						var client = MissionServiceClientFactory.MakeClient();

						var list = client.ListMissionInfos();
						this.Invoke(delegate
							{
								DataGrid.ItemsSource = showHiddenMissionsBox.IsChecked == true ? list : list.Where(m => !m.IsDeleted);
								loadingDialog.Close();
							});
					}
					catch (Exception e)
					{
						MessageBox.Show("Could not get mission list: " + e.Message);
					}
				});
			loadingDialog.ShowDialog();
		}
#pragma warning disable 612,618
		void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			var selectedMission = (Mission)DataGrid.SelectedItem;
			var dialog = new PasswordRequest { Owner = MainWindow.Instance };
			if (dialog.ShowDialog() == true)
			{
				var password = dialog.PasswordBox.Password;
				Utils.InvokeInNewThread(delegate
					{
						try
						{
							var client = MissionServiceClientFactory.MakeClient();
							client.DeleteMission(selectedMission.MissionID, selectedMission.AuthorName, password);
							RefreshList();
						}
						catch (FaultException<ExceptionDetail> ex)
						{
							MessageBox.Show(ex.Message);
						}
					});
			}
		}
#pragma warning restore 612,618

		void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			var loadingDialog = new LoadingDialog { Text = "Opening Mission", Owner = this };
			var selectedMission = (Mission)DataGrid.SelectedItem;
			if (selectedMission == null) return;
			Utils.InvokeInNewThread(delegate
				{
					try
					{
						var client = MissionServiceClientFactory.MakeClient();
						var missionData = client.GetMission(selectedMission.Name);
						loadingDialog.Invoke(delegate
							{
								loadingDialog.Close();
								var filter = "Spring Mod Archive (*.sdz)|*.sdz|All files (*.*)|*.*";
								var saveFileDialog = new SaveFileDialog { DefaultExt = "sdz", Filter = filter, RestoreDirectory = true };
								if (saveFileDialog.ShowDialog() == true) File.WriteAllBytes(saveFileDialog.FileName, missionData.Mutator.ToArray());
								WelcomeDialog.LoadExistingMission(saveFileDialog.FileName);
							});
					}
					catch (FaultException<ExceptionDetail> ex)
					{
						MessageBox.Show(ex.Message);
					}
				});
			loadingDialog.ShowDialog();
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
#pragma warning disable 612,618
		void searchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var text = searchBox.Text;
			var item =
				DataGrid.ItemsSource.Cast<Mission>().FirstOrDefault(
					mission =>
					mission.Name.ToLower().Contains(text.ToLower()) || mission.Description.ToLower().Contains(text.ToLower()) ||
					mission.AuthorName.ToLower().Contains(text.ToLower()));

			if (item == null) return;
			DataGrid.SelectedItem = item;
			DataGrid.ScrollIntoView(item);
		}
#pragma warning restore 612,618

		private void PublishButton_Click(object sender, RoutedEventArgs e)
		{
			Publishing.Publish(MainWindow.Instance.Mission, null);
		}
#pragma warning disable 612,618
		private void UndeleteButton_Click(object sender, RoutedEventArgs e)
		{
			var selectedMission = (Mission)DataGrid.SelectedItem;
			var dialog = new PasswordRequest { Owner = MainWindow.Instance };
			if (dialog.ShowDialog() == true)
			{
				var password = dialog.PasswordBox.Password;
				Utils.InvokeInNewThread(delegate
				{
					try
					{
						var client = MissionServiceClientFactory.MakeClient();

						client.UndeleteMission(selectedMission.MissionID, selectedMission.AuthorName, password);

						RefreshList();
					}
					catch (FaultException<ExceptionDetail> ex)
					{
						MessageBox.Show(ex.Message);
					}
				});
			}
		}
#pragma warning restore 612,618

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			RefreshList();
		}
	}

}