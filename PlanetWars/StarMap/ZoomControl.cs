using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PlanetWars
{
    /// <summary>
    /// based on http://zoomcontrol.codeplex.com/
    /// </summary>
    public class ZoomControl: ContentControl, INotifyPropertyChanged
    {
        const double scrollStep = 0.1;
        Point clickPoint;
        Grid contentControl;
        Point currentOffset;
        bool isDragging;


        Storyboard mouseMoveStory = new Storyboard();
        DoubleAnimation mouseMoveXAnimation = new DoubleAnimation();
        DoubleAnimation mouseMoveYAnimation = new DoubleAnimation();
        MatrixTransform transform;
        double zoom;

        DoubleAnimation zoomM11Animation = new DoubleAnimation();
        DoubleAnimation zoomM22Animation = new DoubleAnimation();
        Storyboard zoomStoryboard = new Storyboard();
        DoubleAnimation zoomXAnimation = new DoubleAnimation();
        DoubleAnimation zoomYAnimation = new DoubleAnimation();


        public double InitialZoom { get; set; }
        public double MaxZoom { get; set; }
        public double MinZoom { get; set; }
        public bool PanEnabled { get; set; }
        public bool UseAnimations { get; set; }

        public double Zoom
        {
            get { return transform.Matrix.M11; }
        }

        public bool ZoomEnabled { get; set; }
        public double ZoomStep { get; set; }

        public ZoomControl()
        {
            DefaultStyleKey = typeof(ZoomControl);
            ZoomEnabled = true;
            PanEnabled = true;
            MinZoom = 0.001;
            MaxZoom = 1;
            InitialZoom = 0.001;
            ZoomStep = 1.3;
            UseAnimations = true;
        }


        public Point ChildToParent(double x, double y)
        {
            return ChildToParent(new Point(x, y));
        }

        public Point ChildToParent(Point childCoords)
        {
            return contentControl.TransformToVisual(this).Transform(childCoords);
        }

        /// <summary>
        /// Takes child coords
        /// </summary>
        public bool IsVisible(double left, double top)
        {
            var topLeft = ParentToChild(0, 0);
            var bottomRight = ParentToChild(ActualWidth, ActualWidth);
            return topLeft.X < left && left < bottomRight.X && topLeft.Y < top && top < bottomRight.Y;
        }


        /// <summary>
        /// Checks if a box is visible (even partially). Takes child coords
        /// </summary>
        public bool IsVisible(double left, double right, double top, double bottom)
        {
            var topLeft = ParentToChild(0, 0);
            var bottomRight = ParentToChild(ActualWidth, ActualWidth);
            return !(right < topLeft.X || left > bottomRight.X || bottom < topLeft.Y || top > bottomRight.Y);
        }

        public void PanToCorner(double x, double y)
        {
            if (UseAnimations) {
                mouseMoveXAnimation.To = x;
                mouseMoveYAnimation.To = y;
                mouseMoveStory.Begin();
            }
            else {
                var m = transform.Matrix;
                transform.Matrix = new Matrix(m.M11, m.M12, m.M21, m.M22, x, y);
            }
        }

        public Point ParentToChild(double x, double y)
        {
            return ParentToChild(new Point(x, y));
        }

        public Point ParentToChild(Point parentCoords)
        {
            try {
                return contentControl.TransformToVisual(this).Inverse.Transform(parentCoords);
            } catch {
                return parentCoords; // fixme: check performance impact of try/catch
            }
        }

        public static void SetClip(FrameworkElement element)
        {
            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            element.Clip = new RectangleGeometry { Rect = new Rect(0, 0, element.ActualWidth, element.ActualHeight) };
        }

        public static Point Subtract(Point a, Point b)
        {
            var ret = new Point();
            ret.X = a.X - b.X;
            ret.Y = a.Y - b.Y;
            return ret;
        }

        /// <summary>
        /// Takes parent coordinates
        /// </summary>
        public void ZoomAndCenter(double zoom, double x, double y)
        {
            ZoomAndCenter(zoom, new Point(x, y));
        }

        /// <summary>
        /// Takes parent coordinates
        /// </summary>
        public void ZoomAndCenter(double zoom, Point position)
        {
            if (!ZoomEnabled) return;
            if (zoom > MaxZoom) zoom = MaxZoom;
            if (zoom < MinZoom) zoom = MinZoom;

            var childToParent = (MatrixTransform)contentControl.TransformToVisual(this);

            var deltaX = position.X - childToParent.Matrix.OffsetX;
            var deltaY = position.Y - childToParent.Matrix.OffsetY;

            var offsetX = transform.Matrix.OffsetX + deltaX*(1 - zoom/transform.Matrix.M11);
            var offsetY = transform.Matrix.OffsetY + deltaY*(1 - zoom/transform.Matrix.M22);

            ZoomToCorner(zoom, offsetX, offsetY);
        }

        /// <summary>
        /// Takes parent coordinates
        /// </summary>
        public void ZoomToCorner(double zoom, double x, double y)
        {
            var changed = zoom != this.zoom;
            this.zoom = zoom;
            if (zoom > MaxZoom) zoom = MaxZoom;
            if (zoom < MinZoom) zoom = MinZoom;
            if (UseAnimations) {
                zoomM11Animation.To = zoom;
                zoomM22Animation.To = zoom;
                zoomXAnimation.To = x;
                zoomYAnimation.To = y;
                zoomStoryboard.Begin();
            }
            else {
                var m = transform.Matrix;
                transform.Matrix = new Matrix(zoom, m.M12, m.M21, zoom, x, y);
            }
            if (changed) PropertyChanged(this, new PropertyChangedEventArgs("Zoom"));
        }

        public override void OnApplyTemplate()
        {
            zoom = InitialZoom;
            contentControl = GetTemplateChild("transformationGrid") as Grid;
            if (contentControl == null) throw new Exception("Error fetching template element 'transformationGrid'.");
            if (contentControl.RenderTransform is MatrixTransform) transform = (contentControl.RenderTransform as MatrixTransform);

            Storyboard.SetTarget(mouseMoveXAnimation, transform);
            Storyboard.SetTargetProperty(mouseMoveXAnimation, new PropertyPath("(Matrix).OffsetX"));
            mouseMoveXAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(50));
            mouseMoveXAnimation.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            mouseMoveStory.Children.Add(mouseMoveXAnimation);

            Storyboard.SetTarget(mouseMoveYAnimation, transform);
            Storyboard.SetTargetProperty(mouseMoveYAnimation, new PropertyPath("(Matrix).OffsetY"));
            mouseMoveYAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(50));
           
					mouseMoveYAnimation.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            mouseMoveStory.Children.Add(mouseMoveYAnimation);

            Storyboard.SetTarget(zoomM11Animation, transform);
            Storyboard.SetTargetProperty(zoomM11Animation, new PropertyPath("(Matrix).M11"));
            zoomM11Animation.Duration = new Duration(TimeSpan.FromMilliseconds(200));
            zoomM11Animation.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            zoomStoryboard.Children.Add(zoomM11Animation);

            Storyboard.SetTarget(zoomM22Animation, transform);
            Storyboard.SetTargetProperty(zoomM22Animation, new PropertyPath("(Matrix).M22"));
            zoomM22Animation.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            zoomM22Animation.Duration = new Duration(TimeSpan.FromMilliseconds(200));
            zoomStoryboard.Children.Add(zoomM22Animation);

            Storyboard.SetTarget(zoomXAnimation, transform);
            Storyboard.SetTargetProperty(zoomXAnimation, new PropertyPath("(Matrix).OffsetX"));
            zoomXAnimation.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            zoomXAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(200));
            zoomStoryboard.Children.Add(zoomXAnimation);

            Storyboard.SetTarget(zoomYAnimation, transform);
            Storyboard.SetTargetProperty(zoomYAnimation, new PropertyPath("(Matrix).OffsetY"));
            zoomYAnimation.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            zoomYAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(200));
            zoomStoryboard.Children.Add(zoomYAnimation);

            if (InitialZoom < MinZoom) InitialZoom = MinZoom;
            transform.Matrix = new Matrix(InitialZoom, 0, 0, InitialZoom, 0, 0);
            PropertyChanged(this, new PropertyChangedEventArgs("Zoom"));
            var resetButton = GetTemplateChild("ResetButton") as Button;
            if (resetButton != null) resetButton.Click += (s, e) => ZoomToCorner(InitialZoom, 0, 0);
            var leftButton = GetTemplateChild("LeftButton") as Button;
            if (leftButton != null) {
                leftButton.Click += (s, e) =>
                    {
                        var x = transform.Matrix.OffsetX + ActualWidth*scrollStep;
                        ZoomToCorner(zoom, x, transform.Matrix.OffsetY);
                    };
            }
            var rightButton = GetTemplateChild("RightButton") as Button;
            if (rightButton != null) {
                rightButton.Click += (s, e) =>
                    {
                        var x = transform.Matrix.OffsetX - ActualWidth*scrollStep;
                        ZoomToCorner(zoom, x, transform.Matrix.OffsetY);
                    };
            }
            var downButton = GetTemplateChild("DownButton") as Button;
            if (downButton != null) {
                downButton.Click += (s, e) =>
                    {
                        var y = transform.Matrix.OffsetY - ActualWidth*scrollStep;
                        ZoomToCorner(zoom, transform.Matrix.OffsetX, y);
                    };
            }
            var upButton = GetTemplateChild("UpButton") as Button;
            if (upButton != null) {
                upButton.Click += (s, e) =>
                    {
                        var y = transform.Matrix.OffsetY + ActualWidth*scrollStep;
                        ZoomToCorner(zoom, transform.Matrix.OffsetX, y);
                    };
            }
            var zoomInButton = GetTemplateChild("ZoomInButton") as Button;
            if (zoomInButton != null) zoomInButton.Click += (s, e) => ZoomAndCenter(zoom*ZoomStep, ActualWidth/2, ActualHeight/2);
            var zoomOutButton = GetTemplateChild("ZoomOutButton") as Button;
            if (zoomOutButton != null) zoomOutButton.Click += (s, e) => ZoomAndCenter(zoom/ZoomStep, ActualWidth/2, ActualHeight/2);
            base.OnApplyTemplate();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!PanEnabled) return;
            isDragging = true;
            e.Handled = true;
            clickPoint = e.GetPosition(null);
            currentOffset = new Point(transform.Matrix.OffsetX, transform.Matrix.OffsetY);
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (!PanEnabled) return;
            isDragging = false;
            base.OnMouseLeftButtonUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!isDragging || !PanEnabled) return;

            var newPoint = e.GetPosition(null);
            var delta = Subtract(clickPoint, newPoint);

            PanToCorner(currentOffset.X - delta.X, currentOffset.Y - delta.Y);

            base.OnMouseMove(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (e.Delta > 0) zoom *= ZoomStep;
            else zoom /= ZoomStep;
            var mousePos = e.GetPosition(this);
            ZoomAndCenter(zoom, mousePos);
            base.OnMouseWheel(e);
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }
}