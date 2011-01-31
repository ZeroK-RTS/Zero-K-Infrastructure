#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using PlasmaShared;
using PlasmaShared.ContentService;

#endregion

namespace LobbyClient
{
	public class SpringLogEventArgs: EventArgs
	{
		readonly string line;
		readonly string username;


		public string Line { get { return line; } }

		public string Username { get { return username; } }

		public SpringLogEventArgs(string username): this(username, "") {}

		public SpringLogEventArgs(string username, string line)
		{
			this.line = line;
			this.username = username;
		}
	} ;

	/// <summary>
	/// represents one install location of spring game
	/// </summary>
	public class Spring
	{
    public class MyStatsPlayer : BattlePlayerResult
    {

      public void AddAward(string award, string description)
      {
        awardList.Add(new PlayerAward() { Award = award, Description = description });
        Awards = awardList.ToArray();
      }
      
      // awards stored here because Awards is array and hard to modify
      List<PlayerAward> awardList = new List<PlayerAward>();
    }

		public const int MaxAllies = 16;
		public const int MaxTeams = 32;

		public delegate void LogLine(string text, bool isError);

		Dictionary<string, int> PlanetWarsMessages = new Dictionary<string, int>();

		DateTime gameEnded;

		DateTime gameStarted;
		readonly SpringPaths paths;


		Process process;
		readonly List<string> readyPlayers = new List<string>();
		string scriptPath;
		Talker talker;

		public DateTime GameEnded { get { return gameEnded; } }

		public DateTime GameStarted { get { return gameStarted; } }

		public bool IsRunning
		{
			get
			{
				try
				{
					return (process != null && !process.HasExited);
				}
				catch (Exception ex)
				{
					Trace.TraceError("Error determining process state: {0}", ex);
					return false;
				}
			}
		}
		public StringBuilder LogLines = new StringBuilder();


		public ProcessPriorityClass ProcessPriority { set { if (IsRunning) process.PriorityClass = value; } }
		public bool UseDedicatedServer;
		string lobbyUserName;
		string lobbyPassword;

		public event EventHandler<SpringLogEventArgs> GameOver; // game has ended
		public event LogLine LogLineAdded = delegate { };
		public event EventHandler<SpringLogEventArgs> PlayerDisconnected;
		public event EventHandler<SpringLogEventArgs> PlayerJoined;
		public event EventHandler<SpringLogEventArgs> PlayerLeft;
		public event EventHandler<SpringLogEventArgs> PlayerLost; // player lost the game
		public event EventHandler<SpringLogEventArgs> PlayerSaid;
		//public event EventHandler<> 
		/// <summary>
		/// Data is true if exit was crash
		/// </summary>
		public event EventHandler<EventArgs<bool>> SpringExited;
		public event EventHandler SpringStarted;

		public Spring(SpringPaths springPaths)
		{
			paths = springPaths;
		}

		/// <summary>
		/// Adds user dynamically to running game - for security reasons add his script
		/// </summary>
		public void AddUser(string name, string scriptPassword)
		{
			if (IsRunning) talker.SendText(string.Format("/adduser {0} {1}", name, scriptPassword));
		}

		public void ExitGame()
		{
			try
			{
				if (IsRunning)
				{
					SayGame("/kill"); // todo dont do this if talker does not work (not a host)
					process.WaitForExit(2000);
					if (!IsRunning) return;
					process.Kill();
					;
					process.WaitForExit(1000);
					if (!IsRunning) return;
					process.Kill();
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("Error quitting spring: {0}", ex);
			}
		}

		public void ForceStart()
		{
			if (IsRunning) talker.SendText("/forcestart");
		}

		public bool IsPlayerReady(string name)
		{
			return readyPlayers.Contains(name);
		}


		public void Kick(string name)
		{
			SayGame("/kick " + name);
		}


		public void SayGame(string text)
		{
			if (IsRunning) talker.SendText(text);
		}

		void ParseInfolog(string text, bool isCrash)
		{
			if (string.IsNullOrEmpty(text))
			{
				Trace.TraceWarning("Infolog is empty");
				return;
			}
			try
			{
				var modOk = false;
				var hasError = false;
				var rev = "";
				string modName = null;
				string mapName = null;
				var isCheating = false;
				string gameId = null;
        string demoFileName = null;
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
						if (line.Contains("Zero-K") && !line.Contains("$VERSION"))
						{
							modOk = true;
							var m = Regex.Match(line, "(r[0-9]+)");
							if (m.Success) rev = m.Groups[1].Value;
						}
					}

					if (mapName == null && line.StartsWith("Using map") && modName == null) mapName = line.Substring(10);

          if (line.StartsWith("Recording demo "))
          {
            demoFileName = line.Substring(15);
          }

				  if (line.StartsWith("Using demofile")) return; // do nothing if its demo

					if (line.StartsWith("GameID: ") && gameId == null) gameId = line.Substring(8);

					if (line.StartsWith("LuaRules: >> ID: ") && !isCheating && !isMission)
					{
						// game score
						var data = Encoding.ASCII.GetString(Convert.FromBase64String(line.Substring(17)));
						var parts = data.Split('/');
						int score = 0;
						if (parts.Length > 1)
						{
							score = Convert.ToInt32(parts[1]);
							gameframe = Convert.ToInt32(parts[0]);
						}
						else score = Convert.ToInt32(data);


						using (var service = new ContentService { Proxy = null })
						{
							service.SubmitMissionScoreCompleted += (s, e) =>
								{
									if (e.Error != null)
									{
										if (e.Error is WebException) Trace.TraceWarning("Error sending score: {0}",e.Error);
										else Trace.TraceError("Error sending score: {0}", e.Error);
									}

								};
							service.SubmitMissionScoreAsync(lobbyUserName,
																							Utils.HashLobbyPassword(lobbyPassword),
																							modName,
																							score,
																							gameframe / 30);
						}
					}

					if (line.StartsWith("STATS:")) statsData.Add(line.Substring(6));

					if (line.StartsWith("Cheating!")) isCheating = true;

					if (line.StartsWith("Error") || line.StartsWith("LuaRules") || line.StartsWith("Internal error") || line.StartsWith("LuaCOB") ||
							(line.StartsWith("Failed to load") && !line.Contains("duplicate name"))) hasError = true;
				}


        // submit main stats
        if (!isCheating && !isCrash && modOk && isHosting)
        {
          var service = new ContentService() { Proxy = null };
          try {
            service.SubmitSpringBattleResult(lobbyUserName,
                                             lobbyPassword,
                                             gameId,
                                             paths.SpringVersion,
                                             modName,
                                             mapName,
                                             isMission,
                                             isBots,
                                             demoFileName,
                                             gameStarted,
                                             (int)gameEnded.Subtract(gameStarted).TotalSeconds,
                                             title,
                                             statsPlayers.Values.ToArray());

          } catch (Exception ex)
          {
            Trace.TraceError("Error sending game stats: {0}", ex);
          }
        }


			  // submit modstats
				if (!isCheating && statsData.Count > 1 && modOk)
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

    Dictionary<string,MyStatsPlayer> statsPlayers = new Dictionary<string, MyStatsPlayer>();
	  bool isHosting;
	  bool isMission;
	  bool isBots;
	  string title;


	  /// <summary>
		/// Starts spring game
		/// </summary>
		/// <param name="client">tasclient to get current battle from</param>
		/// <param name="priority">spring process priority</param>
		/// <param name="affinity">spring process cpu affinity</param>
		/// <param name="scriptOverride">if set, overrides generated script with supplied one</param>
		/// <param name="userName">lobby user name - used to submit score</param>
		/// <param name="passwordHash">lobby password hash - used to submit score</param>
		/// <returns>generates script</returns>
		public string StartGame(TasClient client, ProcessPriorityClass? priority, int? affinity, string scriptOverride)
		{
      if (!File.Exists(paths.Executable) && !File.Exists(paths.DedicatedServer)) throw new ApplicationException("Spring or dedicated server executable not found");

			if (!IsRunning)
			{
			  this.lobbyUserName = client.UserName;
        this.lobbyPassword = client.UserPassword;

				talker = new Talker();
				readyPlayers.Clear();
				talker.SpringEvent += talker_SpringEvent;
			  isHosting = client != null && client.MyBattle != null && client.MyBattle.Founder == client.MyUser.Name;

				if (isHosting) scriptPath = Utils.MakePath(paths.WritableDirectory, "script_" + client.MyBattle.Founder + ".txt").Replace('\\', '/');
				else scriptPath = Utils.MakePath(paths.WritableDirectory, "script.txt").Replace('\\', '/');

			  statsPlayers.Clear();

				string script;
				if (!string.IsNullOrEmpty(scriptOverride))
				{
				  isMission = true;
				  script = scriptOverride;
				}
				else
				{
					List<UserBattleStatus> players;
          script = client.MyBattle.GenerateScript(out players, client.MyUser, talker.LoopbackPort);
				  isMission = client.MyBattle.IsMission;
				  isBots = client.MyBattle.Bots.Any();
				  title = client.MyBattle.Title;
          talker.SetPlayers(players);
				  statsPlayers = players.ToDictionary(x => x.Name,
				                                      x =>
				                                      new MyStatsPlayer
				                                      {
				                                        AccountID = x.LobbyUser.AccountID,
				                                        AllyNumber = x.AllyNumber,
				                                        CommanderType = null,
				                                        // todo commandertype
				                                        IsSpectator = x.IsSpectator,
				                                        IsVictoryTeam = false,
				                                        Rank = x.LobbyUser.Rank,
				                                      });
				}

				File.WriteAllText(scriptPath, script);

				LogLines = new StringBuilder();

				process = new Process();
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.Arguments += string.Format("\"{0}\"", scriptPath);

				if (UseDedicatedServer)
				{
					process.StartInfo.FileName = paths.DedicatedServer;
					process.StartInfo.WorkingDirectory = Path.GetDirectoryName(paths.DedicatedServer);
				}
				else
				{
					process.StartInfo.FileName = paths.Executable;
					process.StartInfo.WorkingDirectory = Path.GetDirectoryName(paths.Executable);
				}
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.Exited += springProcess_Exited;
				process.ErrorDataReceived += process_ErrorDataReceived;
				process.OutputDataReceived += process_OutputDataReceived;
				process.EnableRaisingEvents = true;

				PlanetWarsMessages = new Dictionary<string, int>();
				gameStarted = DateTime.Now;
				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
				if (IsRunning && SpringStarted != null) SpringStarted(this, EventArgs.Empty);

				Utils.StartAsync(() =>
					{
						Thread.Sleep(1000);
						try
						{
							if (priority != null) process.PriorityClass = priority.Value;
							if (affinity != null) process.ProcessorAffinity = (IntPtr)affinity.Value;
						}
						catch (Exception ex)
						{
							Trace.TraceWarning("Error setting spring process affinity: {0}", ex);
						}
					});

				return script;
			}
			else Trace.TraceError("Spring already running");
			return null;
		}


    public enum SpringMessageType
    {
      pwaward,
      pwmorph,
      pwpurchase,
      pwdeath
    }

    private void StatsMarkDead(string name, bool isDead)
    {
      MyStatsPlayer sp;
      if (statsPlayers.TryGetValue(name, out sp))
      {
        sp.LoseTime = isDead ? (int)DateTime.Now.Subtract(gameStarted).TotalSeconds : (int?)null;
      }
    }

    

	  void HandleSpecialMessages(Talker.SpringEventArgs e)
		{
            if (e.Param >= Talker.TO_EVERYONE_LEGACY) return;

            int count;
            if (!PlanetWarsMessages.TryGetValue(e.Text, out count)) count = 0;
            count++;
            PlanetWarsMessages[e.Text] = count;
            if (count != 2) return; // only send if count matches 2 exactly

		      var text = e.Text;
            string txtOrig = text;
            SpringMessageType type = SpringMessageType.pwaward;
            bool found = false;

            foreach (SpringMessageType option in (SpringMessageType[])Enum.GetValues(typeof(SpringMessageType))) {
              string prefix = option + ":";
              if (text.StartsWith(prefix)) {
                text = text.Substring(prefix.Length);
                type = option;
                found = true;
                break;
              }
            }
            if (!found) {
              Trace.TraceWarning("Unexpected message: " + text);
              return;
            }

            string[] parts = text.Split(new[] { ',' });
            try {
              switch (type) {
                case SpringMessageType.pwaward:
                  string[] partsSpace = text.Split(new[] { ' ' }, 3);
                  string name = partsSpace[0];
                  string awardType = partsSpace[1];
                  string awardText = partsSpace[2];
                  MyStatsPlayer sp;
                  if (statsPlayers.TryGetValue(name, out sp))
                  {
                    sp.AddAward(awardType, awardText);
                  }
                  break;

                /*case SpringMessageType.pwmorph:
                  server.UnitDeployed(account,
                                      tas.MyBattle.MapName,
                                      parts[0],
                                      parts[1],
                                      (int)double.Parse(parts[2]),
                                      (int)double.Parse(parts[3]),
                                      parts[4]);
                  break;

                case SpringMessageType.pwpurchase:
                  server.UnitPurchased(account,
                                       parts[0],
                                       parts[1],
                                       double.Parse(parts[2]),
                                       (int)double.Parse(parts[3]),
                                       (int)double.Parse(parts[4]));
                  break;

                case SpringMessageType.pwdeath:
                  server.UnitDied(account, parts[0], parts[1], (int)double.Parse(parts[3]), (int)double.Parse(parts[4]));
                  break;*/

              }
            } catch (Exception ex) {
              Trace.TraceError("Error while processing '{0}' :{1}", txtOrig, ex);
            }
		}

		void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			LogLineAdded(e.Data, true);
		}

		void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			LogLines.AppendLine(e.Data);
			LogLineAdded(e.Data, false);
		}

		void springProcess_Exited(object sender, EventArgs e)
		{
			var isCrash = process.ExitCode != 0;
			process = null;
			talker.Close();
			talker = null;
			gameEnded = DateTime.Now;

			var logText = LogLines.ToString();
			ParseInfolog(logText, isCrash);
			
			try
			{
				File.WriteAllText(Path.Combine(paths.WritableDirectory, "infolog.txt"), logText);
			}
			catch (Exception ex)
			{
				Trace.TraceWarning("Error saving infolog: {0}", ex);
			}

			if (SpringExited != null) SpringExited(this, new EventArgs<bool>(isCrash));
		}

		void talker_SpringEvent(object sender, Talker.SpringEventArgs e)
		{
			switch (e.EventType)
			{
				case Talker.SpringEventType.PLAYER_JOINED:
					if (PlayerJoined != null) PlayerJoined(this, new SpringLogEventArgs(e.PlayerName));
					break;

				case Talker.SpringEventType.PLAYER_LEFT:
					if (e.Param == 0 && PlayerDisconnected != null)
					{
					  PlayerDisconnected(this, new SpringLogEventArgs(e.PlayerName));
					}
					if (PlayerLeft != null) PlayerLeft(this, new SpringLogEventArgs(e.PlayerName));

					break;

				case Talker.SpringEventType.PLAYER_CHAT:

					HandleSpecialMessages(e);

					// only public chat
					if (PlayerSaid != null && (e.Param == Talker.TO_EVERYONE || e.Param == Talker.TO_EVERYONE_LEGACY)) PlayerSaid(this, new SpringLogEventArgs(e.PlayerName, e.Text));
					break;

				case Talker.SpringEventType.PLAYER_DEFEATED:
			    StatsMarkDead(e.PlayerName, true);
  				if (PlayerLost != null) PlayerLost(this, new SpringLogEventArgs(e.PlayerName));
					break;

				case Talker.SpringEventType.SERVER_GAMEOVER:
					if (GameOver != null) GameOver(this, new SpringLogEventArgs(e.PlayerName));
					break;

				case Talker.SpringEventType.PLAYER_READY:
					if (e.Param == 1) readyPlayers.Add(e.PlayerName);
					break;

				case Talker.SpringEventType.SERVER_QUIT:
					//Program.main.AutoHost.SayBattle("dbg quit ");
					//if (GameOver != null) GameOver(this, new SpringLogEventArgs(e.PlayerName));
					break;
			}
		}
	}
}