#region using

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PlanetWarsShared;
using PlanetWarsShared.Events;
using PlanetWarsShared.Springie;

#endregion

namespace PlanetWarsServer
{
	[Serializable]
	public class Server : ISpringieServer, IServer, IDisposable
	{
		#region Constants

		const int MaxPasswordLength = 20;
		const int MaxPlanetNameLength = 20;
		const int MaxRankNameLength = 20;
		const int MinPasswordLength = 1;
		const int MinPlanetNameLength = 3;
		const int MinRankNameLength = 3;
		const int SpringieTimeout = 120; // minutes after which an autohost is considered dead

		#endregion

		#region Fields

		static readonly object locker = new object();

		static Server instance;

		public Server() {}
		public ServerState State { get; private set; }

		public static Server Instance
		{
			get
			{
				lock (locker) {
					if (instance == null) {
						new Server(true);
					}
					return instance;
				}
			}
		}

		public Galaxy Galaxy
		{
			get { return State.Galaxy; }
			set
			{
				if (instance != null) {
					Console.Error.WriteLine("Server instance already exists, recreating");
				}
				State = new ServerState {Galaxy = value};
				instance = this;
			}
		}

		#endregion

		#region Constructors

		DateTime lastSaved = DateTime.Now;

		Server(bool newInstance)
		{
			if (instance != null) {
				Console.Error.WriteLine("Server instance already exists, recreating");
			}
			instance = this;

			var stateFileInfo = new FileInfo(Settings.Default.DefaultServerStatePath);
			if (stateFileInfo.Exists && stateFileInfo.Length > 0) {
				State = ServerState.FromFile(Settings.Default.DefaultServerStatePath);
			} else if (File.Exists(Settings.Default.GalaxyTemplatePath)) {
				Console.Error.Write("No state found, loading galaxy from xml.");
				State = new ServerState {Galaxy = Galaxy.FromFile(Settings.Default.GalaxyTemplatePath)};
				SaveState();
			} else {
				throw new Exception("No state or galaxy found.");
			}
		}

		// for unit testing

		public Server(ServerState state)
		{
			if (instance != null) {
				Console.Error.WriteLine("Server instance already exists, recreating");
			}
			instance = this;
			State = state;
		}

		public Server(Galaxy galaxy)
		{
			Galaxy = galaxy;
		}

		public bool DontSave { get; set; }

		void SaveState()
		{
			SaveState(false);
		}

		void SaveState(bool forceSave)
		{
			LastChanged = DateTime.Now;
			if (!DontSave) {
				if (forceSave || DateTime.Now.Subtract(lastSaved).TotalMinutes >= 15) {
					// only save max every 15 minutes
					State.SaveToFile(Settings.Default.DefaultServerStatePath);
					lastSaved = DateTime.Now;
				}
			}
		}

		#endregion

		#region Public methods

		ICollection<IPlanet> GetAttackOptions(AuthInfo springieLogin, IFaction faction)
		{
			if (faction == null) {
				throw new ArgumentNullException("faction");
			}

			SpringieState state;
			if (SpringieStates.TryGetValue(springieLogin.Login, out state)) {
				state.LastUpdate = DateTime.Now;
			}

			var localFaction = Galaxy.Factions.Single(f => f.Name == faction.Name);
			var alreadyAttacked = from kvp in SpringieStates
			                      where
			                      	(kvp.Key != springieLogin.Login) && (kvp.Value.ReminderEvent == ReminderEvent.OnBattleStarted) &&
			                      	(DateTime.Now - kvp.Value.LastUpdate).TotalMinutes < SpringieTimeout
			                      let status = kvp.Value.GameStartedStatus
			                      select Galaxy.GetPlanet(status.PlanetID);
			var mightBeEncircled = new List<Planet>();
			foreach (var planet in alreadyAttacked) {
				var speculativeGalaxy = Galaxy.BinaryClone();
				var conqueredPlanet = speculativeGalaxy.GetPlanet(planet.ID);
				conqueredPlanet.FactionName = speculativeGalaxy.OffensiveFaction.Name;
				var encircledPlanets = speculativeGalaxy.Planets.Where(speculativeGalaxy.IsPlanetEncircled);
				mightBeEncircled.AddRange(encircledPlanets);
			}

			var dontAttack = alreadyAttacked.Concat(mightBeEncircled);
			return Galaxy.GetAttackOptions(localFaction).Except(dontAttack).Cast<IPlanet>().ToArray();
		}

		#endregion

		#region Other methods

		public bool ValidateLogin(AuthInfo authorization)
		{
			if (authorization == null || string.IsNullOrEmpty(authorization.Login) ||
			    string.IsNullOrEmpty(authorization.Password)) {
				return false;
			}
			if (authorization.Login == null && authorization.Password == null) {
				return false;
			}
			Hash hash;
			return State.Accounts.TryGetValue(authorization.Login, out hash) && hash == Hash.HashString(authorization.Password);
		}

		#endregion

		readonly List<string> disabledUpgradesHosts = new List<string> {"PlanetMatches"};

		readonly Dictionary<string, string> springieLogins = new Dictionary<string, string>
		{
			{"PlanetWars", "RuleAll"},
			{"Springie3", "RuleAll"},
			{"WebClient", "RuleAll"},
			{"PlanetWars2", "RuleAll"},
			{"PlanetBattles", "RuleAll"},
			{"PlanetSkirmishes", "RuleAll"},
			{"PlanetDuels", "RuleAll"},
			{"PlanetMatches", "RuleAll"}
		};

		public SerializableDictionary<string, SpringieState> SpringieStates =
			new SerializableDictionary<string, SpringieState>();

		#region IDisposable Members

		public void Dispose()
		{
			instance = null;
		}

		#endregion

		#region IServer Members

		public bool ChangePlanetDescription(string newDescription, AuthInfo authorization, out string message)
		{
			if (!ValidateLogin(authorization)) {
				message = "Authorization failed";
				return false;
			}
			if (newDescription.Length > 255) {
				message = "Text too long";
				return false;
			}

			Galaxy.GetPlayer(authorization.Login).Description = newDescription;
			SaveState();
			message = "ok";
			return true;
		}

		public bool ChangeCommanderInChiefTitle(string newTitle, AuthInfo authorization, out string message)
		{
			if (newTitle == null) {
				throw new ArgumentNullException("newTitle");
			}
			if (authorization == null) {
				throw new ArgumentNullException("authorization");
			}
			if (!ValidateLogin(authorization)) {
				message = "Wrong username or password.";
				return false;
			}
			if (Regex.IsMatch(newTitle, "[^A-Z-a-z0-9 ]") || newTitle.Length < MinRankNameLength) {
				message = "Invaling rank name.";
				return false;
			}
			if (newTitle.Length > MaxRankNameLength) {
				newTitle = newTitle.Substring(0, MaxRankNameLength);
			}
			var player = Galaxy.Players.SingleOrDefault(p => p.Name == authorization.Login);
			if (player != null) {
				if (player.Rank != Rank.CommanderInChief) {
					message = "You must be commander-in-chief to rename your rank.";
					return false;
				}
				State.Galaxy.Events.Add(
					new RankNameChangedEvent(
						DateTime.Now, authorization.Login, player.Title ?? "Commander-in-chief", newTitle, State.Galaxy));
				player.Title = newTitle;
				SaveState();
				message = "Rank name changed.";
				return true;
			}
			message = "Player not in game";
			return false;
		}

		public bool SendAid(string toPlayer, double ammount, AuthInfo authorization, out string message)
		{
			if (!ValidateLogin(authorization)) {
				message = "Wrong username or password";
				return false;
			}

			var from = Galaxy.GetPlayer(authorization.Login);
			var to = Galaxy.GetPlayer(toPlayer);
			if (from == null || to == null) {
				message = "Player not found";
				return false;
			}
			if (Galaxy.GetPlanet(from.Name) == null && from.Victories >= 3) {
				message = "Only respected planet owners can transfer metal";
				return false;
			}
			if (ammount <= 0 || ammount > from.MetalAvail) {
				message = "Cannot send such ammount";
				return false;
			}
			from.SentMetal += ammount;
			to.RecievedMetal += ammount*0.5;
			from.MetalSpent += ammount;
			to.MetalEarned += ammount*0.5;
			State.Galaxy.Events.Add(new AidSentEvent(DateTime.Now, State.Galaxy, authorization.Login, toPlayer, ammount));
			message = "OK";
			SaveState();
			return true;
		}

		public bool ChangePlayerPassword(string newPassword, AuthInfo authorization, out string message)
		{
			if (newPassword == null) {
				throw new ArgumentNullException("newPassword");
			}
			if (authorization == null) {
				throw new ArgumentNullException("authorization");
			}
			if (newPassword.Length > 20 || newPassword.Length < 1) {
				message = String.Format(
					"Password must be between {0} and {1} characters long.", MinPasswordLength, MaxPasswordLength);
				return false;
			}
			if (!ValidateLogin(authorization)) {
				message = "Wrong username or password.";
				return false;
			}
			State.Accounts[authorization.Login] = Hash.HashString(newPassword);
			SaveState();
			message = "Password changed.";
			return true;
		}

		public bool ChangePlanetName(string newName, AuthInfo authorization, out string message)
		{
			if (newName == null) {
				throw new ArgumentNullException("newName");
			}
			if (authorization == null) {
				throw new ArgumentNullException("authorization");
			}
			var g = Galaxy;
			if (!ValidateLogin(authorization)) {
				message = "Wrong username or password.";
				return false;
			}
			if (Regex.IsMatch(newName, "[^A-Z-a-z0-9 ]")) {
				message = "Name uses invalid characters.";
				return false;
			}
			if (newName.Length < MinPlanetNameLength) {
				message = "Name is too short.";
				return false;
			}
			if (newName.Length > MaxPlanetNameLength) {
				newName = newName.Substring(0, MaxPlanetNameLength);
			}
			newName = newName.Replace("\r\n", " ").Replace("\n", " ");
			if (g.Planets.Any(p => p.Name == newName)) {
				message = "Name already taken.";
				return false;
			}
			var planet = g.GetPlanet(authorization.Login);
			if (planet == null) {
				message = "Player owns no planet";
				return false;
			}
			State.Galaxy.Events.Add(
				new PlanetNameChangedEvent(DateTime.Now, planet.ID, planet.Name, newName, planet.OwnerName, Galaxy));
			planet.Name = newName;
			SaveState();
			message = "ok";
			return true;
		}

		public DateTime LastChanged { get; set; }

		public Galaxy GetGalaxyMap(AuthInfo authorization)
		{
			if (authorization == null) {
				throw new ArgumentNullException("authorization");
			}
			if (ValidateLogin(authorization)) {
				return Galaxy;
			}
			return null;
		}

		public bool ChangePlanetMap(string mapName, AuthInfo authorization, out string message)
		{
			if (mapName == null) {
				throw new ArgumentNullException("mapName");
			}
			if (authorization == null) {
				throw new ArgumentNullException("authorization");
			}
			var g = Galaxy;
			if (!ValidateLogin(authorization)) {
				message = "Wrong username or password.";
				return false;
			}
			var player = g.GetPlayer(authorization.Login);
			var planet = g.GetPlanet(player);
			if (!g.GetAvailableMaps().Contains(mapName)) {
				message = "Map is not available.";
				return false;
			}
			if (player.HasChangedMap) {
				message = "Planet was already chosen.";
				return false;
			}
			State.Galaxy.Events.Add(new MapChangedEvent(DateTime.Now, planet.MapName, mapName, planet.ID, State.Galaxy));
			planet.MapName = mapName;
			player.HasChangedMap = true;
			RemoveAllUpgradesFromPlanet(planet.ID);
			SaveState();
			message = "ok";
			return true;
		}

		public bool SetReminderOptions(ReminderEvent reminderEvent,
		                               ReminderLevel reminderLevel,
		                               ReminderRoundInitiative reminderRoundInitiative,
		                               AuthInfo authorization,
		                               out string message)
		{
			if (authorization == null) {
				throw new ArgumentNullException("authorization");
			}
			if (!ValidateLogin(authorization)) {
				message = "Wrong username or password.";
				return false;
			}
			var player = Galaxy.GetPlayer(authorization.Login);
			player.ReminderEvent = reminderEvent;
			player.ReminderLevel = reminderLevel;
			player.ReminderRoundInitiative = reminderRoundInitiative;
			message = "Success";
			SaveState();
			return true;
		}

		public bool SetTimeZone(TimeZoneInfo LocalTimeZone, AuthInfo authorization, out string message)
		{
			if (authorization == null) {
				throw new ArgumentNullException("authorization");
			}
			if (!ValidateLogin(authorization)) {
				message = "Wrong username or password.";
				return false;
			}
			var player = Galaxy.GetPlayer(authorization.Login);
			player.LocalTimeZone = LocalTimeZone;
			message = "Success";
			SaveState();
			return true;
		}

		public void BuyUpgrade(AuthInfo login, int upgradeDefID, string choice)
		{
			if (!ValidateLogin(login)) {
				throw new Exception("Login failed");
			}
			var udef = new Upgrades().UpgradeDefs.Where(x => x.ID == upgradeDefID).SingleOrDefault();
			if (udef == null || !udef.UnitDefs.Where(x => x.Name == choice).Any()) {
				throw new Exception("No such upgrade definition found");
			}
			// remove upgrade of same level or id if it already exists
			List<UpgradeDef> existing;
			if (UpgradeData.TryGetValue(login.Login, out existing)) {
				var todel =
					existing.Where(
						x => x.ID == upgradeDefID || x.Level == udef.Level && x.Branch == udef.Branch && x.Division == udef.Division).
						ToList();

				foreach (var x in todel) {
					existing.Remove(x);
				}
			}

			var copy = udef.BuyCopy();
			copy.UnitChoice = choice;
            var player = Galaxy.GetPlayer(login.Login);

			if (copy.IsSpaceShip) {
				
				var planet = Galaxy.GetPlanet(player.Name);
				Planet targetPlanet = null;
				if (planet != null && planet.FactionName == player.FactionName) {
					targetPlanet = planet;
				} else {
					targetPlanet =
						Galaxy.Planets.Where(x => x.FactionName == player.FactionName).OrderBy(
							x => Galaxy.GetPlayer(x.OwnerName).RankOrder).FirstOrDefault();
				}
				if (targetPlanet == null) {
					throw new Exception("Cannot find suitable planet for the fleet");
				}

				var fleet = new SpaceFleet
				{TargetPlanetID = targetPlanet.ID, Destination = targetPlanet.Position, OwnerName = player.Name};
				Galaxy.Fleets.Add(fleet);
				copy.Purchased++;
			}

			if (existing == null) {
				existing = new List<UpgradeDef>();
			}
			existing.Add(copy);
            player.PurchaseHistory.Add(new PurchaseData(upgradeDefID, choice));
			UpgradeData[login.Login] = existing;
			Galaxy.GetPlayer(login.Login).MetalSpent += copy.Cost;
			SaveState();
		}

		public ICollection<UpgradeDef> GetAvailableUpgrades(Galaxy galaxy, string playerName)
		{
			var player = Galaxy.GetPlayer(playerName);
			var availableUpgrades = new List<UpgradeDef>();
			var allUpgrades = new Upgrades().UpgradeDefs;
			foreach (var upgradeDef in allUpgrades) {
				if (upgradeDef.Cost <= player.MetalAvail && upgradeDef.FactionName == player.FactionName) {
					List<UpgradeDef> upgrades;
					if (!UpgradeData.TryGetValue(playerName, out upgrades)) {
						upgrades = new List<UpgradeDef>();
					}

					var lesserUpgrade =
						allUpgrades.Where(
							x =>
							x.FactionName == upgradeDef.FactionName && x.Branch == upgradeDef.Branch && x.Division == upgradeDef.Division &&
							x.Level == upgradeDef.Level - 1).SingleOrDefault();

					var hasLesserUpgrade = lesserUpgrade == null || upgrades.Where(x => x.ID == lesserUpgrade.ID).Any();

					if (hasLesserUpgrade) {
						availableUpgrades.Add(upgradeDef);
					}
				}
			}
			return availableUpgrades;
		}

		public IDictionary<string, List<UpgradeDef>> UpgradeData
		{
			get { return State.UpgradeData; }
		}

		public void ForceSaveState()
		{
			SaveState(true);
		}

		public IDictionary<string, SpringieState> GetSpringieStates()
		{
			return SpringieStates;
		}

		public bool SendBlockadeFleet(AuthInfo login, int targetPlanetID, out string message)
		{
			if (!ValidateLogin(login)) {
				message = "Login invalid";
				return false;
			}

			var fleet = Galaxy.Fleets.SingleOrDefault(f => f.OwnerName == login.Login);
			if (fleet == null) {
				message = "You dont own blockade fleet";
				return false;
			}

			if (fleet.Arrives <= Galaxy.Turn &&
			    SpringieStates.Values.Any(
			    	s => s.ReminderEvent == ReminderEvent.OnBattleStarted && s.GameStartedStatus.PlanetID == fleet.TargetPlanetID)) {
				message = "This fleet is currently engaged in combat, orders cancelled";
				return false;
			}

			var ret = fleet.SetDestination(Galaxy, targetPlanetID, out message);
			SaveState();
			return ret;
		}

		#endregion

		#region ISpringieServer Members

		public ICollection<IPlanet> GetAttackOptions(AuthInfo springieLogin)
		{
			return GetAttackOptions(springieLogin, GetOffensiveFaction(springieLogin)).ToList();
		}

		public ICollection<IFaction> GetFactions(AuthInfo springieLogin)
		{
			return Galaxy.Factions.Cast<IFaction>().ToList();
		}

		public ICollection<string> GetPlayersToNotify(AuthInfo springieLogin, string mapName, ReminderEvent reminderEvent)
		{
			if (mapName == null) {
				throw new ArgumentNullException("mapName");
			}
			var planet = Galaxy.Planets.Single(p => p.MapName == mapName);
			if (planet == null) {
				throw new Exception(mapName + " is used by any planet.");
			}

			/* -------------- multiple autohost support ---------------- */
			SpringieState springiestate;
			if (!SpringieStates.TryGetValue(springieLogin.Login, out springiestate)) {
				springiestate = new SpringieState(planet.ID, reminderEvent, Galaxy.OffensiveFaction.Name);
				SpringieStates[springieLogin.Login] = springiestate;
			}
			if (reminderEvent == ReminderEvent.OnBattleStarted) {
				springiestate.GameStartedStatus = springiestate.BinaryClone();
			}
			springiestate.PlanetID = planet.ID;
			springiestate.ReminderEvent = reminderEvent;
			springiestate.OffensiveFactionName = Galaxy.OffensiveFaction.Name;
			springiestate.LastUpdate = DateTime.Now;

			/* ---------------------------------------------------------- */

			var q = from p in Galaxy.Players
			        where (p.ReminderEvent & reminderEvent) == reminderEvent
			        where p.ReminderLevel != ReminderLevel.MyPlanet || p.Name == planet.OwnerName
			        let IsOffensivePlayer = p.FactionName == Galaxy.OffensiveFaction.Name
			        let IsAttackSet =
			        	(p.ReminderRoundInitiative & ReminderRoundInitiative.Offense) == ReminderRoundInitiative.Offense
			        let IsDefenseSet =
			        	(p.ReminderRoundInitiative & ReminderRoundInitiative.Defense) == ReminderRoundInitiative.Defense
			        where (IsAttackSet && IsOffensivePlayer) || (IsDefenseSet && !IsOffensivePlayer)
			        select p.Name;

			if (reminderEvent == ReminderEvent.OnBattleStarted) {
				ForceSaveState();
			}

			return q.ToArray();
		}

		public IFaction GetOffensiveFaction(AuthInfo springieLogin)
		{
			return Galaxy.OffensiveFaction;
		}

		public IPlayer GetPlayerInfo(AuthInfo springieLogin, string name)
		{
			if (name == null) {
				throw new ArgumentNullException("name");
			}
			return (from p in Galaxy.Players
			        where p.Name == name
			        select p).FirstOrDefault();
		}

		public string Register(AuthInfo springieLogin, AuthInfo account, string side, string planetName)
		{
			if (account == null) {
				throw new ArgumentNullException("account");
			}
			if (springieLogin == null) {
				throw new ArgumentNullException("springieLogin");
			}
			if (!ValidateSpringieLogin(springieLogin)) {
				return string.Empty; // silently ignore other than valid springie
			}
			if (Galaxy.Players.Any(p => p.Name == account.Login)) {
				return "Name already taken.";
			}

			var faction =
				Galaxy.Factions.SingleOrDefault(f => string.Equals(f.Name, side, StringComparison.InvariantCultureIgnoreCase));
			if (faction == null) {
				return "Invalid faction chosen.";
			}

			var teams = (from p in Galaxy.Players
			             where p.FactionName != faction.Name && (p.Victories > 0 || p.Defeats > 0)
			             group p by p.FactionName
			             into gr select gr.Count());
			var smallestTeam = teams.Count() > 0 ? teams.Min() : 0;

			var myFaction = Galaxy.Players.Count(p => p.FactionName == faction.Name && (p.Victories > 0 || p.Defeats > 0));

			if (myFaction - smallestTeam > Settings.Default.MaxFactionSizeDifference) {
				return "Selected faction already has too many active players.";
			}

			string result;
			Event playerEvent;
			var planets = Galaxy.GetClaimablePlanets(faction).ToArray();
			if (planets.Any()) {
				var planet = string.IsNullOrEmpty(planetName)
				             	? planets.TakeRandom()
				             	: planets.SingleOrDefault(p => p.Name.ToUpper() == planetName.ToUpper());
				if (planet == null) {
					return string.Format("Planet {0} is occupied or not in the galaxy.", planetName);
				}
				var mapName = Galaxy.GetAvailableMaps().TakeRandom();
				planet.OwnerName = account.Login;
				planet.MapName = mapName;
				planet.FactionName = faction.Name;
				playerEvent = new PlayerRegisteredEvent(DateTime.Now, account.Login, planet.ID, State.Galaxy);
				result = string.Format("Welcome to PlanetWars! Your planet is {0}, with map {1}.", planet.Name, planet.MapName);
			} else {
				result = "Welcome to PlanetWars! You are in, but you don't own any planet (no free planets left.)";
				playerEvent = new PlayerRegisteredEvent(DateTime.Now, account.Login, null, State.Galaxy);
			}
			State.Accounts[account.Login] = Hash.HashString(account.Password);
			Galaxy.Players.Add(new Player(account.Login, faction.Name));
			State.Galaxy.Events.Add(playerEvent);
			SaveState();
			return result;
		}

		public string GetStartupModOptions(AuthInfo springieLogin, string mapName, ICollection<IPlayer> players)
		{
			if (mapName == null) {
				throw new ArgumentNullException("mapName");
			}
			if (players == null) {
				throw new ArgumentNullException("players");
			}

			var planet = Galaxy.Planets.Single(p => p.MapName == mapName);
			if (!GetAttackOptions(springieLogin).Any(p => p.ID == planet.ID)) {
				throw new ApplicationException("You cannot attack this planet right now");
			}

			var planetOwner = Galaxy.GetPlayer(planet.OwnerName);
			var leaderName = players.Any(p => p.Name == planetOwner.Name)
			                 	? planetOwner.Name
			                 	: (from p in players
			                 	   where p.FactionName == planetOwner.FactionName
			                 	   orderby p.RankOrder
			                 	   select p.Name).First();
			var factions = players.Select(p => p.FactionName).Distinct().ToList();
			var difference = players.Count(p => p.FactionName == factions[1]) - players.Count(p => p.FactionName == factions[0]);
			var outNumberedFaction = difference > 0 ? factions[0] : factions[1];
			var playersToReinforce = players // fixme: dont hardcode units and factions
				.Where(p => p.FactionName == outNumberedFaction).OrderBy(p => p.RankOrder).Take(Math.Abs(difference))
				// FIXME: might not give enough comms
				.ToDictionary(p => p.Name, p => 1); // fixme: use right number of comms instead of 1

			File.AppendAllText("modOptionsLog.txt", "Factions: " + String.Join(" ", factions.ToArray()) + "\n");
			File.AppendAllText("modOptionsLog.txt", "Difference: " + difference + "\n");
			File.AppendAllText("modOptionsLog.txt", "Outnumbered faction: " + outNumberedFaction + "\n");
			File.AppendAllText(
				"modOptionsLog.txt", "Players to reinforce: " + String.Join(" ", playersToReinforce.Keys.ToArray()) + "\n");

			if (players.Contains(planetOwner) && planetOwner.Name != leaderName) {
				throw new Exception("Owner is in battle but is not leader");
			}

			// get list of blocked factions
			var blockedFactions = new List<string>();
			var factionFleets =
				Galaxy.Fleets.Where(f => f.TargetPlanetID == planet.ID && f.Arrives <= Galaxy.Turn).GroupBy(
					f => Galaxy.GetPlayer(f.OwnerName).FactionName).OrderByDescending(g => g.Count()).ToList();

			for (var i = 0; i < factionFleets.Count(); i++) {
				blockedFactions.AddRange(Galaxy.Factions.Where(f => f.Name != factionFleets[i].Key).Select(f => f.Name.ToUpper()));
				// disable enemy factions

				if (i < factionFleets.Count() - 1 && factionFleets[i].Count() > factionFleets[i + 1].Count()) {
					break; // more ships than other faction, no other factions block upgrades
				}
			}

			var main = new LuaTable();
			var teams = new LuaTable();
			main["teams"] = teams;
			var teamID = 0;

			var isWithoutUpgrades = disabledUpgradesHosts.Contains(springieLogin.Login);

			foreach (var player in players) {
				var plTable = new LuaTable();
				var mobiles = new LuaTable();
				var structures = new LuaTable();
				var purchases = new LuaTable();
				var galPlayer = Galaxy.GetPlayer(player.Name);
				teams.Add("[" + teamID++ + "]", plTable);

				plTable["mobiles"] = mobiles;
				plTable["structures"] = structures;
				plTable["purchases"] = purchases;
				plTable["money"] = galPlayer.MetalAvail;
				plTable["name"] = galPlayer.Name;

				// extra commanders
				int reinforcementNumber;
				if (playersToReinforce.TryGetValue(player.Name, out reinforcementNumber) && reinforcementNumber > 0) {
					var unit = player.FactionName.ToUpper() == "ARM" ? "armcom" : "corcom";
					for (var i = 0; i < reinforcementNumber; i++) {
						mobiles.Add(new LuaTable {{"unitname", unit}});
					}
				}

				if (isWithoutUpgrades) {
					continue;
				}

				var isAllyOwnedPlanet = player.FactionName.ToUpper() == planetOwner.FactionName.ToUpper();

				List<UpgradeDef> playerUpgrades;
				if (UpgradeData.TryGetValue(player.Name, out playerUpgrades)) {
					if (!blockedFactions.Contains(player.FactionName.ToUpper())) {
						foreach (var upgrade in playerUpgrades.Where(u => !u.IsBuilding || isAllyOwnedPlanet)) {
							purchases.Add(new LuaTable {{"unitname", upgrade.UnitChoice}, {"owner", player.Name}});
							if (!upgrade.IsBuilding && upgrade.QuantityMobiles > 0) {
								for (var i = 0; i < upgrade.QuantityMobiles; i++) {
									mobiles.Add(new LuaTable {{"unitname", upgrade.UnitChoice}, {"owner", player.Name}});
								}
							}
						}
					}

					if (isAllyOwnedPlanet) {
						foreach (var upgrade in playerUpgrades.Where(u => u.IsBuilding)) {
							foreach (var d in upgrade.DeployLocations) {
								if (d.PlanetID == planet.ID) {
									structures.Add(
										new LuaTable
										{
											{"unitname", upgrade.UnitChoice},
											{"x", d.X},
											{"z", d.Z},
											{"orientation", d.Orientation},
											{"owner", player.Name}
										});
								}
							}
						}
					}
				}

				// deploy other structures present on the planet to the leader
				if (player.Name == leaderName) {
					foreach (var kvp in UpgradeData) {
						if (players.Any(x => x.Name == kvp.Key) || Galaxy.GetPlayer(kvp.Key).FactionName != planetOwner.FactionName) {
							continue; //skip players which play
						}
						foreach (var upg in kvp.Value.Where(u => u.IsBuilding)) {
							// give allied deployed units
							foreach (var d in upg.DeployLocations.Where(p => p.PlanetID == planet.ID)) {
								structures.Add(
									new LuaTable
									{{"unitname", upg.UnitChoice}, {"x", d.X}, {"z", d.Z}, {"orientation", d.Orientation}, {"owner", kvp.Key}});
							}
						}
					}
				}
			}
			main["allyteams"] = new LuaTable();
			main["hostname"] = springieLogin.Login;
			var optionsString = "return " + main;

			File.AppendAllText("modOptionsLog.txt", optionsString);

			return Convert.ToBase64String(Encoding.ASCII.GetBytes(optionsString)).Replace("\t", "  ").Replace("=", "_");
		}

		public SendBattleResultOutput SendBattleResult(AuthInfo springieLogin,
		                                               string mapName,
		                                               ICollection<EndGamePlayerInfo> usersInGame)
		{
			var ret = new SendBattleResultOutput();
			ret.RankNotifications = new List<RankNotification>();
			if (springieLogin == null) {
				throw new ArgumentNullException("springieLogin");
			}
			if (usersInGame == null) {
				throw new ArgumentNullException("usersInGame");
			}
			if (!ValidateSpringieLogin(springieLogin)) {
				return ret; // silently ignore other than valid springie
			}

			var invalids = usersInGame.Where(x => !x.Spectator && Galaxy.GetPlayer(x.Name) == null).ToList();
			foreach (var u in invalids) {
				ret.MessageToDisplay += "Invalid players " + u.Name + " ignored";
				usersInGame.Remove(u);
			}

			if (!usersInGame.Any(p => p.OnVictoryTeam)) {
				ret.MessageToDisplay += "No losing players";
				return ret;
			}
			if (!usersInGame.Any(p => !p.OnVictoryTeam)) {
				ret.MessageToDisplay += "Only losing players";
				return ret;
			}

			if (!usersInGame.All(u => u.Spectator || Galaxy.GetPlayer(u.Name) != null)) {
				throw new Exception("Player is not registered.");
			}
			if (
				!usersInGame.All(
				 	u =>
				 	u.Spectator ||
				 	String.Equals(Galaxy.GetPlayer(u.Name).FactionName, u.Side, StringComparison.InvariantCultureIgnoreCase))) {
				throw new Exception("Faction mismatch.");
			}

			var isWithoutUpgrades = disabledUpgradesHosts.Contains(springieLogin.Login);

			Faction victoriousFaction = null;

			foreach (var user in usersInGame) {
				if (user.Spectator) {
					continue;
				}
				var player = Galaxy.Players.Single(p => p.Name == user.Name);
				if (user.OnVictoryTeam) {
					player.Victories++;
					player.MeasuredVictories++;
					player.MetalEarned += Settings.Default.MetalForVictory;
					if (victoriousFaction == null) {
						victoriousFaction = Galaxy.Factions.Single(f => f.Name == player.FactionName);
					}
				} else {
					player.Defeats++;
					player.MeasuredDefeats++;
					player.MetalEarned += Settings.Default.MetalForDefeat;
				}
			}

			if (victoriousFaction == null) {
				ret.MessageToDisplay += "No winners";
				return ret;
			}
			var planet = Galaxy.Planets.Single(p => p.MapName == mapName);
			var isConquest = planet.FactionName != victoriousFaction.Name;
			planet.FactionName = victoriousFaction.Name;

			if (!isWithoutUpgrades) {
				KillFleetsAndUpgrades(victoriousFaction, planet);
			}

			LastChanged = DateTime.Now;

			var sb = new StringBuilder();
			var encircledPlanetIDs = new List<int>();
			if (isConquest) {
				var encircledPlanets =
					Galaxy.Planets.Where(p => p.FactionName != victoriousFaction.Name && Galaxy.IsPlanetEncircled(p));
				foreach (var p in encircledPlanets) {
					p.FactionName = victoriousFaction.Name;
					sb.AppendFormat(",{0} ", p.Name);
					encircledPlanetIDs.Add(p.ID);
				}
				if (sb.Length != 0) {
					sb.Append("have fallen due to lack of supply.");
				}
			}

			if (!SpringieStates.ContainsKey(springieLogin.Login)) {
				throw new Exception("No springie state found!");
			}
			var attackingFaction = SpringieStates[springieLogin.Login].GameStartedStatus.OffensiveFactionName;
			var defendingFaction = Galaxy.Factions.Single(f => f.Name != attackingFaction).Name;
			Galaxy.Turn++;
			Galaxy.OffensiveFactionInternal = victoriousFaction.Name;

			List<PlayerRankChangedEvent> changedRanks;
			Galaxy.CalculatePlayerRanks(out changedRanks);
			Galaxy.SwapUnusedPlanets();
			CalculateEndTurnMetal();

			foreach (var e in changedRanks) {
				if (e.NewRank > e.OldRank) {
					new RankNotification(e.PlayerName, string.Format("Congratulations, you have been promoted to {0}!", e.NewRank));
				} else {
					new RankNotification(e.PlayerName, string.Format("You have just lost a rank! Conquer planets to regain it!"));
				}
			}

			var spaceFleetOwners = from f in Galaxy.Fleets
			                       where f.IsAtDestination(Galaxy.Turn)
			                       select f.OwnerName;

			var battleEvent = new BattleEvent(
				DateTime.Now,
				usersInGame.Where(u => !u.Spectator).ToList(),
				mapName,
				attackingFaction,
				defendingFaction,
				victoriousFaction.Name,
				disabledUpgradesHosts.Contains(springieLogin.Login),
				State.Galaxy,
				encircledPlanetIDs,
				spaceFleetOwners);
			State.Galaxy.Events.Add(battleEvent);
			Galaxy.CalculateBattleElo(battleEvent);

			var enemyPlanetCount = Galaxy.Planets.Count(p => p.FactionName != victoriousFaction.Name);
			if (enemyPlanetCount == 0) {
				State.Galaxy = Galaxy.AdvanceRound(Settings.Default.GalaxyTemplatePath);
				ForceSaveState();
				ret.MessageToDisplay += string.Format(
					"{0} HAS FINALLY CONQUERED THE GALAXY! (New round is starting)", victoriousFaction.Name);
				return ret;
			}

			ForceSaveState();
			ret.MessageToDisplay += string.Format(
				"Congratulations, planet {0} {2} by {1} {3}",
				planet.Name,
				victoriousFaction.Name,
				isConquest ? "conquered" : "held",
				sb);
			return ret;
		}

		public void UnitDeployed(AuthInfo springieLogin,
		                         string mapName,
		                         string playerName,
		                         string unit,
		                         int x,
		                         int z,
		                         string rotation)
		{
			var planetID = GetIngamePlanet(springieLogin);
			List<UpgradeDef> upgrades;
			if (!UpgradeData.TryGetValue(playerName, out upgrades)) {
				throw new Exception("Player has no upgrades.");
			}

			var upgrade = upgrades.Where(p => p.UnitChoice == unit).SingleOrDefault();
			if (upgrade == null) {
				throw new Exception(string.Format("User {0} does not have upgrade for {1}", playerName, unit));
			}

			upgrade.AddStructure(x, z, planetID, rotation);
			SaveState();
		}

		public void AddAward(AuthInfo springieLogin, string name, string type, string text, string mapName)
		{
			if (!ValidateSpringieLogin(springieLogin)) {
				return; // silently ignore other than valid springie
			}

			var p = Galaxy.GetPlayer(name);
			var l = Galaxy.Planets.Single(x => x.MapName == mapName);
			p.Awards.Add(new Award(type, text, l.ID, Galaxy.Turn, Galaxy.Round));
			// awards are always sent after battle = turn - 1
			SaveState();
		}

		public void SendChatLine(AuthInfo springieLogin, string channel, string playerName, string text)
		{
			if (!ValidateSpringieLogin(springieLogin)) {
				return;
			}
			var faction = Galaxy.Factions.Single(x => x.Name.ToLower() == channel.ToLower());

			if (faction.ChatEvents != null && faction.ChatEvents.Count > 0) {
				foreach (var ch in faction.ChatEvents.Reverse<Faction.ChatEvent>().Take(2)) {
					if (ch.Text == text && ch.Name == playerName && Math.Abs(ch.Time.Subtract(DateTime.Now).TotalSeconds) < 5) {
						return;
					}
				}
			}
			faction.ChatEvents.Add(new Faction.ChatEvent(playerName, text, DateTime.Now));
			SaveState();
		}

		public string ResetPassword(AuthInfo springieLogin, string loginName)
		{
			if (!ValidateSpringieLogin(springieLogin)) {
				return null;
			}
			State.Accounts[loginName] = Hash.HashString(loginName);
			return
				"Your password has been reset to your login name. You should change it as soon as possible. You can do so on the website";
		}

		public ICollection<string> GetFactionChannelAllowedExceptions()
		{
			var ret = new List<string>();
			ret.Add("ChanServ");
			foreach (var login in springieLogins) {
				ret.Add(login.Key);
			}
			return ret;
		}

		public void UnitDied(AuthInfo springieLogin, string playerName, string unitName, int x, int z)
		{
			if (!ValidateSpringieLogin(springieLogin)) {
				return;
			}

			List<UpgradeDef> upgrades;
			if (UpgradeData.TryGetValue(playerName, out upgrades)) {
				var upgrade = upgrades.SingleOrDefault(u => u.UnitChoice == unitName);
				if (upgrade != null) {
					upgrade.Died++;
					if (upgrade.IsBuilding) {
						upgrade.KillStructure(x, z, GetIngamePlanet(springieLogin));
					} else {
						upgrade.QuantityMobiles--;
						if (upgrade.QuantityMobiles < 0) {
							upgrade.QuantityMobiles = 0;
							throw new Exception(string.Format("More units died than expected: {0}, {1}", playerName, unitName));
						}
					}
				}
			}
			SaveState();
		}

		public void UnitPurchased(AuthInfo springieLogin, string playerName, string unitName, double cost, int x, int z)
		{
			if (!ValidateSpringieLogin(springieLogin)) {
				return;
			}
			var player = Galaxy.GetPlayer(playerName);
			player.MetalSpent += cost;
			List<UpgradeDef> upgrades;
			if (UpgradeData.TryGetValue(playerName, out upgrades)) {
				var upgrade = upgrades.SingleOrDefault(u => u.UnitChoice == unitName);
				if (upgrade != null) {
					if (upgrade.IsBuilding) {
						upgrade.AddStructure(x, z, GetIngamePlanet(springieLogin), "S"); // todo improve this
					} else {
						upgrade.QuantityMobiles++;
					}
					upgrade.Purchased++;
				}
			}
			SaveState();
		}

		#endregion

		void KillFleetsAndUpgrades(Faction victoriousFaction, Planet planet)
		{
			// kill orbiting spacefleets
			var fleetsToKill = new List<SpaceFleet>();
			foreach (var fleet in Galaxy.Fleets.Where(x => x.TargetPlanetID == planet.ID)) {
				PointF loc;
				if (fleet.GetCurrentPosition(out loc, Galaxy.Turn)) {
					if (Galaxy.GetPlayer(fleet.OwnerName).FactionName != victoriousFaction.Name) {
						fleetsToKill.Add(fleet);
					}
				}
			}
			if (fleetsToKill.Count > 0) {
				var f = fleetsToKill.TakeRandom(); // pick random one to kill
				Galaxy.Fleets.Remove(f);
				List<UpgradeDef> upgrades;
				if (UpgradeData.TryGetValue(f.OwnerName, out upgrades)) {
					var upgrade = upgrades.SingleOrDefault(u => u.UnitChoice == "fleet_blockade");
					if (upgrade != null) {
						upgrades.Remove(upgrade);
					}
				}
			}

			// occupied planet - delete deployed structures
			if (planet.OwnerName != null && Galaxy.GetPlayer(planet.OwnerName).FactionName != planet.FactionName) {
				RemoveAllUpgradesFromPlanet(planet.ID);
			}
		}

		void CalculateEndTurnMetal()
		{
			foreach (var planet in Galaxy.Planets) {
				if (planet.OwnerName != null) {
					var player = Galaxy.GetPlayer(planet.OwnerName);
					if (player.FactionName == planet.FactionName) {
						player.MetalEarned += Settings.Default.MetalForPlanetOwnedFree;
					} else {
						player.MetalEarned += Settings.Default.MetalForPlanetOwnedOccupied;
					}
					player.MetalEarned += player.Clout*Settings.Default.MetalForPlanetOwnedCloutBonus;
				}
			}

			/* // metal for 
            var factionPlanets = new Dictionary<string, int>();
            float galCount = 0;

            foreach (var g in Galaxy.Planets.Where(f=>f.FactionName != null).GroupBy(f => f.FactionName)) {
                factionPlanets[g.Key] = g.Count();
                galCount += g.Count();
            }

            
             foreach (var player in Galaxy.Players) { 
                int facCount;
                factionPlanets.TryGetValue(player.FactionName, out facCount);
                player.MetalEarned += Settings.Default.TotalMetalForFactionPlanets * (facCount / galCount);
                player.MetalEarned = Math.Round(player.MetalEarned);
            }*/
		}

		void RemoveAllUpgradesFromPlanet(int planetID)
		{
			UpgradeData.Values.SelectMany(x => x).ForEach(x => x.DeleteAllFromPlanet(planetID));
		}

		bool ValidateSpringieLogin(AuthInfo springieLogin)
		{
			string password;
			var result = springieLogins.TryGetValue(springieLogin.Login, out password) && springieLogin.Password == password;
			Console.WriteLine("Access {0} to {1}", result ? "granted" : "denied", springieLogin.Login);
			return result;
		}

		int GetIngamePlanet(AuthInfo springieLogin)
		{
			return SpringieStates[springieLogin.Login].GameStartedStatus.PlanetID;
		}
	}
}
