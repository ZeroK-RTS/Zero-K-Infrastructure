using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using ZkData;

namespace ZeroKWeb.missions
{
	// NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IMissionService" in both code and config file together.
	[ServiceContract]
	public interface IMissionService
	{
		[OperationContract]
		void DeleteMission(int missionID, string author, string password);

		[OperationContract]
		Mission GetMission(string missionName);

		[OperationContract]
		Mission GetMissionByID(int missionID);

		[OperationContract]
		IEnumerable<Mission> ListMissionInfos();

		[OperationContract]
		void SendMission(Mission mission, string author, string password);


	}
}
