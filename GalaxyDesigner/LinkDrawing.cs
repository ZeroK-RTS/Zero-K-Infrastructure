using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GalaxyDesigner
{
    public sealed class LinkDrawing : Shape
    {
        public static readonly DependencyProperty X1Property = DependencyProperty.Register(
            "X1",
            typeof (double),
            typeof (LinkDrawing),
            new FrameworkPropertyMetadata(
                0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty X2Property = DependencyProperty.Register(
            "X2",
            typeof (double),
            typeof (LinkDrawing),
            new FrameworkPropertyMetadata(
                0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty Y1Property = DependencyProperty.Register(
            "Y1",
            typeof (double),
            typeof (LinkDrawing),
            new FrameworkPropertyMetadata(
                0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty Y2Property = DependencyProperty.Register(
            "Y2",
            typeof (double),
            typeof (LinkDrawing),
            new FrameworkPropertyMetadata(
                0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        [TypeConverter(typeof (LengthConverter))]
        public double X1
        {
            get { return (double)GetValue(X1Property); }
            set { SetValue(X1Property, value); }
        }

        [TypeConverter(typeof (LengthConverter))]
        public double Y1
        {
            get { return (double)GetValue(Y1Property); }
            set { SetValue(Y1Property, value); }
        }

        [TypeConverter(typeof (LengthConverter))]
        public double X2
        {
            get { return (double)GetValue(X2Property); }
            set { SetValue(X2Property, value); }
        }

        [TypeConverter(typeof (LengthConverter))]
        public double Y2
        {
            get { return (double)GetValue(Y2Property); }
            set { SetValue(Y2Property, value); }
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                var geometry = new StreamGeometry {FillRule = FillRule.EvenOdd};
                using (var context = geometry.Open()) {
                    var pt1 = new Point(X1, Y1);
                    var pt2 = new Point(X2, Y2);
                    context.BeginFigure(pt1, true, false);
                    context.LineTo(pt2, true, true);
                }
                geometry.Freeze();
                return geometry;
            }
        }

        public PlanetDrawing Planet1 { get; set; }
        public PlanetDrawing Planet2 { get; set; }

       public LinkDrawing(PlanetDrawing planet1, PlanetDrawing planet2)
        {
            Planet1 = planet1;
            Planet2 = planet2;
            {
                SetBinding(X1Property, new Binding("Planet1.Position.X") { Source = this });
                SetBinding(Y1Property, new Binding("Planet1.Position.Y") { Source = this });
                SetBinding(X2Property, new Binding("Planet2.Position.X") { Source = this });
                SetBinding(Y2Property, new Binding("Planet2.Position.Y") { Source = this });
            }
           Stroke = Brushes.LightBlue;
           StrokeThickness = 1;
        }

        public bool IsHighlighted
        {
            set
            {
                Stroke = value ? Brushes.Pink : Brushes.LightBlue;
            }
        }
    }
}