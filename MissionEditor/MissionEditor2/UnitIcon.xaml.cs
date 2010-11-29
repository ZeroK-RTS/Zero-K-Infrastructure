using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using CMissionLib;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for UnitIcon.xaml
	/// </summary>
	public partial class UnitIcon: UserControl
	{
		public UnitIcon()
		{
			InitializeComponent();
		}

		bool isSelected;
		public bool IsSelected
		{
			get { return isSelected; }
			set
			{
				selectionBorder.Visibility = value ? Visibility.Visible : Visibility.Hidden;
				isSelected = value;
			}
		}

#if false // does not work when the rotatetransform is applied

        public static DependencyObject GetParentObject(DependencyObject child)
        {
            if (child == null) return null;
            var contentElement = child as ContentElement;
            if (contentElement != null) {
                var parent = ContentOperations.GetParent(contentElement);
                if (parent != null) return parent;
                var fce = contentElement as FrameworkContentElement;
                return fce != null ? fce.Parent : null;
            }
            return VisualTreeHelper.GetParent(child);
        }

        public static T TryFindParent<T>(DependencyObject child)  where T : DependencyObject
        {
            var parentObject = GetParentObject(child);
            if (parentObject == null) return null;
            var parent = parentObject as T;
            return parent ?? TryFindParent<T>(parentObject);
        }



        void onDragDelta(object sender, DragDeltaEventArgs e)
        {
            var item = TryFindParent<ListBoxItem>(this);
            if (item == null) return;
            Canvas.SetLeft(item, Canvas.GetLeft(item) + e.HorizontalChange);
            Canvas.SetTop(item, Canvas.GetTop(item) + e.VerticalChange);
            e.Handled = true;
        }

#endif

		// adds a new simplified unit icon to a canvas
		public static void PlaceSimplifiedUnit(Canvas canvas, UnitStartInfo unit, bool isBlurred = false)
		{
			var border = new Border { BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(1), Height = 16, Width = 16 };
			if (isBlurred) border.Opacity = 0.2;
			canvas.Children.Add(border);
			border.Bind(Border.BorderBrushProperty, unit, "Player.ColorBrush", BindingMode.OneWay);
			Canvas.SetLeft(border, unit.X - border.Width/2);
			Canvas.SetTop(border, unit.Y - border.Height/2);
			Panel.SetZIndex(border, -10);
			var image = new Image { Source = unit.UnitDef.BuildPic };
			// make the icons have the correct size
			var mission = MainWindow.Instance.Mission;
			border.RenderTransform = new ScaleTransform
			                         {
			                         	CenterX = 8,
			                         	CenterY = 8,
			                         	ScaleX = 1/16.0*mission.FromIngameX(unit.UnitDef.FootprintX*16),
			                         	ScaleY = 1/16.0*mission.FromIngameY(unit.UnitDef.FootprintY*16)
			                         };
			border.Child = image;
		}

		void onRotateDelta(object sender, DragDeltaEventArgs e)
		{
			var unit = (UnitStartInfo)DataContext;
			var newHeading = unit.Heading + e.HorizontalChange;
			while (newHeading > 360) newHeading = newHeading - 360;
			while (newHeading < 0) newHeading = newHeading + 360;
			unit.Heading = newHeading;
			e.Handled = true;
		}

		public static readonly RoutedEvent UnitRequestedDeleteEvent = EventManager.RegisterRoutedEvent("UnitRequestedDelete",
		                                                                                               RoutingStrategy.Direct,
		                                                                                               typeof(UnitEventHandler),
		                                                                                               typeof(UnitIcon));

		public static readonly RoutedEvent UnitRequestedSetGroupsEvent = EventManager.RegisterRoutedEvent("UnitRequestedSetGroups",
		                                                                                                  RoutingStrategy.Direct,
		                                                                                                  typeof(UnitEventHandler),
		                                                                                                  typeof(UnitIcon));

		public event UnitEventHandler UnitRequestedSetGroups { add { AddHandler(UnitRequestedSetGroupsEvent, value); } remove { RemoveHandler(UnitRequestedSetGroupsEvent, value); } }

		public event UnitEventHandler UnitRequestedDelete { add { AddHandler(UnitRequestedDeleteEvent, value); } remove { RemoveHandler(UnitRequestedDeleteEvent, value); } }

		public event EventHandler<EventArgs<Player>> UnitRequestedSetOwner = delegate { };


		void RaiseUnitRequestedDeleteEvent()
		{
			var unitInfo = (UnitStartInfo)DataContext;
			var newEventArgs = new UnitEventArgs(unitInfo, UnitRequestedDeleteEvent);
			RaiseEvent(newEventArgs);
		}

		void RaiseUnitRequestedSetGroupsEvent()
		{
			var unitInfo = (UnitStartInfo)DataContext;
			var newEventArgs = new UnitEventArgs(unitInfo, UnitRequestedSetGroupsEvent);
			RaiseEvent(newEventArgs);
		}

		void DeleteItem_Click(object sender, RoutedEventArgs e)
		{
			RaiseUnitRequestedDeleteEvent();
		}

		void UserControl_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			setOwnerItem.Items.Clear();
			foreach (var player in MainWindow.Instance.Mission.Players)
			{
				var playerItem = new MenuItem { Header = player.Name, IsChecked = player == Unit.Player };
				var p = player;
				playerItem.Click += (s, ea) => UnitRequestedSetOwner(this, new EventArgs<Player>(p));
				setOwnerItem.Items.Add(playerItem);
			}
		}

		void SetGroupsItem_Click(object sender, RoutedEventArgs e)
		{
			RaiseUnitRequestedSetGroupsEvent();
		}

		public UnitStartInfo Unit { get { return (UnitStartInfo)DataContext; } }

		void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			// make the icons have the correct size
			var mission = MainWindow.Instance.Mission;
			ScaleTransform.ScaleX = 1/16.0*mission.FromIngameX(Unit.UnitDef.FootprintX*16);
			ScaleTransform.ScaleY = 1/16.0*mission.FromIngameY(Unit.UnitDef.FootprintY*16);
		}
	}
}