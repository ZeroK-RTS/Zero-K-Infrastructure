using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;

namespace ServiceData
{
	public static class BuildingLogic
	{
		public static IEnumerable<StructureOption> GetMotherhipModuleBuildOption(string playerName, string password)
		{
			var db = new DbDataContext();
			var player = db.GetPlayer(playerName, password);

			var structureOptions = new List<StructureOption>();

			var noRoom = player.Level <= player.MothershipStructures.Sum(x => x.Count);

			foreach (var u in db.StructureTypes.Where(x => x.NeedsTechID == null || x.Tech.StarSystemTeches.Any(y => y.StarSystemID == player.SystemID)))
			{
				var option = new StructureOption() { StructureType = u };

				if (noRoom) option.CanBuild = BuildResponse.NotEnoughRoomOrBuildpower;
				else if (u.CostMetal >= player.Metal) option.CanBuild = BuildResponse.NotEnoughResources;
				else option.CanBuild = BuildResponse.Ok;
				structureOptions.Add(option);
			}
			return structureOptions;
		}


		public static BodyResponse BuildStructure(string playerName, string password, int celestialObjectID, int structureTypeID)
		{
			using (var t = new TransactionScope()) {
				var db = new DbDataContext();
				var p = db.GetPlayer(playerName, password);

				var ret = new BodyResponse();

				var planet = db.CelestialObjects.Single(x => x.CelestialObjectID == celestialObjectID && x.OwnerID == p.PlayerID);

				if (planet.Size <= planet.CelestialObjectStructures.Sum(x => x.Count)) {
					ret.Response = BuildResponse.NotEnoughRoomOrBuildpower;
					return ret;
				}

				var structure =
					db.StructureTypes.Single(
						x => x.StructureTypeID == structureTypeID && (x.NeedsTechID == null || x.Tech.StarSystemTeches.Any(y => y.StarSystemID == p.SystemID)));

				if (structure.CostMetal >= p.Metal) {
					ret.Response = BuildResponse.NotEnoughResources;
					return ret;
				}

				planet.SetStructureCount(structureTypeID, planet.GetStructureCount(structureTypeID) + 1);
				p.Metal -= structure.CostMetal;

				db.MarkDirty();
				db.SubmitChanges();
				planet.UpdateIncomeInfo();
				db.SubmitChanges();
				t.Complete();
				return GetBodyOptions(playerName, password, celestialObjectID);
			}
		}


		public static BodyResponse SellStructure(string playerName, string password, int celestialObjectID, int structureTypeID)
		{
			using (var t = new TransactionScope()) {
				var db = new DbDataContext();
				var p = db.GetPlayer(playerName, password);

				var planet = db.CelestialObjects.Single(x => x.CelestialObjectID == celestialObjectID && x.OwnerID == p.PlayerID);

				var ret = new BodyResponse();

				var cnt = planet.GetStructureCount(structureTypeID);
				if (cnt <= 0) {
					ret.Response = BuildResponse.DoesNotExist;
					return ret;
				}
				else planet.SetStructureCount(structureTypeID, cnt - 1);

				p.Metal += db.StructureTypes.Single(x => x.StructureTypeID == structureTypeID).CostMetal/2.0;
				db.MarkDirty();
				db.SubmitChanges();
				planet.UpdateIncomeInfo();
				db.SubmitChanges();
				t.Complete();
				return GetBodyOptions(playerName, password, celestialObjectID);
			}
		}


		public static BodyResponse SellMothershipModule(string playerName, string password, int structureType)
		{
			using (var t = new TransactionScope()) {
				var db = new DbDataContext();
				var p = db.GetPlayer(playerName, password);

				var ret = new BodyResponse();


				var cnt = p.GetStructureCount(structureType);
				if (cnt <= 0) {
					ret.Response = BuildResponse.DoesNotExist;
					return ret;
				}
				p.SetStructureCount(structureType, cnt - 1);

				p.Metal += db.StructureTypes.Single(x => x.StructureTypeID == structureType).CostMetal/2.0;

				db.SubmitChanges();
				if (p.CelestialObject != null) p.CelestialObject.UpdateIncomeInfo();
				db.MarkDirty();
				db.SubmitChanges();
				p.MothershipStructures.Load();
				ret.Player = p;
				ret.NewStructureOptions = GetMotherhipModuleBuildOption(playerName, password);
				ret.Body = p.CelestialObject;
				ret.Response = BuildResponse.Ok;
				t.Complete();
				return ret;
			}
		}

		public static BodyResponse BuildMothershipModule(string playerName, string password, int structureType)
		{
			using (var t = new TransactionScope()) {
				var db = new DbDataContext();
				var p = db.GetPlayer(playerName, password);

				var ret = new BodyResponse();

				if (p.Level <= p.MothershipStructures.Sum(x => x.Count)) {
					ret.Response = BuildResponse.NotEnoughRoomOrBuildpower;
					return ret;
				}

				var structure =
					db.StructureTypes.Single(
						x => x.StructureTypeID == structureType && (x.NeedsTechID == null || x.Tech.StarSystemTeches.Any(y => y.StarSystemID == p.SystemID)));

				if (structure.CostMetal >= p.Metal) {
					ret.Response = BuildResponse.NotEnoughResources;
					return ret;
				}

				p.SetStructureCount(structureType, p.GetStructureCount(structureType) + 1);

				p.Metal -= structure.CostMetal;

				db.SubmitChanges();
				if (p.CelestialObject != null) p.CelestialObject.UpdateIncomeInfo();
				db.MarkDirty();
				db.SubmitChanges();
				p.MothershipStructures.Load();
				ret.Player = p;
				ret.NewStructureOptions = GetMotherhipModuleBuildOption(playerName, password);
				ret.Body = p.CelestialObject;
				ret.Response = BuildResponse.Ok;
				t.Complete();
				return ret;
			}
		}




		public static BodyResponse GetBodyOptions(string playerName, string password, int bodyID)
		{
			var db = new DbDataContext();
			var player = db.GetPlayer(playerName, password);
			if (player == null) throw new ApplicationException("Fail login");
			var body = player.CelestialObjects.Single(x => x.CelestialObjectID == bodyID);
			
			body.CelestialObjectStructures.Load();
			body.CelestialObjectShips.Load();
			
			var shipOptions = new List<ShipOption>();
			foreach (var opt in db.ShipTypes.Where(x => x.NeedsTechID == null || x.Tech.StarSystemTeches.Any(y => y.StarSystemID == player.SystemID))) {
				var so = new ShipOption();
				so.ShipType = opt;
				if (opt.MetalCost > player.Metal || opt.QuantiumCost > player.Quantium || opt.DarkMetalCost > player.DarkMatter) so.CanBuild = BuildResponse.NotEnoughResources;
				if (opt.MetalCost > body.Buildpower - body.BuildpowerUsed) so.CanBuild = BuildResponse.NotEnoughRoomOrBuildpower;
				shipOptions.Add(so);
			}

			var noRoom = body.Size <= body.CelestialObjectStructures.Sum(x => x.Count);

			var structureOptions = new List<StructureOption>();
			foreach (var u in db.StructureTypes.Where(x => x.NeedsTechID == null || x.Tech.StarSystemTeches.Any(y => y.StarSystemID == player.SystemID))) {
				var option = new StructureOption() { StructureType = u };

				if (noRoom) option.CanBuild = BuildResponse.NotEnoughRoomOrBuildpower;
				else if (u.CostMetal >= player.Metal) option.CanBuild = BuildResponse.NotEnoughResources;
				else option.CanBuild = BuildResponse.Ok;
				structureOptions.Add(option);
			}

			var ret = new BodyResponse()
			          	{ Body = body, NewShipOptions = shipOptions, Player = player, Response = BuildResponse.Ok, NewStructureOptions = structureOptions };

			return ret;
		}

		public static BodyResponse BuildShip(int count, string playerName, string password, int celestialObjectID, int shipType)
		{
			using (var t = new TransactionScope()) {
				if (count < 1) throw new ApplicationException("Invalid count");
				var db = new DbDataContext();
				var p = db.GetPlayer(playerName, password);

				var planet = p.CelestialObjects.Single(x => x.CelestialObjectID == celestialObjectID);

				var ship =
					db.ShipTypes.Single(x => x.ShipTypeID == shipType && (x.NeedsTechID == null || x.Tech.StarSystemTeches.Any(y => y.StarSystemID == p.SystemID)));

				var ret = new BodyResponse();

				if (planet.Buildpower - planet.BuildpowerUsed < ship.MetalCost*count) {
					ret.Response = BuildResponse.NotEnoughRoomOrBuildpower;
					return ret;
				}

				if (ship.MetalCost*count > p.Metal || ship.QuantiumCost*count > p.Quantium || ship.DarkMetalCost*count > p.DarkMatter) {
					ret.Response = BuildResponse.NotEnoughResources;
					return ret;
				}

				planet.SetShipCount(shipType, planet.GetShipCount(shipType) + count);

				planet.BuildpowerUsed += ship.MetalCost*count;
				p.Metal -= ship.MetalCost*count;
				p.Quantium -= ship.QuantiumCost*count;
				p.DarkMatter -= ship.DarkMetalCost*count;
				db.MarkDirty();
				db.SubmitChanges();
				t.Complete();
				return GetBodyOptions(playerName, password, celestialObjectID);
			}
		}

	}
}
