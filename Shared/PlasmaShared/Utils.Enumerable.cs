using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using System.ComponentModel;

namespace PlasmaShared
{
    /// <summary>
    /// The part of <see cref="Utils"/> that has no framework dependency. Utils.cs itself
    /// pulls in System.Drawing, which on .NET 9 lives in System.Drawing.Common and is
    /// Windows-only - so it cannot be linked into the .NET 9 test project, and these
    /// helpers can. See Tests.Portable/Tests.Portable.csproj.
    /// </summary>
    public static partial class Utils
    {
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
            {
                action(item);
            }
        }
        public static IEnumerable<Type> GetAllTypesWithAttribute<T>()

        {

            var allowedAssemblies = new string[]

            {

                typeof(T).Assembly.GetName().Name,

                Assembly.GetEntryAssembly()?.GetName().Name, Assembly.GetExecutingAssembly().GetName().Name,

                Assembly.GetCallingAssembly().GetName().Name

            };

            

            return from a in AppDomain.CurrentDomain.GetAssemblies().Where(x=> allowedAssemblies.Contains(x.GetName().Name)).ToList().AsParallel()

                   from t in a.GetLoadableTypes()

                   let attributes = t.GetCustomAttributes(typeof(T), true)

                   where attributes != null && attributes.Length > 0

                   select t;

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
        /// <summary>Moved here from Utils.cs, which cannot compile outside .NET Framework -
        /// it is 900 lines of GDI+. This is eleven lines of System.IO.Compression, and
        /// MapsController.Detail needs it to read the map metadata the registrar wrote.</summary>
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

        /// <summary>Moved here from Utils.cs for the same reason as Decompress: that file
        /// is 900 lines of GDI+ and cannot compile on .NET 9. SpotlightHandler needs this.</summary>
        public static string[] Lines(this string source)
        {
            if (source == null) return new string[] { };
            else return source.Replace("\r\n", "\n").Split('\n');
        }

        /// <summary>
        /// Moved here from Utils.cs for the same reason as Decompress and Lines: that file is
        /// 900 lines of GDI+ and cannot compile on .NET 9. GithubController hashes the webhook
        /// body and compares the hex against the X-Hub-Signature header, so the whole controller
        /// was stranded behind one StringBuilder loop.
        /// </summary>
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

        /// <summary>
        /// Moved here from Utils.cs, which is 900 lines of GDI+ and cannot compile on .NET 9.
        /// PlasmaServer deletes a resource's six files through this, so eleven lines that swallow
        /// an exception were the last thing keeping that file - and ContentServiceController
        /// behind it - off the port.
        /// </summary>
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
        public static string StringJoin(this IEnumerable<string> enumeration)
        {
            return string.Join(", ", enumeration);
        }
        public static string HashLobbyPassword(string pass)
        {
            var md5 = (MD5)HashAlgorithm.Create("MD5");
            md5.Initialize();
            var hashed = md5.ComputeHash(Encoding.ASCII.GetBytes(pass ?? ""));
            return Convert.ToBase64String(hashed);
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

        /// <summary>
        /// The [Description] of an enum value. Moved here from Utils.cs, which cannot
        /// compile outside .NET Framework; this is pure reflection and several views use it.
        /// </summary>
        public static string Description(this Enum e)
        {
            var da = (DescriptionAttribute[])(e.GetType().GetField(e.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false));
            return da.Length > 0 ? da[0].Description : e.ToString();
        }

        // Wanted by ForumParser/Tags/HeaderTag.cs, which the .NET 9 projects link.
        public static string StripInvalidLobbyNameChars(string name)
        {
            if (String.IsNullOrEmpty(name)) return name;
            var sb = new StringBuilder();
            foreach (var c in name.Where(Utils.ValidLobbyNameCharacter)) sb.Append(c);
            return sb.ToString();
        }

        // Wanted by PlanetwarsAdminController, which the .NET 9 projects will link.
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
        /// Pure string formatting, moved here from Utils.cs so the port can link it.
        /// Read by Planetwars/PwMatchMaker.cshtml and Missions/Detail.cshtml, and by the game
        /// client, which is why it stays in PlasmaShared rather than moving to the website.
        /// </summary>
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

        /// <summary>
        /// Pairs each item with its position. Pure generics with no dependencies, moved here
        /// from Utils.cs so LaddersController can be linked - the eighth thing found stranded
        /// in the half that needs System.Drawing.
        /// </summary>
        public static IEnumerable<Indexed<T>> ToIndexedList<T>(this IEnumerable<T> enumeration)
        {
            return enumeration.Select((x, i) => new Indexed<T>(x, i));
        }
    }

    /// <summary>Beside the class, as it was in Utils.cs.</summary>
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
