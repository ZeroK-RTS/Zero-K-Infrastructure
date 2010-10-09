using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
		ListBox unitListBox;
		DragInfo dragInfo = null;
		Canvas unitCanvas;

		public CreateUnitsPane()
		{
			InitializeComponent();
		}


		void CreateUnitsPane_Loaded(object sender, RoutedEventArgs e)
		{

			var playerListBox = (ListBox) FindResource("playerListBox");
			unitListBox = (ListBox) FindResource("unitListBox");
			unitCanvas = (Canvas) this.FindTag("unitCanvas");
			var unitDefGrid = ((UnitDefsGrid) FindResource("unitDefGrid")).Grid;
			action = (CreateUnitsAction) MainWindow.Instance.CurrentLogic;

			// place blurry background units
			var missionUnits = MainWindow.Instance.Mission.AllUnits.ToArray();
			var triggerUnits = action.Units.ToArray();
			foreach (var unit in missionUnits)
			{
				if (!triggerUnits.Contains(unit))
				{
					UnitBackgroundCanvas.PlaceUnit(unit, true);
				}
			}

			unitCanvas.PreviewMouseUp += (s, ea) =>
				{
					if (dragInfo != null)
					{
						ea.Handled = true;
						unitCanvas.ReleaseMouseCapture();
						dragInfo = null;
					}
				};

			unitCanvas.PreviewMouseMove += (s, ea) =>
				{
					if (dragInfo != null && unitCanvas.IsMouseCaptured)
					{
						var currentPosition = ea.GetPosition(unitCanvas);
						var pos = (Positionable) dragInfo.Element.DataContext;
						pos.X = currentPosition.X - dragInfo.MouseOrigin.X + dragInfo.ElementOrigin.X;
						pos.Y = currentPosition.Y - dragInfo.MouseOrigin.Y + dragInfo.ElementOrigin.Y;
					}
				};

			// create new unit

			unitCanvas.MouseDown += (s, eventArgs) =>
				{
					if (unitDefGrid.SelectedItem != null)
					{
						var unitType = (UnitInfo) unitDefGrid.SelectedItem;
						var mousePos = eventArgs.GetPosition(unitCanvas);
						var player = (Player) playerListBox.SelectedItem;
						var unitStartInfo = new UnitStartInfo(unitType, player, mousePos.X, mousePos.Y);
						((INotifyPropertyChanged) unitStartInfo).PropertyChanged += (se, ea) => // fixme: leak
							{
								if (ea.PropertyName == "Groups")
								{
									MainWindow.Instance.Mission.RaisePropertyChanged("AllGroups");
								}
							};
						action.Units.Add(unitStartInfo);
						eventArgs.Handled = true;
					}
				};
		}

		void Unit_RequestedDelete(object sender, UnitEventArgs e)
		{
			foreach (var unit in unitListBox.SelectedItems.Cast<UnitStartInfo>().ToArray())
			{
				action.Units.Remove(unit);
			}
		}

		void Unit_RequestedSetGroups(object sender, UnitEventArgs e)
		{
			var groupsString = unitListBox.SelectedItems.Count == 1 ? String.Join(",", e.UnitInfo.Groups) : String.Empty;
			groupsString = Utils.ShowStringDialog("Insert groups (separate multiple groups with commas).", groupsString);
			if (groupsString != null)
			{
				foreach (UnitStartInfo unit in unitListBox.SelectedItems)
				{
					unit.Groups = new ObservableCollection<string>(groupsString.Split(','));
				}
			}
		}

		void UnitIcon_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.None && e.RightButton == MouseButtonState.Released)
			{
				e.Handled = true;
				if (dragInfo == null)
				{
					var element = (FrameworkElement) e.Source;
					var pos = (Positionable) element.DataContext;
					var origin = new Point(pos.X, pos.Y);
					var startPoint = e.GetPosition(unitCanvas);
					if (unitCanvas.CaptureMouse())
					{
						dragInfo = new DragInfo {Element = element, ElementOrigin = origin, MouseOrigin = startPoint};
					}
				}
			}
		}
	}
}