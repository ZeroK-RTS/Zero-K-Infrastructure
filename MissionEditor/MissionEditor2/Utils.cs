using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using CMissionLib.UnitSyncLib;
using Microsoft.Win32;
using MissionEditor2.Properties;
using MissionEditor2.ServiceReference;
using ZkData;
using Action = System.Action;
using Mission = CMissionLib.Mission;

namespace MissionEditor2
{
	static class Utils
	{
		/// <summary>
		/// asynchronously begins executing a function on the control's thread
		/// </summary>
		public static void Invoke(this DispatcherObject control, Action action)
		{
			control.Dispatcher.BeginInvoke(action);
		}

		public static FrameworkElement FindTag(this DependencyObject obj, string tag)
		{
			var element = obj as FrameworkElement;
			if (element != null && element.Tag is string && (string) element.Tag == tag)
			{
				return element;
			}

			for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
			{
				var child = VisualTreeHelper.GetChild(obj, i);
				var found = FindTag(child, tag);
				if (found != null) return found;
			}

			return null;
		}

		/// <summary>
		/// asynchronously begin executing the function
		/// </summary>
		public static void InvokeInNewThread(this Action action)
		{
			new Thread(new ThreadStart(action)).Start();
		}

		public static void BindCollection<T>(this ListBox list, ICollection<T> collection)
		{
			if (list.SelectedItems == collection) throw new ArgumentException();
			list.SelectedItems.Clear();
			foreach (var item in collection.ToArray()) list.SelectedItems.Add(item);
			SelectionChangedEventHandler onSelectionChanged = (s, e) =>
			{
				foreach (var item in e.AddedItems)
				{
					if (!collection.Contains((T)item)) collection.Add((T)item);
				}
				foreach (var item in e.RemovedItems)
				{
					collection.Remove((T)item);
				}
			};
			list.SelectionChanged += onSelectionChanged;
			// this needs to be done before "Unloaded" because at that point the items will have been all deselected
			MainWindow.Instance.LogicGrid.SelectionChanged += (s, e) => list.SelectionChanged -= onSelectionChanged;
		}

		public static void Bind(this FrameworkElement target, DependencyProperty property, object source, string path, BindingMode mode, IValueConverter converter = null, object converterParameter = null)
		{
			var binding = new Binding(path) {Source = source, Mode = mode, Converter = converter, ConverterParameter = converterParameter};
			target.SetBinding(property, binding);
		}

		public static double GetDistance(double x1, double y1, double x2, double y2)
		{
			return Math.Sqrt((x2 - x1)*(x2 - x1) + (y2 - y1)*(y2 - y1));
		}

		public static string ShowStringDialog(string title, string text)
		{
			var dialog = new StringRequest {Title = title, TextBox = {Text = text}};
			return dialog.ShowDialog() == true ? dialog.TextBox.Text : null;
		}

		public static void AddAction(this ItemsControl menu, string header, Action action)
		{
			var item = new MenuItem {Header = header};
			item.Click += (s, e) => action();
			menu.Items.Add(item);
		}

		public static MenuItem AddContainer(this ItemsControl menu, string header)
		{
			var item = new MenuItem { Header = header };
			menu.Items.Add(item);
			return item;
		}

		public static void SendMissionWithDialog(Mission mission, string password, int? missionId)
		{

			var loading = new LoadingDialog {Text = "Uploading Mission"};
			loading.Loaded += delegate
				{
					InvokeInNewThread(delegate
						{
							SendMission(mission, password, missionId);
							loading.Invoke(loading.Close);
						});
				};
			loading.ShowDialog();
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
				};


			var alliances = mission.Players.Select(p => p.Alliance).Distinct().ToList();
			foreach (var player in mission.Players)
			{
				var missionSlot = new MissionSlot();
				missionSlot.AiShortName = player.AIDll;
				missionSlot.AiVersion = player.AIVersion;
				missionSlot.AllyID = alliances.IndexOf(player.Alliance);
				missionSlot.AllyName = player.Alliance;
				missionSlot.IsHuman = player.IsHuman;
				missionSlot.IsRequired = player.IsRequired;
				missionSlot.TeamID = mission.Players.IndexOf(player);
				missionSlot.TeamName = player.Name;
				missionSlot.Color = (int)(MyCol)player.Color;
				info.MissionSlots.Add(missionSlot);
			}


			if (ApplicationDeployment.IsNetworkDeployed)
			{
				info.MissionEditorVersion = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
			}
			using (var unitSync = new UnitSync(Settings.Default.SpringPath))
			{
				info.SpringVersion = unitSync.Version;
			}

			if (missionId.HasValue) info.MissionID = missionId.Value;

			var tempPath = Path.GetTempFileName();
			mission.CreateArchive(tempPath);
			info.Mutator = new System.Data.Linq.Binary(File.ReadAllBytes(tempPath));
			File.Delete(tempPath);
			using (var client = new MissionServiceClient())
			{
				client.SendMission(info, mission.Author, password);
				MessageBox.Show(
					"Mission successfully uploaded.\n\rIt is now accessible from SpringDownloader.\r\nPlease make sure it works!");
			}

		}

		public static void Publish(Mission mission, int? missionID)
		{
			var dialog = new PublishDialog {DataContext = mission};
			dialog.OKButton.Click += delegate
				{

					SendMissionWithDialog(mission, dialog.PasswordBox.Password, missionID);
					dialog.Close();
				};
			dialog.ShowDialog();
		}
	}
}
