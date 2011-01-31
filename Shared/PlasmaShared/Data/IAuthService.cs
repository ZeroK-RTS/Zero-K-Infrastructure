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
	}
}
