using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CMissionLib;
using CMissionLib.Actions;
using CMissionLib.Conditions;
using CMissionLib.UnitSyncLib;
using Action = CMissionLib.Action;

namespace MissionEditor2
{
	/// <summary>
	/// Here go templates that need to be reloaded each time.
	/// </summary>
	public partial class ListTemplates : UserControl
	{
		public ListTemplates()
		{
			InitializeComponent();
		}

		void Triggers_ListLoaded(object sender, RoutedEventArgs e)
		{
			var list = (ListBox) e.Source;
			var collection = ((TriggersAction) MainWindow.Instance.CurrentLogic).Triggers;
			list.BindCollection(collection);
		}

		void UnitDefGrid_Loaded(object sender, RoutedEventArgs e)
		{
			var dataGrid = ((UnitDefsGrid)e.Source).Grid;
			var currentLogic = MainWindow.Instance.CurrentLogic;

			ObservableCollection<string> logicItemUnitList = null;
			if (currentLogic is UnitCreatedCondition)
			{
				logicItemUnitList = ((UnitCreatedCondition)currentLogic).Units;
			}
			else if (currentLogic is UnitFinishedCondition)
			{
				logicItemUnitList = ((UnitFinishedCondition)currentLogic).Units;
			}
			else if (currentLogic is LockUnitsAction)
			{
				logicItemUnitList = ((LockUnitsAction)currentLogic).Units;
			}
			else if (currentLogic is UnlockUnitsAction)
			{
				logicItemUnitList = ((UnlockUnitsAction)currentLogic).Units;
			}

			if (logicItemUnitList == null) return;

			foreach (var unit in logicItemUnitList.ToArray())
			{
				var unitInfo = MainWindow.Instance.Mission.Mod.UnitDefs.FirstOrDefault(u => u.Name == unit);
				if (unitInfo != null)
				{
					dataGrid.SelectedItems.Add(unitInfo);
				}
			}

			SelectionChangedEventHandler handler = (s, se) =>
				{
					foreach (var item in se.AddedItems)
					{
						var info = (UnitInfo) item;
						logicItemUnitList.Add(info.Name);
					}
					foreach (var item in se.RemovedItems)
					{
						var info = (UnitInfo)item;
						logicItemUnitList.Remove(info.Name);
					}
				};
			dataGrid.SelectionChanged += handler;
			// this needs to be done before "Unloaded" because at that point the items will have been all deselected
			MainWindow.Instance.LogicGrid.SelectionChanged += (s, ea) => dataGrid.SelectionChanged -= handler;
		}


		void PlayersList_Loaded(object sender, RoutedEventArgs e)
		{
			var unitList = (ListBox) e.Source;
			var currentLogic = MainWindow.Instance.CurrentLogic;
			if (currentLogic is UnitCreatedCondition)
			{
				unitList.BindCollection(((UnitCreatedCondition) currentLogic).Players);
			}
			else if (currentLogic is UnitFinishedCondition)
			{
				unitList.BindCollection(((UnitFinishedCondition)currentLogic).Players);
			}
		}

		void MarkerPointCanvas_Loaded(object sender, RoutedEventArgs e)
		{
			var currentLogic = MainWindow.Instance.CurrentLogic;
			if (currentLogic is MarkerPointAction)
			{
				var poleBase = new Point(0, 0);
				var flagPole = (Canvas) new Vectors().FindResource("flagPole");
				var action = (MarkerPointAction) currentLogic;
				var markerCanvas = (Canvas) e.Source;
				markerCanvas.Children.Add(flagPole);
				foreach (var unit in MainWindow.Instance.Mission.AllUnits) markerCanvas.PlaceUnit(unit);
				System.Action refreshPosition = delegate
					{
						Canvas.SetLeft(flagPole, action.X - poleBase.X);
						Canvas.SetTop(flagPole, action.Y - flagPole.Height + poleBase.Y);
					};
				refreshPosition();
				markerCanvas.MouseDown += (s, ea) =>
					{
						var mousePos = ea.GetPosition(markerCanvas);
						action.X = mousePos.X;
						action.Y = mousePos.Y;
						refreshPosition();
					};
				markerCanvas.Unloaded += (s, ea) => markerCanvas.Children.Clear();
			}
			else if (currentLogic is SetCameraPointTargetAction)
			{
				var camera = (Viewbox) new Vectors().FindResource("camera");
				var action = (SetCameraPointTargetAction) currentLogic;
				var markerCanvas = (Canvas) e.Source;
				markerCanvas.Children.Add(camera);
				foreach (var unit in MainWindow.Instance.Mission.AllUnits) markerCanvas.PlaceUnit(unit);
				System.Action refreshPosition = delegate
					{
						Canvas.SetLeft(camera, action.X - camera.Width / 2);
						Canvas.SetTop(camera, action.Y - camera.Height / 2);
					};
				markerCanvas.MouseDown += (s, ea) =>
				{
					var mousePos = ea.GetPosition(markerCanvas);
					action.X = mousePos.X;
					action.Y = mousePos.Y;
					refreshPosition();
				};
			}
		}




		void AddCounterButton_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new StringRequest {Title = "Insert counter name."};
			var result = dialog.ShowDialog();
			if (result.HasValue && result.Value) MainWindow.Instance.Mission.Counters.Add(dialog.TextBox.Text);
		}

		void RemoveCounterButton_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new StringRequest {Title = "Insert counter name."};
			var result = dialog.ShowDialog();
			if (result.HasValue && result.Value && !MainWindow.Instance.Mission.Counters.Remove(dialog.TextBox.Text))
			{
				MessageBox.Show("Error: counter " + dialog.TextBox.Text + " does not exist.");
			}
		}
	}
}