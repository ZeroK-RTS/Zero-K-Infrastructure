using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using PlanetWarsShared;

namespace PlanetWars
{
    public class LinkDrawing
    {
        PointF end;
        PointF tip;

        public LinkDrawing()
        {
            ArrowWidth = 2.5f;
            ArrowHeigth = 3;
            LineWidth = 2;
            FillArrow = true;
        }

        public LinkDrawing(PointF tip, PointF end) : this()
        {
            Tip = tip;
            End = end;
        }

        public PointF Tip
        {
            [DebuggerStepThrough]
            get { return tip; }
            [DebuggerStepThrough]
            set
            {
                if (value.X > 1 || value.Y > 1 || value.X < 0 || value.Y < 0) {
                    throw new ArgumentOutOfRangeException();
                }
                tip = value;
            }
        }

        public PointF End
        {
            [DebuggerStepThrough]
            get { return end; }
            [DebuggerStepThrough]
            set
            {
                if (value.X > 1 || value.Y > 1 || value.X < 0 || value.Y < 0) {
                    throw new ArgumentOutOfRangeException();
                }
                end = value;
            }
        }

        public float LineWidth { get; set; }
        public float ArrowWidth { get; set; }
        public float ArrowHeigth { get; set; }
        public bool FillArrow { get; set; }
        public Color TipColor { get; set; }
        public Color EndColor { get; set; }
        public bool IsArrow { get; set; }
        public bool IsColonyLink { get; set; }

        public float VectorX
        {
            [DebuggerStepThrough]
            get { return Tip.X - End.X; }
        }

        public float VectorY
        {
            [DebuggerStepThrough]
            get { return Tip.Y - End.Y; }
        }

        public float Length
        {
            [DebuggerStepThrough]
            get { return (float)Math.Sqrt(Math.Pow(VectorX, 2) + Math.Pow(VectorY, 2)); }
        }

        public RectangleF Rectangle
        {
            [DebuggerStepThrough]
            get { return new[] {Tip, End}.ToRectangleF(); }
        }

        public PointF Location
        {
            [DebuggerStepThrough]
            get { return Rectangle.Location; }
            set
            {
                if (value.X > 1 || value.Y > 1 || value.X < 0 || value.Y < 0) {
                    throw new ArgumentOutOfRangeException();
                }
                var x = Location.X - value.X;
                var y = Location.Y - value.Y;
                Tip = new PointF(Tip.X - x, Tip.Y - y);
                End = new PointF(End.X - x, End.Y - y);
            }
        }

        public SizeF Size
        {
            [DebuggerStepThrough]
            get { return Rectangle.Size; }
        }

        public float Width
        {
            [DebuggerStepThrough]
            get { return Rectangle.Width; }
        }

        public float Height
        {
            [DebuggerStepThrough]
            get { return Rectangle.Height; }
        }

        public float ArrowOffset { get; set; }

        public override string ToString()
        {
            Func<object, string> s = o => o.GetHashCode().ToString();
            var sb = new StringBuilder();
            var objects = new object[]
            {Tip.X, Tip.Y, End.X, End.Y, TipColor, EndColor, LineWidth, ArrowWidth, ArrowHeigth, FillArrow, IsArrow, ArrowOffset};
            objects.ForEach(o => sb.AppendLine(s(o)));
            return Hash.HashString(sb.ToString()).ToString();
        }

        public Point SetVectorMagnitude(Point vec, int newMagnitude)
        {
            var x = vec.X;
            var y = vec.Y;
            var oldMagnitude = Math.Sqrt(x * x + y * y);
            var scale = newMagnitude / oldMagnitude;
            x = (int)(x * scale);
            y = (int)(y * scale);
            return new Point(x, y);
        }

        public void Draw(Graphics g, Point location, Size mapSize)
        {
            var imageTip = Tip.Scale(mapSize).Translate(location).ToPoint();
            var imageEnd = End.Scale(mapSize).Translate(location).ToPoint();

            // shorten the arrow on one side so it doesn't overlap with the planet
            if (IsArrow && !IsColonyLink) {
                Shorten(imageEnd, ref imageTip);
            }

            using (var brush = new LinearGradientBrush(imageTip, imageEnd, TipColor, EndColor))
            //using (var brush = new SolidBrush(Color.Red))
            using (var pen = new Pen(brush, LineWidth*mapSize.Width)) {
                CustomLineCap cap;
                if (IsArrow) {
                    if (IsColonyLink) {
                        pen.CustomEndCap = PenCapProvider.GetInheritanceRepresentation();
                        g.DrawLines(pen, new[] { imageTip, imageEnd });
                    } else {
                        cap = new AdjustableArrowCap(10, 12);
                        pen.CustomStartCap = cap;
                        g.DrawLines(pen, new[] { imageTip, imageEnd });
                        cap.Dispose();
                    }
                } else {
                    g.DrawLines(pen, new[] { imageTip, imageEnd });
                }

            }
        }

        public void Shorten(Point fixedPoint, ref Point translatedPoint)
        {
            var length = translatedPoint.GetDistance(fixedPoint);
            var newLength = length - ArrowOffset;
            var ratio = newLength / length;
            var x = (int)(fixedPoint.X + ratio * (translatedPoint.X - fixedPoint.X));
            var y = (int)(fixedPoint.Y + ratio * (translatedPoint.Y - fixedPoint.Y));
            translatedPoint = new Point(x, y);
        }
    }
}