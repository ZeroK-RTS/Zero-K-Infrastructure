using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using ServiceData;

namespace PlanetWars.Web
{
	[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
	public class PlanetWarsService: IPlanetWarsService
	{
		public string GetLoginHint()
		{
			var context = OperationContext.Current;

			var messageProperties = context.IncomingMessageProperties;

			var endpointProperty = messageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;

			var db = new DbDataContext();
			var acc = db.SpringAccounts.Where(x => x.LastIP == endpointProperty.Address).OrderByDescending(x => x.LastLogin).FirstOrDefault();
			if (acc != null) return acc.Name;
			return null;
		}


		public IEnumerable<StructureOption> GetMotherhipModuleBuildOptions(string playerName, string password)
		{
			return BuildingLogic.GetMotherhipModuleBuildOption(playerName, password);
		}

		public Invariants GetInvariants()
		{
			return GameLogic.GetInvariants();
		}


		public BodyResponse BuildStructure(string playerName, string password, int celestialObjectID, int structureTypeID)
		{
			return BuildingLogic.BuildStructure(playerName, password, celestialObjectID, structureTypeID);
		}

		public BodyResponse SellStructure(string playerName, string password, int celestialObjectID, int structureTypeID)
		{
			return BuildingLogic.SellStructure(playerName, password, celestialObjectID, structureTypeID);
		}


		public int GetDirtyGameSecond()
		{
			return new DbDataContext().GetConfig().DirtySecond;
		}

		public Fleet CreateFleet(string playerName, string password, int bodyID, IEnumerable<ShipTypeCount> fleetShips)
		{
			return FleetLogic.CreateFleet(playerName, password, bodyID, fleetShips);
		}

		public Fleet ModifyFleet(string playerName, string password, int fleetID, IEnumerable<ShipTypeCount> fleetShips)
		{
			return FleetLogic.ModifyFleet(playerName, password, fleetID, fleetShips);
		}


		public Fleet OrderFleet(string playerName, string password, int fleetID, int toBodyID, int futureOffset)
		{
			return FleetLogic.OrderFleet(playerName, password, fleetID, toBodyID, futureOffset);
		}


		public IEnumerable<Transit> GetTransits(string playerName, string password)
		{
			return FleetLogic.GetTransits(playerName, password);
		}


		public BodyResponse GetBodyOptions(string playerName, string password, int bodyID)
		{
			return BuildingLogic.GetBodyOptions(playerName, password, bodyID);
		}

		public BodyResponse SellMotherhipModule(string playerName, string password, int structureType)
		{
			return BuildingLogic.SellMothershipModule(playerName, password, structureType);
		}


		public IEnumerable<Event1> GetPlayerEvents(int playerID)
		{
			var db = new DbDataContext();
			return db.Players.Single(x => x.PlayerID == playerID).PlayerEvents.Select(x => x.Event1).OrderByDescending(x => x.Time).ToList();
		}

		public IEnumerable<Event1> GetStarSystemEvents(int starSystemID)
		{
			var db = new DbDataContext();
			return db.Events.Where(x => x.PlayerEvents.Any(y => y.Player.SystemID == starSystemID)).OrderByDescending(x => x.Time).ToList();
		}

		public IEnumerable<Event1> GetBattleEvents(int battleID)
		{
			var db = new DbDataContext();
			return db.Battles.Single(x => x.BattleID == battleID).BattleEvents.Select(x => x.Event1).OrderByDescending(x => x.Time).ToList();
		}

		public IEnumerable<Event1> GetBodyEvents(int bodyID)
		{
			var db = new DbDataContext();
			return
				db.CelestialObjects.Single(x => x.CelestialObjectID == bodyID).CelestialObjectEvents.Select(x => x.Event1).OrderByDescending(x => x.Time).ToList();
		}

		public IEnumerable<Player> GetPlayerList()
		{
			return new DbDataContext().Players.Where(x => x.IsActive).ToList();
		}

		public BodyResponse BuildMothershipModule(string playerName, string password, int structureType)
		{
			return BuildingLogic.BuildMothershipModule(playerName, password, structureType);
		}

		public BodyResponse BuildShip(string playerName, string password, int celestialObjectID, int shipType, int count)
		{
			return BuildingLogic.BuildShip(count, playerName, password, celestialObjectID, shipType);
		}


		public void FakeTurn(int count)
		{
			GameLogic.ProcessTurn(count);
		}

		public Player GetPlayerData(string playerName)
		{
			var db = new DbDataContext();
			var p = db.Players.SingleOrDefault(x => x.SpringAccount.Name == playerName);
			p.MothershipStructures.Load();
			return p;
		}


		public LoginResponse Login(string login, string password)
		{
			return GameLogic.Login(login, password);
		}

		public RegisterResponse Register(string login, string password)
		{
			return GameLogic.Register(login, password);
		}


		public StarMap GetMapData(string playerName, string password)
		{
			return GameLogic.GetMapData(playerName, password);
		}

		ServiceData.Tuple<PopulationTransport, CelestialObject> IPlanetWarsService.CreatePopulationTransport(string playerName, string password, int fromBodyID, int toBodyID, int count)
		{
			return FleetLogic.CreatePopulationTransport(playerName, password, fromBodyID, toBodyID, count);
		}

		ServiceData.Tuple<Player, CelestialObject> IPlanetWarsService.OrderMothership(string playerName, string password, int toBodyID)
		{
			return FleetLogic.OrderMothership(playerName, password, toBodyID);
		}
	}
}