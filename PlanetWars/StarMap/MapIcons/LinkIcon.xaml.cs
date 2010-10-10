using System;
using System.Windows.Controls;
using PlanetWars.Tabs;

namespace PlanetWars.MapIcons
{
    public partial class LinkIcon: UserControl, IMapIcon
    {
        readonly MapTab mapTab;
        double strokeThickness = 3;

        public LinkIcon(CelestialObject starA, CelestialObject starB, MapTab mapTab)
        {
            this.mapTab = mapTab;
            InitializeComponent();

            var left = Math.Min(starA.X, starB.X)*MapTab.MapScale;
            var top = Math.Min(starB.Y, starA.Y)*MapTab.MapScale;

            Line.X1 = starA.X*MapTab.MapScale - left;
            Line.Y1 = starA.Y*MapTab.MapScale - top;
            Line.X2 = starB.X*MapTab.MapScale - left;
            Line.Y2 = starB.Y*MapTab.MapScale - top;

            Canvas.SetLeft(this, left);
            Canvas.SetTop(this, top);
            Canvas.SetZIndex(this, -10);

            Update();
        }


        public CelestialObject Body { get; set; }
        public IMapIcon ParentIcon { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public void Update()
        {
            var left = Canvas.GetLeft(this);
            var top = Canvas.GetTop(this);

            var maxX = Math.Max(Line.X1, Line.X2) + left;
            var maxY = Math.Max(Line.Y1, Line.Y2) + top;
            var minX = Math.Min(Line.X1, Line.X2) + left;
            var minY = Math.Min(Line.Y1, Line.Y2) + top;

            if (!mapTab.ZoomControl.IsVisible(minX, maxX, minY, maxY)) {
                // is off-screen
                // hide it to reduce the flashing lines glitch
                if (Line.Opacity != 0) Line.Opacity = 0;
                return;
            }

            var opacity = 0.3;
            var zoom = mapTab.ZoomControl.Zoom;

            const double extremeZoom = 0.01;
            if (zoom > extremeZoom) {
                // fade out at extreme zoom
                opacity *= 1 - (zoom - extremeZoom)/(1 - extremeZoom);
            }

            if (Line.Opacity != opacity) Line.Opacity = opacity;
            if (Line.StrokeThickness != strokeThickness/zoom) Line.StrokeThickness = strokeThickness/zoom;
        }
    }
}