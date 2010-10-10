using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using PlanetWars.Tabs;

namespace PlanetWars.MapIcons
{
    public partial class TransitIcon: UserControl, IUpdatable
    {
        const double lineThickness = 1;
        const double orderClickProximity = 25;
        PlanetIcon cursorPlanet;
        Point? cursorPosition;
        MapTab mapTab;

        double previousZoom;
        Transit transit;

        public TransitIcon(Transit transit, MapTab mapTab)
        {
            this.transit = transit;
            this.mapTab = mapTab;
            InitializeComponent();
            Arrow.Width = 20; //todo: optimization: make the path already have the right size
            Arrow.Height = 20;
            Canvas.SetZIndex(this, 1000);
            Canvas.SetZIndex(PlottedRouteLine, -2);
            Canvas.SetZIndex(PlaceOrderLine, -1);
            DataContext = transit;
        }

        public void Update()
        {
            if (mapTab.StarMap == null) throw new Exception("MapTab.Starmap is null");
            if (DestinationBox.ItemsSource == null) DestinationBox.ItemsSource = mapTab.StarMap.CelestialObjects;

            var zoom = mapTab.ZoomControl.Zoom;
            if (previousZoom != zoom)
            {
                previousZoom = zoom;
                ScaleTransform.ScaleX = 1/zoom;
                ScaleTransform.ScaleY = 1/zoom;
                var opacityMult = 8.0;
                InformationPanel.Opacity = 1 - 1/zoom*opacityMult/1000;
            }

            var turns = mapTab.StarMap.GetGameTurn(mapTab.TimeSlider.Value);
            Point position;
            Point? destination = null;
            if (transit.OrbitsObjectID.HasValue)
            {
                // fleet is in orbit
                var mapIcon = mapTab.MapIcons[transit.OrbitsObjectID.Value];
                var theta = mapIcon.Body.OrbitInitialAngle + mapIcon.Body.OrbitSpeed*turns*2*Math.PI; // current orbit angle
                ArrowRotate.Angle = theta/2/Math.PI*360.0 + 90.0; // set direction to orbit tangent
                position = new Point(mapIcon.X, mapIcon.Y);
                PlottedRouteLine.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (turns >= transit.EndBattleTurn)
                    position = new Point(transit.ToX*MapTab.MapScale, transit.ToY*MapTab.MapScale);
                        // todo this is incorrect, its in orbit of destination object
                else if (turns <= transit.StartBattleTurn) // todo this is incorrect, its in orbit of from object if not null
                {
                    // fleet is at departure
                    position = new Point(transit.FromX*MapTab.MapScale, transit.FromY*MapTab.MapScale);
                    destination = new Point(transit.ToX*MapTab.MapScale, transit.ToY*MapTab.MapScale);
                }
                else
                {
                    var progress = (turns - transit.StartBattleTurn)/(transit.EndBattleTurn - transit.StartBattleTurn);
                    position = new Point((transit.ToX - transit.FromX)*progress + transit.FromX,
                                         (transit.ToY - transit.FromY)*progress + transit.FromY);
                    position = new Point(position.X*MapTab.MapScale, position.Y*MapTab.MapScale); // convert to map coordinates
                    destination = new Point(transit.ToX*MapTab.MapScale, transit.ToY*MapTab.MapScale);
                }
                ArrowRotate.Angle = Math.Atan2(transit.ToY - transit.FromY, transit.ToX - transit.FromX)/Math.PI/2*360.0;
            }

            if (destination.HasValue) // display line to destination
            {
                var dest = destination.Value;
                var left = Math.Min(position.X, dest.X);
                var right = Math.Max(position.X, dest.X);
                var top = Math.Min(position.Y, dest.Y);
                var bottom = Math.Max(position.Y, dest.Y);
                if (mapTab.ZoomControl.IsVisible(left, right, top, bottom))
                {
                    PlottedRouteLine.Visibility = Visibility.Visible;
                    Canvas.SetLeft(PlottedRouteLine, left);
                    Canvas.SetTop(PlottedRouteLine, top);
                    PlottedRouteLine.X1 = position.X - left;
                    PlottedRouteLine.Y1 = position.Y - top;
                    PlottedRouteLine.X2 = dest.X - left;
                    PlottedRouteLine.Y2 = dest.Y - top;
                    PlottedRouteLine.StrokeThickness = lineThickness/zoom;
                }
                else PlottedRouteLine.Visibility = Visibility.Collapsed;
            }

            if (!mapTab.ZoomControl.IsVisible(position.X, position.Y)) IconGrid.Visibility = Visibility.Collapsed;
            else
            {
                IconGrid.Visibility = Visibility.Visible;
                Canvas.SetLeft(IconGrid, position.X - Arrow.ActualWidth/2);
                Canvas.SetTop(IconGrid, position.Y - Arrow.ActualHeight/2);
            }

            if (cursorPlanet != null) // cursor is in order mode and is pointing at a planet
            {
                var left = Math.Min(position.X, cursorPlanet.X);
                var right = Math.Max(position.X, cursorPlanet.X);
                var top = Math.Min(position.Y, cursorPlanet.Y);
                var bottom = Math.Max(position.Y, cursorPlanet.Y);
                if (mapTab.ZoomControl.IsVisible(left, right, top, bottom))
                {
                    PlaceOrderLine.Visibility = Visibility.Visible;
                    Canvas.SetLeft(PlaceOrderLine, left);
                    Canvas.SetTop(PlaceOrderLine, top);
                    PlaceOrderLine.X1 = position.X - left;
                    PlaceOrderLine.Y1 = position.Y - top;
                    PlaceOrderLine.X2 = cursorPlanet.X - left;
                    PlaceOrderLine.Y2 = cursorPlanet.Y - top;
                    PlaceOrderLine.StrokeThickness = lineThickness/mapTab.ZoomControl.Zoom;
                    PlaceOrderLine.Stroke = new SolidColorBrush(Colors.Green);
                }
                else PlaceOrderLine.Visibility = Visibility.Collapsed;
            }
            else if (cursorPosition.HasValue) // cursor is in order mode and is not pointing at a planet
            {
                var cursor = cursorPosition.Value;
                var left = Math.Min(position.X, cursor.X);
                var right = Math.Max(position.X, cursor.X);
                var top = Math.Min(position.Y, cursor.Y);
                var bottom = Math.Max(position.Y, cursor.Y);
                if (mapTab.ZoomControl.IsVisible(left, right, top, bottom))
                {
                    PlaceOrderLine.Visibility = Visibility.Visible;
                    Canvas.SetLeft(PlaceOrderLine, left);
                    Canvas.SetTop(PlaceOrderLine, top);
                    PlaceOrderLine.X1 = position.X - left;
                    PlaceOrderLine.Y1 = position.Y - top;
                    PlaceOrderLine.X2 = cursor.X - left;
                    PlaceOrderLine.Y2 = cursor.Y - top;
                    PlaceOrderLine.StrokeThickness = lineThickness / mapTab.ZoomControl.Zoom;
                    PlaceOrderLine.Stroke = new SolidColorBrush(Colors.Red);
                }
                else PlaceOrderLine.Visibility = Visibility.Collapsed;
            }
            else PlaceOrderLine.Visibility = Visibility.Collapsed; // cursor is not in order mode
        }

        void GoButton_Click(object sender, RoutedEventArgs _e)
        {
            var destination = (CelestialObject)DestinationBox.SelectedItem;
            if (destination == null)
            {
                MessageBox.Show("No destination selected");
                return;
            }
            var service = new PlanetWarsServiceClient();
            service.OrderFleetCompleted += (s, e) =>
                {
                    transit.Fleets = e.Result;
                    MessageBox.Show("Set sail!");
                };
            service.OrderFleetAsync(App.UserName, App.Password, transit.Fleets.FleetID, destination.CelestialObjectID, 0); // todo offset future?
        }


        void mapTab_MouseLeftButtonDown(object sender, MouseButtonEventArgs _)
        {
            mapTab.MapCanvas.MouseLeftButtonDown -= mapTab_MouseLeftButtonDown;
            Application.Current.RootVisual.MouseMove -= mapTab_MouseMove;
            cursorPosition = null;
            if (cursorPlanet == null) return;
            var destination = cursorPlanet;
            cursorPlanet = null;
            var service = new PlanetWarsServiceClient();
            service.OrderFleetCompleted += (s, e) =>
                {
                    transit.Fleets = e.Result;
                    MessageBox.Show(cursorPlanet.Name + " called, they want their transit back!");
                };
            service.OrderFleetAsync(App.UserName, App.Password, transit.Fleets.FleetID, destination.Body.CelestialObjectID, 0);
        }

        void mapTab_MouseMove(object sender, MouseEventArgs e)
        {
            cursorPosition = e.GetPosition(mapTab.MapCanvas);
            var cursor = cursorPosition.Value;
            var planets = from planet in mapTab.MapCanvas.Children.OfType<PlanetIcon>()
                          let distance = Math.Sqrt(Math.Pow(cursor.X - planet.X, 2) + Math.Pow(cursor.Y - planet.Y, 2))
                          where distance < orderClickProximity/mapTab.ZoomControl.Zoom
                          orderby distance
                          select planet;
            cursorPlanet = planets.FirstOrDefault();
        }

        void OrderButton_Click(object sender, RoutedEventArgs e)
        {
            mapTab.MapCanvas.MouseLeftButtonDown += mapTab_MouseLeftButtonDown;
            Application.Current.RootVisual.MouseMove += mapTab_MouseMove;
        }
    }

	public class Transit {}
}