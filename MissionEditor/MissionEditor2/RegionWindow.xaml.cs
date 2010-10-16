using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CMissionLib;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for RegionWindow.xaml
	/// </summary>
	public partial class RegionWindow : Window
	{
		public RegionWindow(Region region)
		{
			
			InitializeComponent();
			DataContext = region;
			Region = region;
		}

		List<AreaDragInfo> areaInfos = new List<AreaDragInfo>();
		const double centerSize = 10.0;
		Region Region { get; set; }
		AreaDragInfo dragInfo = new NoArea();

		void UserControl_Loaded(object sender, RoutedEventArgs e)
		{

			foreach (var unit in MainWindow.Instance.Mission.AllUnits)
			{
				canvas.PlaceUnit(unit);
			}

			foreach (var area in Region.Areas)
			{
				if (area is Cylinder)
				{
					var cylinder = (Cylinder)area;
					AddCylinder(cylinder);
				}
				else if (area is RectangularArea)
				{
					AddRectangle((RectangularArea)area);
				}
				else throw new Exception("Unexpected Area");
			}
		}


		CircleDragInfo AddCylinder(Cylinder area)
		{
			var info = new CircleDragInfo
			{
				Area = area,
				Center = new Ellipse { Fill = Brushes.Yellow, Width = centerSize, Height = centerSize, Opacity = 0.5 },
				Circle = new Ellipse
				{
					Fill = Brushes.Red,
					StrokeThickness = 5,
					Opacity = 0.3,
					Stroke = Brushes.Yellow,
					Width = area.R * 2,
					Height = area.R * 2
				}
			};
			Canvas.SetLeft(info.Circle, area.X - area.R);
			Canvas.SetTop(info.Circle, area.Y - area.R);
			Canvas.SetLeft(info.Center, area.X - centerSize / 2.0);
			Canvas.SetTop(info.Center, area.Y - centerSize / 2.0);
			areaInfos.Add(info);
			canvas.Children.Add(info.Circle);
			canvas.Children.Add(info.Center);
			return info;
		}

		RectangleDragInfo AddRectangle(RectangularArea area)
		{
			var info = new RectangleDragInfo
			{
				Area = area,
				Rectangle = new Rectangle
				{
					Fill = Brushes.Red,
					StrokeThickness = 5,
					Opacity = 0.3,
					Stroke = Brushes.Yellow,
					Width = area.Width,
					Height = area.Height
				},
				StartPoint = new Point(area.X, area.Y)
			};
			Canvas.SetLeft(info.Rectangle, area.X);
			Canvas.SetTop(info.Rectangle, area.Y);
			canvas.Children.Add(info.Rectangle);
			areaInfos.Add(info);
			return info;
		}

		void button_Click(object sender, RoutedEventArgs e)
		{
			// keep only the pressed button checked
			circleButton.IsChecked = e.Source == circleButton;
			rectangleButton.IsChecked = e.Source == rectangleButton;
			deleteButton.IsChecked = e.Source == deleteButton;
		}

		void canvas_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.None)
			{
				e.Handled = true;
				var startPoint = e.GetPosition(canvas);
				if (dragInfo is NoArea)
				{
					if (circleButton.IsChecked == true)
					{
						// create circle
						canvas.CaptureMouse();
						var area = new Cylinder { X = startPoint.X, Y = startPoint.Y, R = 0 };
						var info = AddCylinder(area);
						dragInfo = info;
						Region.Areas.Add(info.Area);
					}
					else if (rectangleButton.IsChecked == true)
					{
						// create rectangle
						canvas.CaptureMouse();
						var area = new RectangularArea { X = startPoint.X, Y = startPoint.Y, Height = 0, Width = 0 };
						var info = AddRectangle(area);
						dragInfo = info;
						Region.Areas.Add(info.Area);
					}
					else if (deleteButton.IsChecked == true && e.Source is Shape)
					{
						// delete shape
						foreach (var info in areaInfos.ToArray())
						{
							// remove area
							var circleDragInfo = info as CircleDragInfo;
							if (circleDragInfo != null &&
								(circleDragInfo.Center == e.Source || circleDragInfo.Circle == e.Source))
							{
								areaInfos.Remove(circleDragInfo);
								Region.Areas.Remove(circleDragInfo.Area);
								canvas.Children.Remove(circleDragInfo.Center);
								canvas.Children.Remove(circleDragInfo.Circle);
							}
							var rectangleDragInfo = info as RectangleDragInfo;
							if (rectangleDragInfo != null && (rectangleDragInfo.Rectangle == e.Source))
							{
								areaInfos.Remove(rectangleDragInfo);
								Region.Areas.Remove(rectangleDragInfo.Area);
								canvas.Children.Remove(rectangleDragInfo.Rectangle);
							}
							if (info is NoArea) throw new Exception("NoArea should not be in the list");
						}
					}
				}
			}
		}

		void UserControl_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			var mousePosition = e.GetPosition(canvas);
			if (canvas.IsMouseCaptured)
			{
				if (dragInfo is CircleDragInfo)
				{
					var info = (CircleDragInfo)dragInfo;
					var r = Utils.GetDistance(info.Area.X, info.Area.Y, mousePosition.X, mousePosition.Y);
					info.Area.R = r;
					info.Circle.Width = r * 2;
					info.Circle.Height = r * 2;
					Canvas.SetLeft(info.Circle, info.Area.X - r);
					Canvas.SetTop(info.Circle, info.Area.Y - r);
				}
				else if (dragInfo is RectangleDragInfo)
				{
					var info = (RectangleDragInfo)dragInfo;
					var dx = mousePosition.X - info.StartPoint.X;
					var dy = mousePosition.Y - info.StartPoint.Y;

					if (dx > 0)
					{
						Canvas.SetLeft(info.Rectangle, info.StartPoint.X);
						info.Rectangle.Width = dx;
						info.Area.X = info.StartPoint.X;
						info.Area.Width = dx;
					}
					else
					{
						Canvas.SetLeft(info.Rectangle, info.StartPoint.X + dx);
						info.Rectangle.Width = -dx;
						info.Area.X = info.StartPoint.X + dx;
						info.Area.Width = -dx;
					}

					if (dy > 0)
					{
						Canvas.SetTop(info.Rectangle, info.StartPoint.Y);
						info.Rectangle.Height = dy;
						info.Area.Y = info.StartPoint.Y;
						info.Area.Height = dy;
					}
					else
					{
						Canvas.SetTop(info.Rectangle, info.StartPoint.Y + dy);
						info.Rectangle.Height = -dy;
						info.Area.Y = info.StartPoint.Y + dy;
						info.Area.Height = -dy;
					}
				}
			}
		}

		void canvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (dragInfo is NoArea) return;
			e.Handled = true;
			canvas.ReleaseMouseCapture();
			dragInfo = new NoArea();
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show("Are you sure you want to delete the region?", "Confirm delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				MainWindow.Instance.Mission.Regions.Remove(Region);
				Close();
			}

		}
	}
}
