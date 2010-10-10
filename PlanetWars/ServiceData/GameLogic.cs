using System;
using System.Data.Linq;
using System.Linq;
using System.Transactions;
using MapGenerator;

namespace ServiceData
{
	public static class GameLogic
	{
		public static StarMap GetMapData(string playerName, string password)
		{
			var db = new DbDataContext();

			var ret = new StarMap();

			var lo = new DataLoadOptions();
			lo.LoadWith<CelestialObject>(x => x.CelestialObjectShips); // todo stealth also in all fleet structures
			lo.LoadWith<CelestialObject>(x => x.CelestialObjectStructures);
			db.LoadOptions = lo;
			ret.CelestialObjects = db.CelestialObjects.ToList();

			ret.Players = db.Players.Where(x => x.IsActive).ToList();

			ret.StarSystems = db.StarSystems.ToList();

			ret.Transits = FleetLogic.GetTransits(playerName, password);

			ret.Config = db.GetConfig();

			ret.ObjectLinks = db.CelestialObjectLinks.ToList();

			return ret;
		}

		public static Invariants GetInvariants()
		{
			var db = new DbDataContext();
      var ret = new Invariants();
			ret.StructureTypes = db.StructureTypes.ToList();
      ret.ShipTypes = db.ShipTypes.ToList();
      ret.Technologies = db.Teches.ToList();
			return ret;
		}

		public static LoginResponse Login(string login, string password)
		{
			var db = new DbDataContext();
			var hp = SpringAccount.HashPassword(password);

			var user = db.SpringAccounts.SingleOrDefault(x => x.Name == login);
			if (user == null) return LoginResponse.InvalidLogin;

			if (user.Password != hp) return LoginResponse.InvalidPassword;

			if (user.Players == null) return LoginResponse.Unregistered;

			user.Players.IsActive = true;
			db.SubmitChanges();
			return LoginResponse.Ok;
		}


		public static void ProcessTurn(int count)
		{
			for (var i = 0; i < count; i++) {
				using (var t = new TransactionScope()) {
					var db = new DbDataContext();

					var config = db.GetConfig();
					config.CombatTurn++;

					FleetLogic.ProcessTransitTurn(db);

					foreach (var o in db.CelestialObjects) {
						o.UpdateIncomeInfo();
					}

					foreach (var player in db.Players.Where(x => x.IsActive)) {
						if (config.CombatTurn%config.PopulationTick == 0) player.ApplyPopulationTick(config.CombatTurn);
						player.ApplyResourceTurn(config.CombatTurn);
					}

					db.MarkDirty();

					config.TurnStarted = DateTime.UtcNow; // todo synchronize properly when not faking
					db.SubmitChanges();
					t.Complete();
				}
			}
		}



		public static RegisterResponse Register(string login, string password)
		{
			using (var t = new TransactionScope()) {
				var db = new DbDataContext();
				var user = db.SpringAccounts.SingleOrDefault(x => x.Name == login);
				if (user == null) return RegisterResponse.NotValidSpringLogin;

				if (user.Players != null) return RegisterResponse.AlreadyRegistered;

				if (db.SpringAccounts.Any(x => x.UserCookie == user.UserCookie && x.Players != null)) return RegisterResponse.IsSmurf;

				if (user.Password != SpringAccount.HashPassword(password)) return RegisterResponse.NotValidSpringPassword;

				// create player and assign to smallest alliance
				var player = new Player() { IsActive = true, StarSystem = db.StarSystems.OrderBy(x => x.Players.Where(y => y.IsActive).Count()).First() };
				user.Players = player;
				player.MothershipName = player.SpringAccount.Name + "'s mothership";

				// create his planet 
				var mg = new MapGen();
				var hisPlanet = mg.CreateHomeworld(player.StarSystem.CelestialObject);
				hisPlanet.Name = player.SpringAccount.Name + "'s home";
				hisPlanet.Player = player;
				player.StarSystem.CelestialObject.ChildCelestialObjects.Add(hisPlanet);

				hisPlanet.UpdateIncomeInfo();

				db.SubmitChanges();

				var e = db.AddEvent(EventType.Player, "{0} joined the {1}", player.Name, player.StarSystem.Name);
				e.Connect(player);
				e.Connect(hisPlanet);
				db.MarkDirty();
				db.SubmitChanges();
				t.Complete();
				return RegisterResponse.Ok;
			}
		}
	}
}