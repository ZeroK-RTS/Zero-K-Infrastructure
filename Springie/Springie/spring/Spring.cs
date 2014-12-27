#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Springie.Client;

#endregion

namespace Springie.SpringNamespace
{
	public class SpringLogEventArgs : EventArgs
	{
		#region Fields

		private List<string> args = new List<string>();
		private string line;
		private string username;

		#endregion

		#region Properties

		public List<string> Args
		{
			get { return args; }
		}

		public string Line
		{
			get { return line; }
		}

		public string Username
		{
			get { return username; }
		}

		#endregion

		#region Constructors

		public SpringLogEventArgs(string username) : this(username, "") {}

		public SpringLogEventArgs(string username, string line)
		{
			this.line = line;
			this.username = username;
		}

		#endregion
	} ;

	/// <summary>
	/// represents one install location of spring game
	/// </summary>
	public class Spring : IDisposable
	{
		#region Constants

		public const int MaxAllies = 16;
		public const int MaxTeams = 32;

		#endregion

		#region Fields

		private DateTime gameEnded;

		private DateTime gameStarted;
		private Dictionary<string, int> PlanetWarsMessages = new Dictionary<string, int>();
		//    const string PathDivider = "/";

		private Process process;
		private List<string> readyPlayers = new List<string>();
		private Talker talker;
		private bool isPreGame = true;


		private UnitSyncWrapper unitSyncWrapper;

		#endregion

		#region Properties

		public DateTime GameEnded
		{
			get { return gameEnded; }
		}

		public DateTime GameStarted
		{
			get { return gameStarted; }
		}

		public bool IsPreGame
		{
			get { return isPreGame; }
		}

		public bool IsRunning
		{
			get
			{
				try {
					return (process != null && !process.HasExited);
				} catch (Exception ex) {
					ErrorHandling.HandleException(ex, "Error determining process state");
					return false;
				}
			}
		}


		public ProcessPriorityClass ProcessPriority
		{
			get
			{
				if (IsRunning) return process.PriorityClass;
				else return Program.main.config.HostingProcessPriority;
			}
			set { if (IsRunning) process.PriorityClass = value; }
		}

		public UnitSyncWrapper UnitSyncWrapper
		{
			get { return unitSyncWrapper; }
		}

		#endregion

		#region Events

		public event EventHandler<SpringLogEventArgs> GameOver; // game has ended
		public event EventHandler NotifyModsChanged;
		public event EventHandler<SpringLogEventArgs> PlayerDisconnected;
		public event EventHandler<SpringLogEventArgs> PlayerJoined;
		public event EventHandler<SpringLogEventArgs> PlayerLeft;
		public event EventHandler<SpringLogEventArgs> PlayerLost; // player lost the game
		public event EventHandler<SpringLogEventArgs> PlayerSaid;
		//public event EventHandler<> 
		public event EventHandler SpringExited;
		public event EventHandler SpringStarted;

		#endregion

		#region Constructors

		public Spring()
		{
			if (!File.Exists(Program.main.config.ExecutableName)) {
				ErrorHandling.HandleException(null, Program.main.config.ExecutableName + " not found");
				if (Program.GuiEnabled) MessageBox.Show(Program.main.config.ExecutableName + " not found", "Cannot find dedicated server executable");
			}

			// init unitsync and load basic info
			unitSyncWrapper = new UnitSyncWrapper();
			unitSyncWrapper.NotifyModsChanged += unitSyncWrapper_NotifyModsChanged;
		}


		public void Dispose()
		{
			if (unitSyncWrapper != null) unitSyncWrapper.Dispose();
		}

		#endregion

		#region Public methods

		public void ExitGame()
		{
			try {
				if (IsRunning) {
					SayGame("/kill");
					process.WaitForExit(2000);
					if (!IsRunning) return;
					process.Kill();
					;
					process.WaitForExit(1000);
					if (!IsRunning) return;
					process.Kill();
				}
			} catch (Exception ex) {
				ErrorHandling.HandleException(ex, "while exiting game");
			}
		}

		public void ForceStart()
		{
			if (IsRunning)
			{
				talker.SendText("/forcestart");
				isPreGame = false;
			}
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

		public void StartGame(Battle battle)
		{
			if (!IsRunning) {
				List<Battle.GrPlayer> players;
				talker = new Talker();
				readyPlayers.Clear();
				talker.SpringEvent += talker_SpringEvent;
				string configName = Path.Combine(Program.WorkPath, "springie" + Program.main.AutoHost.config.HostingPort + ".txt").Replace('\\', '/');
				ConfigMaker.Generate(configName, battle, talker.LoopbackPort, out players);
				talker.SetPlayers(players);


				process = new Process();
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.Arguments += "\"" + configName + "\"";
				process.StartInfo.FileName = Program.main.config.ExecutableName;
				process.StartInfo.WorkingDirectory = Path.GetDirectoryName(Program.main.config.ExecutableName);
				process.StartInfo.UseShellExecute = false;
				process.Exited += springProcess_Exited;
				process.EnableRaisingEvents = true;
				PlanetWarsMessages = new Dictionary<string, int>();
				process.Start();
				gameStarted = DateTime.Now;
				Thread.Sleep(1000);
				if (!Program.IsLinux && IsRunning) process.PriorityClass = Program.main.config.HostingProcessPriority;
				if (IsRunning && SpringStarted != null) SpringStarted(this, EventArgs.Empty);
			}
		}

		#endregion

		#region Other methods

		private void HandlePlanetWarsMessages(Talker.SpringEventArgs e)
		{
			if (!Program.main.config.PlanetWarsEnabled || Program.main.PlanetWars == null) return;
			if (e.Param >= Talker.TO_EVERYONE_LEGACY) return;

			int count;
			if (!PlanetWarsMessages.TryGetValue(e.Text, out count)) count = 0;
			count++;
			PlanetWarsMessages[e.Text] = count;
			if (count != 2) return; // only send if count matches 2 exactly

			if (Program.main.PlanetWars != null) Program.main.PlanetWars.SpringMessage(e.Text);
		}

		#endregion

		#region Event Handlers

		private void springProcess_Exited(object sender, EventArgs e)
		{
			process = null;
			talker.Close();
			talker = null;
			gameEnded = DateTime.Now;
			if (SpringExited != null) SpringExited(this, EventArgs.Empty);
		}

		private void talker_SpringEvent(object sender, Talker.SpringEventArgs e)
		{
			switch (e.EventType) {
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


		private void unitSyncWrapper_NotifyModsChanged(object sender, EventArgs e)
		{
			if (NotifyModsChanged != null) NotifyModsChanged(sender, e);
		}

		#endregion
	}
}
