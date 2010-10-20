using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using LobbyClient;
using ZkData;

namespace NightWatch
{
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
	class AuthService: IAuthService
	{
		readonly TasClient client;

		readonly ConcurrentDictionary<int, RequestInfo> requests = new ConcurrentDictionary<int, RequestInfo>();
		int messageId;
		const int AuthServiceTestLoginWait = 8000;

		public AuthService(TasClient client)
		{
			this.client = client;

			this.client.LoginAccepted += (s, e) => requests.Clear();

			this.client.TestLoginAccepted += (s, e) =>
				{
					RequestInfo entry;
					if (requests.TryGetValue(client.MessageID, out entry))
					{
						if (client.ExistingUsers.ContainsKey(entry.Login)) entry.User = client.ExistingUsers[entry.Login];
						entry.AccountID = Convert.ToInt32(e.ServerParams[0]);
						entry.WaitHandle.Set();
					}

					requests.TryRemove(client.MessageID, out entry);
				};


			this.client.TestLoginDenied += (s, e) =>
				{
					RequestInfo entry;
					if (requests.TryGetValue(client.MessageID, out entry)) entry.WaitHandle.Set();
					requests.TryRemove(client.MessageID, out entry);
				};
		}

		public static ServiceHost CreateServiceHost(TasClient client)
		{
			var host = new ServiceHost(new AuthService(client), new Uri(GlobalConst.AuthServiceUri));
			var tcp = new NetTcpBinding(SecurityMode.None);
			host.AddServiceEndpoint(typeof(IAuthService), tcp , GlobalConst.AuthServiceUri);
			return host;
		}


		public Account VerifyAccount(string login, string hashedPassword)
		{
			var info = requests[Interlocked.Increment(ref messageId)] = new RequestInfo(login);

			client.SendRaw(string.Format("#{0} TESTLOGIN {1} {2}", messageId, login, hashedPassword));
			if (info.WaitHandle.WaitOne(AuthServiceTestLoginWait))
			{
				if (info.AccountID == 0) return null; // not verified/invalid login or password
				else
				{
					var db = new ZkDataContext();
					var acc = db.Accounts.SingleOrDefault(x => x.AccountID == info.AccountID);
					if (acc == null)
					{
						acc = new Account() { AccountID = info.AccountID };
						db.Accounts.InsertOnSubmit(acc);
					}

					acc.Name = login;
					acc.Password = hashedPassword;
					acc.LastLogin = DateTime.UtcNow;

					if (info.User != null) // user was online, we can update his data
					{
						acc.IsBot = info.User.IsBot;
						acc.IsLobbyAdministrator = info.User.IsAdmin;
						acc.Country = info.User.Country;
						acc.LobbyTimeRank = info.User.Rank;
					}

					db.SubmitChanges();
					return acc;
				}
			}
			else // looby timeout, use database
				return new ZkDataContext().Accounts.SingleOrDefault(x => x.Name == login && x.Password == hashedPassword);
		}

		class RequestInfo
		{
			public int AccountID;
			public User User;
			public readonly EventWaitHandle WaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
			public string Login;
			public RequestInfo(string login)
			{
				Login = login;
			}
		}
	}
}