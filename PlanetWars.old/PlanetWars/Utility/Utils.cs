using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PlanetWars.Utility
{
	public static class Utils
	{
		public static string MakePath(params string[] directories)
		{
			string s = Path.DirectorySeparatorChar.ToString();

			string path = string.Join(s, directories);
			path = (s == "/") ? path.Replace("\\", "/") : path.Replace("/", "\\");
			while (path.Contains(s + s)) path = path.Replace(s + s, s);
			if (path.EndsWith(s)) path = path.Substring(0, path.Length - 1);
			// Console.WriteLine("===> " + path);
			return path;
		}


		public static string GetAlternativeFileName(string to)
		{
			if (File.Exists(to))
			{
				string ext = Path.GetExtension(to);
				string name = Path.GetFileNameWithoutExtension(to);
				string dir = Path.GetDirectoryName(to);
				int i = 1;
				do
				{
					to = MakePath(dir, name + "(" + i++ + ")" + ext);
				} while (File.Exists(to));
			}
			return to;
		}
	}
}
