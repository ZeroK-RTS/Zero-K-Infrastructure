using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using PlanetWars.ServiceReference;
using PlanetWars.Tabs;

namespace PlanetWars.MapIcons
{
    public partial class PlanetIcon: UserControl, IMapIcon
    {
        MapTab mapTab;
        bool optionRequestSent;
        double previousZoom;

        public PlanetIcon(CelestialObject planet, IMapIcon parentIcon, MapTab mapTab)
        {
            this.mapTab = mapTab;
            InitializeComponent();
            ParentIcon = parentIcon;
            Body = planet;
            Canvas.SetZIndex(this, planet.Size);
            Label.Text = planet.Name;
            Ellipse.Width = Body.Size;
            Ellipse.Height = Body.Size;
            Ellipse.Fill = Body.Brush;

            for (var i = 0; i < Body.MetalDensity*3; i++) {
                MetalPanel.Children.Add(new Ellipse { Width = 5, Height = 5, Fill = new SolidColorBrush(Colors.Gray), Margin = new Thickness(1) });
            }
            for (var i = 0; i < Body.FoodDensity*3; i++) {
                FoodPanel.Children.Add(new Ellipse { Width = 5, Height = 5, Fill = new SolidColorBrush(Colors.Green), Margin = new Thickness(1) });
            }
            for (var i = 0; i < Body.DarkMatterDensity*3; i++) {
                DarkMatterPanel.Children.Add(new Ellipse
                                                 { Width = 5, Height = 5, Fill = new SolidColorBrush(Colors.Brown), Margin = new Thickness(1) });
            }
            for (var i = 0; i < Body.QuantiumDensity*3; i++) {
                QuantumPanel.Children.Add(new Ellipse { Width = 5, Height = 5, Fill = new SolidColorBrush(Colors.Purple), Margin = new Thickness(1) });
            }
            InfoText.Text = MakeDescription();
            if (App.Player != null && Body.OwnerID != App.Player.PlayerID) {
                Structures.IsEnabled = false;
                Ships.IsEnabled = false;
            }
        }

        static void Fade(Control target)
        {
            var opacityAnimation = new DoubleAnimation();
            Storyboard.SetTarget(opacityAnimation, target);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));
            opacityAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(1000));
            opacityAnimation.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            var storyboard = new Storyboard();
            if (target.Opacity == 1) {
                storyboard.Completed += (s, e) => target.Visibility = Visibility.Collapsed;
                opacityAnimation.To = 0;
            }
            else {
                opacityAnimation.To = 1;
                target.Visibility = Visibility.Visible;
            }

            storyboard.Children.Add(opacityAnimation);
            storyboard.Begin();
        }

        Point GetPosition()
        {
            var turns = mapTab.StarMap.GetGameTurn(mapTab.TimeSlider.Value);
            var theta = Body.OrbitInitialAngle + Body.OrbitSpeed*turns*2*Math.PI;
            var r = Body.OrbitDistance*MapTab.MapScale;
            return new Point(ParentIcon.X + r*Math.Cos(theta), ParentIcon.Y + r*Math.Sin(theta));
        }

        void HandleBuildLists()
        {
            var player = App.Player;
            if (player == null) return;

            var isMine = Body.OwnerID == player.PlayerID;
            if (!isMine) return;
            if (mapTab.ZoomControl.Zoom >= 0.1 && !optionRequestSent) {
                var service = new PlanetWarsServiceClient();
                service.GetBodyOptionsCompleted += (s, e) =>
                    {
                        StructureBuildOptions.ItemsSource = e.Result.NewStructureOptions;
                        StructureBuildOptions.Visibility = Visibility.Visible;
                        ShipBuildOptions.ItemsSource = e.Result.NewShipOptions;
                        ShipBuildOptions.Visibility = Visibility.Visible;
                    };
                service.GetBodyOptionsAsync(App.UserName, App.Password, Body.CelestialObjectID);
                optionRequestSent = true;
            }
            if (mapTab.ZoomControl.Zoom < 0.1) {
                optionRequestSent = false;
                StructureBuildOptions.Visibility = Visibility.Collapsed;
                ShipBuildOptions.Visibility = Visibility.Collapsed;
            }
        }


        void HandleListControl<T>(ItemsControl control, IEnumerable<T> items)
        {
            if (mapTab.ZoomControl.Zoom >= 0.1 && Body.CelestialObjectStructures != null && Body.CelestialObjectStructures.Count > 0) {
                control.Visibility = Visibility.Visible;
                control.ItemsSource = items;
            }
            if (mapTab.ZoomControl.Zoom < 0.1) control.Visibility = Visibility.Collapsed;
        }

        string MakeDescription()
        {
            return String.Format("Metal Density: {0:0.#}\r\n", Body.MetalDensity) + String.Format("Food Density: {0:0.#}\r\n", Body.FoodDensity) +
                   String.Format("Quantium Density: {0:0.#}\r\n", Body.QuantiumDensity) +
                   String.Format("Dark Matter Density: {0:0.#}\r\n", Body.DarkMatterDensity) +
                   String.Format("Metal Income: {0:0.#}\r\n", Body.MetalIncome) + String.Format("Quantium Income: {0:0.#}\r\n", Body.QuantiumIncome) +
                   String.Format("Dark Matter Income: {0:0.#}\r\n", Body.DarkMatterIncome) + String.Format("Size: {0:0.#}\r\n", Body.Size) +
                   String.Format("MaxPopulation: {0:0.#}\r\n", Body.MaxPopulation) + String.Format("Population: {0:0.#}\r\n", Body.Population) +
                   String.Format("Owner: {0}\r\n", Body.OwnerName) + String.Format("Efficiency: {0:0.#}\r\n", Body.Efficiency) +
                   String.Format("Buildpower: {0:0.#}\r\n", Body.Buildpower) + String.Format("Buildpower Used: {0:0.#}\r\n", Body.BuildpowerUsed);
        }

        void PostBuildUpdates(BodyResponse result)
        {
            // update starmap
            mapTab.StarMap.CelestialObjects.Remove(Body);
            var level = Body.OrbitNestingLevel;
            Body = result.Body;
            Body.OrbitNestingLevel = level;
            mapTab.StarMap.CelestialObjects.Add(Body);
            // update planet stats
            InfoText.DataContext = Body;
            // update lists
            Ships.ItemsSource = Body.CelestialObjectShips;
            Structures.ItemsSource = Body.CelestialObjectStructures;
            // display updated resources
            App.Player = result.Player;
            // display new build options
            StructureBuildOptions.ItemsSource = result.NewStructureOptions;
            ShipBuildOptions.ItemsSource = result.NewShipOptions;
        }

        public CelestialObject Body { get; set; }
        public IMapIcon ParentIcon { get; set; }

        public Double X { get; set; }
        public Double Y { get; set; }

        public void Update()
        {
            var zoom = mapTab.ZoomControl.Zoom;

            var position = GetPosition();
            X = position.X;
            Y = position.Y;

            if (mapTab.ZoomControl.IsVisible(X, Y)) {
                Visibility = Visibility.Visible;

                // set position
                var size = Body.Size/zoom;
                Canvas.SetLeft(this, X - size/2);
                Canvas.SetTop(this, Y - size/2);

                if (previousZoom != zoom) {
                    previousZoom = zoom;

                    // set opacities
                    double opacityMult;
                    switch (Body.OrbitNestingLevel) {
                        case 1:
                            opacityMult = 2;
                            break;
                        case 2:
                            opacityMult = 8;
                            break;
                        case 3:
                            opacityMult = 16;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    Label.Opacity = 1 - 1/zoom*opacityMult/1000;
                    var ratingOpacity = 1 - 1/zoom*opacityMult/1000*1.5;
                    FoodPanel.Opacity = ratingOpacity;
                    MetalPanel.Opacity = ratingOpacity;

                    // set sizes

                    if (ratingOpacity > 0.01) InformationPanel.Margin = new Thickness(size*1.1, size*1.1, 0, 0);
                    Ellipse.Width = size;
                    Ellipse.Height = size;
                    planelScale.ScaleX = 1/zoom;
                    planelScale.ScaleY = 1/zoom;

                    if (zoom > 0.1 && App.Player != null && App.Player.PlayerID == Body.OwnerID && Body.CelestialObjectShips.Any(s => s.Count > 0)) CreateFleetButton.Visibility = Visibility.Visible;
                    else CreateFleetButton.Visibility = Visibility.Collapsed;

                    InfoText.Visibility = mapTab.ZoomControl.Zoom > 0.1 ? Visibility.Visible : Visibility.Collapsed;
                }

                HandleListControl(Structures, Body.CelestialObjectStructures);
                HandleListControl(Ships, Body.CelestialObjectShips);
                HandleBuildLists();
            }
            else {
                // not visible
                Visibility = Visibility.Collapsed;
            }
        }

        void CreateFleetButton_Click(object sender, RoutedEventArgs _e)
        {
            var service = new PlanetWarsServiceClient();
            var fleet = new ObservableCollection<ShipTypeCount>();
            foreach (var item in Body.CelestialObjectShips.Select(s => new ShipTypeCount { Count = s.Count, ShipTypeID = s.ShipTypeID })) {
                fleet.Add(item);
            }
            service.CreateFleetCompleted += (s, e) => MessageBox.Show("Done");
            service.CreateFleetAsync(App.UserName, App.Password, Body.CelestialObjectID, fleet);
        }

        void ShipBuyButton_Click(object sender, RoutedEventArgs _e)
        {
            var button = (Button)sender;
            var option = (ShipOption)button.DataContext;
            var service = new PlanetWarsServiceClient();
            service.BuildShipCompleted += (s, e) =>
                {
                    MessageBox.Show(e.Result.Response.ToString());
                    if (e.Result.Response == BuildResponse.Ok) PostBuildUpdates(e.Result);
                };
            service.BuildShipAsync(App.UserName, App.Password, Body.CelestialObjectID, option.ShipType.ShipTypeID, 1);
        }

        void StructureBuyButton_Click(object sender, RoutedEventArgs _e)
        {
            var button = (Button)sender;
            var option = (StructureOption)button.DataContext;
            var service = new PlanetWarsServiceClient();
            service.BuildStructureCompleted += (s, e) =>
                {
                    MessageBox.Show(e.Result.Response.ToString());
                    if (e.Result.Response == BuildResponse.Ok) PostBuildUpdates(e.Result);
                };
            service.BuildStructureAsync(App.UserName, App.Password, Body.CelestialObjectID, option.StructureType.StructureTypeID);
        }

        void StructureSellButton_Click(object sender, RoutedEventArgs _e)
        {
            var button = (Button)sender;
            var selectedStructure = (CelestialObjectStructure)button.DataContext;
            var service = new PlanetWarsServiceClient();
            service.SellStructureCompleted += (s, e) =>
                {
                    MessageBox.Show(e.Result.Response.ToString());
                    if (e.Result.Response == BuildResponse.Ok) PostBuildUpdates(e.Result);
                };
            service.SellStructureAsync(App.UserName, App.Password, Body.CelestialObjectID, selectedStructure.StructureType.StructureTypeID);
        }
    }
}

// basically factor mouse distance into zoom and you are done i think :)
// although maybe visible region is better
// if i figure it out
// (just a matter of time)
// its not and you can use both
// you sometimes want to work with planet without zooming
// its faster to just point
// but planets are really close
// it's hard to point
// big are not
// yeah true
// i could ignore small planets
// at small zooms
// you can easilly calculate visual distance from mouse
// and then pick biggest object 10 pixels from mouse
// you dont have to ignore them just prefer bigger
// same for clicking, you could allow player to click object "selected" in such way
// same for fleets etc, will make it much easier to control
// sure at full zoom you display everything