using System;
using System.ServiceModel;

namespace ZkData
{
	[ServiceContract]
	public interface IAuthService
	{
	  [OperationContract]
	  void SendLobbyMessage(Account account, string text);


		[OperationContract]
		Account VerifyAccount(string login, string hashedPassword);

	  [OperationContract]
	  CurrentLobbyStats GetCurrentStats();

	}

  public class CurrentLobbyStats
  {
    public int UsersIdle;
    public int BattlesRunning;
    public int UsersFighting;
    public int BattlesWaiting;
    public int UsersWaiting;
  }
}
