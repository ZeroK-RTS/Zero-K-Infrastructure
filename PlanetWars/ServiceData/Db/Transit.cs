using System;
using System.Linq;

namespace ServiceData
{
	partial class Transit
	{
		public string GetName()
		{
			if (Fleets != null)
			{
				if (!string.IsNullOrEmpty(Fleets.CustomName)) return Fleets.CustomName;
				else return "fleet " + Fleets.FleetID;
			}
			else if (PopulationTransports != null) return string.Format("transport {0} ({1}k people)", PopulationTransports.PopulationTransportID, PopulationTransports.Count);
			else return Players.First().MothershipName;
		}

		public string GetNameWithOwner()
		{
			return string.Format("{0}'s {1}", GetOwner().Name, GetName());
		}

		public string GetEtaString(int secondsPerTurn)
		{
			return Utils.SecondsToTimeString((EndBattleTurn - StartBattleTurn)*secondsPerTurn);
		}

		public Player GetOwner()
		{
			if (Fleets != null) return Fleets.Player;
			else if (PopulationTransports != null) return PopulationTransports.Player;
			else return Players.First();
		}

		public Position GetPosition(int gameTurn, int secondsPerTurn)
		{
			if (OrbitsObjectID != null) return CelestialObject.GetPosition(gameTurn, secondsPerTurn);
			else
			{
				if (gameTurn >= EndBattleTurn) return new Position(ToX, ToY);
				if (gameTurn <= StartBattleTurn) return new Position(FromX, FromY);
				if (EndBattleTurn - StartBattleTurn <= 0) throw new ApplicationException("Incorrect transit data");

				var part = (double)(gameTurn - StartBattleTurn)/(EndBattleTurn - StartBattleTurn);
				return new Position((ToX - FromX)*part + FromX, (ToY - FromY)*part + FromY);
			}
		}


		public void SetTransit(CelestialObject targetBody, int warpSpeed, int startTurn, Config conf)
		{
			var start = GetPosition(startTurn, conf.SecondsPerTurn); // init from locations and time
			if (OrbitsObjectID != null) FromObjectID = OrbitsObjectID;
			else FromObjectID = null;
			StartBattleTurn = startTurn;
			CelestialObjectByToObjectID = targetBody;
			FromX = (float)start.X;
			FromY = (float)start.Y;
			Warp = warpSpeed;

			if (FromObjectID == ToObjectID) throw new ApplicationException("Invalid order - from == to");

			var turn = startTurn + 1;

			var failSafe = 1000;
			Position dest;
			do
			{
				dest = targetBody.GetPosition(turn*conf.SecondsPerTurn, conf.SecondsPerTurn);
				var vector = dest - start;
				var curPos = vector.Normalized()*(conf.WarpDistance*warpSpeed*(turn - startTurn));
				var targetToCur = curPos - dest;
				if (targetToCur.X/vector.X >= 0 && targetToCur.Y/vector.Y >= 0) break; // we are "past" target
				turn++;
			} while (failSafe-- > 0);

			if (failSafe == 0) throw new ApplicationException("Erro in fleet settransit logic");

			EndBattleTurn = turn;
			ToX = (float)dest.X;
			ToY = (float)dest.Y;

		}
	}
}