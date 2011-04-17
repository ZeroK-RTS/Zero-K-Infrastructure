using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GalaxyDesigner
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow: Window
	{
		Line dragLine;
		readonly string[] fictionalNames;
		GalaxyDrawing galaxyDrawing = new GalaxyDrawing();
		bool isDragging;
		readonly Random random = new Random();
		PlanetDrawing selectedPlanet;
		Point selectedPlanetOrigins;
		Rectangle selectionBox;
		readonly string[] starNames;
		Point startPoint;

		public GalaxyDrawing GalaxyDrawing { get { return galaxyDrawing; } set { galaxyDrawing = value; } }

		public static ToolMode Mode { get; set; }

		public MainWindow()
		{
			try
			{
				InitializeComponent();
				Mode = ToolMode.Delete;
				galaxyDrawing.Canvas = canvas;
				galaxyDrawing.WarningList = warningList;
				Mode = ToolMode.Draw;

				galaxyDrawing.AskForImageSource();
				var gd = new GalaxyDialog();
				if (gd.ShowDialog() == true)
					galaxyDrawing.LoadGalaxy(gd.GalaxyNumber);
				
				galaxyDrawing.GalaxyUpdated();
				starNames = GetTextResource("Names/stars.txt").Replace("\r\n", "\n").Split('\n');
				fictionalNames = GetTextResource("Names/names.txt").Replace("\r\n", "\n").Split('\n');
			}
			catch (Exception e)
			{
				MessageBox.Show("Error: " + e.Message);
			}
		}

		List<PlanetDrawing> GetPlanetsInSelection()
		{
			var left = Canvas.GetLeft(selectionBox);
			var top = Canvas.GetTop(selectionBox);
			var width = selectionBox.Width;
			var height = selectionBox.Height;
			var planets = new List<PlanetDrawing>();
			foreach (var p in galaxyDrawing.PlanetDrawings) if (p.Position.X > left && p.Position.X < left + width && p.Position.Y > top && p.Position.Y < top + height) planets.Add(p);
			return planets;
		}

		static string GetTextResource(string uri)
		{
			using (var steam = new StreamReader(Application.GetResourceStream(new Uri(uri, UriKind.Relative)).Stream)) return steam.ReadToEnd();
		}

		void HightlightPlanetsInSelection()
		{
			var left = Canvas.GetLeft(selectionBox);
			var top = Canvas.GetTop(selectionBox);
			var width = selectionBox.Width;
			var height = selectionBox.Height;
			var planets = new HashSet<PlanetDrawing>();
			foreach (var p in galaxyDrawing.PlanetDrawings)
			{
				if (p.Position.X < left || p.Position.X > left + width || p.Position.Y < top || p.Position.Y > top + height) p.IsHighlighted = false;
				else
				{
					p.IsHighlighted = true;
					planets.Add(p);
				}
			}
			foreach (var l in galaxyDrawing.LinkDrawings) l.IsHighlighted = planets.Contains(l.Planet1) || planets.Contains(l.Planet2);
		}

		void CheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			galaxyDrawing.PlanetDrawings.ForEach(p => p.lbName.Visibility = Visibility.Hidden);
		}

		void DisplayNames_Checked(object sender, RoutedEventArgs e)
		{
			if (galaxyDrawing.PlanetDrawings != null) galaxyDrawing.PlanetDrawings.ForEach(p => p.lbName.Visibility = Visibility.Visible);
		}

		void actionPanel_Click(object sender, RoutedEventArgs e)
		{
			Mode = (ToolMode)((RadioButton)e.Source).Tag;
		}

		void backgroundButton_Click(object sender, RoutedEventArgs e)
		{
			galaxyDrawing.AskForImageSource();
		}

		void canvas_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (Mode != ToolMode.Draw) return;
			var pos = e.GetPosition(canvas);
			var names = new List<string>();
			if (starNamesBox.IsChecked.Value) names.AddRange(starNames);
			if (fictionalNamesBox.IsChecked.Value) names.AddRange(fictionalNames);
			if (customNamesCheckBox.IsChecked.Value) names.AddRange(customNamesTextBox.Text.Replace("\r\n", "\n").Split('\n'));
			names = names.Except(galaxyDrawing.PlanetDrawings.Select(p => p.Planet.Name)).ToList();
			var name = names.Count == 0 ? "No name" : names[random.Next(names.Count)];
			galaxyDrawing.AddPlanet(pos, name).lbName.Visibility = DisplayNames.IsChecked.Value ? Visibility.Visible : Visibility.Hidden;
		}

		void canvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			var planet = e.Source as PlanetDrawing;

			if (e.ChangedButton == MouseButton.Right && planet != null)
			{
			
				var diag = new PlanetDialog(planet, galaxyDrawing.Maps, galaxyDrawing.StructureTypes);
				diag.ShowDialog();
				e.Handled = true;
			} else
			{

				if (Mode == ToolMode.Delete && !isDragging)
				{
					startPoint = e.GetPosition(canvas);
					canvas.CaptureMouse();
					isDragging = true;
					selectionBox = new Rectangle
					               {
					               	Height = 0,
					               	Width = 0,
					               	Stroke = Brushes.LightGreen,
					               	StrokeThickness = 1,
					               	StrokeDashArray = new DoubleCollection(new[] { 2.0, })
					               };
					Canvas.SetLeft(selectionBox, startPoint.X);
					Canvas.SetTop(selectionBox, startPoint.Y);
					canvas.Children.Add(selectionBox);
					e.Handled = true;
				}
				else if (planet != null)
				{
					if (!isDragging)
					{
						startPoint = e.GetPosition(canvas);
						canvas.CaptureMouse();
						isDragging = true;
						selectedPlanet = planet;
						planet.IsHighlighted = true;
						selectedPlanetOrigins = planet.Position;
						if (Mode == ToolMode.Draw)
						{
							dragLine = new Line
							           {
							           	Stroke = Brushes.LightGreen,
							           	StrokeThickness = 1.5,
							           	X1 = selectedPlanetOrigins.X,
							           	Y1 = selectedPlanetOrigins.Y,
							           	X2 = startPoint.X,
							           	Y2 = startPoint.Y,
							           	SnapsToDevicePixels = true,
							           	StrokeEndLineCap = PenLineCap.Round
							           };
							canvas.Children.Add(dragLine);
						}
					}
					e.Handled = true;
				}
			}
		}

		void canvas_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (!canvas.IsMouseCaptured || !isDragging)
			{
				if (Mode == ToolMode.Delete)
				{
					foreach (var p in galaxyDrawing.PlanetDrawings) p.IsHighlighted = p.IsMouseOver;
					foreach (var l in galaxyDrawing.LinkDrawings) l.IsHighlighted = l.IsMouseOver;
				}
				return;
			}
			if (Mode == ToolMode.Delete) {}
			var currentPosition = e.GetPosition(canvas);
			if (Mode == ToolMode.Draw && dragLine != null)
			{
				dragLine.X2 = currentPosition.X;
				dragLine.Y2 = currentPosition.Y;
			}
			else if (Mode == ToolMode.Delete && selectionBox != null)
			{
				var dx = currentPosition.X - startPoint.X;
				var dy = currentPosition.Y - startPoint.Y;
				if (dx > 0)
				{
					selectionBox.Width = dx;
					Canvas.SetLeft(selectionBox, startPoint.X);
				}
				else
				{
					Canvas.SetLeft(selectionBox, startPoint.X + dx);
					selectionBox.Width = -dx;
				}
				if (dy > 0)
				{
					selectionBox.Height = dy;
					Canvas.SetTop(selectionBox, startPoint.Y);
				}
				else
				{
					Canvas.SetTop(selectionBox, startPoint.Y + dy);
					selectionBox.Height = -dy;
				}
				HightlightPlanetsInSelection();
			}
			else
			{
				selectedPlanet.Position = new Point((currentPosition.X - startPoint.X) + selectedPlanetOrigins.X,
				                                    (currentPosition.Y - startPoint.Y) + selectedPlanetOrigins.Y);
			}
		}

		void canvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (!canvas.IsMouseCaptured) return;
			isDragging = false;
			canvas.ReleaseMouseCapture();
			e.Handled = true;
			if (selectedPlanet != null) selectedPlanet.IsHighlighted = false;
			if (dragLine != null)
			{
				var endPlanet = galaxyDrawing.PlanetDrawings.Find(p => p.IsMouseOver);
				if (endPlanet != null) galaxyDrawing.AddLink(selectedPlanet, endPlanet);
				canvas.Children.Remove(dragLine);
				dragLine = null;
			}
			if (selectionBox != null)
			{
				if (selectionBox.Width > 3 || selectionBox.Height > 3) GetPlanetsInSelection().ForEach(galaxyDrawing.RemovePlanet);
				else
				{
					galaxyDrawing.PlanetDrawings.Where(p => p.IsMouseOver).ToList().ForEach(galaxyDrawing.RemovePlanet);
					galaxyDrawing.LinkDrawings.Where(l => l.IsMouseOver).ToList().ForEach(galaxyDrawing.RemoveLink);
				}
				canvas.Children.Remove(selectionBox);
				selectionBox = null;
			}
		}

		void clearButton_Click(object sender, RoutedEventArgs e)
		{
			galaxyDrawing.Clear();
		}

		void exportButton_Click(object sender, RoutedEventArgs e)
		{

			var gd = new GalaxyDialog();
			if (gd.ShowDialog() == true)
				galaxyDrawing.SaveGalaxy(gd.GalaxyNumber);
		}

		void helpButton_Click(object sender, RoutedEventArgs e)
		{
			new Help().ShowDialog();
		}

		void importButton_Click(object sender, RoutedEventArgs e)
		{
			galaxyDrawing.AskForGalaxy();
		}

		void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {}

		private void layoutButton_Click(object sender, RoutedEventArgs e)
		{
			var diagram = new Diagram();
			var nodes = new Dictionary<PlanetDrawing, SpotNode>();
			// add all nodes representing planets to the diagram
			foreach (var planetDrawing in galaxyDrawing.PlanetDrawings)
			{
				var node = new SpotNode();
				diagram.AddNode(node);
				nodes[planetDrawing] = node;
			}
			// add planet links to the diagram
			foreach (var link in galaxyDrawing.LinkDrawings)
			{
				nodes[link.Planet1].AddParent(nodes[link.Planet2]);
			}
			// create the layout
			diagram.Arrange();

			// set planet positions, from their nodes
			var bounds = diagram.GetDiagramBounds();
			foreach (var kvp in nodes)
			{
				var node = kvp.Value;
				var planetDrawing = kvp.Key;
				// get relative coordinates (0 to 1) 
				var x = ((double)node.X - bounds.Left) / bounds.Width;
				var y = ((double)node.Y - bounds.Top)/bounds.Height;
				// add some space between the border and planets
				x = x*0.8 + 0.1;
				y = y*0.8 + 0.1;
				// set the planet position
				planetDrawing.Planet.X = x;
				planetDrawing.Planet.Y = y;
				planetDrawing.Position = new Point(x * galaxyDrawing.ImageSource.Width, y * galaxyDrawing.ImageSource.Height);
			}
		}
	}


	public enum ToolMode
	{
		Draw,
		Move,
		Delete
	}
}