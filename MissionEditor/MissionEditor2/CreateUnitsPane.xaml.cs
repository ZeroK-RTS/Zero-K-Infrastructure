using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CMissionLib;
using CMissionLib.Actions;
using CMissionLib.UnitSyncLib;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for CreateUnitsPane.xaml
	/// </summary>
	/// 
	/// 
	public partial class CreateUnitsPane : UserControl
	{
		CreateUnitsAction action;
		DragInfo dragInfo;
		ObservableCollection<UnitIcon> unitIcons = new ObservableCollection<UnitIcon>();
		DateTime mouseDownDate;

		public CreateUnitsPane()
		{
			InitializeComponent();
		}



		void CreateUnitsPane_Loaded(object sender, RoutedEventArgs e)
		{
			action = (CreateUnitsAction) MainWindow.Instance.CurrentLogic;


			var missionUnits = MainWindow.Instance.Mission.AllUnits.ToArray();
			var triggerUnits = action.Units.ToArray();
			foreach (var unit in missionUnits)
			{
				if (triggerUnits.Contains(unit))
				{
					PlaceUnitIcon(unit);
				}
				else
				{
					UnitIcon.PlaceSimplifiedUnit(unitCanvas, unit, true);
				}
			}
		}

		int gridSize = 16;

		double SnapToGridX(double x)
		{
			var mission = MainWindow.Instance.Mission;
			x = mission.ToIngameX(x);
			x = ((int) x/gridSize) * gridSize;
			x = mission.FromIngameX(x);
			return x;
		}

		double SnapToGridY(double y)
		{
			var mission = MainWindow.Instance.Mission;
			y = mission.ToIngameY(y);
			y = ((int)y / gridSize) * gridSize;
			y = mission.FromIngameY(y);
			return y;
		}



		void PlaceUnitIcon(UnitStartInfo unit) 
		{
			var unitIcon = new UnitIcon();
			unitIcon.DataContext = unit;
			unitIcon.Bind(Canvas.LeftProperty, unit, "X", BindingMode.OneWay, new TranslateConverter(), -8);
			unitIcon.Bind(Canvas.TopProperty, unit, "Y", BindingMode.OneWay, new TranslateConverter(), -8);
			unitIcon.MouseDown += unitIcon_MouseDown;
			unitIcon.UnitRequestedDelete += unitIcon_UnitRequestedDelete;
			unitIcon.UnitRequestedSetGroups += unitIcon_UnitRequestedSetGroups;

			unitCanvas.Children.Add(unitIcon);
			unitIcons.Add(unitIcon);
		}


		void unitIcon_UnitRequestedSetGroups(object sender, UnitEventArgs e)
		{
			var selectedUnits = unitIcons.Where(i => i.IsSelected).ToArray();
			if (!selectedUnits.Any())
			{
				selectedUnits = new []{(UnitIcon) e.Source};
			}
			var groupsString = selectedUnits.Count() == 1 ? String.Join(",", e.UnitInfo.Groups) : String.Empty;
			groupsString = Utils.ShowStringDialog("Insert groups (separate multiple groups with commas).", groupsString);
			if (groupsString != null)
			{
				foreach (var unit in selectedUnits)
				{
					var unitStartInfo = (UnitStartInfo) unit.DataContext;
					unitStartInfo.Groups = new ObservableCollection<string>(groupsString.Split(','));
				}
			}
		}

		void unitIcon_UnitRequestedDelete(object sender, UnitEventArgs e)
		{
			var selectedUnits = unitIcons.Where(i => i.IsSelected).ToArray();
			if (selectedUnits.Any())
			{
				foreach (var unitIcon in selectedUnits)
				{
					var unit = (UnitStartInfo) unitIcon.DataContext;
					RemoveUnitIcon(unitIcon);
					action.Units.Remove(unit);
				}
			} 
			else
			{
				var unitIcon = (UnitIcon) e.Source;
				var unit = (UnitStartInfo)unitIcon.DataContext;
				RemoveUnitIcon(unitIcon);
				action.Units.Remove(unit);
			}
		}

		void RemoveUnitIcon(UnitIcon unitIcon) 
		{
			BindingOperations.ClearBinding(unitIcon, Canvas.LeftProperty);
			BindingOperations.ClearBinding(unitIcon, Canvas.TopProperty);
			unitIcon.MouseDown -= unitIcon_MouseDown;
			unitIcon.UnitRequestedDelete -= unitIcon_UnitRequestedDelete;
			unitIcon.UnitRequestedSetGroups -= unitIcon_UnitRequestedSetGroups;
			unitCanvas.Children.Remove(unitIcon);
			unitIcons.Remove(unitIcon);
		}

		void unitIcon_MouseDown(object sender, MouseButtonEventArgs e)
		{
			var unitIcon = (UnitIcon)e.Source;
			if (Keyboard.Modifiers == ModifierKeys.None && e.RightButton == MouseButtonState.Released)
			{
				e.Handled = true;
				if (dragInfo == null)
				{
					var element = (FrameworkElement)e.Source;
					var pos = (Positionable)element.DataContext;
					var origin = new Point(pos.X, pos.Y);
					var startPoint = e.GetPosition(unitCanvas);
					if (unitCanvas.CaptureMouse())
					{
						dragInfo = new DragInfo { Element = element, ElementOrigin = origin, MouseOrigin = startPoint };
					}
				}
			} 
			else if (Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control)
			{
				unitIcon.IsSelected = !unitIcon.IsSelected;
			}

		}

		private void unitCanvas_MouseDown(object sender, MouseButtonEventArgs e)
		{
			mouseDownDate = DateTime.Now;
		}

		private void unitCanvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (dragInfo != null)
			{
				e.Handled = true;
				unitCanvas.ReleaseMouseCapture();
				dragInfo = null;
			}
			Debug.WriteLine(DateTime.Now - mouseDownDate);
			if (unitDefGrid.Grid.SelectedItem != null && DateTime.Now - mouseDownDate < TimeSpan.FromMilliseconds(150) && e.ChangedButton == MouseButton.Left)
			{
				var unitType = (UnitInfo)unitDefGrid.Grid.SelectedItem;
				var mousePos = e.GetPosition(unitCanvas);
				var player = (Player)playerListBox.SelectedItem;
				var unitStartInfo = new UnitStartInfo(unitType, player, SnapToGridX(mousePos.X), SnapToGridY(mousePos.Y));
				((INotifyPropertyChanged)unitStartInfo).PropertyChanged += (se, ea) => // fixme: leak
				{
					if (ea.PropertyName == "Groups")
					{
						MainWindow.Instance.Mission.RaisePropertyChanged("AllGroups");
					}
				};
				action.Units.Add(unitStartInfo);
				PlaceUnitIcon(unitStartInfo);
			}
		}

		private void unitCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (dragInfo != null && unitCanvas.IsMouseCaptured)
			{
				var currentPosition = e.GetPosition(unitCanvas);
				var pos = (Positionable)dragInfo.Element.DataContext;
				pos.X = SnapToGridX(currentPosition.X - dragInfo.MouseOrigin.X + dragInfo.ElementOrigin.X);
				pos.Y = SnapToGridY(currentPosition.Y - dragInfo.MouseOrigin.Y + dragInfo.ElementOrigin.Y);
			}
		}
	}
}