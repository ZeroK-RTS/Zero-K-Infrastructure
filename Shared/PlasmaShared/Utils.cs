#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using PlasmaShared.Imaging;

#endregion

namespace PlasmaShared
{
    /// <summary>
    /// General purpose static functions here
    /// </summary>
    public static partial class Utils
    {
        public static void SafeDispose(this IDisposable o)
        {
            if (o != null) o.Dispose();
        }

        public static DateTime CeilingToMinute(this DateTime d)
        {
            var floored = new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0, d.Kind);
            return d == floored ? d : floored.AddMinutes(1);
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


        public static int LowerBoundIndex<T>(this IReadOnlyList<T> list, T value) where T : IComparable<T>
        {
            //Requires:
            //  list is sorted

            //Returns:
            //  Smallest value of i for which: !(list[i] < value)
            //  or list.Count, if there is no such value.

            int first = 0;
            int len = list.Count;

            while (len > 0)
            {
                var half = len / 2;
                var mid = first + half;
                if (list[mid].CompareTo(value) < 0)
                {
                    first = mid + 1;
                    len = len - half - 1;
                }
                else
                {
                    len = half;
                }
            }

            return first;
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

        // PrintTimeRemaining moved to Utils.Enumerable.cs, which the .NET 9 port links.

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
        {
            U val = default(U);
            if (key != null) dict.TryGetValue(key, out val);
            return val;
        }
        
        public static U GetOrDefault<T, U>(this IDictionary<T, U> dict, T key, U defaultValue)
        {
            U val = defaultValue;
            if (key != null) dict.TryGetValue(key, out val);
            return val;
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






        /// <summary>
        /// Hash password with default hash used by remote server
        /// </summary>
        /// <param Name="pass">string with password</param>
        /// <returns>hash string</returns>


        static char[] numbers = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        public static string TrimNumbers(this string input)
        {
            return input.TrimEnd(numbers);
        }




        public class FileResponse<T>
        {
            public T Content;
            public bool WasModified;
            public DateTime DateModified;
        }


        public static FileResponse<byte[]> DownloadFile(string url, DateTime? ifModifiedSince = null)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var ms = new MemoryStream();
            var wc = (HttpWebRequest)HttpWebRequest.Create(new Uri(url));
            var ret = new FileResponse<byte[]>();
            
            wc.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

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

            
            wc.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            
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
        
        

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    Process.Start("xdg-open", url);
                }
                else if (Environment.OSVersion.Platform ==PlatformID.MacOSX)
                {
                    Process.Start("open", url);
                }
                else 
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
            }
        }        
    }
}
