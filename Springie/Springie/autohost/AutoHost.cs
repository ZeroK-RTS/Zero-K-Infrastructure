#region using

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Timers;
using System.Xml.Serialization;
using LobbyClient;
using PlasmaShared;
using PlasmaShared.ContentService;
using PlasmaShared.UnitSyncLib;
using Springie.AutoHostNamespace;
using Springie.PlanetWars;
using Springie.SpringNamespace;
using Timer = System.Timers.Timer;

#endregion

namespace Springie.autohost
{
	public partial class AutoHost
	{
		public const string BoxesName = "boxes.bin";
		public const string ConfigName = "autohost.xml";

		public const int PollTimeout = 60;
		static readonly object savLock = new object();

		IVotable activePoll;
		BanList banList;
		string bossName = "";
		string delayedModChange;


		bool kickMinRank;
		ResourceLinkProvider linkProvider;

		AutoManager manager;

		Timer pollTimer;
		public readonly Spring spring;

		public string BossName { get { return bossName; } set { bossName = value; } }
		public int CloneNumber { get; private set; }

		public Dictionary<string, Dictionary<int, BattleRect>> MapBoxes = new Dictionary<string, Dictionary<int, BattleRect>>();

		public PlanetWarsHandler PlanetWars;

		public SpawnConfig SpawnConfig { get; private set; }
		public AutoHostConfig config = new AutoHostConfig();
		public string configPath;
		public Mod hostedMod;
		public int hostingPort { get; private set; }

		public SpringPaths springPaths;
		public TasClient tas;
		public UnitSyncWrapper wrapper { get; private set; }

		public AutoHost(SpringPaths paths, UnitSyncWrapper wrapper, string configPath, int hostingPort, SpawnConfig spawn)
		{
			SpawnConfig = spawn;
			this.configPath = configPath;
			springPaths = paths;
			this.hostingPort = hostingPort;

			LoadConfig();
			SaveConfig();
		
			spring = new Spring(paths, config.PlanetWarsEnabled ?  AutohostMode.Planetwars : AutohostMode.GameTeams) { UseDedicatedServer = true };
			tas = new TasClient(null, "Springie " + MainConfig.SpringieVersion, Program.main.Config.IpOverride);

			banList = new BanList(this, tas);
			banList.Load();


			this.wrapper = wrapper;

			pollTimer = new Timer(PollTimeout*1000);
			pollTimer.Enabled = false;
			pollTimer.AutoReset = false;
			pollTimer.Elapsed += pollTimer_Elapsed;

			spring.SpringExited += spring_SpringExited;
			spring.GameOver += spring_GameOver;

			spring.SpringExited += spring_SpringExited;
			spring.SpringStarted += spring_SpringStarted;
			spring.PlayerSaid += spring_PlayerSaid;

			wrapper.NotifyModsChanged += spring_NotifyModsChanged;

			tas.BattleUserLeft += tas_BattleUserLeft;
			tas.UserStatusChanged += tas_UserStatusChanged;
			tas.BattleUserJoined += tas_BattleUserJoined;
			tas.MyBattleMapChanged += tas_MyBattleMapChanged;
			tas.BattleLockChanged += tas_BattleLockChanged;
			tas.BattleOpened += tas_BattleOpened;
            tas.UserAdded += (o, u) => { if (u.Data.Name == GetAccountName()) Start(null, null); };

			tas.RegistrationDenied += (s, e) =>
				{
					ErrorHandling.HandleException(null, "Registration denied: " + e.ServerParams[0]);
					CloneNumber++;
					tas.Login(GetAccountName(), config.AccountPassword);
				};

			tas.RegistrationAccepted += (s, e) => tas.Login(GetAccountName(), config.AccountPassword);

			tas.AgreementRecieved += (s, e) =>
				{
					tas.AcceptAgreement();

					PlasmaShared.Utils.SafeThread(() =>
						{
							Thread.Sleep(7000);
							tas.Login(GetAccountName(), config.AccountPassword);
						}).Start();
				};

			tas.ConnectionLost += tas_ConnectionLost;
			tas.Connected += tas_Connected;
			tas.LoginDenied += tas_LoginDenied;
			tas.LoginAccepted += tas_LoginAccepted;
			tas.Said += tas_Said;
			tas.MyBattleStarted += tas_MyStatusChangedToInGame;

			linkProvider = new ResourceLinkProvider(this);

			InitializePlanetWarsServer();

			tas.Connect(Program.main.Config.ServerHost, Program.main.Config.ServerPort);
		}

		public void Dispose()
		{
			Stop();
			tas.UnsubscribeEvents(this);
			tas.UnsubscribeEvents(manager);
			tas.UnsubscribeEvents(banList);
			tas.UnsubscribeEvents(PlanetWars);
			spring.UnsubscribeEvents(this);
			spring.UnsubscribeEvents(PlanetWars);
			wrapper.UnsubscribeEvents(this);
			springPaths.UnsubscribeEvents(this);
			tas.Disconnect();
			if (PlanetWars != null) PlanetWars.Dispose();
			pollTimer.Dispose();
			if (manager != null) manager.Stop();
			banList.Close();
			MapBoxes = null;
			banList = null;
			manager = null;
			pollTimer = null;
			PlanetWars = null;
			linkProvider = null;
			wrapper = null;
		}

		public string GetAccountName()
		{
			if (CloneNumber > 0) return config.AccountName + CloneNumber;
			else return config.AccountName;
		}

		/*void fileDownloader_DownloadProgressChanged(object sender, TasEventArgs e)
    {
      if (tas.IsConnected) {
        SayBattle(e.ServerParams[0] + " " + e.ServerParams[1] + "% done");
      }
    }*/


		public int GetUserLevel(TasSayEventArgs e)
		{
			return GetUserLevel(e.UserName);
		}

		public int GetUserLevel(string name)
		{
			foreach (var pu in config.PrivilegedUsers) if (pu.Name == name) return pu.Level;
			User u;
			if (tas.GetExistingUser(name, out u)) if (u.IsAdmin) return config.DefaulRightsLevelForLobbyAdmins;
			return name == bossName ? config.BossRightsLevel : config.DefaulRightsLevel;
		}


		public bool HasRights(string command, TasSayEventArgs e)
		{
			if (banList.IsBanned(e.UserName))
			{
				Respond(e, "tough luck, you are banned");
				return false;
			}
			foreach (var c in config.Commands)
			{
				if (c.Name == command)
				{
					if (c.Throttling > 0)
					{
						var diff = (int)DateTime.Now.Subtract(c.lastCall).TotalSeconds;
						if (diff < c.Throttling)
						{
							Respond(e, "AntiSpam - please wait " + (c.Throttling - diff) + " more seconds");
							return false;
						}
					}

					for (var i = 0; i < c.ListenTo.Length; i++)
					{
						if (c.ListenTo[i] == e.Place)
						{
							var reqLevel = c.Level;
							var ulevel = GetUserLevel(e);

							if (ulevel >= reqLevel)
							{
								// boss stuff
								if (bossName != "" && ulevel <= config.DefaulRightsLevel && e.UserName != bossName && config.DefaultRightsLevelWithBoss < reqLevel)
								{
									Respond(e, "Sorry, you cannot do this right now, ask boss admin " + bossName);
									return false;
								}
								else
								{
									c.lastCall = DateTime.Now;
									return true; // ALL OK
								}
							}
							else
							{
								if (e.Place == TasSayEventArgs.Places.Battle && tas.MyBattle != null && tas.MyBattle.NonSpectatorCount == 1 &&
								    (!command.StartsWith("vote") && HasRights("vote" + command, e)))
								{
									// server only has 1 player and we have rights for vote variant - we might as well just do it
									return true;
								}
								else
								{
									Respond(e, "Sorry, you do not have rights to execute " + command);
									return false;
								}
							}
						}
					}
					return false; // place not allowed for this command = ignore command
				}
			}
			if (e.Place != TasSayEventArgs.Places.Channel) Respond(e, "Sorry, I don't know command '" + command + "'");
			return false;
		}

		public void InitializePlanetWarsServer()
		{
			if (PlanetWars != null) PlanetWars.Dispose();

			if (config.PlanetWarsEnabled) PlanetWars = new PlanetWarsHandler(this, tas, config, spring);
			else PlanetWars = null;
		}

		public void LoadConfig()
		{
			if (File.Exists(configPath + '/' + ConfigName))
			{
				var s = new XmlSerializer(config.GetType());
				var r = File.OpenText(configPath + '/' + ConfigName);
				config = (AutoHostConfig)s.Deserialize(r);
				r.Close();
				config.AddMissingCommands();
			}
			else config.Defaults();


			if (File.Exists(springPaths.Cache + '/' + BoxesName))
			{
				try
				{
					var frm = new BinaryFormatter();
					using (var r = new FileStream(springPaths.Cache + '/' + BoxesName, FileMode.Open))
					{
						MapBoxes = (Dictionary<string, Dictionary<int, BattleRect>>)frm.Deserialize(r);
						r.Close();
					}
				}
				catch (Exception ex3)
				{
					ErrorHandling.HandleException(ex3, "Unable to load boxes");
				}
			}

		}

		public void RegisterVote(TasSayEventArgs e, string[] words)
		{
			if (activePoll != null)
			{
				if (activePoll.Vote(e, words)) StopVote();
			}
			else Respond(e, "There is no poll going on, start some first");
		}

		public void Respond(TasSayEventArgs e, string text)
		{
			Respond(tas, spring, e, text);
		}

		public static void Respond(TasClient tas, Spring spring, TasSayEventArgs e, string text)
		{
			var p = TasClient.SayPlace.User;
			var emote = false;
			if (e.Place == TasSayEventArgs.Places.Battle)
			{
				p = TasClient.SayPlace.Battle;
				emote = true;
			}
			if (e.Place == TasSayEventArgs.Places.Game && spring.IsRunning) spring.SayGame(text);
			else tas.Say(p, e.UserName, text, emote);
		}

		public void SaveConfig()
		{
			lock (savLock)
			{
				config.Commands.Sort(AutoHostConfig.CommandComparer);

				// remove duplicated admins
				var l = new List<PrivilegedUser>();
				foreach (var p in config.PrivilegedUsers) if (l.Find(delegate(PrivilegedUser u) { return u.Name == p.Name; }) == null) l.Add(p);
				;
				config.PrivilegedUsers = l;
				config.PrivilegedUsers.Sort(AutoHostConfig.UserComparer);


				var s = new XmlSerializer(config.GetType());
				var f = File.OpenWrite(configPath + '/' + ConfigName);
				f.SetLength(0);
				s.Serialize(f, config);
				f.Close();

				if (banList != null) banList.Save();

				var fm = new BinaryFormatter();
				using (var fs = new FileStream(springPaths.Cache + '/' + BoxesName, FileMode.Create))
				{
					fm.Serialize(fs, MapBoxes);
					fs.Close();
				}
			}
		}

		public void SayBattle(string text)
		{
			SayBattle(text, true);
		}

		public void SayBattle(string text, bool ingame)
		{
			if (!string.IsNullOrEmpty(text)) foreach (var line in text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) SayBattle(tas, spring, line, ingame);
		}


		public static void SayBattle(TasClient tas, Spring spring, string text, bool ingame)
		{
			tas.Say(TasClient.SayPlace.Battle, "", text, true);
			if (spring.IsRunning && ingame) spring.SayGame(text);
		}

		public void Start(string modname, string mapname)
		{
			Stop();

			manager = new AutoManager(this, tas, spring);
			kickMinRank = config.KickMinRank;

			if (String.IsNullOrEmpty(modname)) modname = config.DefaultMod;
			if (String.IsNullOrEmpty(mapname)) mapname = config.DefaultMap;

			if (!string.IsNullOrEmpty(config.AutoUpdateRapidTag)) modname = config.AutoUpdateRapidTag;

			var title = config.GameTitle.Replace("%1", MainConfig.SpringieVersion);
			var password = config.Password;

			if (SpawnConfig != null)
			{
				modname = SpawnConfig.Mod;
				title = SpawnConfig.Title;
				if (string.IsNullOrEmpty(SpawnConfig.Password)) password = "*";
				else password = SpawnConfig.Password;
			}

			var version = Program.main.Downloader.PackageDownloader.GetByTag(modname);
			if (version != null) modname = version.InternalName;

			if (!wrapper.HasMod(modname)) modname = wrapper.GetFirstMod();
			var modi = wrapper.GetModInfo(modname);
			hostedMod = modi;
			if (hostedMod.IsMission && !string.IsNullOrEmpty(hostedMod.MissionMap)) mapname = hostedMod.MissionMap;

			if (!wrapper.HasMap(mapname)) mapname = wrapper.GetFirstMap();

			int mint, maxt;
			var mapi = wrapper.GetMapInfo(mapname);

			var b = new Battle(password,
			                   hostingPort,
			                   config.MaxPlayers,
			                   config.MinRank,
			                   mapi,
			                   title,
			                   modi,
			                   config.BattleDetails);
			// if hole punching enabled then we use it
			if (config.UseHolePunching) b.Nat = Battle.NatMode.HolePunching;
			else if (Program.main.Config.GargamelMode) b.Nat = Battle.NatMode.FixedPorts;
			else b.Nat = Battle.NatMode.None; // else either no nat or fixed ports (for gargamel fake - to get client IPs)

			for (var i = 0; i < config.DefaultRectangles.Count; ++i) b.Rectangles.Add(i, config.DefaultRectangles[i]);
			tas.OpenBattle(b);

			if (SpawnConfig != null) tas.Say(TasClient.SayPlace.User, SpawnConfig.Owner, "I'm here! Ready to serve you! Join me!", false);
		}


		public void StartVote(IVotable vote, TasSayEventArgs e, string[] words)
		{
			if (vote != null)
			{
				if (activePoll != null)
				{
					Respond(e, "Another poll already in progress, please wait");
					return;
				}
				if (vote.Init(e, words))
				{
					activePoll = vote;
					pollTimer.Interval = PollTimeout*1000;
					pollTimer.Enabled = true;
				}
			}
		}


		public void Stop()
		{
			if (manager != null) manager.Stop();
			StopVote();
			spring.ExitGame();
			tas.ChangeMyUserStatus(false, false);
			tas.LeaveBattle();
		}

		public void StopVote()
		{
			pollTimer.Enabled = false;
			activePoll = null;
		}

		void CheckForBattleExit()
		{
			if ((DateTime.Now - spring.GameStarted) > TimeSpan.FromSeconds(20))
			{
				if (spring.IsRunning)
				{
					var b = tas.MyBattle;
					var count = 0;
					foreach (var p in b.Users)
					{
						if (p.IsSpectator) continue;

						User u;
						if (!tas.GetExistingUser(p.Name, out u)) continue;
						if (u.IsInGame) count++;
					}
					if (count < 1)
					{
						SayBattle("closing game, " + count + " active player left in game");
						spring.ExitGame();
					}
				}
				// kontrola pro pripad ze by se nevypl spring
				User us;
				if (!spring.IsRunning && tas.GetExistingUser(tas.UserName, out us) && us.IsInGame) tas.ChangeMyUserStatus(false, false);
			}
		}

		/// <summary>
		/// Gets free slots, first mandatory then optional
		/// </summary>
		/// <returns></returns>
		IEnumerable<MissionSlot> GetFreeSlots()
		{
			var b = tas.MyBattle;
			return
				hostedMod.MissionSlots.Where(x => x.IsHuman).OrderByDescending(x => x.IsRequired).Where(
					x => !b.Users.Any(y => y.AllyNumber == x.AllyID && y.TeamNumber == x.TeamID && !y.IsSpectator));
		}


		void HandleMinRankKicking()
		{
			if (kickMinRank && config.MinRank > 0)
			{
				var b = tas.MyBattle;
				if (b != null)
				{
					foreach (var u in b.Users)
					{
						User x;
						tas.GetExistingUser(u.Name, out x);
						if (u.Name != tas.UserName && x.Level < config.MinRank)
						{
							SayBattle(x.Name + ", your rank is too low, rank kicking is enabled here");
							ComKick(TasSayEventArgs.Default, new[] { u.Name });
						}
					}
				}
			}
		}

		void pollTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (activePoll != null) activePoll.TimeEnd();
			StopVote();

			if (!spring.IsRunning && delayedModChange != null)
			{
				var mod = delayedModChange;
				delayedModChange = null;
				SayBattle("Updating to latest mod version: " + mod);
				ComRehost(TasSayEventArgs.Default, new[] { mod });
			}
		}

		void spring_GameOver(object sender, SpringLogEventArgs e)
		{
			SayBattle("Game over, exiting");
			PlasmaShared.Utils.SafeThread(() =>
				{
					Thread.Sleep(3000); // wait for stats
					spring.ExitGame();
				}).Start();

/*			try
			{
				var service = new ContentService();
				service.GetRecommendedMap()

			}

			if (config.MapCycle.Length > 0)
			{
				mapCycleIndex = mapCycleIndex%config.MapCycle.Length;
				SayBattle("changing to another map in mapcycle");
				ComMap(TasSayEventArgs.Default, config.MapCycle[mapCycleIndex].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
				mapCycleIndex++;
			}*/
		}

		void spring_NotifyModsChanged(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(config.AutoUpdateRapidTag) && SpawnConfig == null)
			{
				var version = Program.main.Downloader.PackageDownloader.GetByTag(config.AutoUpdateRapidTag);
				if (version != null)
				{
					var latest = version.InternalName;
					if (wrapper.HasMod(latest))
					{
						var b = tas.MyBattle;
						if (!string.IsNullOrEmpty(latest) && b != null && b.ModName != latest)
						{
							config.DefaultMod = latest;
							if (!spring.IsRunning)
							{
								SayBattle("Updating to latest mod version: " + latest);
								ComRehost(TasSayEventArgs.Default, new[] { latest });
							}
							else delayedModChange = latest;
						}
					}
				}
			}
		}


		void spring_PlayerSaid(object sender, SpringLogEventArgs e)
		{
			tas.GameSaid(e.Username, e.Line);
			if (Program.main.Config.RedirectGameChat && e.Username != tas.UserName && !e.Line.StartsWith("Allies:") && !e.Line.StartsWith("Spectators:")) tas.Say(TasClient.SayPlace.Battle, "", "[" + e.Username + "]" + e.Line, false);
		}


		void spring_SpringExited(object sender, EventArgs e)
		{
			tas.ChangeLock(false);
			tas.ChangeMyUserStatus(false, false);
			if (PlanetWars != null) PlanetWars.SpringExited();
			var b = tas.MyBattle;
			foreach (var s in toNotify)
			{
				if (b != null && b.Users.Any(x => x.Name == s)) tas.Ring(s);
				tas.Say(TasClient.SayPlace.User, s, "** Game just ended, join me! **", false);
			}
			toNotify.Clear();
		}

		void spring_SpringStarted(object sender, EventArgs e)
		{
			tas.ChangeLock(false);
			if (hostedMod.IsMission) using (var service = new ContentService() { Proxy = null }) foreach (var u in tas.MyBattle.Users.Where(x => !x.IsSpectator)) service.NotifyMissionRunAsync(u.Name, hostedMod.Name);
		}


		void tas_BattleLockChanged(object sender, BattleInfoEventArgs e1)
		{
			if (e1.BattleID == tas.MyBattleID) SayBattle("game " + (tas.MyBattle.IsLocked ? "locked" : "unlocked"), false);
		}

		void tas_BattleOpened(object sender, TasEventArgs e)
		{
			tas.DisableUnits(config.DisabledUnits.Select(x => x.Name).ToArray());
			tas.ChangeMyBattleStatus(true, false, SyncStatuses.Synced);
			if (hostedMod.IsMission)
			{
				foreach (var slot in hostedMod.MissionSlots.Where(x => !x.IsHuman))
				{
					var ubs = new UserBattleStatus();
					ubs.SyncStatus = SyncStatuses.Synced;
					ubs.TeamColor = slot.Color;
					ubs.AllyNumber = slot.AllyID;
					ubs.TeamNumber = slot.TeamID;
					ubs.IsReady = true;
					ubs.IsSpectator = false;
					ubs.Name = slot.AiShortName;
					tas.AddBot(slot.TeamName, ubs, slot.Color, slot.AiShortName);
				}
			}
		}


		void tas_BattleUserJoined(object sender, BattleUserEventArgs e1)
		{
			if (e1.BattleID != tas.MyBattleID) return;
			var name = e1.UserName;

			var welc = config.Welcome;
			if (welc != "")
			{
				welc = welc.Replace("%1", name);
				welc = welc.Replace("%2", GetUserLevel(name).ToString());
				welc = welc.Replace("%3", MainConfig.SpringieVersion);
				SayBattle(welc, false);
			}
			if (spring.IsRunning)
			{
				spring.AddUser(e1.UserName, e1.ScriptPassword);
				var started = DateTime.Now.Subtract(spring.GameStarted);
				started = new TimeSpan((int)started.TotalHours, started.Minutes, started.Seconds);
				SayBattle(string.Format("GAME IS CURRENTLY IN PROGRESS, PLEASE WAIT TILL IT ENDS! Running for {0}", started), false);
				SayBattle("If you say !notify, I will PM you when game ends.", false);
			}

			HandleMinRankKicking();

			if (PlanetWars != null) PlanetWars.UserJoined(name);

			if (SpawnConfig != null && SpawnConfig.Owner == name) // owner joins, set him boss 
				ComBoss(TasSayEventArgs.Default, new[] { name });
		}

		void tas_BattleUserLeft(object sender, BattleUserEventArgs e1)
		{
			if (e1.BattleID != tas.MyBattleID) return;
			CheckForBattleExit();

			if (spring.IsRunning) spring.SayGame(e1.UserName + " has left lobby");

			if (e1.UserName == bossName)
			{
				SayBattle("boss has left the battle");
				bossName = "";
			}

			var battle = tas.MyBattle;
			if (battle.IsLocked && battle.Users.Count < 2)
			{
				// player left and only 2 remaining (springie itself + some noob) -> unlock
				tas.ChangeLock(false);
			}
		}


		// login accepted - join channels

		// im connected, let's login
		void tas_Connected(object sender, TasEventArgs e)
		{
			tas.Login(GetAccountName(), config.AccountPassword);
		}


		void tas_ConnectionLost(object sender, TasEventArgs e)
		{
			Stop();
		}


		void tas_LoginAccepted(object sender, TasEventArgs e)
		{
			for (var i = 0; i < config.JoinChannels.Count; ++i) tas.JoinChannel(config.JoinChannels[i]);
		}

		void tas_LoginDenied(object sender, TasEventArgs e)
		{
			if (e.ServerParams[0] == "Bad username/password") tas.Register(GetAccountName(), config.AccountPassword);
			else
			{
				CloneNumber++;
				tas.Login(GetAccountName(), config.AccountPassword);
			}
		}

		void tas_MyBattleMapChanged(object sender, BattleInfoEventArgs e1)
		{
			var b = tas.MyBattle;
			var mapName = b.MapName.ToLower();
			if (MapBoxes.ContainsKey(mapName))
			{
				for (var i = 0; i < b.Rectangles.Count; ++i) tas.RemoveBattleRectangle(i);
				var dict = MapBoxes[mapName];
				foreach (var v in dict) tas.AddBattleRectangle(v.Key, v.Value);
			}
		}

		void tas_MyStatusChangedToInGame(object sender, TasEventArgs e)
		{
			var b = tas.MyBattle;
			if (b != null)
			{
				var curMap = b.MapName.ToLower();

				var nd = new Dictionary<int, BattleRect>();
				foreach (var v in b.Rectangles) nd.Add(v.Key, v.Value);

				if (MapBoxes.ContainsKey(curMap)) MapBoxes[curMap] = nd;
				else MapBoxes.Add(curMap, nd);
				SaveConfig();
			}

			spring.StartGame(tas, Program.main.Config.HostingProcessPriority, Program.main.Config.SpringCoreAffinity, null);
		}

		void tas_Said(object sender, TasSayEventArgs e)
		{
			if (string.IsNullOrEmpty(e.UserName)) return;
			if (config.RedirectGameChat && e.Place == TasSayEventArgs.Places.Battle && e.Origin == TasSayEventArgs.Origins.Player && e.UserName != tas.UserName &&
			    e.IsEmote == false) spring.SayGame("[" + e.UserName + "]" + e.Text);

			// check if it's command
			if (e.Origin == TasSayEventArgs.Origins.Player && !e.IsEmote && e.Text.StartsWith("!"))
			{
				if (e.Text.Length < 2) return;
				var allwords = e.Text.Substring(1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (allwords.Length < 1) return;
				var com = allwords[0];

				// remove first word (command)
				var words = Utils.ShiftArray(allwords, -1);

				if (!HasRights(com, e))
				{
					if (!com.StartsWith("vote"))
					{
						com = "vote" + com;

						if (!config.Commands.Any(x => x.Name == com) || !HasRights(com, e)) return;
					}
					else return;
				}

				if (e.Place == TasSayEventArgs.Places.Normal)
				{
					if (com != "say" && com != "admins" && com != "help" && com != "helpall" && com != "springie" && com != "listoptions" &&
					    com != "spawn" && com != "listbans" && com != "stats" && com != "predict" && com != "notify") SayBattle(string.Format("{0} executed by {1}", com, e.UserName));
				}

				switch (com)
				{
					case "listmaps":
						ComListMaps(e, words);
						break;

					case "listmods":
						ComListMods(e, words);
						break;

					case "help":
						ComHelp(e, words);
						break;

					case "map":
						ComMap(e, words);
						break;

					case "admins":
						ComAdmins(e, words);
						break;

					case "start":
						ComStart(e, words);
						break;

					case "forcestart":
						ComForceStart(e, words);
						break;

					case "force":
						ComForce(e, words);
						break;

					case "split":
						ComSplit(e, words);
						break;

					case "corners":
						ComCorners(e, words);
						break;

					case "maplink":
						linkProvider.FindLinks(words, ResourceLinkProvider.FileType.Map, tas, e);
						break;

					case "modlink":
						linkProvider.FindLinks(words, ResourceLinkProvider.FileType.Mod, tas, e);
						break;

					case "ring":
						ComRing(e, words);
						break;

					case "kick":
						ComKick(e, words);
						break;

					case "exit":
						ComExit(e, words);
						break;

					case "lock":
						if (!manager.Enabled) tas.ChangeLock(true);
						break;

					case "unlock":
						if (!manager.Enabled) tas.ChangeLock(false);
						break;

					case "vote":
						RegisterVote(e, words);
						break;

					case "votemap":
						StartVote(new VoteMap(tas, spring, this), e, words);
						break;

					case "votekick":
						StartVote(new VoteKick(tas, spring, this), e, words);
						break;

					case "votespec":
						StartVote(new VoteSpec(tas, spring, this), e, words);
						break;

					case "voteforcestart":
						StartVote(new VoteForceStart(tas, spring, this), e, words);
						break;

					case "voteforce":
						StartVote(new VoteForce(tas, spring, this), e, words);
						break;

					case "voteexit":
						StartVote(new VoteExit(tas, spring, this), e, words);
						break;

					case "predict":
						ComPredict(e, words);
						break;

					case "fix":
						ComFix(e, words);
						break;

					case "rehost":
						ComRehost(e, words);
						break;

					case "voterehost":
						StartVote(new VoteRehost(tas, spring, this), e, words);
						break;

					case "random":
						ComRandom(e, words);
						break;

					case "balance":
						ComBalance(e, words);
						break;

					case "setlevel":
						ComSetLevel(e, words);
						break;

					case "setcommandlevel":
						ComSetCommandLevel(e, words);
						break;

					case "say":
						ComSay(e, words);
						break;

					case "id":
						ComTeam(e, words);
						break;

					case "team":
						ComAlly(e, words);
						break;

					case "helpall":
						ComHelpAll(e, words);
						break;

					case "fixcolors":
						ComFixColors(e, words);
						break;

					case "teamcolors":
						ComTeamColors(e, words);
						break;

					case "springie":
						ComSpringie(e, words);
						break;

					case "endvote":
						StopVote();
						SayBattle("poll cancelled");
						break;

					case "addbox":
						ComAddBox(e, words);
						break;

					case "clearbox":
						ComClearBox(e, words);
						break;


					case "cbalance":
						ComCBalance(e, words);
						break;

					case "listbans":
						banList.ComListBans(e, words);
						break;

					case "ban":
						banList.ComBan(e, words);
						break;

					case "unban":
						banList.ComUnban(e, words);
						break;

					case "stats":
						//RemoteCommand(Stats.StatsScript, e, words);
						// todo new stats
						break;

					case "manage":
						ComManage(e, words, false);
						break;

					case "cmanage":
						ComManage(e, words, true);
						break;

					case "notify":
						ComNotify(e, words);
						break;

					case "boss":
						ComBoss(e, words);
						break;

					case "voteboss":
						StartVote(new VoteBoss(tas, spring, this), e, words);
						break;

					case "setpassword":
						ComSetPassword(e, words);
						break;

					case "setgametitle":
						ComSetGameTitle(e, words);
						break;

					case "setminrank":
						ComSetMinRank(e, words);
						break;

					case "setmaxplayers":
						ComSetMaxPlayers(e, words);
						break;

					case "spec":
						ComForceSpectator(e, words);
						break;

					case "specafk":
						ComForceSpectatorAfk(e, words);
						break;

					case "kickminrank":
						ComKickMinRank(e, words);
						break;

					case "cheats":
						if (spring.IsRunning)
						{
							spring.SayGame("/cheat");
							SayBattle("Cheats!");
						}
						else Respond(e, "Cannot set cheats, game not running");
						break;

					case "listoptions":
						ComListOptions(e, words);
						break;

					case "setoptions":
						ComSetOption(e, words);
						break;

					case "votesetoptions":
						StartVote(new VoteSetOptions(tas, spring, this), e, words);
						break;

					case "spawn":
					{
						var args = Utils.Glue(words);
						if (string.IsNullOrEmpty(args))
						{
							Respond(e, "Please specify parameters");
							return;
						}
						var configKeys = new Dictionary<string, string>();
						foreach (var f in args.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
						{
							var parts = f.Split('=');
							if (parts.Length == 2) configKeys[parts[0].Trim()] = parts[1].Trim();
						}
						var sc = new SpawnConfig(e.UserName, configKeys);
						if (string.IsNullOrEmpty(sc.Mod))
						{
							Respond(e, "Please specify at least mod name: !spawn mod=zk:stable");
							return;
						}
						Program.main.SpawnAutoHost(configPath, sc);
					}
						break;
				}
			}
		}


		void tas_UserStatusChanged(object sender, TasEventArgs e)
		{
			if (spring.IsRunning)
			{
				var b = tas.MyBattle;
				if (e.ServerParams[0] != tas.UserName && b.Users.Any(x => x.Name == e.ServerParams[0])) CheckForBattleExit();
			}
		}
	}
}