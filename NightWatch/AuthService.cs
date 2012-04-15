using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace NightWatch
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class AuthService: IAuthService
    {
        const int AuthServiceTestLoginWait = 8000;
        readonly TasClient client;

        int messageId;
        readonly ConcurrentDictionary<int, RequestInfo> requests = new ConcurrentDictionary<int, RequestInfo>();


        public AuthService(TasClient client)
        {
            this.client = client;

            /*
        this.client.Input += (s, e) =>
      {
          Console.WriteLine(e.Command +" "+ Utils.Glue(e.Args));
      };
      this.client.Output += (s, e) =>
      {
          Console.WriteLine(e.Data.Key + " " +Utils.Glue(e.Data.Value.Select(x=>x.ToString()).ToArray()));
      };*/

            this.client.LoginAccepted += (s, e) => requests.Clear();

            this.client.TestLoginAccepted += (s, e) =>
                {
                    RequestInfo entry;
                    if (requests.TryGetValue(client.MessageID, out entry))
                    {
                        entry.CorrectName = e.ServerParams[0];
                        entry.LobbyID = Convert.ToInt32(e.ServerParams[1]);
                        if (client.ExistingUsers.ContainsKey(entry.CorrectName)) entry.User = client.ExistingUsers[entry.CorrectName];
                        entry.WaitHandle.Set();
                    }

                    requests.TryRemove(client.MessageID, out entry);
                };

            this.client.UserAdded += (s, e) =>
                {
                    using (var db = new ZkDataContext())
                    {
                        var acc = Account.AccountByLobbyID(db, e.Data.LobbyID);
                        if (acc != null)
                        {
                            var data = new Dictionary<string, string>()
                                       {
                                           { ProtocolExtension.Keys.Level.ToString(), acc.Level.ToString() },
                                           { ProtocolExtension.Keys.EffectiveElo.ToString(), ((int)acc.EffectiveElo).ToString() },
                                           { ProtocolExtension.Keys.Faction.ToString(), acc.Faction != null ? acc.Faction.Shortcut : "" },
                                           { ProtocolExtension.Keys.Clan.ToString(), acc.Clan != null ? acc.Clan.Shortcut : "" },
                                           { ProtocolExtension.Keys.Avatar.ToString(), acc.Avatar }
                                       };
                            if (acc.SpringieLevel != 1) data.Add(ProtocolExtension.Keys.SpringieLevel.ToString(), acc.SpringieLevel.ToString());
                            if (acc.IsZeroKAdmin) data.Add(ProtocolExtension.Keys.ZkAdmin.ToString(), "1");

                            if (acc.Punishments.Any(x => x.BanExpires > DateTime.UtcNow && x.BanMute)) data.Add(ProtocolExtension.Keys.BanMute.ToString(), "1");
                            if (acc.Punishments.Any(x => x.BanExpires > DateTime.UtcNow && x.BanLobby)) data.Add(ProtocolExtension.Keys.BanLobby.ToString(), "1");

                            client.Extensions.Publish(e.Data.Name, data);

                            if (acc.Punishments.Any(x => x.BanExpires > DateTime.UtcNow && x.BanLobby)) client.AdminKickFromLobby(e.Data.Name, "Banned");
                        }
                    }
                };

            this.client.UserStatusChanged += (s, e) =>
                {
                    var user = client.ExistingUsers[e.ServerParams[0]];
                    Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                using (var db = new ZkDataContext()) UpdateUser(user.LobbyID, user.Name, user, null, db);
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError(ex.ToString());
                            }
                        }, TaskCreationOptions.LongRunning);
                };

            this.client.BattleUserJoined += (s, e) =>
                {
                    var battle = client.ExistingBattles[e.BattleID];
                    var founder = battle.Founder;
                    if (founder.IsZkLobbyUser)
                    {
                        var user = client.ExistingUsers[e.UserName];

                        if (!user.IsZkLobbyUser && battle.EngineVersion != client.ServerSpringVersion &&
                            battle.EngineVersion != client.ServerSpringVersion + ".0")
                        {
                            client.Say(TasClient.SayPlace.User,
                                       user.Name,
                                       string.Format(
                                           "ALERT! YOU WILL DESYNC!! You NEED SPRING ENGINE {0} to play here. Simply join the game with Zero-K lobby ( http://zero-k.info/Wiki/Download ) OR get the engine from http://springrts.com/dl/buildbot/default/ OR build it on your Linux: http://springrts.com/wiki/Building_Spring_on_Linux ",
                                           battle.EngineVersion),
                                       false);
                        }

                        using (var db = new ZkDataContext())
                        {
                            var acc = Account.AccountByLobbyID(db, user.LobbyID);
                            var name = founder.Name.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
                            var aconf = db.AutohostConfigs.FirstOrDefault(x => x.Login == name);
                            if (acc != null &&
                                (acc.LastLobbyVersionCheck == null || DateTime.UtcNow.Subtract(acc.LastLobbyVersionCheck.Value).TotalDays > 3) &&
                                aconf.AutohostMode != 0) client.RequestLobbyVersion(user.Name);
                        }
                    }
                };

            this.client.TestLoginDenied += (s, e) =>
                {
                    RequestInfo entry;
                    if (requests.TryGetValue(client.MessageID, out entry)) entry.WaitHandle.Set();
                    requests.TryRemove(client.MessageID, out entry);
                };

            this.client.UserLobbyVersionRecieved += (s, e) =>
                {
                    using (var db = new ZkDataContext())
                    {
                        var acc = Account.AccountByName(db, e.Name);
                        if (acc != null)
                        {
                            acc.LobbyVersion = e.LobbyVersion;
                            acc.LastLobbyVersionCheck = DateTime.UtcNow;
                            db.SubmitAndMergeChanges();
                            if (!acc.LobbyVersion.StartsWith("ZK"))
                            {
                                client.Say(TasClient.SayPlace.User,
                                           e.Name,
                                           string.Format(
                                               "WARNING: You are connected using {0} which is not fully compatible with this host. Please use Zero-K lobby. Download it from http://zero-k.info   NOTE: to play all Spring games/mods with Zero-K lobby, untick \"Official games\" on its multiplayer tab. Thank you!",
                                               e.LobbyVersion),
                                           false);
                            }
                        }
                    }
                };

            this.client.BattleFound +=
                (s, e) => { if (e.Data.Founder.IsZkLobbyUser && !e.Data.Founder.IsBot) client.SetBotMode(e.Data.Founder.Name, true); };
        }


        Account UpdateUser(int lobbyID, string name, User user, string hashedPassword, ZkDataContext db = null)
        {
            Account acc = null;
            if (db == null) db = new ZkDataContext();

            acc = Account.AccountByLobbyID(db, lobbyID);
            if (acc == null)
            {
                acc = new Account();
                db.Accounts.InsertOnSubmit(acc);
            }

            acc.LobbyID = lobbyID;
            acc.Name = name;
            if (!string.IsNullOrEmpty(hashedPassword)) acc.Password = hashedPassword;
            acc.LastLogin = DateTime.UtcNow;

            if (user != null) // user was online, we can update his data
            {
                acc.IsBot = user.IsBot;
                acc.IsLobbyAdministrator = user.IsAdmin;
                acc.Country = user.Country;
                acc.LobbyTimeRank = user.Rank;
            }

            db.SubmitAndMergeChanges();

            return acc;
        }


        public void SendLobbyMessage(Account account, string text)
        {
            User ex;
            if (client.ExistingUsers.TryGetValue(account.Name, out ex)) client.Say(TasClient.SayPlace.User, account.Name, text, false);
            else
            {
                var message = new LobbyMessage()
                              {
                                  SourceLobbyID = client.MyUser.LobbyID,
                                  SourceName = client.MyUser.Name,
                                  Created = DateTime.UtcNow,
                                  Message = text,
                                  TargetName = account.Name,
                                  TargetLobbyID = account.LobbyID
                              };
                using (var db = new ZkDataContext())
                {
                    db.LobbyMessages.InsertOnSubmit(message);
                    db.SubmitChanges();
                }
            }
        }

        public Account VerifyAccount(string login, string hashedPassword)
        {
            var info = requests[Interlocked.Increment(ref messageId)] = new RequestInfo();

            client.SendRaw(string.Format("#{0} TESTLOGIN {1} {2}", messageId, login, hashedPassword));
            if (info.WaitHandle.WaitOne(AuthServiceTestLoginWait))
            {
                if (info.LobbyID == 0) return null; // not verified/invalid login or password
                else
                {
                    var acc = UpdateUser(info.LobbyID, info.CorrectName, info.User, hashedPassword);
                    return acc;
                }
            }
            return null; // timeout
        }

        public CurrentLobbyStats GetCurrentStats()
        {
            var ret = new CurrentLobbyStats();
            foreach (var u in client.ExistingUsers.Values) if (!u.IsBot && !u.IsInGame && !u.IsInBattleRoom) ret.UsersIdle++;

            foreach (var b in client.ExistingBattles.Values)
            {
                if (!GlobalConst.IsZkMod(b.ModName)) continue;
                foreach (var u in b.Users.Select(x => x.LobbyUser))
                {
                    if (u.IsBot) continue;
                    if (u.IsInGame) ret.UsersFighting++;
                    else if (u.IsInBattleRoom) ret.UsersWaiting++;
                }
                if (b.IsInGame) ret.BattlesRunning++;
                else ret.BattlesWaiting++;
            }
            return ret;
        }

        class RequestInfo
        {
            public string CorrectName;
            public int LobbyID;
            public User User;
            public readonly EventWaitHandle WaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        }
    }
}