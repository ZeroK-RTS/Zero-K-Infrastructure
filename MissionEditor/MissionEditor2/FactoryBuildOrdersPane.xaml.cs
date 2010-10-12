using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using CMissionLib.Actions;
using CMissionLib.UnitSyncLib;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for FactoryBuildOrdersPane.xaml
	/// </summary>
	public partial class FactoryBuildOrdersPane : UserControl
	{
		GiveFactoryOrdersAction giveFactoryOrdersAction;

		ObservableCollection<QueueItem> queue;
		UnitInfo[] unitDefs;

		public FactoryBuildOrdersPane()
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
			giveFactoryOrdersAction = (GiveFactoryOrdersAction) MainWindow.Instance.CurrentLogic;
			queue = new ObservableCollection<QueueItem>();
			
			foreach (var buildOrderName in giveFactoryOrdersAction.BuildOrders)
			{
				queue.Add(new QueueItem { Info = unitDefs.First(unitDef => buildOrderName == unitDef.Name) });
			}
			queueGrid.ItemsSource = queue;
			
		}

		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			foreach (var item in buildOptionsGrid.Grid.SelectedItems)
			{
				var unitInfo = (UnitInfo)item;
				queue.Add(new QueueItem{Info = unitInfo});
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

		}

		private void MoveDownButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void MoveUpButton_Click(object sender, RoutedEventArgs e)
		{

		}






	}
}
