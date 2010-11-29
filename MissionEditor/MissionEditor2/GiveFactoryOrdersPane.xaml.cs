using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CMissionLib;
using CMissionLib.Actions;
using CMissionLib.UnitSyncLib;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for FactoryBuildOrdersPane.xaml
	/// </summary>
	public partial class GiveFactoryOrdersPane : UserControl
	{
		GiveFactoryOrdersAction action;

		ObservableCollection<QueueItem> queue;
		UnitInfo[] unitDefs;

		public GiveFactoryOrdersPane()
		{
			InitializeComponent();
		}

		class QueueItem
		{
			public UnitInfo Info { get; set; }
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			
			unitDefs = MainWindow.Instance.Mission.Mod.UnitDefs;
			factoryGrid.Tag = unitDefs.Where(u => u.IsFactory);
			factoryGrid.Grid.SelectionChanged += FactoryGrid_SelectionChanged;
			action = (GiveFactoryOrdersAction) MainWindow.Instance.CurrentLogic;
			((INotifyPropertyChanged)action).PropertyChanged += action_PropertyChanged;
			DataContext = action;
			PopulateQueueGrid();
			setGroupsButton.Content= String.Join("\r\n", action.BuiltUnitsGroups);
			action.BuiltUnitsGroups.CollectionChanged += (s, ea) => setGroupsButton.Content = String.Join("\r\n", action.BuiltUnitsGroups); // leak
			factoryGroupsList.BindCollection(action.FactoryGroups);

		}

		void PopulateQueueGrid() 
		{
			action.BuildOrders.CollectionChanged -= BuildOrders_CollectionChanged;
			queue = new ObservableCollection<QueueItem>();
			foreach (var buildOrderName in action.BuildOrders)
			{
				queue.Add(new QueueItem { Info = unitDefs.First(unitDef => buildOrderName == unitDef.Name) });
			}
			queueGrid.ItemsSource = queue;
			action.BuildOrders.CollectionChanged += BuildOrders_CollectionChanged; // leak
		}

		void BuildOrders_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Add)
			{
				foreach (string unitName in e.NewItems)
				{
					queue.Add(new QueueItem {Info = unitDefs.First(unitDef => unitName == unitDef.Name)});
				}
			} 
			else if (e.Action == NotifyCollectionChangedAction.Move)
			{
				queue.Move(e.OldStartingIndex, e.NewStartingIndex);
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove)
			{
				queue.RemoveAt(e.OldStartingIndex);
			}
		}

		void action_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "BuiltUnitsGroups")
			{
				setGroupsButton.Content = String.Join("\r\n", action.BuiltUnitsGroups);
			} 
			else if (e.PropertyName == "BuildOrders")
			{
				PopulateQueueGrid();
			}
		}

		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			foreach (var item in buildOptionsGrid.Grid.SelectedItems)
			{
				var unitInfo = (UnitInfo)item;
				action.BuildOrders.Add(unitInfo.Name);
			}
		}

		void FactoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var units = new List<UnitInfo>();
			foreach (var item in factoryGrid.Grid.SelectedItems)
			{
				var factoryInfo = (UnitInfo)item;
				foreach (var buildOptionName in factoryInfo.BuildOptions)
				{
					var buildOptionInfo = unitDefs.First(unitDef => buildOptionName == unitDef.Name);
					units.Add(buildOptionInfo);
				}
			}
			buildOptionsGrid.Tag = units;
		}

		private void RemoveButton_Click(object sender, RoutedEventArgs e)
		{
			var index = queueGrid.SelectedIndex;
			action.BuildOrders.RemoveAt(index);
		}

		private void MoveDownButton_Click(object sender, RoutedEventArgs e)
		{
			var index = queueGrid.SelectedIndex;
			if (index + 2 > action.BuildOrders.Count) return;
			action.BuildOrders.Move(index, index + 1);

		}

		private void MoveUpButton_Click(object sender, RoutedEventArgs e)
		{
			var index = queueGrid.SelectedIndex;
			if (index == 0) return;
			action.BuildOrders.Move(index, index - 1);
		}

		private void SetGroupsButton_Click(object sender, RoutedEventArgs e)
		{
			var groupsString = String.Join(",", action.BuiltUnitsGroups);
			groupsString = Utils.ShowStringDialog("Insert groups (separate multiple groups with commas).", groupsString);
			if (groupsString != null)
			{
				var groups = groupsString.Split(',');
				action.BuiltUnitsGroups = new ObservableCollection<string>(groups);
			}
		}






	}
}
