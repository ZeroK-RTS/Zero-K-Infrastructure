#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PlasmaShared;
using Encoder = System.Drawing.Imaging.Encoder;

#endregion

namespace ZkData
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

        public static IEnumerable<Indexed<T>> ToIndexedList<T>(this IEnumerable<T> enumeration)
        {
            return enumeration.Select((x, i) => new Indexed<T>(x, i));
        }

        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
            {
                action(item);
            }
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            //from https://stackoverflow.com/questions/489258/linqs-distinct-on-a-particular-property
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        public static string StringJoin(this IEnumerable<string> enumeration)
        {
            return string.Join(", ", enumeration);
        }


        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null)
        {
            return new HashSet<T>(source, comparer);
        }

        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        public static void RenameWithOverwrite(this string sourceFile, string targetFile)
        {
            if (File.Exists(targetFile)) File.Delete(targetFile);
            File.Move(sourceFile, targetFile);
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

        /*public static double StdDev(this IEnumerable<double> values)
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
        }*/



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
            if (n > 1)
                stdDev = Math.Sqrt(sum / (n - 1));

            return stdDev;
        }

        public static double StdDevSquared(this IEnumerable<double> values)
        {
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
            if (n > 1) stdDev = sum / (n - 1);

            return stdDev;
        }




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
                catch { }
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

        public static Bitmap GetResized(this Image original, int newWidth, int newHeight, InterpolationMode mode = InterpolationMode.HighQualityBicubic)
        {
            var resized = new Bitmap(newWidth, newHeight);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = mode;
                g.DrawImage(original, 0, 0, newWidth, newHeight);
            }
            return resized;
        }

        public static Image GetResizedWithCache(this Image original, int newWidth, int newHeight, InterpolationMode mode = InterpolationMode.HighQualityBicubic)
        {
            return ResizedImageCache.Instance.GetResizedWithCache(original, newWidth, newHeight, mode);
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

        public static void SaveJpeg(this Image img, string path, int quality)
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
            if (String.IsNullOrEmpty(path)) return path;
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

        public static IEnumerable<string> SplitEvery(this string s, int length)
        {
            return s.Where((c, index) => index % length == 0)
                   .Select((c, index) => String.Concat(
                        s.Skip(index * length).Take(length)
                     )
                   );
        }

        /// <summary>
        /// Creates paths in a cross-platform way.
        /// </summary>
        public static string MakePath(params string[] directories)
        {
            return Path.Combine(directories);
        }


        public static string PrintByteLength(long bytes)
        {
            if (bytes < 1024) return bytes.ToString();
            if (bytes < 1024 * 1024) return ((double)bytes / 1024).ToString("F2") + "k";
            if (bytes < 1024 * 1024 * 1024) return ((double)bytes / 1024 / 1024).ToString("F2") + "M";
            return ((double)bytes / 1024 / 1024 / 1024).ToString("F2") + "G";
        }

        public static string PrintTimeRemaining(long secs)
        {
            if (secs <= 0) return "";
            if (secs < 60) return String.Format("{0}s", secs);
            if (secs < 3600) return String.Format("{0}m {1}s", secs / 60, secs % 60);
            return String.Format("{0}h {1}m {2}s", secs / 3600, secs / 60 % 60, secs % 60);
        }

        public static string PrintTimeRemaining(this TimeSpan timeSpan)
        {
            return PrintTimeRemaining((int)timeSpan.TotalSeconds);
        }

        public static void RaiseAsyncEvent<T>(this EventHandler<T> e, object o, T args) where T : EventArgs
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



        public static U Get<T, U>(this IDictionary<T, U> dict, T key)
    where U : class
        {
            U val = default(U);
            if (key != null) dict.TryGetValue(key, out val);
            return val;
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

        public static string ExecuteConsoleCommand(string command, string args = null)
        {
            string response = null;
            try
            {
                var pi = new ProcessStartInfo(command, args) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, };
                var p = Process.Start(pi);
                p.Start();
                response = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error executing {0} {1}: {2}", command, args, ex.Message);
            }
            return response;
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



        public static byte[] ToBytes(this Image image, int size)
        {
            var stream = new MemoryStream();

            var ratio = (float)image.Size.Width / image.Size.Height;
            var newSize = ratio > 1 ? new Size(image.Size.Width, (int)(image.Size.Height / ratio)) : new Size((int)(image.Size.Width * ratio), image.Size.Height);
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



        /// <summary>
        /// Hash password with default hash used by remote server
        /// </summary>
        /// <param Name="pass">string with password</param>
        /// <returns>hash string</returns>
        public static string HashLobbyPassword(string pass)
        {
            var md5 = (MD5)HashAlgorithm.Create("MD5");
            md5.Initialize();
            var hashed = md5.ComputeHash(Encoding.ASCII.GetBytes(pass ?? ""));
            return Convert.ToBase64String(hashed);
        }

        static char[] numbers = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        public static string TrimNumbers(this string input)
        {
            return input.TrimEnd(numbers);
        }

        public static void SafeDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch { }
        }


        public static string ToHex(this byte[] array)
        {
            var sb = new StringBuilder();

            for (var i = 0; i < array.Length; i++)
            {
                var hex = array[i].ToString("X");
                if (hex.Length != 2) sb.Append("0");
                sb.Append(hex);
            }
            return sb.ToString();
        }


        public class FileResponse<T>
        {
            public T Content;
            public bool WasModified;
            public DateTime DateModified;
        }


        public static FileResponse<byte[]> DownloadFile(string url, DateTime? ifModifiedSince = null)
        {
            var ms = new MemoryStream();
            var wc = (HttpWebRequest)HttpWebRequest.Create(new Uri(url));
            var ret = new FileResponse<byte[]>();

            if (ifModifiedSince != null) wc.IfModifiedSince = ifModifiedSince.Value;

            try
            {
                using (var response = (HttpWebResponse)wc.GetResponse())
                {
                    ret.WasModified = true;
                    ret.DateModified = response.LastModified;

                    using (var stream = response.GetResponseStream())
                    {
                        stream.CopyTo(ms);
                        ret.Content = ms.ToArray();
                        return ret;
                    }
                }
            }
            catch (WebException e)
            {
                if (e.Response != null && ((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.NotModified) return ret;
                throw;
            }
        }


        public static FileResponse<string> DownloadString(string url, DateTime? ifModifiedSince = null)
        {
            var file = DownloadFile(url, ifModifiedSince);
            return new FileResponse<string>()
            {
                WasModified = file.WasModified,
                DateModified = file.DateModified,
                Content = file.Content != null ? Encoding.UTF8.GetString(file.Content) : null
            };
        }


        public static async Task<FileResponse<byte[]>> DownloadFileAsync(string url, DateTime? ifModifiedSince = null)
        {
            var ms = new MemoryStream();
            var wc = (HttpWebRequest)HttpWebRequest.Create(new Uri(url));
            var ret = new FileResponse<byte[]>();

            if (ifModifiedSince != null) wc.IfModifiedSince = ifModifiedSince.Value;

            try
            {
                using (var response = (HttpWebResponse)await wc.GetResponseAsync().ConfigureAwait(false))
                {
                    ret.WasModified = true;
                    ret.DateModified = response.LastModified;

                    using (var stream = response.GetResponseStream())
                    {
                        await stream.CopyToAsync(ms).ConfigureAwait(false);
                        ret.Content = ms.ToArray();
                        return ret;
                    }
                }
            }
            catch (WebException e)
            {
                if (e.Response != null && ((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.NotModified) return ret;
                throw;
            }
        }

        public static async Task<FileResponse<string>> DownloadStringAsync(string url, DateTime? ifModifiedSince = null)
        {
            var file = await DownloadFileAsync(url, ifModifiedSince).ConfigureAwait(false);
            return new FileResponse<string>()
            {
                WasModified = file.WasModified,
                DateModified = file.DateModified,
                Content = file.Content != null ? Encoding.UTF8.GetString(file.Content) : null
            };
        }

        public static string Description(this Enum e)
        {
            var da = (DescriptionAttribute[])(e.GetType().GetField(e.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false));
            return da.Length > 0 ? da[0].Description : e.ToString();
        }

        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }


        public static IEnumerable<Type> GetAllTypesWithAttribute<T>()
        {
            return from a in AppDomain.CurrentDomain.GetAssemblies().AsParallel()
                   from t in a.GetLoadableTypes()
                   let attributes = t.GetCustomAttributes(typeof(T), true)
                   where attributes != null && attributes.Length > 0
                   select t;
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
            else if (bynum < 0) for (int i = 0; i < ret.Length; ++i) ret[i] = input[i - bynum];
            else if (bynum > 0) for (int i = 0; i < input.Length; ++i) ret[i + bynum] = input[i];
            return ret;
        }

        public static int VerifyUdpPort(int port)
        {
            try
            {
                var ports = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().OrderBy(x => x.Port).Select(x => x.Port).ToList();
                if (ports.Contains(port))
                {
                    var blockedPort = ports;
                    while (ports.Contains(port)) port++;
                    Trace.TraceWarning("Host port {0} was used, using backup port {1}", blockedPort, port);
                }
            }
            catch { }
            return port;
        }

        /// <summary>
        /// Converts wait handle into awaitable task
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static Task AsTask(this WaitHandle handle, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<object>();
            var registration = ThreadPool.RegisterWaitForSingleObject(handle, (state, timedOut) =>
            {
                var localTcs = (TaskCompletionSource<object>)state;
                if (timedOut)
                    localTcs.TrySetCanceled();
                else
                    localTcs.TrySetResult(null);
            }, tcs, timeout, executeOnlyOnce: true);
            tcs.Task.ContinueWith((_, state) => ((RegisteredWaitHandle)state).Unregister(null), registration, TaskScheduler.Default);
            return tcs.Task;
        }



        public static string Truncate(this string input, int length)
        {
            if (input == null) return input;
            if (input.Length < length) return input;
            return input.Substring(0, length);
        }

        public static bool ValidLobbyNameCharacter(char c)
        {
            if (c >= 'a' && c <= 'z') return true;
            if (c >= 'A' && c <= 'Z') return true;
            if (c >= '0' && c <= '9') return true;
            if (c == '_') return true;
            if (c == '[' || c == ']') return true;
            return false;
        }

        public static string StripInvalidLobbyNameChars(string name)
        {
            if (String.IsNullOrEmpty(name)) return name;
            var sb = new StringBuilder();
            foreach (var c in name.Where(Utils.ValidLobbyNameCharacter)) sb.Append(c);
            return sb.ToString();
        }

        public static string GetMyInstallID()
        {

            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "chobbyla");
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "chobbyla"); 
            }
            var path = Path.Combine(dir, "id.txt");
            System.IO.Directory.CreateDirectory(dir);
            if (!File.Exists(path))
            {
                string guid = Guid.NewGuid().ToString();
                File.WriteAllText(path, guid);
            }
            return File.ReadAllText(path);
        }

        public static long GetMyUserID()
        {
            try
            {
                var nics =
                    NetworkInterface.GetAllNetworkInterfaces()
                        .Where(
                            x =>
                                !String.IsNullOrWhiteSpace(x.GetPhysicalAddress().ToString()) &&
                                (x.NetworkInterfaceType != NetworkInterfaceType.Loopback) && (x.NetworkInterfaceType != NetworkInterfaceType.Tunnel));

                var wantedNic = nics.FirstOrDefault();

                if (wantedNic != null) return Crc.Crc32(wantedNic.GetPhysicalAddress().GetAddressBytes());
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Failed to get the userID: {0}", ex);
            }
            return 0;
        }


        public static T Pin<T>(this Array array, Func<IntPtr, T> action)
        {
            var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                return action(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// Initializes a struct from binary data
        /// </summary>
        public static T ToStruct<T>(this byte[] buffer)
        {
            return buffer.Pin(p => (T)Marshal.PtrToStructure(p, typeof(T)));
        }

        /// <summary>
        /// Initializes a struct from a stream (reads only the reqired bytes)
        /// </summary>
        public static T ReadStruct<T>(this Stream stream)
        {
            var buffer = new byte[Marshal.SizeOf(typeof(T))];
            stream.Read(buffer, 0, buffer.Length);
            return buffer.ToStruct<T>();
        }

        /// <summary>
        /// Initializes a struct from a binary reader (reads only the reqired bytes)
        /// </summary>
        public static T ReadStruct<T>(this BinaryReader reader)
        {
            return reader.ReadBytes(Marshal.SizeOf(typeof(T))).ToStruct<T>();
        }

        public static DateTime UnixToDateTime(this UInt64 secondsFrom1970)
        {
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(secondsFrom1970).ToLocalTime();
            return dtDateTime;
        }

        public static int? ToInt(this string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            int intval;
            if (int.TryParse(value, out intval)) return intval;
            return null;
        }
        
        public static string Base64Encode(this string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(this string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
        
    }

    public struct Indexed<T>
    {
        public readonly T Item;
        public readonly int Index;

        public Indexed(T item, int index)
        {
            Item = item;
            Index = index;
        }
    }
}