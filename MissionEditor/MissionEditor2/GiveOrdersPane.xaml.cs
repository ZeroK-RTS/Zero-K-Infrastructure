using System;
using System.Collections.Specialized;
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
using Action = System.Action;
using Trigger = CMissionLib.Trigger;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for GiveOrdersPane.xaml
	/// </summary>
	public partial class GiveOrdersPane : UserControl
	{
		DragInfo dragInfo;
		GiveOrdersAction action;
		DateTime canvasMouseDownDate;
		Trigger trigger;

		public GiveOrdersPane()
		{
			InitializeComponent();
		}

		void OrderMouseDown(object sender, MouseButtonEventArgs eventArgs)
		{
			if (Keyboard.Modifiers == ModifierKeys.None && eventArgs.RightButton == MouseButtonState.Released)
			{
				eventArgs.Handled = true;
				if (dragInfo == null)
				{
					var element = (FrameworkElement) eventArgs.Source;
					var pos = (Positionable) element.DataContext;
					var origin = new Point(pos.X, pos.Y);
					var startPoint = eventArgs.GetPosition(canvas);
					if (canvas.CaptureMouse())
					{
						dragInfo = new DragInfo {Element = element, ElementOrigin = origin, MouseOrigin = startPoint};
					}
				}
			}
		}

		void OrderGroupsListLoaded(object sender, RoutedEventArgs e)
		{
			var list = (ListBox) e.Source;
			list.BindCollection(action.Groups);
		}

		private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
		{
			canvasMouseDownDate = DateTime.Now;
		}

		void CreateNewOrder(Point position) 
		{
			var selectedItem = (ListBoxItem) orderTypeListBox.SelectedItem;
			var orderTypeName = (string)selectedItem.Content;
			IOrder newOrder;
			switch (orderTypeName)
			{
				case "Move":
					newOrder = new MoveOrder(position.X, position.Y);
					break;
				case "Patrol":
					newOrder = new PatrolOrder(position.X, position.Y);
					break;
				case "Stop":
					newOrder = new StopOrder();
					break;
				case "Fight":
					newOrder = new FightOrder(position.X, position.Y);
					break;
				case "Attack":
					newOrder = new AttackOrder(position.X, position.Y);
					break;
				case "Enable Repeat Mode":
					newOrder = new RepeatOrder(1);
					break;
				case "Disable Repeat Mode":
					newOrder = new RepeatOrder(0);
					break;
				default:
					throw new Exception("Ordertype not expected: " + orderTypeName);
			}
			action.Orders.Add(newOrder);
		}

		private void canvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (dragInfo == null)
			{
				if (DateTime.Now - canvasMouseDownDate < TimeSpan.FromMilliseconds(150))
				{
					var mousePos = e.GetPosition(canvas);
					CreateNewOrder(mousePos);
				}
			}
			else
			{
				e.Handled = true;
				canvas.ReleaseMouseCapture();
				dragInfo = null;
			}
		}

		private void UserControl_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (dragInfo != null && canvas.IsMouseCaptured)
			{
				var currentPosition = e.GetPosition(canvas);
				var pos = (Positionable)dragInfo.Element.DataContext;
				pos.X = currentPosition.X - dragInfo.MouseOrigin.X + dragInfo.ElementOrigin.X;
				pos.Y = currentPosition.Y - dragInfo.MouseOrigin.Y + dragInfo.ElementOrigin.Y;
			}
		}

		void NewBoundLine(Positionable positionable1, Positionable positionable2)
		{
			var line = new Line
			{
				Stroke = Brushes.Red,
				StrokeThickness = 2.5,
				Opacity = 0.5,
				StrokeStartLineCap = PenLineCap.Round,
				StrokeEndLineCap = PenLineCap.Round
			};
			line.Bind(Line.X1Property, positionable1, "X", BindingMode.OneWay);
			line.Bind(Line.Y1Property, positionable1, "Y", BindingMode.OneWay);
			line.Bind(Line.X2Property, positionable2, "X", BindingMode.OneWay);
			line.Bind(Line.Y2Property, positionable2, "Y", BindingMode.OneWay);
			canvas.Children.Add(line);
		}

		void UpdateLines()
		{

			foreach (var child in canvas.Children.Cast<UIElement>().Where(e => !(e is Border)).ToArray())
			{
				canvas.Children.Remove(child);
			}
			
			foreach (var order in action.Orders.OfType<Positionable>())
			{
				var icon = new OrderIcon();
				icon.DataContext = order;
				icon.Bind(Canvas.LeftProperty, order, "X", BindingMode.OneWay, new TranslateConverter(), -4);
				icon.Bind(Canvas.TopProperty, order, "Y", BindingMode.OneWay, new TranslateConverter(), -2);
				Canvas.SetZIndex(icon, 10);
				canvas.Children.Add(icon);
				icon.MouseDown += OrderMouseDown;
			}


			var firstPositionable = action.Orders.OfType<Positionable>().FirstOrDefault();
			if (firstPositionable == null) return;


			var createUnitsActions = trigger.Logic.OfType<CreateUnitsAction>();
			var previousActions = createUnitsActions.Where(a => trigger.Logic.IndexOf(a) < trigger.Logic.IndexOf(action));
			var previousUnits = previousActions.SelectMany(a => a.Units).ToArray();


			var firstUnits = action.Groups.Any()
			                 	? previousUnits.Where(u => u.Groups.Any(t => action.Groups.Contains(t)))
			                 	: previousUnits;
			foreach (var unit in firstUnits)
			{
				NewBoundLine(unit, firstPositionable);
			}
			// draw lines from all affected units to the first order
			var positionables = action.Orders.OfType<Positionable>().ToArray();
			// draw lines between the orders
			for (var i = 0; i + 1 < positionables.Length; i++)
			{
				NewBoundLine(positionables[i], positionables[i + 1]);
			}


		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			action = (GiveOrdersAction)MainWindow.Instance.CurrentLogic;
			trigger = MainWindow.Instance.Mission.FindLogicOwner(action);

			var missionUnits = MainWindow.Instance.Mission.AllUnits.ToArray();
			var triggerUnits = trigger.AllUnits.ToArray();
			foreach (var unit in missionUnits)
			{
				UnitIcon.PlaceSimplifiedUnit(canvas, unit, !triggerUnits.Contains(unit));
			}

			UpdateLines();

			action.Groups.CollectionChanged += (s, ea) => UpdateLines();
			trigger.Logic.CollectionChanged += (s, ea) => UpdateLines();

			NotifyCollectionChangedEventHandler handler = (s, ea) => UpdateLines();
			action.Orders.CollectionChanged += handler;
			canvas.Unloaded += (s, ea) => action.Orders.CollectionChanged -= handler;
		}
	}
}