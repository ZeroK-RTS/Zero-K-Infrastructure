using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Transactions;

namespace ServiceData
{
	public static class FleetLogic
	{
		public const int FleetBaseWarp = 2;

		public static Fleet CreateFleet(string playerName, string password, int bodyID, IEnumerable<ShipTypeCount> fleetShips)
		{
			using (var t = new TransactionScope())
			{
				var db = new DbDataContext();
				var player = db.GetPlayer(playerName, password);

				var body = player.CelestialObjects.Single(x => x.CelestialObjectID == bodyID);

				var fleet = new Fleet();
				fleet.Transit = new Transit();
				fleet.Transit.OrbitsObjectID = bodyID;

				foreach (var order in fleetShips)
				{
					var garrisonCount = body.GetShipCount(order.ShipTypeID);
					if (order.Count > garrisonCount) throw new ApplicationException(string.Format("Not enough ships of type {0}", order.ShipTypeID));

					body.SetShipCount(order.ShipTypeID, garrisonCount - order.Count);
					fleet.SetShipCount(order.ShipTypeID, order.Count);
				}
				player.Fleets.Add(fleet);

				db.MarkDirty();
				db.SubmitChanges();
				fleet.FleetShips.Load();
				t.Complete();
				return fleet;
			}
		}

		public static IEnumerable<Transit> GetTransits(string playerName, string password)
		{
			var db = new DbDataContext();
			var dl = new DataLoadOptions();
			dl.LoadWith<Transit>(x => x.Fleets);
			dl.LoadWith<Fleet>(x => x.FleetShips);
			dl.LoadWith<Transit>(x => x.PopulationTransports);
			dl.LoadWith<Transit>(x => x.Players);
			db.LoadOptions = dl;
			return db.Transits.ToList();
		}

		public static Fleet ModifyFleet(string playerName, string password, int fleetID, IEnumerable<ShipTypeCount> fleetShips)
		{
			using (var t = new TransactionScope())
			{
				var db = new DbDataContext();
				var player = db.GetPlayer(playerName, password);

				var fleet = player.Fleets.SingleOrDefault(x => x.FleetID == fleetID);
				if (fleet.Transit.OrbitsObjectID == null) throw new ApplicationException("Fleet not in orbit");
				var body = fleet.Transit.CelestialObject;
				if (body.OwnerID != player.PlayerID) throw new ApplicationException("Not my planet");

				foreach (var order in fleetShips)
				{
					if (order.Count > 0)
					{
						var garrisonCount = body.GetShipCount(order.ShipTypeID);
						if (order.Count > garrisonCount) throw new ApplicationException(string.Format("Not enough ships of type {0}", order.ShipTypeID));

						body.SetShipCount(order.ShipTypeID, garrisonCount - order.Count);
						fleet.SetShipCount(order.ShipTypeID, order.Count);
					}
					else
					{
						var change = order.Count;
						var fleetCount = fleet.GetShipCount(order.ShipTypeID);
						if (change > fleetCount) throw new ApplicationException(string.Format("Not enough ships of type {0} in fleet", order.ShipTypeID));

						fleet.SetShipCount(order.ShipTypeID, fleetCount - change);
						body.SetShipCount(order.ShipTypeID, change);
					}
				}
				if (!fleet.FleetShips.Any())
				{
					db.Fleets.DeleteOnSubmit(fleet);
					fleet = null;
				}

				db.MarkDirty();
				db.SubmitChanges();
				if (fleet != null) fleet.FleetShips.Load();
				t.Complete();
				return fleet;
			}
		}

		public static Fleet OrderFleet(string playerName, string password, int fleetID, int toBodyID, int futureOffset)
		{
			if (futureOffset < 0) throw new ApplicationException("Future offset cannot be smaller than 0");
			using (var t = new TransactionScope())
			{
				var db = new DbDataContext();
				var player = db.GetPlayer(playerName, password);
				var fleet = player.Fleets.Single(x => x.FleetID == fleetID);
				var target = db.CelestialObjects.Single(x => x.CelestialObjectID == toBodyID);

				var oldOrbit = fleet.Transit.OrbitsObjectID;
				var oldTarget = fleet.Transit.CelestialObjectByToObjectID;

				fleet.Transit.SetTransit(target, FleetBaseWarp, db.GetConfig().CombatTurn + 1 + futureOffset, db.GetConfig()); // will start next turn

				if (oldOrbit == null && oldTarget != null) // was heading to another planet
				{
					var transit = fleet.Transit;
					var ev = db.AddEvent(EventType.Fleet, "{0} changed destination from {1} to {2}, ETA {3}", transit.GetNameWithOwner(), oldTarget.GetNameWithOwner(), transit.CelestialObjectByToObjectID.GetNameWithOwner(), transit.GetEtaString(db.GetConfig().SecondsPerTurn));
					ev.Connect(transit.Fleets);
					ev.Connect(transit.GetOwner());
					ev.Connect(transit.CelestialObjectByToObjectID, oldTarget);
					ev.Connect(transit.CelestialObjectByToObjectID.Player, oldTarget.Player);
				}


				db.MarkDirty();
				db.SubmitChanges();
				fleet.FleetShips.Load();
				t.Complete();
				return fleet;
			}
		}

		public static Tuple<Player, CelestialObject> OrderMothership(string playerName, string password, int toBodyID)
		{
			using (var t = new TransactionScope())
			{
				var db = new DbDataContext();
				var player = db.GetPlayer(playerName, password);
				if (player.Transit == null) player.Transit = new Transit()
				                                             	{
				                                             		CelestialObject = player.CelestialObject
				                                             	};
				var originalBody = player.CelestialObject;
				var target = db.CelestialObjects.Single(x => x.CelestialObjectID == toBodyID);
				var transit = player.Transit;

				var oldOrbit = transit.OrbitsObjectID;
				var oldTarget = transit.CelestialObjectByToObjectID;

				transit.SetTransit(target, FleetBaseWarp, db.GetConfig().CombatTurn + 1, db.GetConfig()); // will start next turn

				if (oldOrbit == null && oldTarget != null) // was heading to another planet
				{
					var ev = db.AddEvent(EventType.Fleet, "{0} changed destination from {1} to {2}, ETA {3}", transit.GetNameWithOwner(), oldTarget.GetNameWithOwner(), transit.CelestialObjectByToObjectID.GetNameWithOwner(), transit.GetEtaString(db.GetConfig().SecondsPerTurn));
					ev.Connect(transit.Fleets);
					ev.Connect(transit.GetOwner());
					ev.Connect(transit.CelestialObjectByToObjectID, oldTarget);
					ev.Connect(transit.CelestialObjectByToObjectID.Player, oldTarget.Player);
				}

				if (originalBody != null) originalBody.UpdateIncomeInfo();
				db.MarkDirty();
				db.SubmitChanges();
				t.Complete();
				return new Tuple<Player, CelestialObject>(player, originalBody);
			}
		}


		public static void ProcessTransitTurn(DbDataContext db)
		{
			var config = db.GetConfig();
			foreach (var transit in db.Transits)
			{
				if (transit.StartBattleTurn >= config.CombatTurn && transit.EndBattleTurn >= config.CombatTurn && transit.OrbitsObjectID != null)
				{
					// log set sail
					var owner = transit.GetOwner();
					var ev = db.AddEvent(EventType.Fleet,
					                     "{0} set sail from {1} to {2}. It will need {3} to get there",
					                     transit.GetNameWithOwner(),
					                     transit.CelestialObjectByFromObjectID.GetNameWithOwner(),
					                     transit.CelestialObjectByToObjectID.GetNameWithOwner(),
					                     transit.GetEtaString(config.SecondsPerTurn));
					ev.Connect(transit.Fleets);
					ev.Connect(owner);
					if (transit.FromObjectID != null)
					{
						ev.Connect(transit.CelestialObjectByFromObjectID);
						ev.Connect(transit.CelestialObjectByFromObjectID.Player);
					}
					ev.Connect(transit.CelestialObjectByToObjectID);
					ev.Connect(transit.CelestialObjectByToObjectID.Player);
          
					// process set sail
					transit.OrbitsObjectID = null;
					if (transit.Players.Any()) // its mothership and it just left homeworld
					{
						var p = transit.Players.First();
						if (p.HomeworldID != null)
						{
							var eve = db.AddEvent(EventType.Player, "{0} has lost homeworld {1}", p.Name, p.CelestialObject.GetName());
							eve.Connect(p);
							eve.Connect(p.CelestialObject);
						}
						p.HomeworldID = null;
					}
				}
				else if (transit.EndBattleTurn <= config.CombatTurn && transit.OrbitsObjectID == null)
				{
					// log arrival
					var owner = transit.GetOwner();
					var ev = db.AddEvent(EventType.Fleet, "{0} arrived to {1}", transit.GetNameWithOwner(), transit.CelestialObjectByToObjectID.GetNameWithOwner());
					ev.Connect(transit.Fleets);
					ev.Connect(owner);
					ev.Connect(transit.CelestialObjectByToObjectID);
					ev.Connect(transit.CelestialObjectByToObjectID.Player);

					// process arrival
					transit.CelestialObject = transit.CelestialObjectByToObjectID;
					transit.FromObjectID = null;
					transit.ToObjectID = null;
					transit.FromX = 0;
					transit.ToX = 0;
					transit.FromY = 0;
					transit.ToY = 0;

					PopulationTransport transport = transit.PopulationTransports;
					if (transport != null)
					{
						if (transit.CelestialObjectByToObjectID.OwnerID == transport.OwnerID) // todo handle alliances!!
						{ 
							transit.CelestialObjectByToObjectID.Population += transport.Count;
							if (transit.CelestialObjectByToObjectID.Population > transit.CelestialObjectByToObjectID.MaxPopulation) transit.CelestialObjectByToObjectID.Population = transit.CelestialObjectByToObjectID.MaxPopulation;
							transit.PopulationTransports = null;
							db.Transits.DeleteOnSubmit(transit);
							db.PopulationTransports.DeleteOnSubmit(transport);
						}
					}

					if (transit.Players.Any())
					{
						var p = transit.Players.First();
						p.CelestialObject = transit.CelestialObjectByToObjectID; // set homeworld to object where we arrived
					
						var eve = db.AddEvent(EventType.Player, "{0} has new homeworld {1}", p.Name, p.CelestialObject.GetName());
						eve.Connect(p);
						eve.Connect(p.CelestialObject);
	
					}
				}
			}
		}

		public static Tuple<PopulationTransport, CelestialObject> CreatePopulationTransport(string playerName, string password, int fromBodyID, int toBodyID, int count)
		{
			using (var t = new TransactionScope())
			{
				var db = new DbDataContext();
				var player = db.GetPlayer(playerName, password);
				var from = player.CelestialObjects.Single(x => x.CelestialObjectID == fromBodyID);
				var target = db.CelestialObjects.Single(x => x.CelestialObjectID == toBodyID);
				
				if (from.Population < count) throw new ApplicationException("There are not enough people on this planet");
				from.Population -= count;
				from.UpdateIncomeInfo();
				var transport = new PopulationTransport() { Count = count, Player = player, Transit = new Transit() };
				player.PopulationTransports.Add(transport);

				var transit = transport.Transit;

				transit.SetTransit(target, FleetBaseWarp, db.GetConfig().CombatTurn + 1 , db.GetConfig()); // will start next turn
        

				db.MarkDirty();
				db.SubmitChanges();
				t.Complete();
				return new Tuple<PopulationTransport, CelestialObject>(transport, from);
			}

		}
	}
}