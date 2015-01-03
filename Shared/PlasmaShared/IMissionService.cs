using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using ZkData.UnitSyncLib;
using ZkData;

namespace ZeroKWeb
{
	[ServiceContract]
	public interface IMissionService
	{
		[OperationContract]
		void DeleteMission(int missionID, string author, string password);

		[OperationContract]
		void UndeleteMission(int missionID, string author, string password);

		[OperationContract]
		Mission GetMission(string missionName);

		[OperationContract]
		Mission GetMissionByID(int missionID);

		[OperationContract]
		IEnumerable<Mission> ListMissionInfos();

		[OperationContract]
		void SendMission(Mission mission, List<MissionSlot> slots, string author, string password, Mod modInfo);


	}
}
