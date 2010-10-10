#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

#endregion

namespace PlasmaShared
{
	public class SpringPaths
	{
		string springVersion;
		public string Cache { get; set; }
		public List<string> DataDirectories { get; private set; }
		public string DedicatedServer { get; set; }
		public string Executable { get; set; }

		public string SpringVersion
		{
			get
			{
				if (springVersion == "fail") return null;
				if (springVersion != null) return springVersion;

				if (string.IsNullOrEmpty(Executable)) throw new ApplicationException("Version can only be determined after executable path is known");
				try
				{
					var p = new Process();
					p.StartInfo = new ProcessStartInfo(Executable, "--version") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
					p.Start();
					var data = p.StandardOutput.ReadToEnd();
					data = data.Trim();
					var match = Regex.Match(data, "Spring (\\d+\\.\\d+\\.\\d+)\\.\\d+.*");
					if (match.Success) springVersion = match.Groups[1].Value;
				}
				catch (Exception ex)
				{
					Trace.TraceError("Error determining spring version: {0}", ex);
				}
				return springVersion;
			}
		}

		public string UnitSyncDirectory { get; set; }

		public string WritableDirectory { get; set; }


		public static string GetMySpringDocPath()
		{
			if (Environment.OSVersion.Platform == PlatformID.Unix) return Utils.MakePath(Environment.GetEnvironmentVariable("HOME"), ".spring");
			else
			{
				var dir = Utils.MakePath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Spring");
				if (!IsDirectoryWritable(dir))
				{
					//if not writable - this should be writable
					dir = Utils.MakePath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Spring");
				}
				return dir;
			}
		}

		public static string GetSpringConfigPath()
		{
			if (Environment.OSVersion.Platform == PlatformID.Unix) return Utils.MakePath(Environment.GetEnvironmentVariable("HOME"), ".springrc");
			else return Utils.MakePath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "springsettings.cfg");
		}

		public static bool IsDirectoryWritable(string directory)
		{
			try
			{
				try
				{
					if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
				}
				catch
				{
					return false;
				}

				var fullPath = Utils.GetAlternativeFileName(Path.Combine(directory, "test.dat"));
				File.WriteAllText(fullPath, "test");
				if (File.Exists(fullPath))
				{
					File.ReadAllLines(fullPath);
					File.Delete(fullPath);
					return true;
				}
			}
			catch {}
			return false;
		}

		public void MakeFolders()
		{
			CreateFolder(Utils.MakePath(WritableDirectory, "mods"));
			CreateFolder(Utils.MakePath(WritableDirectory, "maps"));
			CreateFolder(Utils.MakePath(WritableDirectory, "packages"));
			CreateFolder(Utils.MakePath(WritableDirectory, "pool"));
			if (!string.IsNullOrEmpty(Cache)) CreateFolder(Cache);
		}


		public void SetWindowsPaths(string springPath)
		{
			DataDirectories = new List<string> { DetectSpringConfigDataPath(GetSpringConfigPath()), GetMySpringDocPath(), springPath };

			DataDirectories = DataDirectories.Where(Directory.Exists).ToList();

			WritableDirectory = DataDirectories.First(IsDirectoryWritable);
			UnitSyncDirectory = springPath;
			Executable = Utils.MakePath(springPath, "Spring.exe");
		}

		void CreateFolder(string path)
		{
			if (!Directory.Exists(path)) Directory.CreateDirectory(path);
		}


		// can be null
		string DetectSpringConfigDataPath(string configPath)
		{
			string springDataPath = null;
			try
			{
				foreach (var line in File.ReadAllLines(configPath))
				{
					var kvp = line.Split('=');
					if (kvp.Length == 2 && kvp[0] == "SpringData" && kvp[1] != String.Empty) springDataPath = kvp[1];
				}
			}
			catch (Exception e)
			{
				Trace.WriteLine("Unable to open springrc: " + e);
			}
			return springDataPath;
		}
	}
}