using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace NightWatch
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class AuthService: IAuthService
    {
        const int AuthServiceTestLoginWait = 8000;
        public const string ModeratorChannel = "zkadmin";
        public const string Top20Channel = "zktop20";

        readonly TasClient client;

        int messageId;
        readonly ConcurrentDictionary<int, RequestInfo> requests = new ConcurrentDictionary<int, RequestInfo>();
        readonly TopPlayers topPlayers = new TopPlayers();
        public static string[] blockedCompanies = new string[] { "PRIVAX-LTD", "NetcoSolution-BLK-IP" };
        public static string[] blockedHosts = new string[] { "anchorfree.com", "leaseweb.com", "uk2net.com", "privax.com", "hidemyass.com", "hotspotshield.com" };

        public AuthService(TasClient client) {
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

            this.client.LoginAccepted += (s, e) =>
                {
                    requests.Clear();
                    client.JoinChannel(ModeratorChannel);
                    client.JoinChannel(Top20Channel);
                    using (var db = new ZkDataContext()) foreach (var fac in db.Factions.Where(x => !x.IsDeleted)) client.JoinChannel(fac.Shortcut);
                };

            this.client.TestLoginAccepted += (s, e) =>
                {
                    RequestInfo entry;
                    if (requests.TryGetValue(client.MessageID, out entry)) {
                        entry.CorrectName = e.ServerParams[0];
                        entry.LobbyID = Convert.ToInt32(e.ServerParams[1]);
                        if (client.ExistingUsers.ContainsKey(entry.CorrectName)) entry.User = client.ExistingUsers[entry.CorrectName];
                        entry.WaitHandle.Set();
                    }

                    requests.TryRemove(client.MessageID, out entry);
                };

            this.client.UserAdded += (s, e) =>
                {
                    using (var db = new ZkDataContext()) {
                        var acc = Account.AccountByLobbyID(db, e.Data.LobbyID);
                        if (acc != null) {
                            this.client.Extensions.PublishAccountData(acc);
                            if (acc.SpringieLevel > 1 || acc.IsZeroKAdmin) client.ForceJoinChannel(e.Data.Name, ModeratorChannel);
                            if (topPlayers.IsTop20(e.Data.LobbyID)) client.ForceJoinChannel(e.Data.Name, Top20Channel);
                            if (acc.Clan != null) client.ForceJoinChannel(e.Data.Name, acc.Clan.Shortcut, acc.Clan.Password);
                            if (acc.Faction != null && acc.Level >= GlobalConst.FactionChannelMinLevel) client.ForceJoinChannel(e.Data.Name, acc.Faction.Shortcut);
                        }
                        client.RequestUserIP(e.Data.Name);
                        client.RequestUserID(e.Data.Name);
                    }
                };

            this.client.UserIDRecieved += (sender, args) =>
                {
                    Task.Factory.StartNew(() =>
                        {
                            try {
                                using (var db = new ZkDataContext()) {
                                    var acc = Account.AccountByName(db, args.Name);
                                    var penalty = Punishment.GetActivePunishment(acc != null ? acc.AccountID : 0, null, args.ID, x => x.BanLobby, db);

                                    if (penalty != null)
                                        client.AdminKickFromLobby(args.Name,
                                                                  string.Format("Banned until {0}, reason: {1}", penalty.BanExpires, penalty.Reason));

                                    if (acc != null && args.ID != 0) {
                                        var entry = acc.AccountUserIDS.FirstOrDefault(x => x.UserID == args.ID);
                                        if (entry == null) {
                                            entry = new AccountUserID { AccountID = acc.AccountID, UserID = args.ID, FirstLogin = DateTime.UtcNow };
                                            db.AccountUserIDS.InsertOnSubmit(entry);
                                        }
                                        entry.LoginCount++;
                                        entry.LastLogin = DateTime.UtcNow;
                                    }
                                    db.SubmitChanges();
                                }
                            } catch (Exception ex) {
                                Trace.TraceError("Error getting user ID: {0}", ex);
                            }
                        });
                };

            this.client.UserIPRecieved += (sender, args) =>
                {
                    Task.Factory.StartNew(() =>
                        {
                            try {
                                Account acc = null;
                                using (var db = new ZkDataContext()) {
                                    acc = Account.AccountByName(db, args.Name);

                                    var penalty = Punishment.GetActivePunishment(acc != null ? acc.AccountID : 0, args.IP, null, x => x.BanLobby, db);
                                    if (penalty != null)
                                        client.AdminKickFromLobby(args.Name,
                                                                  string.Format("Banned until {0}, reason: {1}", penalty.BanExpires, penalty.Reason));
                                    if (acc != null) {
                                        var entry = acc.AccountIPS.FirstOrDefault(x => x.IP == args.IP);
                                        if (entry == null) {
                                            entry = new AccountIP { AccountID = acc.AccountID, IP = args.IP, FirstLogin = DateTime.UtcNow };
                                            db.AccountIPS.InsertOnSubmit(entry);
                                        }
                                        entry.LoginCount++;
                                        entry.LastLogin = DateTime.UtcNow;
                                    }
                                    db.SubmitChanges();
                                }

                                //if (GlobalConst.VpnCheckEnabled) {
                                    if (acc == null || !acc.HasVpnException) {
                                        if (GlobalConst.VpnCheckEnabled)
                                        {
                                            var reversedIP = string.Join(".", args.IP.Split('.').Reverse().ToArray());
                                            var resolved = Dns.GetHostEntry(string.Format("{0}.dnsbl.tornevall.org", reversedIP)).AddressList;
                                            if (resolved != null && resolved.Length > 0)
                                            {
                                                client.AdminKickFromLobby(args.Name,
                                                                          "Connection using proxy or VPN is not allowed! (You can ask for exception). See http://dnsbl.tornevall.org/removal.php to get your IP removed from the blacklist.");
                                            }
                                        }

                                        var whois = new Whois();
                                        var data = whois.QueryByIp(args.IP);
                                        if (blockedCompanies.Contains(data["netname"]) || blockedHosts.Any(x => data["abuse-mailbox"].Contains(x))) client.AdminKickFromLobby(args.Name, "Connection using VPN is not allowed! (You can ask for exception)");

                                        var hostname = Dns.GetHostEntry(args.IP).HostName;
                                        if (blockedHosts.Any(hostname.Contains))
                                            client.AdminKickFromLobby(args.Name,
                                                                      "Connection using proxy or VPN is not allowed! (You can ask for exception)");
                                        
                                    }
                                //}
                            } catch (Exception ex) {
                                Trace.TraceError("Error getting user IP: {0}", ex);
                            }
                        });
                };

            this.client.UserStatusChanged += (s, e) =>
                {
                    var user = client.ExistingUsers[e.ServerParams[0]];
                    Task.Factory.StartNew(() =>
                        {
                            try {
                                using (var db = new ZkDataContext()) UpdateUser(user.LobbyID, user.Name, user, null, db);
                            } catch (Exception ex) {
                                Trace.TraceError(ex.ToString());
                            }
                        },
                                          TaskCreationOptions.LongRunning);
                };

            this.client.BattleUserJoined += (s, e) =>
                {
                    var battle = client.ExistingBattles[e.BattleID];
                    var founder = battle.Founder;
                    if (founder.IsZkLobbyUser) {
                        var user = client.ExistingUsers[e.UserName];

                        if (!user.IsZkLobbyUser && !user.IsNotaLobby && battle.EngineVersion != client.ServerSpringVersion &&
                            battle.EngineVersion != client.ServerSpringVersion + ".0") {
                            client.Say(TasClient.SayPlace.User,
                                       user.Name,
                                       string.Format(
                                           "ALERT! YOU WILL DESYNC!! You NEED SPRING ENGINE {0} to play here. Simply join the game with Zero-K lobby ( http://zero-k.info/Wiki/Download ) OR get the engine from http://springrts.com/dl/buildbot/default/ OR build it on your Linux: http://springrts.com/wiki/Building_Spring_on_Linux ",
                                           battle.EngineVersion),
                                       false);
                        }

                        using (var db = new ZkDataContext()) {
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
                    using (var db = new ZkDataContext()) {
                        var acc = Account.AccountByName(db, e.Name);
                        if (acc != null) {
                            acc.LobbyVersion = e.LobbyVersion;
                            acc.LastLobbyVersionCheck = DateTime.UtcNow;
                            db.SubmitAndMergeChanges();
                            if (!acc.LobbyVersion.StartsWith("ZK")) {
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

            this.client.ChannelUserAdded += (sender, args) =>
                {
                    try {
                        var channel = args.ServerParams[0];
                        var user = args.ServerParams[1];
                        if (channel == ModeratorChannel) {
                            var u = client.ExistingUsers[user];
                            if (u.SpringieLevel <= 1 && !u.IsZeroKAdmin) client.ForceLeaveChannel(user, ModeratorChannel);
                        }
                        else if (channel == Top20Channel) {
                            var u = client.ExistingUsers[user];
                            if (!topPlayers.IsTop20(u.LobbyID) && u.Name != client.UserName) client.ForceLeaveChannel(user, Top20Channel);
                        }
                        else {
                            using (var db = new ZkDataContext()) {
                                var fac = db.Factions.FirstOrDefault(x => x.Shortcut == channel);
                                if (fac != null) {
                                    // faction channel
                                    var u = client.ExistingUsers[user];
                                    var acc = Account.AccountByLobbyID(db, u.LobbyID);
                                    if (acc == null || acc.FactionID != fac.FactionID || acc.Level < GlobalConst.FactionChannelMinLevel) client.ForceLeaveChannel(user, channel);
                                }
                            }
                        }
                    } catch (Exception ex) {
                        Trace.TraceError("Error procesisng channel user added: {0}", ex);
                    }
                };
            this.client.ChannelUserRemoved += (sender, args) =>
                {
                    try {
                        var channel = args.ServerParams[0];
                        var user = args.ServerParams[1];
                        if (channel == ModeratorChannel) {
                            var u = client.ExistingUsers[user];
                            if (u.SpringieLevel > 1 || u.IsZeroKAdmin) client.ForceJoinChannel(user, ModeratorChannel);
                        }
                    } catch (Exception ex) {
                        Trace.TraceError("Error procesisng channel user added: {0}", ex);
                    }
                };
        }


        Account UpdateUser(int lobbyID, string name, User user, string hashedPassword, ZkDataContext db = null) {
            Account acc = null;
            if (db == null) db = new ZkDataContext();

            acc = Account.AccountByLobbyID(db, lobbyID);
            if (acc == null) {
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


        public void SendLobbyMessage(Account account, string text) {
            User ex;
            if (client.ExistingUsers.TryGetValue(account.Name, out ex)) client.Say(TasClient.SayPlace.User, account.Name, text, false);
            else {
                var message = new LobbyMessage
                              {
                                  SourceLobbyID = client.MyUser.LobbyID,
                                  SourceName = client.MyUser.Name,
                                  Created = DateTime.UtcNow,
                                  Message = text,
                                  TargetName = account.Name,
                                  TargetLobbyID = account.LobbyID
                              };
                using (var db = new ZkDataContext()) {
                    db.LobbyMessages.InsertOnSubmit(message);
                    db.SubmitChanges();
                }
            }
        }

        public Account VerifyAccount(string login, string hashedPassword) {
            var info = requests[Interlocked.Increment(ref messageId)] = new RequestInfo();

            client.SendRaw(string.Format("#{0} TESTLOGIN {1} {2}", messageId, login, hashedPassword));
            if (info.WaitHandle.WaitOne(AuthServiceTestLoginWait)) {
                if (info.LobbyID == 0) return null; // not verified/invalid login or password
                else {
                    var acc = UpdateUser(info.LobbyID, info.CorrectName, info.User, hashedPassword);
                    return acc;
                }
            }
            return null; // timeout
        }

        public CurrentLobbyStats GetCurrentStats() {
            var ret = new CurrentLobbyStats();
            foreach (var u in client.ExistingUsers.Values) if (!u.IsBot && !u.IsInGame && !u.IsInBattleRoom) ret.UsersIdle++;

            foreach (var b in client.ExistingBattles.Values) {
                if (!GlobalConst.IsZkMod(b.ModName)) continue;
                foreach (var u in b.Users.Select(x => x.LobbyUser)) {
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