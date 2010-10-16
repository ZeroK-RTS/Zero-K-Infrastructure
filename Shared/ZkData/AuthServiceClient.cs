using System.ServiceModel;
using System.ServiceModel.Description;

namespace ZkData
{
	public class AuthServiceClient
	{
		IAuthService channel;
		public AuthServiceClient()
		{
			var factory = new ChannelFactory<IAuthService>(new NetTcpBinding(), GlobalConst.AuthServiceHost);
			channel = factory.CreateChannel();
		}


		public Account VerifyAccount(string login, string passwordHash)
		{
			return channel.VerifyAccount(login, passwordHash);
		}
	}
}
