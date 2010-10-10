using System;
using System.Windows;
using System.Windows.Controls;
using PlanetWars.ServiceReference;
using PlanetWars.Tabs;

namespace PlanetWars.MapIcons
{
    public partial class StarIcon: UserControl, IMapIcon
    {
        double fontSize = 10;
        readonly MapTab mapTab;
        double previousZoom;

        public StarIcon(CelestialObject star, MapTab mapTab)
        {
            this.mapTab = mapTab;
            if (star.CelestialObjectType != CelestialObjectType.Star) throw new Exception("body not star");

            InitializeComponent();
            Body = star;
            Label.Text = star.Name;
            Canvas.SetZIndex(this, 30);
            X = Body.X*MapTab.MapScale;
            Y = Body.Y*MapTab.MapScale;
            Ellipse.Fill = Body.Brush;
            Ellipse.Width = Body.Size;
            Ellipse.Height = Body.Size;
            Update();
        }

        public void Update()
        {
            var zoom = mapTab.ZoomControl.Zoom;
            if (previousZoom == zoom) return;

            if (!mapTab.ZoomControl.IsVisible(X, Y)) return; // not visible

            var size = Body.Size/zoom;
            ScaleTransform.ScaleX = 1/zoom;
            ScaleTransform.ScaleY = 1/zoom;
            Label.FontSize = fontSize/zoom;
            Canvas.SetLeft(this, X - size/2);
            Canvas.SetTop(this, Y - size/2);
            Label.Padding = new Thickness(size*1.1, size*1.1, 0, 0);
            previousZoom = zoom;
        }


        public CelestialObject Body { get; set; }
        public IMapIcon ParentIcon { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }
}