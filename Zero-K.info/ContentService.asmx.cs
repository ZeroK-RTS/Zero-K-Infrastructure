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
		public string AutohostPlayerJoined(string autohostName, string mapName, int accountID)
		{
			var db = new ZkDataContext();
			var mode = GetModeFromHost(autohostName);
			if (mode == AutohostMode.Planetwars)
			{
				var planet = db.Galaxies.Single(x => x.IsDefault).Planets.Single(x => x.Resource.InternalName == mapName);
				var account = db.Accounts.SingleOrDefault(x => x.AccountID == accountID);
				if (account.Clan == null)
				{
					AuthServiceClient.SendLobbyMessage(account, "To play here, join a clan first http://zero-k.info/Planetwars/ClanList");
					return string.Format("{0} this is competetive PlanetWars campaign server. Join a clan to fight http://zero-k.info/Planetwars/ClanList",
					                     account.Name);
				}
				if (!account.Name.Contains(account.Clan.Shortcut))
				{
					AuthServiceClient.SendLobbyMessage(account,
					                                   string.Format("Your name must contain clan tag {0}, rename for example by saying: /rename [{0}]{1}",
					                                                 account.Clan.Shortcut,
					                                                 account.Name));
					return string.Format("{0} cannot play, name must contain clan tag {1}", account.Name, account.Clan.Shortcut);
				}
				string owner = "";
				if (planet.Account != null) owner = planet.Account.Name;
				return string.Format("Greetings {0} {1} of {2}, welcome to {3} planet {4} http://zero-k.info/PlanetWars/Planet/{5}",
				                     account.IsClanFounder ? account.Clan.LeaderTitle : "",
				                     account.Name,
				                     account.IsClanFounder ? account.Clan.ClanName : account.Clan.Shortcut,
														 owner,
				                     planet.Name,
				                     planet.PlanetID);
			}
			return null;
		}


		[WebMethod]
		public BalanceTeamsResult BalanceTeams(string autoHost, string map, List<AccountTeam> currentTeams)
		{
			var mode = GetModeFromHost(autoHost);
			if (currentTeams.Count < 1) return new BalanceTeamsResult() { Message = "Not enough players" };
			using (var db = new ZkDataContext())
			{
				var res = new BalanceTeamsResult();
				res.Message = "";
				var idList = currentTeams.Select(x => x.AccountID).ToList();
				var players = new List<Account>();
				foreach (var p in db.Accounts.Where(x => idList.Contains(x.AccountID)))
				{
					if (p.ClanID == null)
					{
						res.Message += string.Format("{0} cannot play, must join a clan first http://zero-k.info/Planetwars/ClanList\n", p.Name);
						AuthServiceClient.SendLobbyMessage(p, "To play here, join a clan first http://zero-k.info/Planetwars/ClanList");
					}
					else if (!p.Name.Contains(p.Clan.Shortcut))
					{
						res.Message += string.Format("{0} cannot play, name must contain clan tag {1}\n", p.Name, p.Clan.Shortcut);
						AuthServiceClient.SendLobbyMessage(p,
						                                   string.Format("Your name must contain clan tag {0}, rename for example by saying: /rename [{0}]{1}",
						                                                 p.Clan.Shortcut,
						                                                 p.Name));
					}
					else players.Add(p);
				}
				var clans = players.Where(x => x.Clan != null).Select(x => x.Clan).ToList();
				var treaties = new Dictionary<Tuple<Clan, Clan>, EffectiveTreaty>();
				var planet = db.Galaxies.Single(x => x.IsDefault).Planets.Single(x => x.Resource.InternalName == map);

				// bots game
				if (planet.PlanetStructures.Any(x => !string.IsNullOrEmpty(x.StructureType.EffectBots)))
				{
					var teamID = 0;
					for (var i = 0; i < players.Count; i++) res.BalancedTeams.Add(new AccountTeam() { AccountID = players[i].AccountID, Name = players[i].Name, AllyID = 0, TeamID = teamID++ });
					foreach (var b in planet.PlanetStructures.Select(x => x.StructureType).Where(x => !string.IsNullOrEmpty(x.EffectBots))) res.Bots.Add(new BotTeam() { AllyID = 1, BotName = b.EffectBots, TeamID = teamID++ });

					res.Message += string.Format("This planet is infested by aliens, fight for your survival");
					return res;
				}

				if (currentTeams.Count < 2) return new BalanceTeamsResult() { Message = "Not enough players" };

				for (var i = 1; i < clans.Count; i++)
				{
					for (var j = 0; j < i; j++)
					{
						var treaty = clans[i].GetEffectiveTreaty(clans[j]);
						treaties[Tuple.Create(clans[i], clans[j])] = treaty;
						treaties[Tuple.Create(clans[j], clans[i])] = treaty;
					}
				}

				var sameTeamScore = new double[players.Count,players.Count];
				for (var i = 1; i < players.Count; i++)
				{
					for (var j = 0; j < i; j++)
					{
						var c1 = players[i].Clan;
						var c2 = players[j].Clan;
						var points = 0.0;
						if (c1 != null && c2 != null)
						{
							if (c1 == c2) points = 3;
							else
							{
								var treaty = treaties[Tuple.Create(players[i].Clan, players[j].Clan)];
								if (treaty.AllyStatus == AllyStatus.Alliance) points = 1.5;
								else if (treaty.AllyStatus == AllyStatus.Ceasefire) points = 0.5;
								else if (treaty.AllyStatus == AllyStatus.War) points = -1.5;
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
					else if (planet.Account != null && planet.Account.ClanID == player.AccountID) mult += 0.3; // owner's clan 30% 
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
						var team = (combinator & (1 << i)) > 0 ? 1 : 0;
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
					var teamDiffScore = -(30.0*Math.Abs(team0count - team1count)/(double)(team0count + team1count)) - Math.Abs(team0count - team1count);
					if (teamDiffScore < -10) continue; // max imabalance 50% (1v2)

					double balanceModifier = 0;
					if (team0count < team1count) balanceModifier = -teamDiffScore;
					else balanceModifier = teamDiffScore;

					// calculate score for elo difference
					team0Elo = team0Elo/team0Weight;
					team1Elo = team1Elo/team1Weight;
					var eloScore = -Math.Abs(team0Elo - team1Elo)/20;
					if (eloScore < -15) continue; // max 300 elo = 85% chance

					if (team0Elo < team1Elo) balanceModifier += -eloScore;
					else balanceModifier += eloScore;

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
							compoScore += playerScoreMultiplier[i]*sum/cnt;
					}

					if (compoScore < 0) continue; // get meaningfull teams only
					var score = -Math.Abs(balanceModifier)*.65 + (eloScore + teamDiffScore)*.35 + compoScore;

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
					res.Message += "Cannot be balanced well at this point";
				}
				else
				{
					var differs = false;
					for (var i = 0; i < players.Count; i++)
					{
						var allyID = ((bestCombination & (1 << i)) > 0) ? 1 : 0;
						if (!differs && allyID != currentTeams.First(x => x.AccountID == players[i].AccountID).AllyID) differs = true;
						res.BalancedTeams.Add(new AccountTeam() { AccountID = players[i].AccountID, Name = players[i].Name, AllyID = allyID, TeamID = i });
					}
					if (differs)
					{
						res.Message += string.Format("Winning combination  score: {0:0.##} team difference,  {1:0.##} elo,  {2:0.##} composition. Win chance {3}%",
						                             bestTeamDiffs,
						                             bestElo,
						                             bestCompo,
						                             Utils.GetWinChancePercent(bestElo*20));
					}
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

		/// <summary>
		/// This is backup function, remove when not needed
		/// </summary>
		public static AutohostMode GetModeFromHost(string hostname)
		{
			// hack this whole function is hack
			if (hostname.StartsWith("PlanetWars")) return AutohostMode.Planetwars;
			else return AutohostMode.GameTeams;
		}


		[WebMethod]
		public RecommendedMapResult GetRecommendedMap(string autohostName, List<AccountTeam> accounts)
		{
			var mode = GetModeFromHost(autohostName);
			var res = new RecommendedMapResult();
			using (var db = new ZkDataContext())
			{
				if (mode == AutohostMode.Planetwars)
				{
					var gal = db.Galaxies.Single(x => x.IsDefault);
					var valids =
						gal.Planets.Where(
							x => (x.AccountPlanets.Sum(y => (int?)y.DropshipCount) ?? 0) >= (x.PlanetStructures.Sum(y => y.StructureType.EffectDropshipDefense) ?? 0));
					var maxc = valids.Max(x => (int?)x.AccountPlanets.Sum(y => y.DropshipCount)) ?? 0;

					List<Planet> targets = null;
					// if there are no dropships and there are unclaimed planets, target those
					if (maxc == 0 && gal.Planets.Any(x => x.OwnerAccountID == null)) targets = gal.Planets.Where(x => x.OwnerAccountID == null).ToList();
					else
						targets = valids.Where(x => (x.AccountPlanets.Sum(y => (int?)y.DropshipCount) ?? 0) == maxc).ToList();
							// target valid planets with most dropships

					var r = new Random(autohostName.GetHashCode() + gal.Turn); // randomizer based on autohost name + turn to always return same
					var planet = targets[r.Next(targets.Count)];
					res.MapName = planet.Resource.InternalName;
					var owner = "";
					if (planet.Account != null) owner = planet.Account.Name;
					res.Message = string.Format("Welcome to {0} planet {1} http://zero-k.info/PlanetWars/Planet/{2}", owner, planet.Name, planet.PlanetID);
					//if (planet.Account != null) AuthServiceClient.SendLobbyMessage(planet.Account,string.Format("Your planet {0} is going to be attacked! spring://@join_player:{1}", planet.Name, autohostName));
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
		public SpringBattleStartSetup GetSpringBattleStartSetup(string hostName,
		                                                        string map,
		                                                        string mod,
		                                                        List<BattleStartSetupPlayer> players,
		                                                        AutohostMode mode = AutohostMode.GameTeams)
		{
			mode = GetModeFromHost(hostName);
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
					if (mode != AutohostMode.Planetwars) foreach (var unlock in user.AccountUnlocks.Select(x => x.Unlock)) pu.Add(unlock.Code);
					else foreach (var unlock in Galaxy.ClanUnlocks(db, user.ClanID).Select(x => x.Unlock)) pu.Add(unlock.Code);
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
			if (mode == AutohostMode.Planetwars)
			{
				var planet = db.Galaxies.Single(x => x.IsDefault).Planets.Single(x => x.Resource.InternalName == map);

				var pwStructures = new LuaTable();
				foreach (var s in planet.PlanetStructures.Where(x => !x.IsDestroyed && !string.IsNullOrEmpty(x.StructureType.IngameUnitName))) pwStructures.Add("s" + s.StructureTypeID, s.StructureType.IngameUnitName);
				ret.ModOptions.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "pwStructures", Value = pwStructures.ToBase64String() });

				var owner = "";
				var second = "";
				var clanInfluences = planet.GetClanInfluences().Where(x => x.Influence > 0);
				var firstEntry = clanInfluences.FirstOrDefault();
				var secondEntry = clanInfluences.Skip(1).FirstOrDefault();
				if (firstEntry != null) owner = string.Format("{0} ", firstEntry.Clan.Shortcut);
				if (secondEntry != null) second = string.Format("{0} needs {1} influence - ", secondEntry.Clan.Shortcut, firstEntry.Influence - secondEntry.Influence);

				pwStructures = new LuaTable();
				foreach (var s in planet.PlanetStructures.Where(x => !string.IsNullOrEmpty(x.StructureType.IngameUnitName)))
				{
					pwStructures.Add("s" + s.StructureTypeID,
					                 new LuaTable()
					                 {
					                 	{ "unitname", s.StructureType.IngameUnitName },
					                 	{ "isDestroyed", s.IsDestroyed ? 1 : 0 },
					                 	{ "name", owner + s.StructureType.Name },
					                 	{ "description", second + s.StructureType.Description }
					                 });
				}
				ret.ModOptions.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "planetwarsStructures", Value = pwStructures.ToBase64String() });
			}

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
		                                       List<string> extraData)
		{
			try
			{
				var acc = AuthServiceClient.VerifyAccountPlain(accountName, password);
				if (acc == null) throw new Exception("Account name or password not valid");
				if (extraData == null) extraData = new List<string>();

				var mode = GetModeFromHost(accountName);

				var db = new ZkDataContext();
				var sb = new SpringBattle()
				         {
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

				foreach (var p in players)
				{
					sb.SpringBattlePlayers.Add(new SpringBattlePlayer()
					                           {
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

				// awards
				foreach (var line in extraData.Where(x => x.StartsWith("award")))
				{
					var partsSpace = line.Substring(6).Split(new[] { ' ' }, 3);
					var name = partsSpace[0];
					var awardType = partsSpace[1];
					var awardText = partsSpace[2];

					var player = sb.SpringBattlePlayers.Single(x => x.Account.Name == name);
					db.AccountBattleAwards.InsertOnSubmit(new AccountBattleAward()
					                                      {
					                                      	AccountID = player.AccountID,
					                                      	SpringBattleID = sb.SpringBattleID,
					                                      	AwardKey = awardType,
					                                      	AwardDescription = awardText
					                                      });
				}
				db.SubmitChanges();

				var orgLevels = sb.SpringBattlePlayers.Select(x => x.Account).ToDictionary(x => x.AccountID, x => x.Level);

				sb.CalculateElo();
				try
				{
					db.SubmitChanges();
				}
				catch (ChangeConflictException e)
				{
					db.ChangeConflicts.ResolveAll(RefreshMode.KeepChanges);
					db.SubmitChanges();
				}

				var text = new StringBuilder();

				if (mode == AutohostMode.Planetwars && sb.SpringBattlePlayers.Any())
				{
					var gal = db.Galaxies.Single(x => x.IsDefault);
					var planet = gal.Planets.Single(x => x.MapResourceID == sb.MapResourceID);

					text.AppendFormat("Battle on http://zero-k.info/PlanetWars/Planet/{0} has ended\n", planet.PlanetID);

					// handle infelunce
					Clan ownerClan = null;
					if (planet.Account != null) ownerClan = planet.Account.Clan;
					//var prizeIp = 40.0*sb.SpringBattlePlayers.Where(x => !x.IsSpectator).Count()/(double)sb.SpringBattlePlayers.Count(x => !x.IsSpectator && x.IsInVictoryTeam);

					var clanTechIp =
						sb.SpringBattlePlayers.Where(x => !x.IsSpectator).Select(x => x.Account).Where(x => x.ClanID != null).GroupBy(x => x.ClanID).ToDictionary(
							x => x.Key, z => Galaxy.ClanUnlocks(db, z.Key).Count()*3.0/z.Count());

					var ownerMalus = 0;
					if (ownerClan != null)
					{
						var entries = planet.GetClanInfluences();
						if (entries.Count() > 1)
						{
							var diff = entries.First().Influence - entries.Skip(1).First().Influence;
							ownerMalus = (int)((diff/60.0)*(diff/60.0));
						}
					}

					foreach (var p in sb.SpringBattlePlayers.Where(x => !x.IsSpectator && x.IsInVictoryTeam))
					{
						var techBonus = p.Account.ClanID != null ? (int)clanTechIp[p.Account.ClanID] : 0;
						var gapMalus = 0;
						if (ownerClan != null && p.Account.Clan == ownerClan) gapMalus = ownerMalus;
						p.Influence += (techBonus - gapMalus);
						if (p.Influence < 0) p.Influence = 0;

						var entry = planet.AccountPlanets.SingleOrDefault(x => x.AccountID == p.AccountID);
						if (entry == null)
						{
							entry = new AccountPlanet() { AccountID = p.AccountID, PlanetID = planet.PlanetID };
							db.AccountPlanets.InsertOnSubmit(entry);
						}

						var infl = p.Influence ?? 0;

						if (ownerClan != null && p.Account.Clan != null && p.Account.Clan != ownerClan)
						{
							var treaty = ownerClan.GetEffectiveTreaty(p.Account.Clan); // if ceasefired/allianced - give ip to owner
							if (treaty.AllyStatus == AllyStatus.Ceasefire || treaty.AllyStatus == AllyStatus.Alliance)
							{
								var tax = treaty.AllyStatus == AllyStatus.Ceasefire ? 0.33 : 0.5;

								var ownerEntry = planet.Account.AccountPlanets.SingleOrDefault(x => x.PlanetID == planet.PlanetID);
								if (ownerEntry != null)
								{
									var allyInfl = (int)Math.Round(infl*tax);
									infl = (int)Math.Round(infl*(1.0 - tax));
									ownerEntry.Influence += allyInfl;
									db.Events.InsertOnSubmit(Global.CreateEvent("{0} got {1} influence at {2} thanks to ally {3} from {4}",
									                                            planet.Account,
									                                            allyInfl,
									                                            planet,
									                                            p.Account,
									                                            sb));
								}
							}
						}

						entry.Influence += infl;
						db.Events.InsertOnSubmit(Global.CreateEvent("{0} got {1} ({4} from techs {5}) influence at {2} from {3}",
						                                            p.Account,
						                                            p.Influence ?? 0,
						                                            planet,
						                                            sb,
						                                            techBonus,
						                                            gapMalus > 0 ? "-" + gapMalus + " from domination" : ""));

						text.AppendFormat("{0} got {1} ({3} from techs {4}) influence at {2}\n",
						                  p.Account.Name,
						                  p.Influence ?? 0,
						                  planet.Name,
						                  techBonus,
						                  gapMalus > 0 ? "-" + gapMalus + " from domination" : "");
					}

					db.SubmitChanges();

					var bleed = 0;
					// destroy existing dropships
					var noGrowAccount = new List<int>();
					foreach (var ap in planet.AccountPlanets.Where(x => x.DropshipCount > 0))
					{
						if (ap.Account.Clan != ownerClan) bleed += ap.DropshipCount*GlobalConst.PlanetwarsDropshipBleed; // bleed credits for each enemy dropsihp in combat
						ap.DropshipCount = 0;
						noGrowAccount.Add(ap.AccountID);
					}
					if (bleed > 0 && ownerClan != null)
					{
						planet.Account.Credits -= bleed;
						db.Events.InsertOnSubmit(Global.CreateEvent("{0} of {4} lost ${1} in combat at {2} {3}", planet.Account, bleed, planet, sb, planet.Account.Clan));
						text.AppendFormat("{0} lost ${1} due to combat\n", planet.Account.Name, bleed);
					}
					db.SubmitChanges();

					// destroy pw structures
					var handled = new List<string>();
					foreach (var line in extraData.Where(x => x.StartsWith("structurekilled")))
					{
						var data = line.Substring(16).Split(',');
						var unitName = data[0];
						if (handled.Contains(unitName)) continue;
						handled.Add(unitName);
						foreach (var s in db.PlanetStructures.Where(x => x.PlanetID == planet.PlanetID && x.StructureType.IngameUnitName == unitName && !x.IsDestroyed))
						{
							if (s.StructureType.IsIngameDestructible)
							{
								if (s.StructureType.IngameDestructionNewStructureTypeID != null)
								{
									db.PlanetStructures.DeleteOnSubmit(s);
									db.PlanetStructures.InsertOnSubmit(new PlanetStructure()
									                                   {
									                                   	PlanetID = planet.PlanetID,
									                                   	StructureTypeID = s.StructureType.IngameDestructionNewStructureTypeID.Value,
									                                   	IsDestroyed = true
									                                   });
								}
								else s.IsDestroyed = true;
								db.Events.InsertOnSubmit(Global.CreateEvent("{0} has been destroyed on {1} planet {2}. {3}", s.StructureType.Name, ownerClan, planet, sb));
							}
						}
					}
					db.SubmitChanges();

					// destroy structures (usually defenses)
					foreach (var s in planet.PlanetStructures.Where(x => !x.IsDestroyed && x.StructureType.BattleDeletesThis).ToList()) planet.PlanetStructures.Remove(s);
					db.SubmitChanges();

					// spawn new dropships
					foreach (var a in
						sb.SpringBattlePlayers.Where(x => !x.IsSpectator).Select(x => x.Account).Where(x => x.ClanID != null && !noGrowAccount.Contains(x.AccountID)))
					{
						var capacity = GlobalConst.DefaultDropshipCapacity +
						               (a.Planets.SelectMany(x => x.PlanetStructures).Sum(x => x.StructureType.EffectDropshipCapacity) ?? 0);
						var income = GlobalConst.DefaultDropshipProduction +
						             (a.Planets.SelectMany(x => x.PlanetStructures).Sum(x => x.StructureType.EffectDropshipProduction) ?? 0);
						var used = a.AccountPlanets.Sum(x => x.DropshipCount);

						a.DropshipCount += income;
						a.DropshipCount = Math.Min(a.DropshipCount, capacity - used);
					}
					db.SubmitChanges();

					// mines
					foreach (var linkedplanet in gal.Planets.Where(x => (x.PlanetStructures.Sum(y => y.StructureType.EffectLinkStrength) ?? 0) > 0))
					{
						var owner = linkedplanet.Account;
						if (owner != null) owner.Credits += linkedplanet.PlanetStructures.Sum(x => x.StructureType.EffectCreditsPerTurn) ?? 0;
					}

					var oldOwner = planet.OwnerAccountID;
					gal.Turn++;
					db.SubmitChanges();
					db = new ZkDataContext(); // is this needed - attempt to fix setplanetownersbeing buggy
					PlanetwarsController.SetPlanetOwners(db, sb);
					gal = db.Galaxies.Single(x => x.IsDefault);

					// give free planet to each clan with none here
					foreach (
						var kvp in sb.SpringBattlePlayers.Where(x => !x.IsSpectator && x.Account != null && x.Account.ClanID != null).GroupBy(x => x.Account.ClanID))
					{
						var clan = db.Clans.Single(x => x.ClanID == kvp.Key);
						var changed = false;
						if (clan.Accounts.Sum(x => x.Planets.Count()) == 0)
						{
							var planetList = gal.Planets.Where(x => x.OwnerAccountID == null).Shuffle();
							if (planetList.Count > 0)
							{
								var freePlanet = planetList[new Random().Next(planetList.Count)];
								foreach (var ac in kvp) db.AccountPlanets.InsertOnSubmit(new AccountPlanet() { PlanetID = freePlanet.PlanetID, AccountID = ac.AccountID, Influence = 1 });
								db.Events.InsertOnSubmit(Global.CreateEvent("{0} was awarded empty planet {1} {2}", clan, freePlanet, sb));
								changed = true;
							}
						}
						if (changed)
						{
							db.SubmitChanges();
							db = new ZkDataContext();
							PlanetwarsController.SetPlanetOwners(db, sb);
							gal = db.Galaxies.Single(x => x.IsDefault);
						}
					}

					planet = gal.Planets.Single(x => x.Resource.InternalName == result.Map);
					if (planet.OwnerAccountID != oldOwner && planet.OwnerAccountID != null)
					{
						text.AppendFormat("Congratulations!! Planet {0} was conquered by {1} !!  http://zero-k.info/PlanetWars/Planet/{2}\n",
						                  planet.Name,
						                  planet.Account.Name,
						                  planet.PlanetID);
					}

					try
					{
						// store history
						foreach (var p in gal.Planets)
						{
							db.PlanetOwnerHistories.InsertOnSubmit(new PlanetOwnerHistory()
							                                       {
							                                       	PlanetID = p.PlanetID,
							                                       	OwnerAccountID = p.OwnerAccountID,
							                                       	OwnerClanID = p.OwnerAccountID != null ? p.Account.ClanID : null,
							                                       	Turn = gal.Turn
							                                       });

							foreach (var pi in p.AccountPlanets.Where(x => x.Account.ClanID != null))
							{
								db.PlanetInfluenceHistories.InsertOnSubmit(new PlanetInfluenceHistory()
								                                           {
								                                           	PlanetID = p.PlanetID,
								                                           	AccountID = pi.AccountID,
								                                           	ClanID = pi.Account.ClanID.Value,
								                                           	Influence = pi.Influence + pi.ShadowInfluence,
								                                           	Turn = gal.Turn
								                                           });
							}
						}

						db.SubmitChanges();
					}
					catch (Exception ex)
					{
						text.AppendLine("error saving history: " + ex.ToString());
					}
				}

				foreach (var account in sb.SpringBattlePlayers.Select(x => x.Account))
				{
					if (account.Level > orgLevels[account.AccountID])
					{
						try
						{
							var message = string.Format("Congratulations {1}! You just leveled up to level {0}. http://zero-k.info/Users/{1}", account.Level, account.Name);
							text.AppendLine(message);
							AuthServiceClient.SendLobbyMessage(account, message);
						}
						catch (Exception ex)
						{
							Trace.TraceError("Error sending level up lobby message: {0}", ex);
						}
					}
				}

				text.AppendLine(string.Format("View full battle details and demo at http://zero-k.info/Battles/Detail/{0}", sb.SpringBattleID));
				return text.ToString();
			}
			catch (Exception ex)
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


		[WebMethod]
		public bool VerifyAccountData(string login, string password)
		{
			var acc = AuthServiceClient.VerifyAccountPlain(login, password);
			if (acc == null) return false;
			return true;
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
			public bool IsIngameReady;
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
		public List<BotTeam> Bots = new List<BotTeam>();
		public string Message;
	}

	public class AccountTeam
	{
		public int AccountID;
		public int AllyID;
		public string Name;
		public bool Spectate;
		public int TeamID;
	}

	public class BotTeam
	{
		public int AllyID;
		public string BotName;
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