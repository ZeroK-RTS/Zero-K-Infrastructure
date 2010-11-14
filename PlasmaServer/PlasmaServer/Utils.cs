#region using

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Text;

#endregion

namespace PlasmaServer
{
    /// <summary>
    /// General purpose static functions here
    /// </summary>
    public static class Utils
    {

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


        public static string EscapePath(this string path)
        {
            StringBuilder escaped = new StringBuilder();
            foreach (var c in path)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '(' || c == ')' || c == '.') escaped.Append(c);
                else escaped.Append('_');
            }
            return escaped.ToString();
        }



        /// <summary>
        /// Converts a unicode string to ASCII
        /// </summary>
        public static string ToAscii(this string text)
        {
            return Encoding.ASCII.GetString(Encoding.Convert(Encoding.Unicode, Encoding.ASCII, Encoding.Unicode.GetBytes(text)));
        }

    	public static void SafeDelete(string path)
    	{
    		if (File.Exists(path)) File.Delete(path);
    	}
    }
}