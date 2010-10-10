#region using

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ZeroKLobby.ModStats;

#endregion

namespace ZeroKLobby
{
	public class SpringInfologWatcher
	{
		const string infologName = "infolog.txt";

		readonly string infologPath;
		FileSystemWatcher watch;

		public SpringInfologWatcher(string springPath)
		{
			infologPath = Path.Combine(springPath, infologName);
			watch = new FileSystemWatcher(springPath, infologName) { EnableRaisingEvents = true };
			watch.Changed += watch_Changed;
		}

		public bool WatcherEnabled
		{
			set
			{
				watch.EnableRaisingEvents = value;
			}
		}


		public void ParseInfolog(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				Trace.TraceWarning("Infolog is empty");
				return;
			}
			try
			{
				var modOk = false;
#pragma warning disable 219
				var hasError = false;
#pragma warning restore 219
				var rev = "";
				string modName = null;
				string mapName = null;
				var isCheating = false;
				string gameId = null;
				var statsData = new List<string>();
				foreach (var cycleline in text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
				{
					var line = cycleline;
					var gameframe = 0;
					if (line.StartsWith("["))
					{
						var idx = line.IndexOf("] ");
						if (idx > 0)
						{
							int.TryParse(line.Substring(1, idx - 1), out gameframe);
							if (idx >= 0) line = line.Substring(idx + 2);
						}
					}

					if (!modOk && modName == null && line.StartsWith("Using mod"))
					{
						modName = line.Substring(10);
						if (line.Contains("Complete Annihilation") && !line.Contains("$VERSION"))
						{
							modOk = true;
							var m = Regex.Match(line, "(r[0-9]+)");
							if (m.Success) rev = m.Groups[1].Value;
						}
					}

					if (mapName == null && line.StartsWith("Using map") && modName == null) mapName = line.Substring(10);

					if (line.StartsWith("Using demofile")) return; // do nothing if its demo

					if (line.StartsWith("GameID: ") && gameId == null) gameId = line.Substring(8);

					if (line.StartsWith("ID: ") && !isCheating)
					{
						// game score
						var data = line.Substring(4);
						var score = Convert.ToInt32(Encoding.ASCII.GetString(Convert.FromBase64String(data)));
						using (var service = new EditorService { Proxy = null }) service.SubmitScoreAsync(modName, Program.Conf.LobbyPlayerName, score, gameframe/30);
					}

					if (line.StartsWith("STATS:")) statsData.Add(line.Substring(6));

					if (line.StartsWith("Cheating!")) isCheating = true;

					if (line.StartsWith("Error") || line.StartsWith("LuaRules") || line.StartsWith("Internal error") || line.StartsWith("LuaCOB") ||
					    (line.StartsWith("Failed to load") && !line.Contains("duplicate name"))) hasError = true;
				}
				if (!isCheating && statsData.Count > 1)
				{
					// must be more than 1 line - 1 is player list
					var service = new StatsCollector { Proxy = null };
					try
					{
						service.SubmitGameEx(gameId, modName, mapName, statsData.ToArray());
					}
					catch (Exception ex)
					{
						Trace.TraceError("Error sending game stats: {0}", ex);
					}
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("Error processing spring log: {0}", ex);
			}
		}

		void watch_Changed(object sender, FileSystemEventArgs e)
		{
			if (Utils.CanWrite(e.FullPath))
			{
				try
				{
					var text = File.ReadAllText(e.FullPath);
					ParseInfolog(text);
				}
				catch (Exception ex)
				{
					Trace.TraceError("Error parsing spring infolog: {0}", ex);
				}
			}
		}
	}
}