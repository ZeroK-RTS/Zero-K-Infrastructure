using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using PlasmaShared.UnitSyncLib;

namespace LobbyClient
{
	public class Battle : ICloneable
	{
		public enum NatMode
		{
			None = 0,
			HolePunching = 1,
			FixedPorts = 2
		} ;

		string founder;
		/// <summary>
		/// Full map metadata - loaded only for joined battle
		/// </summary>
		readonly Map map;
		/// <summary>
		/// Full mod metadata - loaded only for joined battle
		/// </summary>
		readonly Mod mod;
		public int BattleID { get; set; }
		public List<BotBattleStatus> Bots { get; set; }
		public BattleDetails Details { get; set; }
		public List<string> DisabledUnits { get; set; }

		public string Founder
		{
			get { return founder; }
			set
			{
				if (value == null) throw new Exception("Founder can't be null");
				founder = value;
			}
		}

		public int HostPort { get; set; }
		public string Ip { get; set; }
		public bool IsFull { get { return NonSpectatorCount == MaxPlayers; } }
		public bool IsLocked { get; set; }
		public bool IsPassworded { get { return Password != "*"; } }
		public bool IsReplay { get; set; }

		public int? MapHash { get; set; }
		public string MapName { get; set; }

		public int MaxPlayers { get; set; }

		public int? ModHash { get; set; }
		public string ModName { get; set; }
		public Dictionary<string, string> ModOptions { get; private set; }
		public NatMode Nat { get; set; }

		public int NonSpectatorCount { get { return Users.Count - SpectatorCount; } }

		public string Password = "*";

		public int Rank { get; set; }
		public Dictionary<int, BattleRect> Rectangles { get; set; }
		public List<string> ScriptTags = new List<string>();
		public int SpectatorCount { get; set; }
		public string Title { get; set; }

		public List<UserBattleStatus> Users { get; set; }

		internal Battle()
		{
			Bots = new List<BotBattleStatus>();
			Details = new BattleDetails();
			ModOptions = new Dictionary<string, string>();
			Rectangles = new Dictionary<int, BattleRect>();
			DisabledUnits = new List<string>();
			Password = "*";
			Nat = NatMode.None;
			Users = new List<UserBattleStatus>();
		}


		public Battle(string password, int port, int maxplayers, int rank, Map map, string title, Mod mod, BattleDetails details)
			: this()
		{
			if (!String.IsNullOrEmpty(password)) Password = password;
			if (port == 0) HostPort = 8452;
			else HostPort = port;
			MaxPlayers = maxplayers;
			Rank = rank;
			this.map = map;
			MapName = map.Name;
			MapHash = map.Checksum;
			Title = title;
			this.mod = mod;
			ModName = mod.Name;
			ModHash = mod.Checksum;
			if (details != null) Details = details;
		}


		public bool CanBeJoined(int playerRank)
		{
			return NonSpectatorCount > 0 && !IsLocked && MaxPlayers > NonSpectatorCount && Password == "*" && Rank >= playerRank;
		}

		public bool ContainsUser(string name, out UserBattleStatus status)
		{
			status = Users.SingleOrDefault(x => x.Name == name);
			return status != null;
		}

		public string GenerateScript(out List<GrPlayer> players, User localUser, int loopbackListenPort)
		{
			var previousCulture = Thread.CurrentThread.CurrentCulture;
			try
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

				List<GrTeam> teams;
				List<GrAlly> alliances;

				if (mod == null || !mod.IsMission)
				{
					GroupData(out players, out teams, out alliances);
				}
				else
				{
					players = Users.Select(u => new GrPlayer(u)).ToList();

					var playerTeamNumbers = Users.Where(u => !u.IsSpectator).Select(u => u.TeamNumber).Distinct();
					var aiTeamNumbers = Bots.Select(b => b.TeamNumber).Distinct();
					var allTeamNumbers = playerTeamNumbers.Concat(aiTeamNumbers).Distinct();
					var maxTeamNumber = allTeamNumbers.Max();
					var grTeams = new GrTeam[maxTeamNumber + 1];
					foreach (var teamNumber in allTeamNumbers)
					{
						GrTeam grTeam;
						var playerLeader = players.FirstOrDefault(p => !p.user.IsSpectator && p.user.TeamNumber == teamNumber);
						if (playerLeader != null)
						{
							// is player
							var leaderIndex = players.IndexOf(playerLeader);
							grTeam = new GrTeam(leaderIndex);
						}
						else
						{
							// is bot
							var bot = Bots.FirstOrDefault(p => p.TeamNumber == teamNumber);
							var botOwner = players.First(p => p.user.Name == bot.owner);
							grTeam = new GrTeam(players.IndexOf(botOwner)) { bot = bot }; // team leader in bots is the player ID of the bot owner
						}
						grTeams[teamNumber] = grTeam;
					}
					teams = grTeams.ToList();


					var playerAlliances = Users.Where(u => !u.IsSpectator).Select(u => u.AllyNumber).Distinct();
					var botAlliances = Bots.Select(b => b.AllyNumber).Distinct();
					var allAlliances = playerAlliances.Concat(botAlliances).Distinct();
					alliances = allAlliances.Select(n => new GrAlly()).ToList();
				}

				var isHost = localUser.Name == Founder;
				var myUbs = players.Single(x => x.user.Name == localUser.Name).user;
				if (!isHost)
				{
					var sb = new StringBuilder();
					sb.AppendLine("[GAME]");
					sb.AppendLine("{");
					sb.AppendFormat("HostIP={0};\n", Ip);
					sb.AppendFormat("HostPort={0};\n", HostPort);
					sb.AppendLine("IsHost=0;");
					sb.AppendFormat("MyPlayerName={0};\n", localUser.Name);
					if (myUbs.ScriptPassword != null) sb.AppendFormat("MyPasswd={0};\n", myUbs.ScriptPassword);
					sb.AppendLine("}");
					return sb.ToString();
				}
				else
				{
					if (mod == null) throw new ApplicationException("Mod not downloaded yet");

					var script = new StringBuilder();

					script.AppendLine("[GAME]");
					script.AppendLine("{");

          script.AppendFormat("  Mapname={0};\n", MapName);

					if (mod.IsMission)
					{
						script.AppendFormat("  StartPosType=3;\n");
					} 
					else 
					{
						if (Details.StartPos == BattleStartPos.Choose) script.AppendFormat("  StartPosType=2;\n");
						else script.AppendFormat("  StartPosType=3;\n"); // workaround for random/fixed
						// script.AppendFormat("  StartPosType={0};\n", (int)Details.StartPos);
					}

					script.AppendFormat("  GameType={0};\n", ModName);
					if (ModHash.HasValue) script.AppendFormat("  ModHash={0};\n", (uint)ModHash.Value);
					if (MapHash.HasValue) script.AppendFormat("  MapHash={0};\n", (uint)MapHash.Value);
					script.AppendFormat("  AutohostPort={0};\n", loopbackListenPort);
					script.AppendLine();
					script.AppendFormat("  HostIP={0};\n", Ip);
					script.AppendFormat("  HostPort={0};\n", HostPort);
					script.AppendFormat("  SourcePort={0};\n", 8300);
					script.AppendFormat("  IsHost={0};\n", localUser.Name == founder ? 1 : 0);
					script.AppendLine();
					script.AppendFormat("  MyPlayerNum={0};\n", players.FindIndex(player => player.user.Name == localUser.Name));
					script.AppendFormat("  MyPlayerName={0};\n", localUser.Name);

					var bots = teams.Where(grTeam => grTeam != null && grTeam.bot != null).Select(team => team.bot).ToList();
					script.AppendLine();
					script.AppendFormat("  NumPlayers={0};\n", players.Count);
					script.AppendFormat("  NumUsers={0};\n", players.Count + bots.Count);
					script.AppendFormat("  NumTeams={0};\n", teams.Count);
					script.AppendFormat("  NumAllyTeams={0};\n", alliances.Count);
					script.AppendLine();

					// PLAYERS
					for (var i = 0; i < players.Count; ++i)
					{
						var u = players[i].user;
						script.AppendFormat("  [PLAYER{0}]\n", i);
						script.AppendLine("  {");
						script.AppendFormat("     name={0};\n", u.Name);

						script.AppendFormat("     Spectator={0};\n", u.IsSpectator ? 1 : 0);
						if (!u.IsSpectator) script.AppendFormat("     team={0};\n", u.TeamNumber);

						script.AppendFormat("     Rank={0};\n", u.LobbyUser.Rank);
						script.AppendFormat("     CountryCode={0};\n", u.LobbyUser.Country);
						if (u.ScriptPassword != null) script.AppendFormat("     Password={0};\n", u.ScriptPassword);

						script.AppendLine("  }");
					}

					// AI's
					for (var i = 0; i < bots.Count; i++)
					{
						var split = bots[i].aiLib.Split('|');
						script.AppendFormat("  [AI{0}]\n", i);
						script.AppendLine("  {");
						script.AppendFormat("    ShortName={0};\n", split[0]);
						script.AppendFormat("    Version={0};\n", split.Length > 1 ? split[1] : "");
						script.AppendFormat("    Team={0};\n", bots[i].TeamNumber);
						script.AppendFormat("    Host={0};\n", players.FindIndex(x => x.user.Name == bots[i].owner));
						script.AppendLine("    IsFromDemo=0;");
						script.AppendLine("    [Options]");
						script.AppendLine("    {");
						script.AppendLine("    }");
						script.AppendLine("  }\n");
					}

					var r = new Random();
					var positions = map.Positions;
					var tpos = new List<StartPos>();
					if (Details.StartPos == BattleStartPos.Random)
					{
						var org = new List<StartPos>(positions);
						while (org.Count > 0)
						{
							var t = org[r.Next(org.Count)];
							org.Remove(t);
							tpos.Add(t);
						}
					}

					// TEAMS
					script.AppendLine();
					for (var teamNumber = 0; teamNumber < teams.Count; ++teamNumber)
					{
						var grTeam = teams[teamNumber];
						if (grTeam == null && mod.IsMission) continue; // skip unoccupied slot
						var grLeader = players[grTeam.leader];
						var leaderStatus = grTeam.bot ?? grLeader.user;
						var teamAlly = leaderStatus.AllyNumber;
						script.AppendFormat("  [TEAM{0}]\n", teamNumber);
						script.AppendLine("  {");
						script.AppendFormat("     TeamLeader={0};\n", grTeam.leader);
						script.AppendFormat("     AllyTeam={0};\n", teamAlly);
						script.AppendFormat("     RGBColor={0:F5} {1:F5} {2:F5};\n",
											(leaderStatus.TeamColor & 255) / 255.0,
											((leaderStatus.TeamColor >> 8) & 255) / 255.0,
											((leaderStatus.TeamColor >> 16) & 255) / 255.0);
						string side = "mission";
						if (mod.Sides.Length > leaderStatus.Side) side = mod.Sides[leaderStatus.Side];
						script.AppendFormat("     Side={0};\n", side);

						script.AppendFormat("     Handicap={0};\n", 0);
						if (mod.IsMission)
						{
							script.AppendFormat("      StartPosX={0};\n", 0);
							script.AppendFormat("      StartPosZ={0};\n", 0);
						}
						else
						{
							StartPos? pos = null;
							if (Details.StartPos == BattleStartPos.Random)
							{
								if (tpos != null && tpos.Count() > teamNumber) pos = tpos.Skip(teamNumber).First();
							}
							else if (Details.StartPos == BattleStartPos.Fixed) if (positions != null && positions.Length > teamNumber) pos = positions[teamNumber];
							if (pos != null)
							{
								script.AppendFormat("      StartPosX={0};\n", pos.Value.x);
								script.AppendFormat("      StartPosZ={0};\n", pos.Value.z);
							}
						}
						script.AppendLine("  }");
					}


					// ALLIANCES
					script.AppendLine();
					for (var allyNumber = 0; allyNumber < alliances.Count; ++allyNumber)
					{
						script.AppendFormat("[ALLYTEAM{0}]\n", allyNumber);
						script.AppendLine("{");
						script.AppendFormat("     NumAllies={0};\n", 0);
						double left, top, right, bottom;
						alliances[allyNumber].rect.ToFractions(out left, out top, out right, out bottom);
						script.AppendFormat("     StartRectLeft={0};\n", left);
						script.AppendFormat("     StartRectTop={0};\n", top);
						script.AppendFormat("     StartRectRight={0};\n", right);
						script.AppendFormat("     StartRectBottom={0};\n", bottom);
						script.AppendLine("}");
					}

					script.AppendLine();
					script.AppendFormat("  NumRestrictions={0};\n", DisabledUnits.Count);
					script.AppendLine();

					if (!mod.IsMission)
					{
						script.AppendLine("  [RESTRICT]");
						script.AppendLine("  {");
						for (var i = 0; i < DisabledUnits.Count; ++i)
						{
							script.AppendFormat("    Unit{0}={1};\n", i, DisabledUnits[i]);
							script.AppendFormat("    Limit{0}=0;\n", i);
						}
						script.AppendLine("  }");


						script.AppendLine("  [MODOPTIONS]");
						script.AppendLine("  {");
						foreach (var o in mod.Options)
						{
							if (o.Type != OptionType.Section)
							{
								var v = o.Default;
								if (ModOptions.ContainsKey(o.Key)) v = ModOptions[o.Key];
								script.AppendFormat("    {0}={1};\n", o.Key, v);
							}
						}
						script.AppendLine("  }");
					}

					script.AppendLine("}");

					return script.ToString();
				}
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = previousCulture;
			}
		}


		public int GetFirstEmptyRectangle()
		{
			for (var i = 0; i < Spring.MaxAllies; ++i) if (!Rectangles.ContainsKey(i)) return i;
			return -1;
		}

		public int GetFreeTeamID(string exceptUser)
		{
			return
				Enumerable.Range(0, TasClient.MaxTeams - 1).FirstOrDefault(
					teamID => !Users.Where(u => !u.IsSpectator).Any(user => user.Name != exceptUser && user.TeamNumber == teamID) && !Bots.Any(x => x.TeamNumber == teamID));
		}

		public int GetState(User founder)
		{
			var battleState = 0;
			if (founder.IsInGame) battleState += 2;
			if (IsFull) battleState++;
			if (IsPassworded) battleState += 3;
			if (IsReplay) battleState += 6;
			return battleState;
		}

		public int GetUserIndex(string name)
		{
			for (var i = 0; i < Users.Count; ++i) if (Users[i].Name == name) return i;
			return -1;
		}

		/// <summary>
		/// Groups tam and ally numbers, so that they both start from 0
		/// </summary>
		public void GroupData(out List<GrPlayer> players, out List<GrTeam> teams, out List<GrAlly> alliances)
		{
			var teamNums = new Dictionary<int, int>();
			var allyNums = new Dictionary<int, int>();

			players = new List<GrPlayer>();
			teams = new List<GrTeam>();
			alliances = new List<GrAlly>();

			foreach (var p in Users)
			{
				var u = (UserBattleStatus)p.Clone();

				if (!u.IsSpectator)
				{
					if (!teamNums.ContainsKey(u.TeamNumber))
					{
						teamNums.Add(u.TeamNumber, teams.Count); // add transformation of team
						teams.Add(new GrTeam(players.Count));
					}
					u.TeamNumber = teamNums[u.TeamNumber];

					if (!allyNums.ContainsKey(u.AllyNumber))
					{
						allyNums.Add(u.AllyNumber, alliances.Count); // add transformation of ally
						alliances.Add(new GrAlly());
					}
					u.AllyNumber = allyNums[u.AllyNumber];
				}
				players.Add(new GrPlayer(u));
			}

			foreach (var p in Bots)
			{
				var u = (BotBattleStatus)p.Clone();

				if (!teamNums.ContainsKey(u.TeamNumber))
				{
					teamNums.Add(u.TeamNumber, teams.Count); // add transformation of team
					var leader = 0;
					for (leader = 0; leader < players.Count; ++leader) if (players[leader].user.Name == u.owner) break;
					var gr = new GrTeam(leader) { bot = u };
					teams.Add(gr);
				}
				u.TeamNumber = teamNums[u.TeamNumber];

				if (!allyNums.ContainsKey(u.AllyNumber))
				{
					allyNums.Add(u.AllyNumber, alliances.Count); // add transformation of ally
					alliances.Add(new GrAlly());
				}
				u.AllyNumber = allyNums[u.AllyNumber];
			}

			// now assign rectangles and skip unused
			var rects = new List<BattleRect>();
			foreach (var r in Rectangles)
			{
				if (allyNums.ContainsKey(r.Key)) alliances[allyNums[r.Key]] = new GrAlly(r.Value);
				else rects.Add(r.Value);
			}
			if (Rectangles.Count > alliances.Count && rects.Count > 0)
			{
				// add last unused rectangle too (KOH mode)
				foreach (var r in rects) alliances.Add(new GrAlly(r));
			}
		}

		public void RemoveUser(string name)
		{
			var ret = GetUserIndex(name);
			if (ret != -1) Users.RemoveAt(ret);
		}

		public override string ToString()
		{
			return String.Format("{0} {1} ({2}+{3}/{4})", ModName, MapName, NonSpectatorCount, SpectatorCount, MaxPlayers);
		}

		public object Clone()
		{
			var b = (Battle)MemberwiseClone();
			if (Details != null) b.Details = (BattleDetails)Details.Clone();
			if (Users != null) b.Users = new List<UserBattleStatus>(Users);
			if (Rectangles != null)
			{
				// copy the dictionary
				b.Rectangles = new Dictionary<int, BattleRect>();
				foreach (var kvp in Rectangles) b.Rectangles.Add(kvp.Key, kvp.Value);
			}

			if (DisabledUnits != null) b.DisabledUnits = new List<string>(DisabledUnits);
			return b;
		}


		public class GrAlly
		{
			public BattleRect rect;


			public GrAlly() {}


			public GrAlly(BattleRect r)
			{
				rect = r;
			}
		} ;

		public class GrPlayer
		{
			public UserBattleStatus user;

			public GrPlayer(UserBattleStatus ubs)
			{
				user = ubs;
			}
		} ;

		public class GrTeam
		{
			public BotBattleStatus bot;
			public int leader;

			public GrTeam(int leader)
			{
				bot = null;
				this.leader = leader;
			}
		} ;
	}
}