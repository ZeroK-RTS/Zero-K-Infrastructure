using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PlanetWars.Tabs;

namespace PlanetWars.MapIcons
{
    public partial class OrbitIcon: UserControl, IMapIcon
    {
        readonly IMapIcon mapIcon;
        MapTab mapTab;
        double maxOrbitRadius;
        double previousZoom;
        double strokeSize = 1;


        public OrbitIcon(IMapIcon mapIcon, MapTab mapTab)
        {
            this.mapIcon = mapIcon;
            this.mapTab = mapTab;
            InitializeComponent();
            ParentIcon = mapIcon.ParentIcon;

            maxOrbitRadius = mapIcon.Body.Children.Max(c => c.OrbitDistance)*MapTab.MapScale;

            foreach (var child in mapIcon.Body.Children) {
                var orbitRadius = child.OrbitDistance;
                var ellipse = new Ellipse();
                ellipse.Stroke = new SolidColorBrush(Colors.White);
                ellipse.StrokeThickness = strokeSize/mapTab.ZoomControl.Zoom;
                Canvas.SetLeft(ellipse, maxOrbitRadius - child.OrbitDistance*MapTab.MapScale);
                Canvas.SetTop(ellipse, maxOrbitRadius - child.OrbitDistance*MapTab.MapScale);
                ellipse.Width = orbitRadius*2*MapTab.MapScale;
                ellipse.Height = orbitRadius*2*MapTab.MapScale;
                LayoutRoot.Children.Add(ellipse);
            }

            Canvas.SetZIndex(this, -1);

            if (mapIcon.Body.OrbitNestingLevel == 0) {
                // orbit doesn't move, just set the position once
                Canvas.SetLeft(this, mapIcon.X - maxOrbitRadius);
                Canvas.SetTop(this, mapIcon.Y - maxOrbitRadius);
            }

            Update();
        }


        public CelestialObject Body { get; set; }
        public IMapIcon ParentIcon { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public void Update()
        {
            X = mapIcon.X;
            Y = mapIcon.Y;

            var left = X - maxOrbitRadius;
            var top = Y - maxOrbitRadius;

            var minX = left;
            var minY = top;
            var maxX = left + maxOrbitRadius*2;
            var maxY = top + maxOrbitRadius*2;

            if (!mapTab.ZoomControl.IsVisible(minX, maxX, minY, maxY)) {
                Visibility = Visibility.Collapsed;
                return;
            }

            Visibility = Visibility.Visible;

            if (mapIcon.Body.OrbitNestingLevel > 0) {
                Canvas.SetLeft(this, mapIcon.X - maxOrbitRadius);
                Canvas.SetTop(this, mapIcon.Y - maxOrbitRadius);
            }

            var zoom = mapTab.ZoomControl.Zoom;
            if (previousZoom != zoom) {
                foreach (var child in LayoutRoot.Children) {
                    var ellipse = (Ellipse)child;
                    ellipse.StrokeThickness = strokeSize/zoom;
                }
                previousZoom = zoom;
            }

            var opacity = 0.2;

            const double extremeZoom = 0.1;
            if (zoom > extremeZoom) {
                // fade out at extreme zoom
                opacity *= 1 - (zoom - extremeZoom)/(1 - extremeZoom);
            }

            Opacity = opacity;
        }
    }
}