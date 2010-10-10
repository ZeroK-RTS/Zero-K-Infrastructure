using System.Collections.Generic;
using System.Data.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace ServiceData
{
	[ServiceContract]
	public interface IPlanetWarsService
	{
		//[OperationContract]
		//void CancelTradeOrder(string playerName, string password, int orderId);

		[OperationContract]
		BodyResponse BuildMothershipModule(string playerName, string password, int structureType);

		[OperationContract]
		BodyResponse BuildShip(string playerName, string password, int celestialObjectID, int shipType, int count);

		[OperationContract]
		BodyResponse BuildStructure(string playerName, string password, int celestialObjectID, int structureTypeID);

		[OperationContract]
		Fleet CreateFleet(string playerName, string password, int bodyID, IEnumerable<ShipTypeCount> fleetShips);

		[OperationContract]
		Tuple<PopulationTransport, CelestialObject> CreatePopulationTransport(string playerName, string password, int fromBodyID, int toBodyID, int count);


		[OperationContract]
		void FakeTurn(int count);

		[OperationContract]
		IEnumerable<Event1> GetBattleEvents(int battleID);

		[OperationContract]
		IEnumerable<Event1> GetBodyEvents(int bodyID);

		[OperationContract]
		BodyResponse GetBodyOptions(string playerName, string password, int bodyID);

		[OperationContract]
		string GetLoginHint();

		[OperationContract]
		StarMap GetMapData(string playerName, string password);

		[OperationContract]
		IEnumerable<StructureOption> GetMotherhipModuleBuildOptions(string playerName, string password);

		[OperationContract]
		Invariants GetInvariants();

		[OperationContract]
		Player GetPlayerData(string playerName);

		[OperationContract]
		IEnumerable<Event1> GetPlayerEvents(int playerID);

		[OperationContract]
		IEnumerable<Player> GetPlayerList();

		[OperationContract]
		IEnumerable<Event1> GetStarSystemEvents(int starSystemID);

		/// <summary>
		/// Gets full list of current objects in space and their positions/destinations
		/// </summary>
		[OperationContract]
		IEnumerable<Transit> GetTransits(string playerName, string password);

		[OperationContract]
		LoginResponse Login(string login, string password);

		//[OperationContract]
		//void PlaceTradeOrder(string playerName, string password, string itemName, int price, OrderType orderType, OrderScope scope, string otherParty);

		[OperationContract]
		Fleet ModifyFleet(string playerName, string password, int fleetID, IEnumerable<ShipTypeCount> fleetShips);

		/// <summary>
		/// Sends fleet to new body
		/// </summary>
		/// <param name="playerName"></param>
		/// <param name="password"></param>
		/// <param name="fleetID"></param>
		/// <param name="toBodyID"></param>
		/// <param name="futureOffset">fleet will embark later in the future, futureOffset delayed</param>
		/// <returns></returns>
		[OperationContract]
		Fleet OrderFleet(string playerName, string password, int fleetID, int toBodyID, int futureOffset);

		/// <summary>
		/// Sends mothership to new planet, returns modified player with transit and modified old homeworld
		/// </summary>
		[OperationContract]
		Tuple<Player, CelestialObject> OrderMothership(string playerName, string password, int toBodyID);

		[OperationContract]
		RegisterResponse Register(string login, string password);

		[OperationContract]
		BodyResponse SellMotherhipModule(string playerName, string password, int structureTypeID);

		[OperationContract]
		BodyResponse SellStructure(string playerName, string password, int celestialObjectID, int structureTypeID);

		/// <summary>
		/// Returns second when state was last modified
		/// </summary>
		[OperationContract]
		int GetDirtyGameSecond();
	}

	[DataContract]
	public class Tuple<T>
	{
		public Tuple(T first)
		{
			First = first;
		}

		[DataMember]
		public T First { get; set; }
	}

	[DataContract()]
	public class Tuple<T, T2> : Tuple<T>
	{
		public Tuple(T first, T2 second)
			: base(first)
		{
			Second = second;
		}
		[DataMember]
		public T2 Second { get; set; }
	}

	

	[DataContract()]
	public class StarMap
	{
		[DataMember]
		public IEnumerable<Transit> Transits { get; set; }

		[DataMember]
		public IEnumerable<Player> Players;

		[DataMember]
		public IEnumerable<StarSystem> StarSystems;

		[DataMember]
		public IEnumerable<CelestialObject> CelestialObjects;

		[DataMember]
		public IEnumerable<CelestialObjectLink> ObjectLinks;

		[DataMember]
		public Config Config;
	}

	[DataContract]
	public class Invariants
	{
		[DataMember]
		public IEnumerable<StructureType> StructureTypes { get; set; }

		[DataMember]
		public IEnumerable<ShipType> ShipTypes { get; set; }

		[DataMember]
		public IEnumerable<Tech> Technologies { get; set; }
	}


	[DataContract]
	public class ShipTypeCount
	{
		[DataMember]
		public int Count { get; set; }
		[DataMember]
		public int ShipTypeID { get; set; }
	}

	[DataContract]
	public class OrderMothershipResponse
	{
		[DataMember]
		public CelestialObject Body { get; set; }
		[DataMember]
		public Player Player { get; set; }
	}

	[DataContract]
	public class BodyResponse
	{
		[DataMember]
		public CelestialObject Body { get; set; }

		[DataMember]
		public IEnumerable<ShipOption> NewShipOptions { get; set; }

		[DataMember]
		public IEnumerable<StructureOption> NewStructureOptions { get; set; }
		[DataMember]
		public Player Player { get; set; }

		[DataMember]
		public BuildResponse Response { get; set; }
	}

	[DataContract]
	public class StructureOption
	{
		[DataMember]
		public BuildResponse CanBuild { get; set; }
		[DataMember]
		public StructureType StructureType { get; set; }
	}


	[DataContract]
	public class ShipOption
	{
		[DataMember]
		public BuildResponse CanBuild { get; set; }

		[DataMember]
		public ShipType ShipType { get; set; }
	}

	public enum BuildResponse
	{
		[DataMember]
		Ok = 0,
		[DataMember]
		NotEnoughRoomOrBuildpower = 1,
		[DataMember]
		NotEnoughResources = 2,
		[DataMember]
		DoesNotExist = 3
	}

	public enum LoginResponse
	{
		[DataMember]
		Ok = 0,
		[DataMember]
		InvalidPassword = 1,
		[DataMember]
		InvalidLogin = 2,
		[DataMember]
		Unregistered = 3
	}

	public enum RegisterResponse
	{
		[DataMember]
		Ok = 0,

		[DataMember]
		AlreadyRegistered = 1,

		[DataMember]
		IsSmurf = 2,

		[DataMember]
		NotValidSpringLogin = 3,

		[DataMember]
		NotValidSpringPassword = 4
	}
}