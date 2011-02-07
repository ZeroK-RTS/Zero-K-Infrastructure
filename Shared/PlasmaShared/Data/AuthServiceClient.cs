using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using PlasmaShared;
using System.Linq;

namespace ZkData
{
	public class AuthServiceClient
	{
		static IAuthService channel;
		
		static AuthServiceClient()
		{
			var factory = new ChannelFactory<IAuthService>(new NetTcpBinding(SecurityMode.None),GlobalConst.AuthServiceUri);
			channel = factory.CreateChannel();
		}

    public static void SendLobbyMessage(Account account, string text)
    {
      channel.SendLobbyMessage(account, text);
    }

	  public static Account VerifyAccountPlain(string login, string password)
		{
			return VerifyAccountHashed(login, Utils.HashLobbyPassword(password));
		}

		public static Account VerifyAccountHashed(string login, string passwordHash)
		{
      if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(passwordHash)) return null;
			var db = new ZkDataContext();
			var acc = db.Accounts.FirstOrDefault(x => x.Name == login && x.Password == passwordHash);
			if (acc != null) return acc; else return channel.VerifyAccount(login, passwordHash);

		}

    static CurrentLobbyStats cachedStats = new CurrentLobbyStats();
	  static DateTime lastStatsCheck = DateTime.MinValue;
	  public static CurrentLobbyStats GetLobbyStats()
	  {
      if (DateTime.UtcNow.Subtract(lastStatsCheck).TotalMinutes < 2) return cachedStats;
      else
      {
        lastStatsCheck = DateTime.UtcNow;
        try
        {
          cachedStats = channel.GetCurrentStats();
        }
        catch (Exception ex)
        {
          Trace.TraceError("Error getting lobby stats: {0}", ex);
        }
        return cachedStats;
      }
	  }
	}
}
