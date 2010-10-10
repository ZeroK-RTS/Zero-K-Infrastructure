using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceData
{
	partial class Fleet
	{
		public int GetShipCount(int shipTypeID)
		{
			
			var entry = FleetShips.SingleOrDefault(x => x.ShipTypeID == shipTypeID);
			if (entry != null) return entry.Count;
			else return 0;
		}

		public void SetShipCount(int shipTypeID, int count)
		{
			var entry = FleetShips.SingleOrDefault(x => x.ShipTypeID == shipTypeID);
			if (entry == null)
			{
				entry = new FleetShip();
				entry.ShipTypeID = shipTypeID;
				FleetShips.Add(entry);
			}
			entry.Count = count;
			if (count == 0) FleetShips.Remove(entry);
		}

	}
}
