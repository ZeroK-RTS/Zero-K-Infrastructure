#region using

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using LobbyClient;

#endregion

namespace Springie.autohost
{
	public partial class AutoHost
	{
		const int MaxMapListLength = 400;

		readonly List<string> toNotify = new List<string>();

		public bool AllReadyAndSynced(out List<string> usname)
		{
			usname = new List<string>();
			foreach (var p in tas.MyBattle.Users)
			{
				if (p.IsSpectator || tas.IsTeamSpec(p.Side)) continue;
				if (p.SyncStatus != SyncStatuses.Synced || !p.IsReady) usname.Add(p.Name);
			}
			return usname.Count == 0;
		}

		public bool AllUniqueTeams(out List<string> username)
		{
			var teams = new List<int>();
			username = new List<string>();
			foreach (var p in tas.MyBattle.Users)
			{
				if (p.IsSpectator) continue;
				if (teams.Contains(p.TeamNumber)) username.Add(p.Name);
				else teams.Add(p.TeamNumber);
			}
			return username.Count == 0;
		}


		public void BalanceTeams(int teamCount, bool clanwise)
		{
			try
			{
				var ranker = new List<UsRank>();
				var b = tas.MyBattle;

				foreach (var u in b.Users)
				{
					if (!u.IsSpectator && !tas.IsTeamSpec(u.Side))
					{
						double elo;
						double w;
						Program.main.SpringieServer.GetElo(u.Name, out elo, out w);
						ranker.Add(new UsRank(ranker.Count, elo, w, clanwise ? GetClan(u.Name) : "", u));
					}
				}
				var totalPlayers = ranker.Count;

				// for each follower replace clan of follower and followed player
				foreach (var usRank in ranker.Where(x =>
					{
						var qm = quickMatchTracker.GetQuickMatchInfo(x.User.Name);
						return qm != null && qm.CurrentMode == BattleMode.Follow;
					}))
				{
					var followed = quickMatchTracker.GetQuickMatchInfo(usRank.User.Name).GameName;
					var fp = ranker.SingleOrDefault(x => x.User.Name == followed);
					if (fp != null)
					{
						usRank.Clan = followed;
						fp.Clan = followed;
					}
				}

				var rand = new Random();

				if (teamCount < 1) teamCount = 1;
				if (teamCount > ranker.Count) teamCount = ranker.Count;

				var teamUsers = new List<UsRank>[teamCount];
				for (var i = 0; i < teamUsers.Length; ++i) teamUsers[i] = new List<UsRank>();
				var teamSums = new double[teamCount];

				var teamClans = new List<string>[teamCount];
				for (var i = 0; i < teamClans.Length; ++i) teamClans[i] = new List<string>();

				var clans = "";
				// remove clans that have less than 2 members - those are irelevant
				foreach (var u in ranker)
				{
					if (u.Clan != "")
					{
						if (ranker.FindAll(delegate(UsRank x) { return x.Clan == u.Clan; }).Count < 2) u.Clan = "";
						else clans += u.Clan + ", ";
					}
				}
				if (clans != "") SayBattle("those clan are being balanced: " + clans);

				// this cycle performs actual user adding to teams
				var cnt = 0;
				while (ranker.Count > 0)
				{
					var minsum = double.MaxValue;
					var minid = 0;
					for (var i = 0; i < teamCount; ++i)
					{
						var l = teamUsers[i];
						// pick only current "row" and find the one with least sum
						if (l.Count == cnt/teamCount)
						{
							if (teamSums[i] < minsum)
							{
								minid = i;
								minsum = teamSums[i];
							}
						}
					}

					var candidates = new List<UsRank>();

					// get list of clans assigned to other teams
					var assignedClans = new List<string>();
					for (var i = 0; i < teamClans.Length; ++i) if (i != minid) assignedClans.AddRange(teamClans[i]);

					// first try to get some with same clan
					if (teamClans[minid].Count > 0) candidates.AddRange(ranker.Where(x => x.Clan != "" && teamClans[minid].Contains(x.Clan)));

					// we dont have any candidates try to get clanner from unassigned clan
					if (candidates.Count == 0) candidates.AddRange(ranker.Where(x => x.Clan != "" && !assignedClans.Contains(x.Clan)));

					// we still dont have any candidates try to get anyone
					if (candidates.Count == 0) candidates.AddRange(ranker);

					var maxElo = double.MinValue;
					var maxUsers = new List<UsRank>();
					// get candidate which increases team elo most (round elo to tens to add some randomness)
					foreach (var c in candidates)
					{
						var newElo = (teamUsers[minid].Sum(x => x.Weight*x.Elo) + c.Weight*Math.Round(c.Elo/10)*10)/(teamUsers[minid].Sum(x => x.Weight) + c.Weight);
						if (newElo > maxElo)
						{
							maxUsers.Clear();
							maxUsers.Add(c);
							maxElo = newElo;
						}
						else if (newElo == maxElo) maxUsers.Add(c);
					}
					var pickedUser = maxUsers[rand.Next(maxUsers.Count)];

					teamUsers[minid].Add(pickedUser);
					teamSums[minid] = maxElo;

					if (pickedUser.Clan != "")
					{
						// if we work with clans add user's clan to clan list for his team
						if (!teamClans[minid].Contains(pickedUser.Clan)) teamClans[minid].Add(pickedUser.Clan);
					}

					ranker.Remove(pickedUser);

					cnt++;
				}

				// alliances for allinace permutations
				var allys = new List<int>();
				for (var i = 0; i < teamCount; ++i) allys.Add(i);

				var t = "";

				for (var i = 0; i < teamCount; ++i)
				{
					// permute one alliance
					var rdindex = rand.Next(allys.Count);
					var allynum = allys[rdindex];
					allys.RemoveAt(rdindex);

					if (teamUsers[i].Count > 0)
					{
						if (i > 0) t += ":";
						t += (allynum + 1) + "=" + Math.Round(teamSums[i]);
					}

					foreach (var u in teamUsers[i]) tas.ForceAlly(u.User.Name, allynum);
				}

				t += ")";

				SayBattle(string.Format("{0} players balanced {2} to {1} teams (ratings {3}", totalPlayers, teamCount, clanwise ? "respecting clans" : "", t));
			}
			catch (Exception ex)
			{
				ErrorHandling.HandleException(ex, "Error balancing teams");
			}
		}

		public bool BalancedTeams(out int allyno, out int alliances)
		{
			var counts = new int[16];
			allyno = 0;

			foreach (var p in tas.MyBattle.Users)
			{
				if (p.IsSpectator || tas.IsTeamSpec(p.Side)) continue;
				counts[p.AllyNumber]++;
			}

			alliances = counts.Count(x => x > 0);

			var tsize = 0;
			for (var i = 0; i < counts.Length; ++i)
			{
				if (counts[i] != 0)
				{
					if (tsize == 0) tsize = counts[i];
					else if (tsize != counts[i])
					{
						allyno = i;
						return false;
					}
				}
			}
			if (ladder != null)
			{
				int mint, maxt;
				ladder.CheckBattleDetails(null, out mint, out maxt);
				if (tsize < mint || tsize > maxt)
				{
					SayBattle("Ladder only allows team sizes " + mint + " - " + maxt);
					return false;
				}
			}
			return true;
		}


		public void ComAddBox(TasSayEventArgs e, string[] words)
		{
			if (words.Length < 4)
			{
				Respond(e, "This command needs at least 4 parameters");
				return;
			}
			int x, y, w, h;
			if (!int.TryParse(words[0], out x) || !int.TryParse(words[1], out y) || !int.TryParse(words[2], out w) || !int.TryParse(words[3], out h))
			{
				Respond(e, "All parameters must be numbers");
				return;
			}
			var numrect = 0;
			if (words.Length > 4) int.TryParse(words[4], out numrect);

			if (numrect == 0)
			{
				numrect = tas.MyBattle.GetFirstEmptyRectangle();
				if (numrect == -1)
				{
					Respond(e, "Cannot add more boxes");
					return;
				}
				numrect++;
			}
			tas.AddBattleRectangle(numrect - 1, new BattleRect(x*2, y*2, (x + w)*2, (y + h)*2));
		}

		public void ComAlly(TasSayEventArgs e, string[] words)
		{
			if (words.Length < 2)
			{
				Respond(e, "this command needs 2 parameters (ally number and player name)");
				return;
			}
			var allyno = 0;
			if (!int.TryParse(words[0], out allyno) || --allyno < 0 || allyno >= Spring.MaxAllies)
			{
				Respond(e, "invalid ally number");
				return;
			}
			string[] usrs;
			int[] idx;
			if (FilterUsers(Utils.ShiftArray(words, -1), out usrs, out idx) == 0) Respond(e, "no such player found");
			else
			{
				SayBattle("Forcing " + usrs[0] + " to alliance " + (allyno + 1));
				tas.ForceAlly(usrs[0], allyno);
			}
		}

		public void ComAutoLock(TasSayEventArgs e, string[] words)
		{
			if (words.Length == 0)
			{
				autoLock = 0;
				Respond(e, "AutoLocking disabled");
				return;
			}
			var num = 0;
			int.TryParse(words[0], out num);
			var maxp = tas.MyBattle.MaxPlayers;
			if (num < config.AutoLockMinPlayers || num > maxp)
			{
				autoLock = 0;
				Respond(e, "number of players must be between " + config.AutoLockMinPlayers + " and " + maxp + ", AutoLocking disabled");
				return;
			}
			autoLock = num;
			HandleAutoLocking();
			Respond(e, "AutoLock set to " + autoLock + " players");
		}

		public void ComBalance(TasSayEventArgs e, string[] words)
		{
			int teamCount;
			if (words.Length > 0) int.TryParse(words[0], out teamCount);
			else teamCount = 2;
			ComFix(e, words);
			BalanceTeams(teamCount, false);
		}

		public void ComBoss(TasSayEventArgs e, string[] words)
		{
			if (words.Length == 0)
			{
				if (bossName == "")
				{
					Respond(e, "there is currently no active boss");
					return;
				}
				SayBattle("boss " + bossName + " removed");
				bossName = "";
				return;
			}
			else
			{
				string[] usrs;
				int[] idx;
				if (FilterUsers(words, out usrs, out idx) == 0) Respond(e, "no such player found");
				else
				{
					SayBattle("New boss is " + usrs[0]);
					bossName = usrs[0];
				}
			}
		}

		public void ComCBalance(TasSayEventArgs e, string[] words)
		{
			var teamCount = 2;
			if (words.Length > 0) int.TryParse(words[0], out teamCount);
			else teamCount = 2;
			ComFix(e, words);
			BalanceTeams(teamCount, true);
		}

		public void ComClearBox(TasSayEventArgs e, string[] words)
		{
			if (words.Length == 0) foreach (var i in tas.MyBattle.Rectangles.Keys) tas.RemoveBattleRectangle(i);
			else
			{
				var numrect = 0;
				if (!int.TryParse(words[0], out numrect)) Respond(e, "paramater must by a number of rectangle");
				tas.RemoveBattleRectangle(numrect - 1);
			}
		}

		public void ComCorners(TasSayEventArgs e, string[] words)
		{
			if (words.Length != 2)
			{
				Respond(e, "This command needs 2 parameters");
				return;
			}
			if (words[0] != "a" && words[0] != "b") Respond(e, "first parameter must be 'a' or 'b'");
			else
			{
				int perc;
				int.TryParse(words[1], out perc);
				if (perc < 0 || perc > 50) Respond(e, "second parameter must be between 0 and 50");
				else
				{
					var p = perc/100.0;
					if (words[0] == "a")
					{
						tas.AddBattleRectangle(0, new BattleRect(0, 0, p, p));
						tas.AddBattleRectangle(1, new BattleRect(1 - p, 1 - p, 1, 1));
						tas.AddBattleRectangle(2, new BattleRect(1 - p, 0, 1, p));
						tas.AddBattleRectangle(3, new BattleRect(0, 1 - p, p, 1));
					}
					else
					{
						tas.AddBattleRectangle(0, new BattleRect(1 - p, 0, 1, p));
						tas.AddBattleRectangle(1, new BattleRect(0, 1 - p, p, 1));
						tas.AddBattleRectangle(2, new BattleRect(0, 0, p, p));
						tas.AddBattleRectangle(3, new BattleRect(1 - p, 1 - p, 1, 1));
					}
				}
			}
		}


		public void ComExit(TasSayEventArgs e, string[] words)
		{
			if (spring.IsRunning) SayBattle("exiting game");
			else Respond(e, "cannot exit, not in game");
			spring.ExitGame();
		}


		/// <summary>
		/// fixes ids
		/// </summary>
		/// <param name="e"></param>
		/// <param name="words">if param is "silent" does not advertise id fixing</param>
		/// <returns>true if id teams were already fixed</returns>
		public bool ComFix(TasSayEventArgs e, params string[] words)
		{
			var b = tas.MyBattle;
			var groups = b.Users.Where(x => !x.IsSpectator && x.SyncStatus != SyncStatuses.Unknown).GroupBy(x => x.TeamNumber).Where(g => g.Count() > 1);
			if (groups.Count() > 0)
			{
				var id = 0;
				foreach (var u in b.Users.Where(x => !x.IsSpectator && x.SyncStatus != SyncStatuses.Unknown)) tas.ForceTeam(u.Name, id++);
				if (words == null || words.Length == 0 || words[0] != "silent") SayBattle("team numbers fixed");
				return false;
			}
			else return true;
		}


		public void ComFixColors(TasSayEventArgs e, string[] words)
		{
			var cols = new List<MyCol>();
			var b = tas.MyBattle;

			foreach (var u in b.Users) if (!u.IsSpectator) cols.Add((MyCol)u.TeamColor);
			var arcols = cols.ToArray();

			MyCol.FixColors(arcols, 30000);

			var changed = false;
			var cnt = 0;
			foreach (var u in b.Users)
			{
				if (!u.IsSpectator)
				{
					if (u.TeamColor != (int)arcols[cnt])
					{
						tas.ForceColor(u.Name, (int)arcols[cnt]);
						changed = true;
					}
					cnt++;
				}
			}
			if (changed) SayBattle("colors fixed");
		}

		public void ComForce(TasSayEventArgs e, string[] words)
		{
			if (spring.IsRunning)
			{
				SayBattle("forcing game start by " + e.UserName);
				spring.ForceStart();
			}
			else Respond(e, "cannot force, game not started");
		}

		public void ComForceSpectator(TasSayEventArgs e, string[] words)
		{
			if (words.Length == 0)
			{
				Respond(e, "You must specify player name");
				return;
			}

			int[] indexes;
			string[] usrlist;
			if (FilterUsers(words, out usrlist, out indexes) == 0)
			{
				Respond(e, "Cannot find such player");
				return;
			}

			tas.ForceSpectator(usrlist[0]);
			Respond(e, "Forcing " + usrlist[0] + " to spectator");
		}

		public void ComForceSpectatorAfk(TasSayEventArgs e, string[] words)
		{
			var b = tas.MyBattle;
			if (b != null)
			{
				foreach (var u in b.Users)
				{
					User u2;
					if (u.Name != tas.UserName && !u.IsSpectator && !u.IsReady && tas.GetExistingUser(u.Name, out u2)) if (u2.IsAway) ComForceSpectator(e, new[] { u.Name });
				}
			}
		}

		public void ComForceStart(TasSayEventArgs e, string[] words)
		{
			/*string usname;
      if (!AllReadyAndSynced(out usname)) {
        SayBattle("cannot start, " + usname + " not ready and synced");
        return;
      }*/
			if (PlanetWars == null || PlanetWars.StartGame(e))
			{
				SayBattle("please wait, game is about to start");

				StopVote();
				tas.StartGame();
			}
		}

		public void ComKick(TasSayEventArgs e, string[] words)
		{
			if (words.Length == 0)
			{
				Respond(e, "You must specify player name");
				return;
			}

			int[] indexes;
			string[] usrlist;
			if (FilterUsers(words, out usrlist, out indexes) == 0)
			{
				if (spring.IsRunning) spring.Kick(Utils.Glue(words));
				Respond(e, "Cannot find such player");
				return;
			}

			if (usrlist[0] == tas.UserName)
			{
				Respond(e, "won't kick myself, not in suicidal mood today");
				return;
			}

			if (spring.IsRunning) spring.Kick(usrlist[0]);
			tas.Kick(usrlist[0]);
		}

		public void ComKickMinRank(TasSayEventArgs e, string[] words)
		{
			if (words.Length > 0 && (words[0] == "1" || words[0] == "0")) kickMinRank = (words[0] == "1");
			else kickMinRank = !kickMinRank;

			if (kickMinRank) SayBattle("automatic minrank kicking is now ENABLED");
			else SayBattle("automatic minrank kicking is now DISABLED");

			HandleMinRankKicking();
		}

		public void ComKickSpec(TasSayEventArgs e, string[] words)
		{
			if (words.Length > 0 && (words[0] == "1" || words[0] == "0")) kickSpectators = (words[0] == "1");
			else kickSpectators = !kickSpectators;

			if (kickSpectators) SayBattle("automatic spectator kicking is now ENABLED");
			else SayBattle("automatic spectator kicking is now DISABLED");

			if (kickSpectators)
			{
				SayBattle(config.KickSpectatorText);
				var b = tas.MyBattle;
				if (b != null) foreach (var u in b.Users) if (u.Name != tas.UserName && u.IsSpectator) ComKick(e, new[] { u.Name });
			}
		}

		public void ComManage(TasSayEventArgs e, string[] words, bool clanBased)
		{
			if (words.Length < 1)
			{
				Respond(e, "this command needs 1 parameters (minimum number of players to manage for)");
				return;
			}
			var min = 0;
			int.TryParse(words[0], out min);
			var max = min;
			if (words.Length > 1) int.TryParse(words[1], out max);
			var allyCount = 2;
			if (words.Length > 2) int.TryParse(words[2], out allyCount);
			manager.Manage(min, max, allyCount, e, clanBased);
		}

		public void ComPredict(TasSayEventArgs e, string[] words)
		{
			var b = tas.MyBattle;
			var grouping = b.Users.Where(u => !u.IsSpectator && !tas.IsTeamSpec(u.Side)).GroupBy(u => u.AllyNumber);

			IGrouping<int, UserBattleStatus> oldg = null;
			foreach (var g in grouping)
			{
				if (oldg != null)
				{

					var t1entries = oldg.Select(x => Program.main.SpringieServer.GetEloEntry(x.Name));
					var t1elo = t1entries.Sum(x => x.Elo*x.W)/t1entries.Sum(x => x.W);

					var t2entries = g.Select(x => Program.main.SpringieServer.GetEloEntry(x.Name));
					var t2elo = t2entries.Sum(x => x.Elo * x.W) / t2entries.Sum(x => x.W);
					Respond(e, string.Format("team {0} has {1}% chance to win over team {2}", oldg.Key + 1, GetWinChancePercent(t1elo, t2elo), g.Key + 1));
				}
				oldg = g;
			}
		}


		public void ComPreset(TasSayEventArgs e, string[] words)
		{
			string[] vals;
			int[] indexes;
			if (FilterPresets(words, out vals, out indexes) > 0)
			{
				var p = presets[indexes[0]];
				Respond(e, "applying preset " + p.Name + " (" + p.Description + ")");
				p.Apply(tas, ladder);
			}
			else Respond(e, "no such preset found");
		}

		public void ComPresetDetails(TasSayEventArgs e, string[] words)
		{
			string[] vals;
			int[] indexes;
			if (FilterPresets(words, out vals, out indexes) > 0)
			{
				tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
				foreach (var line in presets[indexes[0]].ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) tas.Say(TasClient.SayPlace.User, e.UserName, line, false);
				tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
			}
			else Respond(e, "no such preset found");
		}


		public void ComRandom(TasSayEventArgs e, string[] words)
		{
			ComFix(e, words);
			var b = tas.MyBattle;

			var actUsers = new List<UserBattleStatus>();
			foreach (var u in b.Users) if (!u.IsSpectator && !tas.IsTeamSpec(u.Side)) actUsers.Add(u);

			var teamCount = 0;
			if (words.Length > 0) int.TryParse(words[0], out teamCount);
			else teamCount = 2;
			if (teamCount < 2) teamCount = 2;
			if (teamCount > actUsers.Count) teamCount = 2;
			var r = new Random();

			var al = 0;
			while (actUsers.Count > 0)
			{
				var index = r.Next(actUsers.Count);
				tas.ForceAlly(actUsers[index].Name, al);
				actUsers.RemoveAt(index);
				al++;
				al = al%teamCount;
			}
			SayBattle("players assigned to " + teamCount + " random teams");
		}

		public void ComRehost(TasSayEventArgs e, string[] words)
		{
			if (words.Length == 0) Start(null, null);
			else
			{
				string[] mods;
				int[] indexes;
				if (FilterMods(words, out mods, out indexes) == 0) Respond(e, "cannot find such mod");
				else Start(mods[0], null);
			}
		}

		public void ComRing(TasSayEventArgs e, string[] words)
		{
			var usrlist = new List<string>();

			if (words.Length == 0)
			{
				// ringing idle
				foreach (var p in tas.MyBattle.Users)
				{
					if (p.IsSpectator) continue;
					if ((!p.IsReady || p.SyncStatus != SyncStatuses.Synced) && (!spring.IsRunning || !spring.IsPlayerReady(p.Name))) usrlist.Add(p.Name);
				}
			}
			else
			{
				string[] vals;
				int[] indexes;
				FilterUsers(words, out vals, out indexes);
				usrlist = new List<string>(vals);
			}

			var rang = "";
			foreach (var s in usrlist)
			{
				tas.Ring(s);
				rang += s + ", ";
			}

			if (words.Length == 0 && usrlist.Count > 7) SayBattle("ringing all unready");
			else SayBattle("ringing " + rang);
		}


		// user and rank info


		public void ComSay(TasSayEventArgs e, string[] words)
		{
			if (words.Length == 0)
			{
				Respond(e, "This command needs 1 parameter (say text)");
				return;
			}
			SayBattle("[" + e.UserName + "]" + Utils.Glue(words));
		}

		public void ComSetCommandLevel(TasSayEventArgs e, string[] words)
		{
			if (words.Length != 2)
			{
				Respond(e, "This command needs 2 parameters");
				return;
			}
			int lvl;
			int.TryParse(words[0], out lvl);
			var com = config.Commands.Find(x => x.Name == words[1]);
			if (com != null)
			{
				com.Level = lvl;
				SaveConfig();
				Respond(e, string.Format("Level of command {0} was set to {1}", words[1], lvl));
			}
			else Respond(e, "No such command found");
		}

		public void ComSetLevel(TasSayEventArgs e, string[] words)
		{
			if (words.Length != 2)
			{
				Respond(e, "This command needs 2 parameters");
				return;
			}
			int lvl;
			int.TryParse(words[0], out lvl);
			config.SetPrivilegedUser(words[1], lvl);
			SaveConfig();
			Respond(e, words[1] + " has rights level " + lvl);
		}


		public void ComSplit(TasSayEventArgs e, string[] words)
		{
			if (words.Length != 2)
			{
				Respond(e, "This command needs 2 parameters");
				return;
			}
			if (words[0] != "h" && words[0] != "v") Respond(e, "first parameter must be 'h' or 'v'");
			else
			{
				int perc;
				int.TryParse(words[1], out perc);
				if (perc < 0 || perc > 50) Respond(e, "second parameter must be between 0 and 50");
				else
				{
					if (words[0] == "h")
					{
						tas.AddBattleRectangle(0, new BattleRect(0, 0, 1.0, perc/100.0));
						tas.AddBattleRectangle(1, new BattleRect(0, 1.0 - perc/100.0, 1.0, 1.0));
					}
					else
					{
						tas.AddBattleRectangle(0, new BattleRect(0, 0, perc/100.0, 1.0));
						tas.AddBattleRectangle(1, new BattleRect(1.0 - perc/100.0, 0, 1.0, 1.0));
					}
					tas.RemoveBattleRectangle(2);
					tas.RemoveBattleRectangle(3);
				}
			}
		}


		public void ComSpringie(TasSayEventArgs e, string[] words)
		{
			var b = tas.MyBattle;

			var running = DateTime.Now.Subtract(Program.startupTime);
			running = new TimeSpan((int)running.TotalHours, running.Minutes, running.Seconds);

			var started = DateTime.Now.Subtract(spring.GameStarted);
			started = new TimeSpan((int)started.TotalHours, started.Minutes, started.Seconds);

			Respond(e, tas.UserName + " (" + MainConfig.SpringieVersion + ") running for " + running);
			Respond(e, "players: " + (b.Users.Count - b.NonSpectatorCount) + "/" + b.MaxPlayers);
			Respond(e, "mod: " + b.ModName);
			Respond(e, "map: " + b.MapName);
			Respond(e,
			        "game " + (spring.IsRunning ? "running since " : "not running, last started ") +
			        (spring.GameStarted != DateTime.MinValue ? started + " ago" : "never"));
		}

		public void ComStart(TasSayEventArgs e, string[] words)
		{
			List<string> usname;
			if (!AllReadyAndSynced(out usname))
			{
				SayBattle("cannot start, " + Utils.Glue(usname.ToArray()) + " not ready and synced");
				return;
			}

			if (!AllUniqueTeams(out usname))
			{
				SayBattle("cannot start, " + Utils.Glue(usname.ToArray()) + " is sharing teams. Use !forcestart to override");
				return;
			}

			int allyno;
			int alliances;
			if (!BalancedTeams(out allyno, out alliances))
			{
				SayBattle("cannot start, alliance " + (allyno + 1) + " not fair. Use !forcestart to override");
				return;
			}

			if (PlanetWars == null || PlanetWars.StartGame(e))
			{
				SayBattle("please wait, game is about to start");

				StopVote();

				tas.StartGame();
			}
		}

		public void ComTeam(TasSayEventArgs e, string[] words)
		{
			if (words.Length < 2)
			{
				Respond(e, "this command needs 2 parameters (team number and player name)");
				return;
			}
			var teamno = 0;
			if (!int.TryParse(words[0], out teamno) || --teamno < 0 || teamno >= Spring.MaxTeams)
			{
				Respond(e, "invalid team number");
				return;
			}
			string[] usrs;
			int[] idx;
			if (FilterUsers(Utils.ShiftArray(words, -1), out usrs, out idx) == 0) Respond(e, "no such player found");
			else
			{
				SayBattle("Forcing " + usrs[0] + " to team " + (teamno + 1));
				tas.ForceTeam(usrs[0], teamno);
			}
		}

		public void ComTeamColors(TasSayEventArgs e, string[] words)
		{
			var players = tas.MyBattle.Users.Where(u => !u.IsSpectator).ToArray();
			var alliances = players.GroupBy(u => u.AllyNumber).ToArray();
			var teamCounts = alliances.Select(g => g.Count()).ToArray();
			var colors = TeamColorMaker.GetTeamColors(teamCounts);
			var changed = false;
			for (var allianceIndex = 0; allianceIndex < alliances.Length; allianceIndex++)
			{
				var alliance = alliances[allianceIndex].ToArray();
				for (var teamIndex = 0; teamIndex < alliance.Length; teamIndex++)
				{
					var user = alliance[teamIndex];
					var newColor = (int)(MyCol)colors[allianceIndex][teamIndex];
					if (user.TeamColor == newColor) continue;
					tas.ForceColor(user.Name, newColor);
					changed = true;
				}
			}
			if (changed) SayBattle("team colors set");
		}

		internal static int Filter(string[] source, string[] words, out string[] resultVals, out int[] resultIndexes)
		{
			int i;

			// search by direct index
			if (words.Length == 1)
			{
				if (int.TryParse(words[0], out i))
				{
					if (i >= 0 && i < source.Length)
					{
						resultVals = new[] { source[i] };
						resultIndexes = new[] { i };
						return 1;
					}
				}

				// search by direct word
				var glued = Utils.Glue(words);
				for (i = 0; i < source.Length; ++i)
				{
					if (source[i] == glued)
					{
						resultVals = new[] { source[i] };
						resultIndexes = new[] { i };
						return 1;
					}
				}
			}

			var res = new List<string>();
			var resi = new List<int>();

			for (i = 0; i < words.Length; ++i) words[i] = words[i].ToLower();
			for (i = 0; i < source.Length; ++i)
			{
				if (source[i] + "" == "") continue;
				var item = source[i];
				var isok = true;
				for (var j = 0; j < words.Length; ++j)
				{
					if (!item.ToLower().Contains(words[j]))
					{
						isok = false;
						break;
					}
				}
				if (isok)
				{
					res.Add(item);
					resi.Add(i);
				}
			}

			resultVals = res.ToArray();
			resultIndexes = resi.ToArray();

			return res.Count;
		}

		public int FilterMaps(string[] words, out string[] vals, out int[] indexes)
		{
			return FilterMaps(words, this, ladder, out vals, out indexes);
		}

		internal static int FilterMods(string[] words, AutoHost ah, out string[] vals, out int[] indexes)
		{
			var temp = new string[Program.main.UnitSyncWrapper.ModList.Keys.Count];
			var cnt = 0;
			foreach (var s in Program.main.UnitSyncWrapper.ModList.Keys)
			{
				var limit = ah.config.LimitMods;
				if (limit != null && limit.Length > 0)
				{
					var allowed = false;
					for (var i = 0; i < limit.Length; ++i)
					{
						if (s.ToLower().Contains(limit[i].ToLower()))
						{
							allowed = true;
							break;
						}
					}
					if (allowed) temp[cnt++] = s;
				}
				else temp[cnt++] = s;
			}

			return Filter(temp, words, out vals, out indexes);
		}


		internal static int FilterPresets(string[] words, AutoHost autohost, out string[] vals, out int[] indexes)
		{
			var temp = new string[autohost.presets.Count];
			var cnt = 0;
			foreach (var p in autohost.presets) temp[cnt++] = p.Name + " --> " + p.Description;
			return Filter(temp, words, out vals, out indexes);
		}

		internal static int FilterUsers(string[] words, TasClient tas, Spring spring, out string[] vals, out int[] indexes)
		{
			var b = tas.MyBattle;
			var temp = new string[b.Users.Count];
			var i = 0;
			foreach (var u in b.Users) temp[i++] = u.Name;
			return Filter(temp, words, out vals, out indexes);
		}


		public string GetOptionsString(TasSayEventArgs e, string[] words)
		{
			var s = Utils.Glue(words);
			var result = "";
			var pairs = s.Split(new[] { ',' });
			if (pairs.Length == 0 || pairs[0].Length == 0)
			{
				Respond(e, "requires key=value format");
				return "";
			}
			foreach (var pair in pairs)
			{
				var parts = pair.Split(new[] { '=' }, 2);
				if (parts.Length != 2)
				{
					Respond(e, "requires key=value format");
					return "";
				}
				var b = tas.MyBattle;
				var key = parts[0];
				var val = parts[1];

				var found = false;
				var mod = wrapper.GetModInfo(b.ModName);
				foreach (var o in mod.Options)
				{
					if (o.Key == key)
					{
						found = true;
						string res;
						if (o.GetPair(val, out res))
						{
							if (result != "") result += "\t";
							result += res;
						}
						else Respond(e, "Value " + val + " is not valid for this option");

						break;
					}
				}
				if (!found)
				{
					Respond(e, "No option called " + key + " found");
					return "";
				}
			}
			return result;
		}


		public static int GetWinChancePercent(double elo1, double elo2)
		{
			return (int)Math.Round((1.0/(1.0 + Math.Pow(10, (elo2 - elo1)/400.0)))*100.0);
		}

		void ComAdmins(TasSayEventArgs e, string[] words)
		{
			tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
			foreach (var u in config.PrivilegedUsers) tas.Say(TasClient.SayPlace.User, e.UserName, " " + u.Name + " (level " + u.Level + ")", false);
			tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
		}

		void ComHelp(TasSayEventArgs e, string[] words)
		{
			var ulevel = GetUserLevel(e);
			tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
			foreach (var c in config.Commands) if (c.Level <= ulevel) tas.Say(TasClient.SayPlace.User, e.UserName, " !" + c.Name + " " + c.HelpText, false);
			tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
		}


		void ComHelpAll(TasSayEventArgs e, string[] words)
		{
			var copy = new List<CommandConfig>(config.Commands);
			copy.Sort(delegate(CommandConfig a, CommandConfig b)
				{
					if (a.Level != b.Level) return a.Level.CompareTo(b.Level);
					else return a.Name.CompareTo(b.Name);
				});

			tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
			foreach (var c in copy) tas.Say(TasClient.SayPlace.User, e.UserName, "Level " + c.Level + " --> !" + c.Name + " " + c.HelpText, false);
			tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
		}

		void ComListMaps(TasSayEventArgs e, string[] words)
		{
			string[] vals;
			int[] indexes;
			int count;
			if ((count = FilterMaps(words, out vals, out indexes)) > 0)
			{
				if (count > MaxMapListLength)
				{
					Respond(e, string.Format("This has {0} results, please narrow down your search", count));
					return;
				}
				tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
				for (var i = 0; i < vals.Length; ++i) tas.Say(TasClient.SayPlace.User, e.UserName, indexes[i] + ": " + vals[i], false);
				tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
			}
			else Respond(e, "no such map found");
		}

		void ComListMods(TasSayEventArgs e, string[] words)
		{
			string[] vals;
			int[] indexes;
			int count;
			if ((count = FilterMods(words, out vals, out indexes)) > 0)
			{
				if (count > MaxMapListLength)
				{
					Respond(e, string.Format("This has {0} results, please narrow down your search", count));
					return;
				}
				tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
				for (var i = 0; i < vals.Length; ++i) tas.Say(TasClient.SayPlace.User, e.UserName, indexes[i] + ": " + vals[i], false);
				tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
			}
			else Respond(e, "no such mod found");
		}

		void ComListOptions(TasSayEventArgs e, string[] words)
		{
			var b = tas.MyBattle;
			var mod = wrapper.GetModInfo(b.ModName);
			if (mod.Options.Length == 0) Respond(e, "this mod has no options");
			else foreach (var opt in mod.Options) Respond(e, opt.ToString());
		}


		void ComListPresets(TasSayEventArgs e, string[] words)
		{
			string[] vals;
			int[] indexes;

			if (FilterPresets(words, out vals, out indexes) > 0)
			{
				tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
				for (var i = 0; i < vals.Length; ++i) tas.Say(TasClient.SayPlace.User, e.UserName, indexes[i] + ": " + vals[i], false);
				tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
			}
			else Respond(e, "no such preset found");
		}

		void ComMap(TasSayEventArgs e, string[] words)
		{
			if (words.Length == 0)
			{
				Respond(e, "You must specify a map name");
				return;
			}
			string[] vals;
			int[] indexes;
			if (FilterMaps(words, out vals, out indexes) > 0)
			{
				SayBattle("changing map to " + vals[0]);
				var mapi = wrapper.MapList[vals[0]];
				tas.ChangeMap(mapi.Name, mapi.Checksum);
			}
			else Respond(e, "Cannot find such map.");
		}


		void ComNotify(TasSayEventArgs e, string[] words)
		{
			if (!toNotify.Contains(e.UserName)) toNotify.Add(e.UserName);
			Respond(e, "I will notify you when game ends");
		}


		void ComSetGameTitle(TasSayEventArgs e, string[] words)
		{
			if (words.Length == 0) Respond(e, "this command needs one parameter - new game title");
			else
			{
				config.GameTitle = Utils.Glue(words);
				SaveConfig();
				Respond(e, "game title changed");
			}
		}

		void ComSetMaxPlayers(TasSayEventArgs e, string[] words)
		{
			if (words.Length == 0) Respond(e, "this command needs one parameter - number of players");
			else
			{
				int plr;
				int.TryParse(words[0], out plr);
				if (plr < 1) plr = 1;
				if (plr > Spring.MaxTeams) plr = Spring.MaxTeams;
				config.MaxPlayers = plr;
				SaveConfig();
				Respond(e, "server size changed");
			}
		}

		void ComSetMinCpuSpeed(TasSayEventArgs e, string[] words)
		{
			if (words.Length == 0) Respond(e, "this command needs one parameter - minimal CPU speed");
			else
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
				double minCpu;
				double.TryParse(words[0], out minCpu);
				minCpuSpeed = minCpu;
				SayBattle("minimal CPU speed is now " + minCpuSpeed + "GHz");
				if (minCpuSpeed > 0)
				{
					var b = tas.MyBattle;
					if (b != null)
					{
						foreach (var ubs in b.Users)
						{
							User u;
							if (ubs.Name != tas.UserName && tas.GetExistingUser(ubs.Name, out u)) if (u.Cpu > 0 && u.Cpu < minCpuSpeed*1000) ComKick(e, new[] { u.Name });
						}
					}
				}
			}
		}

		void ComSetMinRank(TasSayEventArgs e, string[] words)
		{
			if (words.Length == 0) Respond(e, "this command needs one parameter - rank number");
			else
			{
				int rank;
				int.TryParse(words[0], out rank);
				if (rank < 0) rank = 0;
				if (rank > User.RankLimits.Length) rank = User.RankLimits.Length;
				config.MinRank = rank;
				SaveConfig();
				Respond(e, "server rank changed");
				HandleMinRankKicking();
			}
		}


		void ComSetOption(TasSayEventArgs e, string[] words)
		{
			var ret = GetOptionsString(e, words);
			if (ret != "")
			{
				tas.SetScriptTag(ret);
				Respond(e, "Options set");
			}
		}

		void ComSetPassword(TasSayEventArgs e, string[] words)
		{
			if (words.Length == 0)
			{
				config.Password = "";
				Respond(e, "password remoded");
			}
			else
			{
				config.Password = words[0];
				SaveConfig();
				Respond(e, "password changed");
			}
		}

		static int FilterMaps(string[] words, AutoHost ah, Ladder ladder, out string[] vals, out int[] indexes)
		{
			var temp = new string[Program.main.UnitSyncWrapper.MapList.Keys.Count];
			var cnt = 0;
			foreach (var s in Program.main.UnitSyncWrapper.MapList.Keys)
			{
				if (ladder != null)
				{
					if (ladder.Maps.Contains(s.ToLower())) temp[cnt++] = s;
				}
				else
				{
					var limit = ah.config.LimitMaps;
					if (limit != null && limit.Length > 0)
					{
						var allowed = false;
						for (var i = 0; i < limit.Length; ++i)
						{
							if (s.ToLower().Contains(limit[i].ToLower()))
							{
								allowed = true;
								break;
							}
						}
						if (allowed) temp[cnt++] = s;
					}
					else temp[cnt++] = s;
				}
			}
			return Filter(temp, words, out vals, out indexes);
		}

		int FilterMods(string[] words, out string[] vals, out int[] indexes)
		{
			return FilterMods(words, this, out vals, out indexes);
		}

		int FilterPresets(string[] words, out string[] vals, out int[] indexes)
		{
			return FilterPresets(words, this, out vals, out indexes);
		}

		int FilterUsers(string[] words, out string[] vals, out int[] indexes)
		{
			return FilterUsers(words, tas, spring, out vals, out indexes);
		}

		static string GetClan(string name)
		{
			foreach (Match m in Regex.Matches(name, "^\\[([^\\]]+)\\]")) return m.Groups[1].Value;
			return "";
		}

		void RemoteCommand(string scriptName, TasSayEventArgs e, string[] words)
		{
			if (stats == null)
			{
				Respond(e, "Stats system is disabled on this autohost.");
				return;
			}
			var b = tas.MyBattle;
			if (b != null)
			{
				var query = string.Format("user={0}&map={1}&mod={2}&p={3}", e.UserName, b.MapName, b.ModName, Utils.Glue(words));
				foreach (var u in b.Users) if (u.Name != tas.UserName) query += string.Format("&users[]={0}|{1}|{2}", u.Name, (u.IsSpectator ? "1" : "0"), u.AllyNumber);
				var response = stats.SendCommand(scriptName, query, false, true).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

				if (response.Length == 0)
				{
					Respond(e, "error accessing stats server");
					return;
				}

				if (response[0].StartsWith("RESPOND")) for (var i = 1; i < response.Length; ++i) Respond(e, response[i]);
				else foreach (var line in response) tas.Say(TasClient.SayPlace.User, e.UserName, line, false);
			}
		}

		void SayLines(TasSayEventArgs e, string what)
		{
			foreach (var line in what.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)) tas.Say(TasClient.SayPlace.User, e.UserName, line, false);
		}

		class UsRank
		{
			public string Clan;
			public readonly double Elo;
			public readonly int Id;
			public readonly UserBattleStatus User;
			public readonly double Weight;

			public UsRank(int id, double elo, double weight, string clan, UserBattleStatus user)
			{
				Id = id;
				Elo = elo;
				Weight = weight;
				User = user;
				Clan = clan;
			}
		}
	}
}