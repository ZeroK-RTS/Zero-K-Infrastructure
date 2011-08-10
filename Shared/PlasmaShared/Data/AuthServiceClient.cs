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
		//static IAuthService channel;
		
		static AuthServiceClient()
		{
			factory = new ChannelFactory<IAuthService>(new NetTcpBinding(SecurityMode.None),GlobalConst.AuthServiceUri);
		}

		public static void SendLobbyMessage(Account account, string text)
    {
			factory.CreateChannel().SendLobbyMessage(account, text);
    }

	  public static Account VerifyAccountPlain(string login, string password)
		{
			return VerifyAccountHashed(login, Utils.HashLobbyPassword(password));
		}

		public static Account VerifyAccountHashed(string login, string passwordHash)
		{
      if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(passwordHash)) return null;
			var db = new ZkDataContext();
			var acc = db.Accounts.FirstOrDefault(x => x.Name == login && x.Password == passwordHash && x.LobbyID != null);			
			if (acc != null || Debugger.IsAttached) return acc; else return factory.CreateChannel().VerifyAccount(login, passwordHash);

		}

    static CurrentLobbyStats cachedStats = new CurrentLobbyStats();
	  static DateTime lastStatsCheck = DateTime.MinValue;
		static ChannelFactory<IAuthService> factory;

		public static CurrentLobbyStats GetLobbyStats()
	  {
      if (DateTime.UtcNow.Subtract(lastStatsCheck).TotalMinutes < 2) return cachedStats;
      else
      {
        lastStatsCheck = DateTime.UtcNow;
        try
        {
          cachedStats = factory.CreateChannel().GetCurrentStats();
					cachedStats.UsersLastMonth = new ZkDataContext().Accounts.Count(x => x.LastLogin > DateTime.Now.AddDays(-31));
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
