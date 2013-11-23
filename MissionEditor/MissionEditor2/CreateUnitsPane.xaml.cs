using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using CMissionLib;
using CMissionLib.Actions;
using CMissionLib.UnitSyncLib;
using System.Windows.Documents;
using System.Collections.Generic;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for CreateUnitsPane.xaml
	/// </summary>
	/// 
	/// 
	public partial class CreateUnitsPane: UserControl
	{
		CreateUnitsAction action;
		DragInfo dragInfo;
		int gridSize = 8;   //16
		DateTime mouseDownDate;
		ObservableCollection<UnitIcon> unitIcons = new ObservableCollection<UnitIcon>();
        Point boxStartPos;
        bool isBoxSelecting = false;

		public CreateUnitsPane()
		{
			InitializeComponent();
		}


		void PlaceUnitIcon(UnitStartInfo unit)
		{
			var unitIcon = new UnitIcon();
			unitIcon.DataContext = unit;
			unitIcon.Bind(Canvas.LeftProperty, unit, "X", BindingMode.OneWay, new TranslateConverter(), -8);
			unitIcon.Bind(Canvas.TopProperty, unit, "Y", BindingMode.OneWay, new TranslateConverter(), -8);
			unitIcon.MouseDown += unitIcon_MouseDown;
            unitIcon.MouseUp += unitIcon_MouseUp;
			unitIcon.UnitRequestedDelete += unitIcon_UnitRequestedDelete;
			unitIcon.UnitRequestedSetGroups += unitIcon_UnitRequestedSetGroups;
			unitIcon.UnitRequestedSetOwner += unitIcon_UnitRequestedSetOwner;

			unitCanvas.Children.Add(unitIcon);
			unitIcons.Add(unitIcon);
		}

		void RemoveUnitIcon(UnitIcon unitIcon)
		{
			BindingOperations.ClearBinding(unitIcon, Canvas.LeftProperty);
			BindingOperations.ClearBinding(unitIcon, Canvas.TopProperty);
			unitIcon.MouseDown -= unitIcon_MouseDown;
			unitIcon.UnitRequestedDelete -= unitIcon_UnitRequestedDelete;
			unitIcon.UnitRequestedSetGroups -= unitIcon_UnitRequestedSetGroups;
			unitIcon.UnitRequestedSetOwner -= unitIcon_UnitRequestedSetOwner;
			unitCanvas.Children.Remove(unitIcon);
			unitIcons.Remove(unitIcon);
		}

		double SnapToGridX(double x)
		{
			var mission = MainWindow.Instance.Mission;
			x = mission.ToIngameX(x);
			x = ((int)x/gridSize)*gridSize;
			x = mission.FromIngameX(x);
			return x;
		}

		double SnapToGridY(double y)
		{
			var mission = MainWindow.Instance.Mission;
			y = mission.ToIngameY(y);
			y = ((int)y/gridSize)*gridSize;
			y = mission.FromIngameY(y);
			return y;
		}

		void CreateUnitsPane_Loaded(object sender, RoutedEventArgs e)
		{
			action = (CreateUnitsAction)MainWindow.Instance.CurrentLogic;

			var missionUnits = MainWindow.Instance.Mission.AllUnits.ToArray();
			var triggerUnits = action.Units.ToArray();
			foreach (var unit in missionUnits)
			{
				if (triggerUnits.Contains(unit)) PlaceUnitIcon(unit);
				else UnitIcon.PlaceSimplifiedUnit(unitCanvas, unit, true);
			}
		}

		void searchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			unitDefGrid.GoToText(searchBox.Text);
		}

		void unitCanvas_MouseDown(object sender, MouseButtonEventArgs e)
		{
			mouseDownDate = DateTime.Now;
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.ChangedButton == MouseButton.Left)
            {
                boxStartPos = e.GetPosition(unitCanvas);
                //MessageBox.Show(string.Format("X = {0}, Y = {1}", boxStartPos.X, boxStartPos.Y));
                if (boxStartPos.X > 0 && boxStartPos.Y > 0)
                {
                    isBoxSelecting = true;
                    unitCanvas.CaptureMouse();

                    // Initial placement of the drag selection box.         
                    Canvas.SetLeft(selectionBox, boxStartPos.X);
                    Canvas.SetTop(selectionBox, boxStartPos.Y);
                    selectionBox.Width = 0;
                    selectionBox.Height = 0;

                    // Make the drag selection box visible.
                    selectionBox.Visibility = Visibility.Visible;
                    e.Handled = true;
                }
            }
		}

		void unitCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (unitCanvas.IsMouseCaptured)
			{
                if (dragInfo != null)
                {
                    var currentPosition = e.GetPosition(unitCanvas);
                    foreach (var element in dragInfo.Elements)
                    {
                        Positionable pos = (Positionable)element.DataContext;
                        Point p = dragInfo.ElementOrigins[element];
                        pos.X = SnapToGridX(currentPosition.X - dragInfo.MouseOrigin.X + p.X);
                        pos.Y = SnapToGridY(currentPosition.Y - dragInfo.MouseOrigin.Y + p.Y);
                    }
                }
                else if (isBoxSelecting) {
                    Point mousePos = e.GetPosition(unitCanvas);

                    if (boxStartPos.X < mousePos.X)
                    {
                        Canvas.SetLeft(selectionBox, boxStartPos.X);
                        selectionBox.Width = mousePos.X - boxStartPos.X;
                    }
                    else
                    {
                        Canvas.SetLeft(selectionBox, mousePos.X);
                        selectionBox.Width = boxStartPos.X - mousePos.X;
                    }

                    if (boxStartPos.Y < mousePos.Y)
                    {
                        Canvas.SetTop(selectionBox, boxStartPos.Y);
                        selectionBox.Height = mousePos.Y - boxStartPos.Y;
                    }
                    else
                    {
                        Canvas.SetTop(selectionBox, mousePos.Y);
                        selectionBox.Height = boxStartPos.Y - mousePos.Y;
                    }
                    if (mousePos.X < 0) mousePos.X = 0; 
                    if (mousePos.X > unitCanvas.ActualWidth) mousePos.X = unitCanvas.ActualWidth; 
                    if (mousePos.Y < 0) mousePos.Y = 0;
                    if (mousePos.Y > unitCanvas.ActualHeight) mousePos.Y = unitCanvas.ActualHeight;
                }
			}
		}

		void unitCanvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{ 
			if (dragInfo != null)
			{
				e.Handled = true;
				unitCanvas.ReleaseMouseCapture();
				dragInfo = null;
			}
            if (isBoxSelecting)
            {
                // hide and release the box
                selectionBox.Visibility = Visibility.Collapsed;
                unitCanvas.ReleaseMouseCapture();
                isBoxSelecting = false;

                Point boxEndPos = e.GetPosition(unitCanvas);
                double x1 = boxStartPos.X, y1 = boxStartPos.Y, x2 = boxEndPos.X, y2 = boxEndPos.Y;
                if (x2 < x1)
                {
                    double temp = x1;
                    x1 = x2;
                    x2 = temp;
                }
                if (y2 < y1)
                {
                    double temp = y1;
                    y1 = y2;
                    y2 = temp;
                }
                
                // clear existing selection if not Shift
                if (Keyboard.Modifiers != ModifierKeys.Shift && Keyboard.Modifiers != ModifierKeys.Control)
                {
                    List<UnitIcon> select = unitIcons.Where(i => i.IsSelected).ToList();
                    foreach (UnitIcon i in select) i.IsSelected = false;
                    select = new List<UnitIcon>();
                }
                // add to selection
                foreach (UnitIcon icon in unitIcons)
                {
                    Positionable pos = (Positionable)icon.DataContext;
                    if (pos.X >= x1 && pos.X <= x2 && pos.Y >= y1 && pos.Y <= y2)
                    {
                        icon.IsSelected = true;
                    }
                }
            }

			if (unitDefGrid.Grid.SelectedItem != null && DateTime.Now - mouseDownDate < TimeSpan.FromMilliseconds(150) && e.ChangedButton == MouseButton.Left &&
			    Keyboard.Modifiers == ModifierKeys.None)
			{
				var unitType = (UnitInfo)unitDefGrid.Grid.SelectedItem;
				var mousePos = e.GetPosition(unitCanvas);
				var player = (Player)playerListBox.SelectedItem;
				var unitStartInfo = new UnitStartInfo(unitType, player, SnapToGridX(mousePos.X), SnapToGridY(mousePos.Y));
				((INotifyPropertyChanged)unitStartInfo).PropertyChanged += (se, ea) => // fixme: leak
					{ if (ea.PropertyName == "Groups") MainWindow.Instance.Mission.RaisePropertyChanged("AllGroups"); };
				action.Units.Add(unitStartInfo);
				PlaceUnitIcon(unitStartInfo);
			}
		}

		void unitIcon_MouseDown(object sender, MouseButtonEventArgs e)
		{
			var unitIcon = (UnitIcon)e.Source;
			if (Keyboard.Modifiers == ModifierKeys.None && e.RightButton == MouseButtonState.Released)
			{
				e.Handled = true;
				if (dragInfo == null)
				{
					var thisElement = (FrameworkElement)unitIcon;
					var startPoint = e.GetPosition(unitCanvas);
                    var elements = unitIcons.Where(i => i.IsSelected).Select(x => (FrameworkElement)x).ToList();
                    if(!elements.Contains(thisElement)) elements = new List<FrameworkElement> {thisElement};

                    var origins = new Dictionary<FrameworkElement, Point>();
                    foreach (var el in elements) {
                        var pos = (Positionable)el.DataContext;
                        origins.Add(el, new Point(pos.X, pos.Y));
                    }
					if (unitCanvas.CaptureMouse()) dragInfo = new DragInfo { Elements = elements, ElementOrigins = origins, MouseOrigin = startPoint };
				}
			}
		}

        void unitIcon_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var unitIcon = (UnitIcon)e.Source;
            if (e.ChangedButton == MouseButton.Left)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control) unitIcon.IsSelected = !unitIcon.IsSelected;
                //else
                //{
                //    foreach (UnitIcon i in unitIcons.Where(i => i.IsSelected)) i.IsSelected = false;
                //    unitIcon.IsSelected = true;
                //}
            }
        }

		void unitIcon_UnitRequestedDelete(object sender, UnitEventArgs e)
		{
			var selectedUnits = unitIcons.Where(i => i.IsSelected).ToList();
			if (selectedUnits.Contains((UnitIcon)e.Source))
			{
				foreach (var unitIcon in selectedUnits)
				{
					var unit = (UnitStartInfo)unitIcon.DataContext;
					RemoveUnitIcon(unitIcon);
					action.Units.Remove(unit);
				}
			}
			else
			{
				var unitIcon = (UnitIcon)e.Source;
				var unit = (UnitStartInfo)unitIcon.DataContext;
				RemoveUnitIcon(unitIcon);
				action.Units.Remove(unit);
			}
		}

		void unitIcon_UnitRequestedSetGroups(object sender, UnitEventArgs e)
		{
            var source = (UnitIcon)e.Source;
			var selectedUnits = unitIcons.Where(i => i.IsSelected).ToList();
            if (!selectedUnits.Contains(source)) selectedUnits = new List<UnitIcon> { source };

			var groupsString = selectedUnits.Count() == 1 ? String.Join(",", e.UnitInfo.Groups) : String.Empty;
			groupsString = Utils.ShowStringDialog("Insert groups (separate multiple groups with commas).", groupsString);
			if (groupsString != null)
			{
				foreach (var unit in selectedUnits)
				{
					var unitStartInfo = (UnitStartInfo)unit.DataContext;
					unitStartInfo.Groups = new ObservableCollection<string>(groupsString.Split(','));
				}
			}
		}

		void unitIcon_UnitRequestedSetOwner(object sender, EventArgs<Player> e)
		{
            var source = (UnitIcon)sender;
			var selectedUnits = unitIcons.Where(i => i.IsSelected).ToList();
			if (!selectedUnits.Contains(source)) selectedUnits = new List<UnitIcon> { source };
			foreach (var unit in selectedUnits) unit.Unit.Player = e.Data;
		}
	}
}