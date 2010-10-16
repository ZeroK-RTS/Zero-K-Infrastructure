using System.ServiceModel;

namespace ZkData
{
	[ServiceContract]
	public interface IAuthService
	{

		[OperationContract]
		Account VerifyAccount(string login, string hashedPassword);
	}
}
