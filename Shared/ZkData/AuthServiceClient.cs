using System.ServiceModel;
using System.ServiceModel.Description;
using PlasmaShared;

namespace ZkData
{
	public class AuthServiceClient
	{
		IAuthService channel;
		public AuthServiceClient()
		{
			var factory = new ChannelFactory<IAuthService>(new NetTcpBinding(), GlobalConst.AuthServiceUri);
			channel = factory.CreateChannel();
		}


		public Account VerifyAccount(string login, string password)
		{
			return channel.VerifyAccount(login, Utils.HashLobbyPassword(password));
		}
	}
}
