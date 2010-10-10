using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PlanetWars.Utility
{
	static class Extensions
	{
		//
		// ICollection
		//

		static readonly Dictionary<string, string> plurals = new Dictionary<string, string> {{"warning", "warnings"}};

		public static bool IsNullOrEmpty<T>(this ICollection<T> obj)
		{
			return (obj == null || obj.Count == 0);
		}

		//
		// IEnumerable
		//

		public static IEnumerable<T> Duplicates<T>(this IEnumerable<T> source)
		{
			return source.Except(source.Distinct());
		}

		public static bool IsEmpty<T>(this IEnumerable<T> source)
		{
			return !source.Any();
		}

#if false
		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (var element in source) {
				action(element);
			}
		}
#endif

		//
		// Array
		//

		public static int IndexOf<T>(this T[] array, T value)
		{
			return Array.IndexOf(array, value);
		}

		public static int LastIndexOf<T>(this T[] array, T value)
		{
			return Array.LastIndexOf(array, value);
		}

		public static ReadOnlyCollection<T> AsReadOnly<T>(this T[] array)
		{
			return Array.AsReadOnly(array);
		}

		public static void ClearAll<T>(this T[] array)
		{
			Array.Clear(array, 0, array.Length);
		}

		public static T[] MakeCopy<T>(this T[] original)
		{
			T[] copy = new T[original.Length];
			Array.Copy(original, copy, original.Length);
			return copy;
		}

		//
		// IDictionary
		//

		public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
		{
			return new ReadOnlyDictionary<TKey, TValue>(dictionary);
		}

		//
		// String
		//

		public static bool IsNullOrEmpty(this string value)
		{
			return (string.IsNullOrEmpty(value));
		}

		public static bool IsMatch(this string input, string pattern)
		{
			return Regex.IsMatch(input, pattern);
		}

		public static bool IsMatch(this string input, string pattern, RegexOptions options)
		{
			return Regex.IsMatch(input, pattern, options);
		}

		public static string FormatWith(this string format, params object[] args)
		{
			return String.Format(format, args);
		}

		public static bool EndsWith(this string s, params string[] values)
		{
			return values.Where(value => s.EndsWith(value)).Any();
		}

		public static string ToPlural(this string singular)
		{
			try {
				return plurals[singular];
			} catch (KeyNotFoundException e) {
				throw new ApplicationException("No plural defined for {0}.".FormatWith(singular), e);
			}
		}

		public static string SetNumber(this string singular, int number)
		{
			if (number < 1) {
				throw new ArgumentException("Number must be larger than zero.", "number");
			}
			return number == 1 ? singular : singular.ToPlural();
		}

		public static string Capitalize(this string word)
		{
			return Char.ToUpper(word[0]) + word.Substring(1, word.Length - 1).ToLower();
		}

		public static string Combine(this string path, params string[] fileOrFolders)
		{
			var list = new List<string> {path};
			list.AddRange(fileOrFolders);
			return MakePath(list.ToArray());
		}

		public static string MakePath(params string[] directories)
		{
			string s = Path.DirectorySeparatorChar.ToString();

			string path = string.Join(s, directories);
			path = (s == "/") ? path.Replace("\\", "/") : path.Replace("/", "\\");
			while (path.Contains(s + s)) {
				path = path.Replace(s + s, s);
			}
			if (path.EndsWith(s)) {
				path = path.Substring(0, path.Length - 1);
			}
			return path;
		}

		public static string ToWindowsLineEndings(this string s)
		{
			return s.Contains("\r\n") ? s : s.Replace("\n", "\r\n");
		}

		//
		// ToolStripItemCollection
		//

		public static void ForEach(this ToolStripItemCollection items, Action<ToolStripItem> action)
		{
			foreach (object i in items) {
				action(i as ToolStripItem);
			}
		}

		//
		// ControlCollection
		//

		public static Control[] ToArray(this Control.ControlCollection controls)
		{
			var array = new Control[controls.Count];
			for (int i = 0; i < controls.Count; i++) {
				array[i] = controls[i];
			}
			return array;
		}

		//
		// Size
		//

		public static Size Scale(this Size size, float scaleFactor)
		{
			checked {
				int width = (int)(size.Width*scaleFactor);
				int heigth = (int)(size.Height*scaleFactor);
				return new Size(width, heigth);
			}
		}

		public static Size Cap(this Size size, int cap)
		{
			int maxSize = Math.Max(size.Width, size.Height);
			return maxSize < cap ? size : size.Scale((float)cap/maxSize);
		}

		//
		// Point
		//

		public static Point Translate(this Point point, Vector vector)
		{
			return new Point(point.X + vector.X, point.Y + vector.Y);
		}

		//
		// PointF
		//

		public static PointF Translate(this PointF point, Vector vector)
		{
			return new PointF(point.X + vector.X, point.Y + vector.Y);
		}

		public static T FindClosest<T>(this PointF startPoint, ICollection<T> items) where T : IPositionable
		{
			if (items.IsNullOrEmpty()) {
				throw new ArgumentException("Must not be null or empty.", "items");
			}
			T closestPoint = default(T);
			float closestDistance = float.MaxValue;
			foreach (T candidate in items) {
				float candidateDistance = (candidate.Position.X - startPoint.X).Pow(2) +
				                          (candidate.Position.Y - startPoint.Y).Pow(2);
				if (candidateDistance < closestDistance) {
					closestPoint = candidate;
					closestDistance = candidateDistance;
				}
			}
			return closestPoint;
		}

		//
		// Color
		//

		public static Color Invert(this Color color)
		{
			return Color.FromArgb(color.A, 255 - color.R, 255 - color.G, 255 - color.B);
		}

		//
		//  int
		//

		public static int Pow(this int x, int y)
		{
			return (int)Math.Pow(x, y);
		}

		public static int Constrain(this int x, int minimum, int maximum)
		{
			if (minimum > maximum) {
				throw new ArgumentException("Minimum must be smaller than maximum.", "minimum");
			}
			return Math.Min(Math.Max(x, minimum), maximum);
		}

		public static IEnumerable<int> To(this int start, int end)
		{
			return Enumerable.Range(start, end - start);
		}

		public static TimeSpan Seconds(this int s)
		{
			return TimeSpan.FromSeconds(s);
		}

		public static string ToOrdinal(this int number)
		{
			int mod10 = number%10;
			int mod100 = number%100;

			if (mod10 - mod100 == 10) {
				return "th";
			}

			switch (mod10) {
				case 1:
					return number + "st";
				case 2:
					return number + "nd";
				case 3:
					return number + "rd";
				default:
					return number + "th";
			}
		}

		//
		// float
		//

		public static float Constrain(this float x, float minimum, float maximum)
		{
			if (minimum > maximum) {
				throw new ArgumentException("Minimum must be smaller than maximum.", "minimum");
			}
			return Math.Min(Math.Max(x, minimum), maximum);
		}

		public static float Pow(this float x, float y)
		{
			return (float)Math.Pow(x, y);
		}
	}
}