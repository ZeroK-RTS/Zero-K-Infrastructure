using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Web.Services;
using PlasmaShared;
using ZeroKWeb.Controllers;
using ZkData;

namespace ZeroKWeb
{
	/// <summary>
	/// Summary description for ContentService
	/// </summary>
	[WebService(Namespace = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[ToolboxItem(false)]
	// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
		// [System.Web.Script.Services.ScriptService]
	public class ContentService: WebService
	{
		[WebMethod]
		public BalanceTeamsResult BalanceTeams(string autoHost, string map, List<AccountTeam> currentTeams, AutohostMode mode = AutohostMode.Planetwars)
		{
			if (currentTeams.Count < 2)
			{
				return new BalanceTeamsResult() { Message = "Not enough players"} ;
			}
			using (var db = new ZkDataContext())
			{
				var res = new BalanceTeamsResult();
				var idList = currentTeams.Select(x => x.AccountID).ToList();
				var players = db.Accounts.Where(x => idList.Contains(x.AccountID)).ToList();
				var clans = players.Where(x => x.Clan != null).Select(x => x.Clan).ToList();
				var treaties = new Dictionary<Tuple<Clan, Clan>, EffectiveTreaty>();
				var planet = db.Galaxies.Single(x => x.IsDefault).Planets.Single(x => x.Resource.InternalName == map);
				for (var i = 1; i < clans.Count; i++)
				{
					for (var j = 0; j < i; j++)
					{
						var treaty = clans[i].GetEffectiveTreaty(clans[j].ClanID);
						treaties[new Tuple<Clan, Clan>(clans[i], clans[j])] = treaty;
						treaties[new Tuple<Clan, Clan>(clans[j], clans[i])] = treaty;
					}
				}

				var sameTeamScore = new int[players.Count,players.Count];
				for (var i = 1; i < players.Count; i++)
				{
					for (var j = 0; j < i; j++)
					{
						var c1 = players[i].Clan;
						var c2 = players[j].Clan;
						var points = 0;
						if (c1 != null && c2 != null)
						{
							if (c1 == c2) points = 3;
							else
							{
								var treaty = treaties[new Tuple<Clan, Clan>(players[i].Clan, players[j].Clan)];
								if (treaty.AllyStatus == AllyStatus.Alliance) points = 2;
								else if (treaty.AllyStatus == AllyStatus.Ceasefire) points = 1;
								else if (treaty.AllyStatus == AllyStatus.War) points = -2;
							}
						}
						sameTeamScore[i, j] = points;
						sameTeamScore[j, i] = points;
					}
				}

				var playerScoreMultiplier = new double[players.Count];
				for (var i = 0; i < players.Count; i++)
				{
					var mult = 1.0;
					var player = players[i];
					if (planet.OwnerAccountID == player.AccountID) mult += 0.5; // owner 50%
					else if (planet.Account.ClanID == player.AccountID) mult += 0.3; // owner's clan 30% 
					if (planet.AccountPlanets.Any(x => x.AccountID == player.AccountID && x.DropshipCount > 0)) mult += 0.2; // own dropship +20%
					else if (planet.AccountPlanets.Any(x => x.DropshipCount > 0 && x.Account.ClanID == player.ClanID)) mult += 0.1; // clan's dropship +10%
					playerScoreMultiplier[i] = mult;
				}

				

				var limit = 1 << (players.Count);
				var bestCombination = -1;
				var bestScore = double.MinValue;
				double bestCompo = 0;
				double bestElo = 0;
				double bestTeamDiffs = 0;
				var playerAssignments = new int[players.Count];
				for (var combinator = 0; combinator < limit; combinator++)
				{
					double team0Weight = 0;
					double team0Elo = 0;
					double team1Weight = 0;
					double team1Elo = 0;
					var team0count = 0;
					var team1count = 0;

					// determine where each player is amd dp some adding
					for (var i = 0; i < players.Count; i++)
					{
						var player = players[i];
						var team = (combinator & (1 << i)) > 0 ? 1: 0;
						playerAssignments[i] = team;
						if (team == 0)
						{
							team0Elo += player.Elo*player.EloWeight;
							team0Weight += player.EloWeight;
							team0count++;
						}
						else
						{
							team1Elo += player.Elo*player.EloWeight;
							team1Weight += player.EloWeight;
							team1count++;
						}
					}
					if (team0count == 0 || team1count == 0) continue; // skip combination, empty team

					// calculate score for team difference
					var teamDiffScore = -(30.0*Math.Abs(team0count - team1count)/(double)(team0count + team1count));
					if (teamDiffScore < -10) continue; // max imabalance 50% (1v2)

					// calculate score for elo difference
					team0Elo = team0Elo/team0Weight;
					team1Elo = team1Elo/team1Weight;
					var eloScore = -Math.Abs(team0Elo - team1Elo)/20;
					if (eloScore < -15) continue; // max 300 elo = 85% chance

					// calculate score for meaningfull teams
					var compoScore = 0.0;
					for (var i = 0; i < players.Count; i++) // for every player calculate his score as average of relations to other plaeyrs
					{
						double sum = 0;
						var cnt = 0;
						for (var j = 0; j < players.Count; j++)
						{
							if (i != j)
							{
								var sts = sameTeamScore[i, j];
								if (sts > 0) // we only consider no-neutral people 
								{
									if (playerAssignments[i] == playerAssignments[j]) sum += sts;
									else sum -= sts; // different teams - score is equal to negation of same team score
									cnt++;
								}
							}
						}
						if (cnt > 0) // player can be meaningfully ranked, he had at least one non zero relation
							compoScore += playerScoreMultiplier[i] * sum/cnt;
					}
					var score = teamDiffScore + eloScore + compoScore;

					if (score > bestScore)
					{
						bestCombination = combinator;
						bestScore = score;
						bestElo = eloScore;
						bestCompo = compoScore;
						bestTeamDiffs = teamDiffScore;
					}
				}

				if (bestCombination == -1)
				{
					res.BalancedTeams = null;
					res.Message = "Cannot be balanced well at this point";
				}
				else
				{
					var differs = false;
					for (var i = 0; i < players.Count; i++)
					{
						int allyID = ((bestCombination & (1 << i)) > 0) ? 1 : 0;
						if (!differs &&  allyID != currentTeams.First(x => x.AccountID == players[i].AccountID).AllyID) differs = true;
						res.BalancedTeams.Add(new AccountTeam()
						                      { AccountID = players[i].AccountID, Name = players[i].Name, AllyID = allyID, TeamID = i });
					}

					if (differs) res.Message = string.Format("Winning combination  score: {0:0.##} team difference,  {1:0.##} elo,  {2:0.##} composition. Win chance {3}%",
					                            bestTeamDiffs,
					                            bestElo,
					                            bestCompo,
					                            Utils.GetWinChancePercent(bestElo*20));
				}

				return res;
			}
		}

		[WebMethod]
		public bool DownloadFile(string internalName,
		                         out List<string> links,
		                         out byte[] torrent,
		                         out List<string> dependencies,
		                         out ResourceType resourceType,
		                         out string torrentFileName)
		{
			return PlasmaServer.DownloadFile(internalName, out links, out torrent, out dependencies, out resourceType, out torrentFileName);
		}

		[WebMethod]
		public EloInfo GetEloByAccountID(int accountID)
		{
			var db = new ZkDataContext();
			var user = db.Accounts.FirstOrDefault(x => x.AccountID == accountID);
			var ret = new EloInfo();
			if (user != null)
			{
				ret.Elo = user.Elo;
				ret.Weight = user.EloWeight;
			}
			return ret;
		}

		[WebMethod]
		public EloInfo GetEloByName(string name)
		{
			var db = new ZkDataContext();
			var user = db.Accounts.FirstOrDefault(x => x.Name == name);
			var ret = new EloInfo();
			if (user != null)
			{
				ret.Elo = user.Elo;
				ret.Weight = user.EloWeight;
			}
			return ret;
		}

		[WebMethod]
		public List<string> GetEloTop10()
		{
			var db = new ZkDataContext();
			return
				db.Accounts.Where(x => x.SpringBattlePlayers.Any(y => y.SpringBattle.StartTime > DateTime.UtcNow.AddMonths(-1))).OrderByDescending(x => x.Elo).
					Select(x => x.Name).Take(10).ToList();
		}



		[WebMethod]
		public RecommendedMapResult GetRecommendedMap(string autohostName, AutohostMode mode = AutohostMode.GameTeams)
		{
			var res = new RecommendedMapResult();
			using (var db = new ZkDataContext())
			{
				if (mode == AutohostMode.Planetwars)
				{
					var gal = db.Galaxies.Single(x => x.IsDefault);
					var maxc = gal.Planets.SelectMany(x => x.AccountPlanets).Max(y => (int?)y.DropshipCount) ?? 0;
					var targets = gal.Planets.Where(x => (x.AccountPlanets.Sum(y => (int?)y.DropshipCount) ?? 0) == maxc).ToList();
					var r = new Random(autohostName.GetHashCode()); // randomizer based on autohost name to always return same
					var planet = targets[r.Next(targets.Count)];
					res.MapName = planet.Resource.InternalName;
				}
				else
				{
					var list = db.Resources.Where(x => x.FeaturedOrder != null && x.MapIsFfa != true && x.ResourceContentFiles.Any(y => y.LinkCount > 0)).ToList();
					var r = new Random();
					res.MapName = list[r.Next(list.Count)].InternalName;
				}
			}
			return res;
		}


		/// <summary>
		/// Finds resource by either md5 or internal name
		/// </summary>
		/// <param name="md5"></param>
		/// <param name="internalName"></param>
		/// <returns></returns>
		[WebMethod]
		public PlasmaServer.ResourceData GetResourceData(string md5, string internalName)
		{
			return PlasmaServer.GetResourceData(md5, internalName);
		}


		[WebMethod]
		public List<PlasmaServer.ResourceData> GetResourceList(DateTime? lastChange, out DateTime currentTime)
		{
			return PlasmaServer.GetResourceList(lastChange, out currentTime);
		}


		[WebMethod]
		public ScriptMissionData GetScriptMissionData(string name)
		{
			using (var db = new ZkDataContext())
			{
				var m = db.Missions.Single(x => x.Name == name && x.IsScriptMission);
				return new ScriptMissionData()
				       {
				       	MapName = m.Map,
				       	ModTag = m.ModRapidTag,
				       	StartScript = m.Script,
				       	ManualDependencies = m.ManualDependencies != null ? new List<string>(m.ManualDependencies.Split('\n')) : null,
				       	Name = m.Name
				       };
			}
		}

		[WebMethod]
		public SpringBattleStartSetup GetSpringBattleStartSetup(string hostName, string map, string mod, List<BattleStartSetupPlayer> players)
		{
			var ret = new SpringBattleStartSetup();
			var commanderTypes = new LuaTable();
			var db = new ZkDataContext();
			foreach (var p in players.Where(x => !x.IsSpectator))
			{
				var user = db.Accounts.SingleOrDefault(x => x.AccountID == p.AccountID);
				if (user != null)
				{
					var userParams = new List<SpringBattleStartSetup.ScriptKeyValuePair>();
					ret.UserParameters.Add(new SpringBattleStartSetup.UserCustomParameters { AccountID = p.AccountID, Parameters = userParams });

					var pu = new LuaTable();
					foreach (var unlock in user.AccountUnlocks.Select(x => x.Unlock)) pu.Add(unlock.Code);
					userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "unlocks", Value = pu.ToBase64String() });

					var pc = new LuaTable();

					foreach (var c in user.Commanders)
					{
						var morphTable = new LuaTable();
						pc["[\"" + c.Name + "\"]"] = morphTable;
						for (var i = 1; i <= 4; i++)
						{
							var key = "c" + user.AccountID + "_" + c.CommanderID + "_" + i;
							morphTable.Add(key);

							var comdef = new LuaTable();
							commanderTypes[key] = comdef;

							comdef["chassis"] = c.Unlock.Code + i;

							var modules = new LuaTable();
							comdef["modules"] = modules;

							comdef["cost"] = c.GetTotalMorphLevelCost(i);

							comdef["name"] = c.Name + " level " + i;

							foreach (var m in
								c.CommanderModules.Where(x => x.CommanderSlot.MorphLevel <= i).OrderBy(x => x.Unlock.UnlockType).ThenBy(x => x.SlotID).Select(x => x.Unlock)) modules.Add(m.Code);
						}
					}

					userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "commanders", Value = pc.ToBase64String() });
				}
			}

			ret.ModOptions.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "commanderTypes", Value = commanderTypes.ToBase64String() });

			return ret;
		}


		[WebMethod]
		public void NotifyMissionRun(string login, string missionName)
		{
			using (var db = new ZkDataContext())
			using (var scope = new TransactionScope())
			{
				db.Missions.Single(x => x.Name == missionName).MissionRunCount++;
				db.Accounts.Single(x => x.Name == login).MissionRunCount++;
				db.SubmitChanges();
				scope.Complete();
			}
		}


		[WebMethod]
		public PlasmaServer.ReturnValue RegisterResource(int apiVersion,
		                                                 string springVersion,
		                                                 string md5,
		                                                 int length,
		                                                 ResourceType resourceType,
		                                                 string archiveName,
		                                                 string internalName,
		                                                 int springHash,
		                                                 byte[] serializedData,
		                                                 List<string> dependencies,
		                                                 byte[] minimap,
		                                                 byte[] metalMap,
		                                                 byte[] heightMap,
		                                                 byte[] torrentData)
		{
			return PlasmaServer.RegisterResource(apiVersion,
			                                     springVersion,
			                                     md5,
			                                     length,
			                                     resourceType,
			                                     archiveName,
			                                     internalName,
			                                     springHash,
			                                     serializedData,
			                                     dependencies,
			                                     minimap,
			                                     metalMap,
			                                     heightMap,
			                                     torrentData);
		}

		[WebMethod]
		public void SubmitMissionScore(string login, string passwordHash, string missionName, int score, int gameSeconds)
		{
			using (var db = new ZkDataContext())
			{
				var acc = AuthServiceClient.VerifyAccountHashed(login, passwordHash);
				if (acc == null) throw new ApplicationException("Invalid login or password");

				acc.XP += GlobalConst.XpForMissionOrBots;

				var mission = db.Missions.Single(x => x.Name == missionName);

				var scoreEntry = mission.MissionScores.FirstOrDefault(x => x.AccountID == acc.AccountID);
				if (scoreEntry == null)
				{
					scoreEntry = new MissionScore() { MissionID = mission.MissionID, AccountID = acc.AccountID, Score = int.MinValue };
					mission.MissionScores.Add(scoreEntry);
				}

				if (score > scoreEntry.Score)
				{
					var max = mission.MissionScores.Max(x => (int?)x.Score);
					if (max == null || max <= score)
					{
						mission.TopScoreLine = login;
						acc.XP += 150; // 150 for getting top score
					}
					scoreEntry.Score = score;
					scoreEntry.Time = DateTime.UtcNow;
					scoreEntry.MissionRevision = mission.Revision;
					scoreEntry.GameSeconds = gameSeconds;
					db.SubmitChanges();
				}
			}
		}


		[WebMethod]
		public string SubmitSpringBattleResult(string accountName,
		                                       string password,
		                                       BattleResult result,
		                                       List<BattlePlayerResult> players,
		                                       AutohostMode mode = AutohostMode.GameTeams)
		{
			try {
				var acc = AuthServiceClient.VerifyAccountPlain(accountName, password);
				if (acc == null) throw new Exception("Account name or password not valid");

				var db = new ZkDataContext();
				var sb = new SpringBattle() {
					HostAccountID = acc.AccountID,
					Duration = result.Duration,
					EngineGameID = result.EngineBattleID,
					MapResourceID = db.Resources.Single(x => x.InternalName == result.Map).ResourceID,
					ModResourceID = db.Resources.Single(x => x.InternalName == result.Mod).ResourceID,
					HasBots = result.IsBots,
					IsMission = result.IsMission,
					PlayerCount = players.Count(x => !x.IsSpectator),
					StartTime = result.StartTime,
					Title = result.Title,
					ReplayFileName = result.ReplayName,
					EngineVersion = result.EngineVersion ?? "0.82.7",
					// hack remove when fixed
				};
				db.SpringBattles.InsertOnSubmit(sb);

				foreach (var p in players) {
					sb.SpringBattlePlayers.Add(new SpringBattlePlayer() {
						AccountID = p.AccountID,
						AllyNumber = p.AllyNumber,
						CommanderType = p.CommanderType,
						IsInVictoryTeam = p.IsVictoryTeam,
						IsSpectator = p.IsSpectator,
						Rank = p.Rank,
						LoseTime = p.LoseTime
					});
				}

				db.SubmitChanges();
				foreach (var p in players) {
					foreach (var a in p.Awards) {
						db.AccountBattleAwards.InsertOnSubmit(new AccountBattleAward() {
							AccountID = p.AccountID,
							SpringBattleID = sb.SpringBattleID,
							AwardKey = a.Award,
							AwardDescription = a.Description
						});
					}

					foreach (var s in p.Stats) {
						db.AccountBattleStats.InsertOnSubmit(new AccountBattleStat() { AccountID = p.AccountID, SpringBattleID = sb.SpringBattleID, StatsKey = s.Key, Value = s.Value });
					}
				}

				db.SubmitChanges();

				var orgLevels = sb.SpringBattlePlayers.Select(x => x.Account).ToDictionary(x => x.AccountID, x => x.Level);

				sb.CalculateElo(mode == AutohostMode.Planetwars);
				try {
					db.SubmitChanges();
				} catch (ChangeConflictException e) {
					db.ChangeConflicts.ResolveAll(RefreshMode.KeepChanges);
					db.SubmitChanges();
				}

				if (mode == AutohostMode.Planetwars) {
					var gal = db.Galaxies.Single(x => x.IsDefault);
					var planet = gal.Planets.Single(x => x.MapResourceID == sb.MapResourceID);
					foreach (var p in sb.SpringBattlePlayers.Where(x => x.Influence > 0)) {
						var entry = planet.AccountPlanets.SingleOrDefault(x => x.AccountID == p.AccountID);
						if (entry == null) {
							entry = new AccountPlanet() { AccountID = p.AccountID, PlanetID = planet.PlanetID };
							db.AccountPlanets.InsertOnSubmit(entry);
						}
						entry.Influence += (p.Influence ?? 0);
					}
					PlanetwarsController.SetPlanetOwners(db);
				}


				var text = new StringBuilder();

				foreach (var account in sb.SpringBattlePlayers.Select(x => x.Account)) {
					if (account.Level > orgLevels[account.AccountID]) {
						try {
							var message = string.Format("Congratulations {1}! You just leveled up to level {0}. http://zero-k.info/Users/{1}", account.Level, account.Name);
							text.AppendLine(message);
							AuthServiceClient.SendLobbyMessage(account, message);
						} catch (Exception ex) {
							Trace.TraceError("Error sending level up lobby message: {0}", ex);
						}
					}
				}

				text.AppendLine(string.Format("View full battle details and demo at http://zero-k.info/Battles/Detail/{0}", sb.SpringBattleID));
				return text.ToString();
			} catch (Exception ex)
			{
				return ex.ToString();
			}
		}

		[WebMethod]
		public void SubmitStackTrace(ProgramType programType, string playerName, string exception, string extraData, string programVersion)
		{
			using (var db = new ZkDataContext())
			{
				var exceptionLog = new ExceptionLog
				                   {
				                   	ProgramID = programType,
				                   	Time = DateTime.UtcNow,
				                   	PlayerName = playerName,
				                   	ExtraData = extraData,
				                   	Exception = exception,
				                   	ExceptionHash = new Hash(exception).ToString(),
				                   	ProgramVersion = programVersion,
				                   	RemoteIP = GetUserIP()
				                   };
				db.ExceptionLogs.InsertOnSubmit(exceptionLog);
				db.SubmitChanges();
			}
		}


		string GetUserIP()
		{
			var ip = Context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
			if (string.IsNullOrEmpty(ip) || ip.Equals("unknown", StringComparison.OrdinalIgnoreCase)) ip = Context.Request.ServerVariables["REMOTE_ADDR"];
			return ip;
		}

		public class BattlePlayerResult
		{
			public int AccountID;
			public int AllyNumber;
			public List<PlayerAward> Awards;
			public string CommanderType;
			public bool IsSpectator;
			public bool IsVictoryTeam;
			public int? LoseTime;
			public int Rank;
			public List<PlayerStats> Stats;

			public class PlayerAward
			{
				public string Award;
				public string Description;
			}

			public class PlayerStats
			{
				public string Key;
				public double Value;
			}
		}

		public class BattleResult
		{
			public int Duration;
			public string EngineBattleID;
			public string EngineVersion;
			public bool IsBots;
			public bool IsMission;
			public string Map;
			public string Mod;
			public string ReplayName;
			public DateTime StartTime;
			public string Title;
		}

		public class BattleStartSetupPlayer
		{
			public int AccountID;
			public int AllyTeam;
			public bool IsSpectator;
			public int SpringPlayerID;
		}

		public class EloInfo
		{
			public double Elo = 1500;
			public double Weight = 1;
		}

		public class ScriptMissionData
		{
			public List<string> ManualDependencies;
			public string MapName;
			public string ModTag;
			public string Name;
			public string StartScript;
		}

		public class SpringBattleStartSetup
		{
			public List<ScriptKeyValuePair> ModOptions = new List<ScriptKeyValuePair>();
			public List<UserCustomParameters> UserParameters = new List<UserCustomParameters>();

			public class ScriptKeyValuePair
			{
				public string Key;
				public string Value;
			}

			public class UserCustomParameters
			{
				public int AccountID;
				public List<ScriptKeyValuePair> Parameters = new List<ScriptKeyValuePair>();
			}
		}
	}

	public class RecommendedMapResult
	{
		public string MapName;
		public string Message;

	}

	public class BalanceTeamsResult
	{
		public List<AccountTeam> BalancedTeams = new List<AccountTeam>();
		public string Message;
	}

	public class AccountTeam
	{
		public int AccountID;
		public int AllyID;
		public string Name;
		public int TeamID;
	}

	public enum AutohostMode
	{
		Planetwars = 1,
		Game1v1 = 2,
		GameTeams = 3,
		GameFFA = 4,
		GameChickens = 5
	}
}