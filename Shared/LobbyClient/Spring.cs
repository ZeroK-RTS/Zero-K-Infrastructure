#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using PlasmaShared;

#endregion

namespace LobbyClient
{
	public class SpringLogEventArgs: EventArgs
	{
		readonly List<string> args = new List<string>();
		readonly string line;
		readonly string username;

		public List<string> Args { get { return args; } }

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
		public const int MaxAllies = 16;
		public const int MaxTeams = 32;

		public delegate void LogLine(string text, bool isError);

		Dictionary<string, int> PlanetWarsMessages = new Dictionary<string, int>();

		DateTime gameEnded;

		DateTime gameStarted;
		readonly SpringPaths paths;
		//    const string PathDivider = "/";

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
			if (!File.Exists(paths.Executable) && !File.Exists(paths.DedicatedServer)) throw new ApplicationException("Spring or dedicated server executable not found");
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

		/// <summary>
		/// Starts spring game
		/// </summary>
		/// <param name="client">tasclient to get current battle from</param>
		/// <param name="priority">spring process priority</param>
		/// <param name="affinity">spring process cpu affinity</param>
		/// <param name="scriptOverride">if set, overrides generated script with supplied one</param>
		/// <returns>generates script</returns>
		public string StartGame(TasClient client, ProcessPriorityClass? priority, int? affinity, string scriptOverride)
		{
			if (!IsRunning)
			{
				talker = new Talker();
				readyPlayers.Clear();
				talker.SpringEvent += talker_SpringEvent;

				if (client != null && client.MyBattle != null && client.MyBattle.Founder == client.MyUser.Name) scriptPath = Utils.MakePath(paths.WritableDirectory, "script_" + client.MyBattle.Founder + ".txt").Replace('\\', '/');
				else scriptPath = Utils.MakePath(paths.WritableDirectory, "script.txt").Replace('\\', '/');

				string script;
				if (!string.IsNullOrEmpty(scriptOverride)) script = scriptOverride;
				else
				{
					List<Battle.GrPlayer> players;
					script = client.MyBattle.GenerateScript(out players, client.MyUser, talker.LoopbackPort);
					talker.SetPlayers(players);
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

		void HandlePlanetWarsMessages(Talker.SpringEventArgs e)
		{
			/*if (!Program.main.config.PlanetWarsEnabled || Program.main.PlanetWars == null) return;
            if (e.Param >= Talker.TO_EVERYONE_LEGACY) return;

            int count;
            if (!PlanetWarsMessages.TryGetValue(e.Text, out count)) count = 0;
            count++;
            PlanetWarsMessages[e.Text] = count;
            if (count != 2) return; // only send if count matches 2 exactly

            if (Program.main.PlanetWars != null) Program.main.PlanetWars.SpringMessage(e.Text);*/
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

			if (SpringExited != null) SpringExited(this, new EventArgs<bool>(isCrash));
		}

		void talker_SpringEvent(object sender, Talker.SpringEventArgs e)
		{
			switch (e.EventType)
			{
				case Talker.SpringEventType.PLAYER_JOINED:
					//Program.main.AutoHost.SayBattle("dbg joined " + e.PlayerName);
					if (PlayerJoined != null) PlayerJoined(this, new SpringLogEventArgs(e.PlayerName));
					break;

				case Talker.SpringEventType.PLAYER_LEFT:
					//Program.main.AutoHost.SayBattle("dbg left " + e.PlayerName);
					if (e.Param == 0 && PlayerDisconnected != null) PlayerDisconnected(this, new SpringLogEventArgs(e.PlayerName));
					if (PlayerLeft != null) PlayerLeft(this, new SpringLogEventArgs(e.PlayerName));

					break;

				case Talker.SpringEventType.PLAYER_CHAT:

					HandlePlanetWarsMessages(e);

					// only public chat
					if (PlayerSaid != null && (e.Param == Talker.TO_EVERYONE || e.Param == Talker.TO_EVERYONE_LEGACY)) PlayerSaid(this, new SpringLogEventArgs(e.PlayerName, e.Text));
					break;

				case Talker.SpringEventType.PLAYER_DEFEATED:
					//Program.main.AutoHost.SayBattle("dbg defeated " + e.PlayerName);
					if (PlayerLost != null) PlayerLost(this, new SpringLogEventArgs(e.PlayerName));
					break;

				case Talker.SpringEventType.SERVER_GAMEOVER:
					//Program.main.AutoHost.SayBattle("dbg gameover " + e.PlayerName);
					if (GameOver != null) GameOver(this, new SpringLogEventArgs(e.PlayerName));
					break;

				case Talker.SpringEventType.PLAYER_READY:
					//Program.main.AutoHost.SayBattle("dbg ready " + e.PlayerName);
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