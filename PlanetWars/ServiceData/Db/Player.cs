using System;
using System.Linq;
using System.Runtime.Serialization;

namespace ServiceData
{
	partial class Player
	{
		[DataMember]
		public string Name
		{
			get { return SpringAccount.Name; }
			set { throw new NotSupportedException(); }
		}

		public int GetStructureCount(int structureTypeID)
		{
			var entry = MothershipStructures.SingleOrDefault(x => x.StructureTypeID == structureTypeID);
			if (entry != null) return entry.Count;
			else return 0;
		}

		public void SetStructureCount(int shipTypeID, int count)
		{
			var entry = MothershipStructures.SingleOrDefault(x => x.StructureTypeID == shipTypeID);
			if (entry == null)
			{
				entry = new MothershipStructure();
				entry.StructureTypeID = shipTypeID;
				MothershipStructures.Add(entry);
			}
			entry.Count = count;
			if (count == 0) MothershipStructures.Remove(entry);
		}


		[DataMember]
		public int NextLevelXP
		{
			get 
			{
				return Level*Level*100;
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		[DataMember]
		public int ThisLevelXP
		{
			get
			{
				return (Level-1) * (Level-1) * 100;
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public void ApplyResourceTurn(int turn)
		{
			MetalIncome = 0;
			FoodIncome = 0;
			QuantiumIncome = 0;
			DarkMatterIncome = 0;
			ResearchIncome = 0;
			PopulationCapacity = 0;
			

			var curPeople = 0;
			foreach (var o in CelestialObjects) {
				curPeople = o.Population;
			}

			foreach (var t in PopulationTransports) {
				curPeople += t.Count;
			}

			FoodIncome -= curPeople;

			foreach (var o in CelestialObjects)
			{
				MetalIncome += o.MetalIncome * o.Efficiency; 
				FoodIncome += o.FoodIncome * o.Efficiency;
				QuantiumIncome += o.QuantiumIncome * o.Efficiency;
				DarkMatterIncome += o.DarkMatterIncome * o.Efficiency;
				ResearchIncome += o.ResearchIncome * o.Efficiency;
				PopulationCapacity += o.MaxPopulation;
				o.BuildpowerUsed = 0;// reset bp to 0
			}

			Metal += MetalIncome;
			Food += FoodIncome;
			Quantium += QuantiumIncome;
			DarkMatter += DarkMatterIncome;
			ResearchPoints += ResearchIncome;

			if (Food < 0)
			{
				var toKill = 1 + (int)-Food;

				curPeople -= toKill;
				if (curPeople < 0) curPeople = 0;

				Food = 0;
				foreach (var o in CelestialObjects.OrderByDescending(x => x.Population))
				{
					if (o.Population > 0)
					{
						var killed = Math.Min(o.Population, toKill);
						o.Population -= killed;
						o.UpdateIncomeInfo();
						var ev = Event1.CreateEvent(EventType.Player, turn, "{0}m people have died on planet {1} from starvation", killed, o.Name);
						ev.Connect(this);
						ev.Connect(o);
						
						toKill -= killed;
					}
					if (toKill <= 0) break;
				}
			}

			Population = curPeople;
    }

		public void ApplyPopulationTick(int turn)
		{
			foreach (var body in CelestialObjects) {
				var growth = body.CelestialObjectStructures.Sum(x => (int?)x.StructureType.MakesPeople*x.Count);
				if ((growth??0) > 0) {
					if (body.Population + growth > body.MaxPopulation) {
						var ev = Event1.CreateEvent(EventType.Player, turn, "Population cannot grow anymore on {0}, transport it to other planet!", body.Name);
						ev.Connect(this);
						ev.Connect(body);
					}
					body.Population = Math.Min(body.Population + growth??0, body.MaxPopulation);
				}
			}			
		}
	}
}