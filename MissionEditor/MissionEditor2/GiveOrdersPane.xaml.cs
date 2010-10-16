using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CMissionLib;
using CMissionLib.Actions;
using Action = System.Action;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for GiveOrdersPane.xaml
	/// </summary>
	public partial class GiveOrdersPane : UserControl
	{
		DragInfo dragInfo;
		Canvas transparentCanvas;

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
					var startPoint = eventArgs.GetPosition(transparentCanvas);
					if (transparentCanvas.CaptureMouse())
					{
						dragInfo = new DragInfo {Element = element, ElementOrigin = origin, MouseOrigin = startPoint};
					}
				}
			}
		}

		void TransparentCanvasLoaded(object sender, RoutedEventArgs e)
		{
			transparentCanvas = (Canvas) e.Source;
			if (MainWindow.Instance.CurrentLogic is GiveOrdersAction)
			{
				// new order creation in GiveOrdersAction map        
				transparentCanvas.MouseDown += (s, eventArgs) =>
					{
						if (MainWindow.Instance.CurrentLogic is GiveOrdersAction)
						{
							var ordersTypeListBox = (ListBox) FindResource("orderTypeListBox");
							var selectedItem = (ListBoxItem) ordersTypeListBox.SelectedItem;
							var orderTypeName = (string) selectedItem.Content;
							var mousePos = eventArgs.GetPosition(transparentCanvas);
							var action = (GiveOrdersAction) MainWindow.Instance.CurrentLogic;
							IOrder newOrder;
							switch (orderTypeName)
							{
								case "Move":
									newOrder = new MoveOrder(mousePos.X, mousePos.Y);
									break;
								case "Patrol":
									newOrder = new PatrolOrder(mousePos.X, mousePos.Y);
									break;
								case "Stop":
									newOrder = new StopOrder();
									break;
								case "Fight":
									newOrder = new FightOrder(mousePos.X, mousePos.Y);
									break;
								case "Attack":
									newOrder = new AttackOrder(mousePos.X, mousePos.Y);
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
					};

				// set-up event handling for the unit placement map
				transparentCanvas.PreviewMouseUp += (s, eventArgs) =>
					{
						if (dragInfo != null)
						{
							eventArgs.Handled = true;
							transparentCanvas.ReleaseMouseCapture();
							dragInfo = null;
						}
					};

				PreviewMouseMove += (s, eventArgs) =>
					{
						if (dragInfo != null && transparentCanvas.IsMouseCaptured)
						{
							var currentPosition = eventArgs.GetPosition(transparentCanvas);
							var pos = (Positionable) dragInfo.Element.DataContext;
							pos.X = currentPosition.X - dragInfo.MouseOrigin.X + dragInfo.ElementOrigin.X;
							pos.Y = currentPosition.Y - dragInfo.MouseOrigin.Y + dragInfo.ElementOrigin.Y;
						}
					};
			}
		}

		void OrderGroupsListLoaded(object sender, RoutedEventArgs e)
		{
			var action = (GiveOrdersAction) MainWindow.Instance.CurrentLogic;
			var list = (ListBox) e.Source;
			list.BindCollection(action.Groups);
		}

		void OrderLineCanvasLoaded(object sender, RoutedEventArgs e)
		{
			var canvas = (Canvas) e.Source;
			canvas.Children.Clear();
			var orderAction = (GiveOrdersAction) MainWindow.Instance.CurrentLogic;
			var trigger = MainWindow.Instance.CurrentTrigger;

			var missionUnits = MainWindow.Instance.Mission.AllUnits.ToArray();
			var triggerUnits = trigger.AllUnits.ToArray();

			foreach (var borders in canvas.Children.OfType<Border>().ToArray())
			{
				canvas.Children.Remove(borders);
			}
			foreach (var unit in missionUnits)
			{
				canvas.PlaceUnit(unit, !triggerUnits.Contains(unit));
			}

			Action<Positionable, Positionable> newBoundLine = (positionable1, positionable2) =>
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
				};

			Action updateLines = delegate
				{
					foreach (var lines in canvas.Children.OfType<Line>().ToArray())
					{
						canvas.Children.Remove(lines);
					}
					var firstPositionable = orderAction.Orders.OfType<Positionable>().FirstOrDefault();
					if (firstPositionable != null)
					{
						var createUnitsActions = trigger.Logic.OfType<CreateUnitsAction>();
						var previousActions = createUnitsActions.Where(a => trigger.Logic.IndexOf(a) < trigger.Logic.IndexOf(orderAction));
						var previousUnits = previousActions.SelectMany(a => a.Units);
						var firstUnits = orderAction.Groups.Any()
						                 	? previousUnits.Where(u => u.Groups.Any(t => orderAction.Groups.Contains(t)))
						                 	: previousUnits;
						foreach (var unit in firstUnits)
						{
							newBoundLine(unit, firstPositionable);
						}
						// draw lines from all affected units to the first order
						var positionables = orderAction.Orders.OfType<Positionable>().ToArray();
						// draw lines between the orders
						for (var i = 0; i + 1 < positionables.Length; i++)
						{
							newBoundLine(positionables[i], positionables[i + 1]);
						}
					}
				};

			updateLines();

			orderAction.Groups.CollectionChanged += (s, ea) => updateLines();
			trigger.Logic.CollectionChanged += (s, ea) => updateLines();

			NotifyCollectionChangedEventHandler handler = (s, ea) => updateLines();
			orderAction.Orders.CollectionChanged += handler;
			canvas.Unloaded += (s, ea) => orderAction.Orders.CollectionChanged -= handler;
		}
	}
}