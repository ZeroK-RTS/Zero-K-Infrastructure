using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace PlanetWarsShared
{
	public static class Extensions
	{
		static Random random = new Random();

		public static Random Random
		{
			get { return random; }
			set { random = value; }
		}

		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
		{
			return new HashSet<T>(source);
		}

		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (var element in source) {
				action(element);
			}
			return source;
		}

		public static Color Interpolate(this Color c1, Color c2)
		{
			return Interpolate(c1, c2, 0.5);
		}

		public static Color Interpolate(this Color c1, Color c2, double x)
		{
			Func<int, int, int> i = (a, b) => (int)Math.Round(a + (b - a)*x);
			return Color.FromArgb(i(c1.A, c2.A), i(c1.R, c2.R), i(c1.G, c2.G), i(c1.B, c2.B));
		}

		public static PointF GetTopLeft(this PointF[] corners)
		{
			if (corners.Length != 2) {
				throw new ArgumentException("Must contain 2 points", "corners");
			}
			var x = Math.Min(corners[0].X, corners[1].X);
			var y = Math.Min(corners[1].Y, corners[0].Y);
			return new PointF(x, y);
		}

		public static RectangleF ToRectangleF(this PointF[] corners)
		{
			var width = Math.Abs(corners[0].X - corners[1].X);
			var height = Math.Abs(corners[0].Y - corners[1].Y);
			var location = GetTopLeft(corners);
			return new RectangleF(location, new SizeF(width, height));
		}

		public static Rectangle ToRectangle(this RectangleF rectangleF)
		{
			return new Rectangle(rectangleF.Location.ToPoint(), rectangleF.Size.ToSize());
		}

		public static PointF[] Corners(this RectangleF rectangle)
		{
			return new[] {PointF.Empty, new PointF(rectangle.Width, rectangle.Height)};
		}

		public static T TakeRandom<T>(this IEnumerable<T> source)
		{
			if (source.Count() == 0) {
				throw new Exception("Source can't be empty");
			}
			var a = source.ToArray();
			return a[Random.Next(a.Length)];
		}

		public static T TakeRandom<T>(this T[] source)
		{
			if (!source.Any()) {
				throw new Exception("Source array can't be empty.");
			}
			return source[Random.Next(source.Length)];
		}

		public static void Shuffle<T>(this T[] array)
		{
			for (int i = array.Length - 1; i > 1; --i) {
				var r = Random.Next(i);
				T temp = array[i];
				array[i] = array[r];
				array[r] = temp;
			}
		}

		public static T[] TakeRandom<T>(this IEnumerable<T> source, int count)
		{
			var sourceArray = source.ToArray();
			count = Math.Min(sourceArray.Length, count);
			sourceArray.Shuffle();
			var picks = new T[count];
			Array.Copy(sourceArray, picks, count);
			return picks;
		}

		public static PointF Scale(this PointF point, Size size)
		{
			checked {
				int width = (int)Math.Round(size.Width*point.X);
				int heigth = (int)Math.Round(size.Height*point.Y);
				return new Point(width, heigth);
			}
		}

		public static Bitmap HighQualityResize(this Image image, float scaleFactor)
		{
			var scaledBitmap = new Bitmap((int)Math.Round(image.Width*scaleFactor), (int)Math.Round(image.Height*scaleFactor));
			using (var g = Graphics.FromImage(scaledBitmap)) {
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.DrawImage(
					image, new Rectangle(Point.Empty, scaledBitmap.Size), new Rectangle(Point.Empty, image.Size), GraphicsUnit.Pixel);
			}
			return scaledBitmap;
		}

		public static Size Multiply(this Size size, float factor)
		{
			checked {
				int width = (int)Math.Round(size.Width*factor);
				int heigth = (int)Math.Round(size.Height*factor);
				return new Size(width, heigth);
			}
		}

		public static SizeF Multiply(this SizeF size, SizeF factors)
		{
			checked {
				int width = (int)Math.Round(size.Width*factors.Width);
				int heigth = (int)Math.Round(size.Height*factors.Height);
				return new Size(width, heigth);
			}
		}

		public static Point ToPoint(this PointF point)
		{
			return new Point((int)Math.Round(point.X), (int)Math.Round(point.Y));
		}

		public static Size ToSize(this SizeF size)
		{
			return new Size((int)Math.Round(size.Width), (int)Math.Round(size.Height));
		}

		public static Point Translate(this Point point, int x, int y)
		{
			return new Point(point.X + x, point.Y + y);
		}

		public static PointF Translate(this PointF point, float x, float y)
		{
			return new PointF(point.X + x, point.Y + y);
		}

		public static PointF Translate(this PointF point, PointF vector)
		{
			return point.Translate(vector.X, vector.Y);
		}

		public static Rectangle PadRectangle(this Rectangle rectangle, int padding)
		{
			var location = rectangle.Location.Translate(-padding, -padding);
			var size = rectangle.Size + new Size(2*padding, 2*padding);
			return new Rectangle(location, size);
		}

		public static float GetDistance(this Point a, Point b)
		{
			return (float)Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
		}

		public static T BinaryClone<T>(this T source)
		{
			if (!typeof (T).IsSerializable) {
				throw new ArgumentException("The type must be serializable.", "source");
			}

			if (ReferenceEquals(source, null)) {
				return default(T);
			}

			IFormatter formatter = new BinaryFormatter();
			Stream stream = new MemoryStream();
			using (stream) {
				formatter.Serialize(stream, source);
				stream.Seek(0, SeekOrigin.Begin);
				return (T)formatter.Deserialize(stream);
			}
		}

        public static string ToTime(this int seconds)
        {
            if (seconds == 0) return "";
            else return String.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);

        }
	}
}