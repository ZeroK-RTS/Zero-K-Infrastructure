using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using JetBrains.Annotations;
using PlanetWars.MapIcons;

namespace PlanetWars.Tabs
{
    public partial class MapTab: UserControl
    {
    	public StarMap StarMap;
    	public const double MapScale = 1000;
        public const double UpdatesPerSecond = 40.0;

        public IDictionary<int, IMapIcon> MapIcons { get; set; }

        public MapTab()
        {
            InitializeComponent();
            ZoomControl.PropertyChanged += (s, e) => UpdateAllIcons();

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1/UpdatesPerSecond) };
            timer.Tick += (s, e) => UpdateAllIcons();
            timer.Start();
        }


        void ProcessSatellites(IMapIcon parent, IEnumerable<CelestialObject> objects)
        {
            foreach (var body in objects.Where(o => o.ParentObject == parent.Body)) {
                var icon = new PlanetIcon(body, parent, this);
                MapCanvas.Children.Add(icon); // add planet/moon/asteroid to canvas
                if (body.Children.Any()) MapCanvas.Children.Add(new OrbitIcon(icon, this)); // add orbits to canvas
                ProcessSatellites(icon, objects);
            }
        }

        void UpdateAllIcons()
        {

          // todo  if (MainPage.Instance.CurrentContent != this) return; // if map tab is not active, don't update
            foreach (var child in MapCanvas.Children.OfType<IUpdatable>()) {
                child.Update();
            }
        }

        void PlanetTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var body = e.NewValue as CelestialObject;
            if (body == null) return;
            var mapIcon = MapCanvas.Children.OfType<IMapIcon>().Single(icon => icon.Body == body);
            var childCoords = new Point(mapIcon.X, mapIcon.Y);
            ZoomControl.ZoomAndCenter(0.25, ZoomControl.ChildToParent(childCoords));
        }

        void Service_GetMapDataCompleted(StarMap result)
        {
            StarMap = result;
            StarMap.Initialize();

            MapCanvas.Children.Clear();

            // add bodies and orbits to canvas
            foreach (var star in StarMap.CelestialObjects.Where(o => o.CelestialObjectType == CelestialObjectType.Star)) {
                var starIcon = new StarIcon(star, this);
                MapCanvas.Children.Add(starIcon);
                MapCanvas.Children.Add(new OrbitIcon(starIcon, this));
                ProcessSatellites(starIcon, StarMap.CelestialObjects);
            }

            MapIcons = MapCanvas.Children.OfType<IMapIcon>().Where(icon => icon.Body != null).ToDictionary(icon => icon.Body.CelestialObjectID);

            // add links to canvas
            foreach (var link in result.ObjectLinks) {
                var linkIcon = new LinkIcon(MapIcons[link.FirstObjectID].Body, MapIcons[link.SecondObjectID].Body, this);
                MapCanvas.Children.Add(linkIcon);
            }

            // add transits to canvas
            foreach (var transit in e.Result.Transits) {
                MapCanvas.Children.Add(new TransitIcon(transit, this));
            }

            PlanetTree.ItemsSource = e.Result.CelestialObjects.Where(body => body.OrbitNestingLevel == 0);
        }

        void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (StarMap == null) return;
            var time = TimeSpan.FromSeconds(TimeSlider.Value);
            var timeText = string.Format("{0:00}:{1:00}:{2:00}", (int)time.TotalHours, time.Minutes, time.Seconds);
            var turns = (int)(TimeSlider.Value / StarMap.Config.SecondsPerTurn);
            TimeWarpButton.Content = String.Format("+{0} ({1:0} turns) to turn {2:0})", timeText, turns, StarMap.GetGameTurn(TimeSlider.Value));
        }

        void TimeWarpButton_Click(object sender, RoutedEventArgs e)
        {
            if (StarMap == null) return;
            var turns = (int)(TimeSlider.Value/StarMap.Config.SecondsPerTurn);
            App.Service.FakeTurnAsync(turns);
            App.Service.GetMapDataAsync(App.UserName, App.Password);
            MessageBox.Show("Warped by " + turns + " turns");
        }
    }
}