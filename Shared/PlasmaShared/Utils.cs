#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Encoder = System.Drawing.Imaging.Encoder;

#endregion

namespace PlasmaShared
{
	/// <summary>
	/// General purpose static functions here
	/// </summary>
	public static class Utils
	{
		public static void SafeDispose(this IDisposable o)
		{
			if (o != null) o.Dispose();
		}


        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source,
    Func<TSource, TKey> selector)
        {
            return source.MinBy(selector, Comparer<TKey>.Default);
        }

        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> selector, IComparer<TKey> comparer)
        {
            using (IEnumerator<TSource> sourceIterator = source.GetEnumerator())
            {
                if (!sourceIterator.MoveNext())
                {
                    throw new InvalidOperationException("Sequence was empty");
                }
                TSource min = sourceIterator.Current;
                TKey minKey = selector(min);
                while (sourceIterator.MoveNext())
                {
                    TSource candidate = sourceIterator.Current;
                    TKey candidateProjected = selector(candidate);
                    if (comparer.Compare(candidateProjected, minKey) < 0)
                    {
                        min = candidate;
                        minKey = candidateProjected;
                    }
                }
                return min;
            }
        }

        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source,
           Func<TSource, TKey> selector, IComparer<TKey> comparer)
        {
            using (IEnumerator<TSource> sourceIterator = source.GetEnumerator())
            {
                if (!sourceIterator.MoveNext())
                {
                    throw new InvalidOperationException("Sequence was empty");
                }
                TSource min = sourceIterator.Current;
                TKey minKey = selector(min);
                while (sourceIterator.MoveNext())
                {
                    TSource candidate = sourceIterator.Current;
                    TKey candidateProjected = selector(candidate);
                    if (comparer.Compare(candidateProjected, minKey) > 0)
                    {
                        min = candidate;
                        minKey = candidateProjected;
                    }
                }
                return min;
            }
        }

        public static double StdDev(this IEnumerable<double> values)
        {
            double ret = 0;
            int count = values.Count();
            if (count > 1)
            {
                //Compute the Average
                double avg = values.Average();

                //Perform the Sum of (value-avg)^2
                double sum = values.Sum(d => (d - avg) * (d - avg));

                //Put it all together
                ret = Math.Sqrt(sum / count);
            }
            return ret;
        }


        /*
        public static double StdDev(this IEnumerable<double> values)
        {
            // ref: http://warrenseen.com/blog/2006/03/13/how-to-calculate-standard-deviation/
            double mean = 0.0;
            double sum = 0.0;
            double stdDev = 0.0;
            int n = 0;
            foreach (double val in values)
            {
                n++;
                double delta = val - mean;
                mean += delta / n;
                sum += delta * (val - mean);
            }
            if (1 < n)
                stdDev = Math.Sqrt(sum / (n - 1));

            return stdDev;
        }
        */


		public static bool CanRead(string filename)
		{
			if (!File.Exists(filename)) return true;
			try
			{
				using (var f = File.Open(filename, FileMode.Open, FileAccess.Read)) f.Close();
				return true;
			}
			catch
			{
				return false;
			}
		}


		public static bool CanWrite(string filename)
		{
			if (!File.Exists(filename)) return true;
			try
			{
				using (var f = File.Open(filename, FileMode.Open, FileAccess.Write, FileShare.None)) f.Close();
				return true;
			}
			catch
			{
				return false;
			}
		}


		public static bool CheckPath(string path)
		{
			return CheckPath(path, false);
		}

		public static bool CheckPath(string path, bool delete)
		{
			if (delete)
			{
				try
				{
					Directory.Delete(path, true);
				}
				catch {}
			}
			try
			{
				if (!Directory.Exists(path)) Directory.CreateDirectory(path);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static byte[] Compress(this string data)
		{
			return Compress(Encoding.Unicode.GetBytes(data));
		}

		public static byte[] Compress(this byte[] data)
		{
			using (var compressedStream = new MemoryStream())
			using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
			{
				zipStream.Write(data, 0, data.Length);
				zipStream.Close();
				return compressedStream.ToArray();
			}
		}

		public static Bitmap GetResized(this Image original, int newWidth, int newHeight, InterpolationMode mode)
		{
			var resized = new Bitmap(newWidth, newHeight);
			using (var g = Graphics.FromImage(resized))
			{
				g.InterpolationMode = mode;
				g.DrawImage(original, 0, 0, newWidth, newHeight);
			}
			return resized;
		}


		private static ImageCodecInfo GetEncoderInfo(string mimeType)
		{
			// Get image codecs for all image formats 
			ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

			// Find the correct image codec 
			for (int i = 0; i < codecs.Length; i++)
				if (codecs[i].MimeType == mimeType)
					return codecs[i];
			return null;
		}

		public static void SaveJpeg(this Image img,string path, int quality)
		{
			if (quality < 0 || quality > 100)
				throw new ArgumentOutOfRangeException("quality must be between 0 and 100.");


			// Encoder parameter for image quality 
			EncoderParameter qualityParam =
					new EncoderParameter(Encoder.Quality, quality);
			// Jpeg image codec 
			ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");

			EncoderParameters encoderParams = new EncoderParameters(1);
			encoderParams.Param[0] = qualityParam;

			img.Save(path, jpegCodec, encoderParams);
		}


		public static int Constrain(this int value, int min, int max)
		{
			return Math.Max(min, Math.Min(max, value));
		}

		public static double Constrain(this double value, double min, double max)
		{
			return Math.Max(min, Math.Min(max, value));
		}

		public static decimal Constrain(this decimal value, decimal min, decimal max)
		{
			return Math.Max(min, Math.Min(max, value));
		}

		public static byte[] Decompress(this byte[] data)
		{
			using (var compressedStream = new MemoryStream(data))
			using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
			using (var resultStream = new MemoryStream())
			{
				var buffer = new byte[4096];
				int read;
				while ((read = zipStream.Read(buffer, 0, buffer.Length)) > 0) resultStream.Write(buffer, 0, read);
				return resultStream.ToArray();
			}
		}

		public static string EscapePath(this string path)
		{
            if (string.IsNullOrEmpty(path)) return path;
			var escaped = new StringBuilder();
			foreach (var c in path)
			{
				if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '(' || c == ')' || c == '.') escaped.Append(c);
				else escaped.Append('_');
			}
			return escaped.ToString();
		}

		public static string GetAlternativeDirectoryName(string to)
		{
			if (Directory.Exists(to))
			{
				var name = to;
				while (name.EndsWith("\\") || name.EndsWith("/")) name = name.Substring(0, name.Length - 1);
				var i = 1;
				do
				{
					to = name + "(" + i++ + ")";
				} while (Directory.Exists(to));
			}
			return to;
		}

		public static string GetAlternativeFileName(string to)
		{
			if (File.Exists(to))
			{
				var ext = Path.GetExtension(to);
				var name = Path.GetFileNameWithoutExtension(to);
				var dir = Path.GetDirectoryName(to);
				var i = 1;
				do
				{
					to = MakePath(dir, name + "(" + i++ + ")" + ext);
				} while (File.Exists(to));
			}
			return to;
		}


		/// <summary>
		/// Glues remaining arguments together
		/// </summary>
		/// <param name="args">argument array</param>
		/// <param name="startindex">index to start gluing</param>
		/// <returns>glued string</returns>
		public static string Glue(string[] args, int startindex)
		{
			if (args.Length <= startindex) return "";
			var ret = args[startindex];
			for (var i = startindex + 1; i < args.Length; ++i) ret += ' ' + args[i];
			return ret;
		}

		public static string Glue(string[] args)
		{
			return Glue(args, 0);
		}

		public static string[] Lines(this string source)
		{
			if (source == null) return new string[] { };
			else return source.Replace("\r\n", "\n").Split('\n');
		}

		/// <summary>
		/// Creates paths in a cross-platform way.
		/// </summary>
		public static string MakePath(params string[] directories)
		{
			var separator = Path.DirectorySeparatorChar.ToString();
			var path = String.Join(separator, directories);
			path = (separator == "/") ? path.Replace("\\", "/") : path.Replace("/", "\\");
			while (path.Contains(separator + separator)) path = path.Replace(separator + separator, separator);
			if (path.EndsWith(separator)) path = path.Substring(0, path.Length - 1);
			return path;
		}

		public static EventWaitHandle ParalellAction(Action action, ParalellActionContext context)
		{
			var waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
			context.WaitHandles.Add(waitHandle);
			ThreadPool.QueueUserWorkItem(delegate
				{
					try
					{
						action();
					}
					catch (Exception ex)
					{
						if (context.Error == null) context.Error = ex;
					}
					finally
					{
						waitHandle.Set();
					}
				});
			return waitHandle;
		}

		public static string PrintByteLength(long bytes)
		{
			if (bytes < 1024) return bytes.ToString();
			if (bytes < 1024*1024) return ((double)bytes/1024).ToString("F2") + "k";
			if (bytes < 1024*1024*1024) return ((double)bytes/1024/1024).ToString("F2") + "M";
			return ((double)bytes/1024/1024/1024).ToString("F2") + "G";
		}

		public static string PrintTimeRemaining(long secs)
		{
			if (secs <= 0) return "";
			if (secs < 60) return String.Format("{0}s", secs);
			if (secs < 3600) return String.Format("{0}m {1}s", secs/60, secs%60);
			return String.Format("{0}h {1}m {2}s", secs/3600, secs/60%60, secs%60);
		}

		public static string PrintTimeRemaining(this TimeSpan timeSpan)
		{
			return PrintTimeRemaining((int)timeSpan.TotalSeconds);
		}

		public static void RaiseAsyncEvent<T>(this EventHandler<T> e, object o, T args) where T: EventArgs
		{
			if (e == null) return;
			ThreadPool.QueueUserWorkItem(delegate
				{
					try
					{
						if (e != null) e(o, args);
					}
					catch (Exception ex)
					{
						Trace.TraceError("Unhandled exception in async event: {0}", ex);
					}
				});
		}

		public static string SafeFormat(string format, params object[] args)
		{
			if (args == null || args.Length <= 0) return format;
			try
			{
				return String.Format(format, args);
			}
			catch
			{
				var ret = "Error format: " + format + " ";
				foreach (var o in args) ret += "," + o;
				return ret;
			}
		}

		public static Thread SafeThread(Action action)
		{
			var t = new Thread(() =>
				{
					try
					{
						action();
					}
					catch (Exception ex)
					{
						Trace.TraceError("Unhandled thread exception: {0}", ex);
					}
				});
			return t;
		}


		/// <summary>
		/// shifts array by given number of indexes
		/// </summary>
		/// <param name="input">input array</param>
		/// <param name="bynum">if bynum is negative, creates shorter array starting at abs(bynum) element of original arraym, if bynum is positive creates new array and makes bynum empty pieces in the beginning</param>
		/// <returns>returned new array</returns>
		public static T[] ShiftArray<T>(T[] input, int bynum)
		{
			var ret = new T[input.Length + bynum];
			if (bynum == 0) input.CopyTo(ret, 0);
			else if (bynum < 0) for (var i = 0; i < ret.Length; ++i) ret[i] = input[i - bynum];
			else if (bynum > 0) for (var i = 0; i < input.Length; ++i) ret[i + bynum] = input[i];
			return ret;
		}

		public static List<T> Shuffle<T>(this IEnumerable<T> source)
		{
			var list = source.ToList();
			ShuffleInPlace(list);
			return list;
		}

		public static void ShuffleInPlace<T>(IList<T> array)
		{
			var rng = new Random();
			var n = array.Count;
			while (n > 1)
			{
				var k = rng.Next(n);
				n--;
				var temp = array[n];
				array[n] = array[k];
				array[k] = temp;
			}
		}

		/// <summary>
		/// Invokes in the threadpool in a non-blocking way
		/// </summary>
		public static void StartAsync(Action action)
		{
			ThreadPool.QueueUserWorkItem(delegate
				{
					try
					{
						action();
					}
					catch (Exception ex)
					{
						Trace.TraceError("Unhandled async exception: {0}", ex);
					}
				});
		}

		/// <summary>
		/// Converts a unicode string to ASCII
		/// </summary>
		public static string ToAscii(this string text)
		{
			return Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.ASCII, Encoding.Unicode.GetBytes(text)));
		}

		public static byte[] ToBytes(this Image image, int size)
		{
			var stream = new MemoryStream();

			var ratio = (float)image.Size.Width/image.Size.Height;
			var newSize = ratio > 1 ? new Size(image.Size.Width, (int)(image.Size.Height/ratio)) : new Size((int)(image.Size.Width*ratio), image.Size.Height);
			var resizedImage = new Bitmap(newSize.Width, newSize.Height, PixelFormat.Format24bppRgb);
			using (var graphics = Graphics.FromImage(resizedImage))
			{
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.DrawImage(image, new Rectangle(Point.Empty, newSize), new Rectangle(Point.Empty, image.Size), GraphicsUnit.Pixel);
			}
			resizedImage.Save(stream, ImageFormat.Jpeg);
			stream.Position = 0;
			return stream.ToArray();
		}

		public static long ToUnix(DateTime t)
		{
			if (t == DateTime.MinValue) return 0;
			return (long)(t.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
		}

		public static long ToUnix(TimeSpan t)
		{
			if (t == TimeSpan.MinValue) return 0;
			return (long)t.TotalSeconds;
		}

		public class ParalellActionContext
		{
			public Exception Error { get; set; }
			public List<EventWaitHandle> WaitHandles = new List<EventWaitHandle>();

			public bool WaitAll(TimeSpan timeout)
			{
				return WaitHandle.WaitAll(WaitHandles.ToArray());
			}

			public bool WaitAll()
			{
				return WaitHandle.WaitAll(WaitHandles.ToArray());
			}
		}

		/// <summary>
		/// Hash password with default hash used by remote server
		/// </summary>
		/// <param Name="pass">string with password</param>
		/// <returns>hash string</returns>
		public static string HashLobbyPassword(string pass)
		{
			var md5 = (MD5)HashAlgorithm.Create("MD5");
			md5.Initialize();
			var hashed = md5.ComputeHash(Encoding.ASCII.GetBytes(pass??""));
			return Convert.ToBase64String(hashed);
		}

	  public static void SafeDelete(string path)
	  {
      try {
        if (File.Exists(path)) {
          File.Delete(path);
        }
      } catch { }
	  }

		public static int GetWinChancePercent(double eloDiff)
		{
			return (int)Math.Round((1.0/(1.0 + Math.Pow(10, (eloDiff)/400.0)))*100.0);
		}
	}
}