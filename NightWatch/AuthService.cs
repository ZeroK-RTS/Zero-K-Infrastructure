using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace NightWatch
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class AuthService
    {
        const int AuthServiceTestLoginWait = 8000;
        public const string ModeratorChannel = "zkadmin";
        public const string Top20Channel = "zktop20";

        readonly TasClient client;

        int messageId;
        readonly TopPlayers topPlayers = new TopPlayers();

        public AuthService(TasClient client)
        {
            this.client = client;


            this.client.LoginAccepted += (s, e) =>
                {
                    client.JoinChannel(ModeratorChannel);
                    client.JoinChannel(Top20Channel);
                    using (var db = new ZkDataContext()) foreach (var fac in db.Factions.Where(x => !x.IsDeleted)) client.JoinChannel(fac.Shortcut);
                };


            this.client.UserAdded += (s, e) =>
                {
                    using (var db = new ZkDataContext())
                    {
                        var acc = db.Accounts.Find(e.Data.LobbyID);
                        if (acc != null)
                        {
                            this.client.Extensions.PublishAccountData(acc);
                            if (acc.SpringieLevel > 2 || acc.IsZeroKAdmin) client.ForceJoinChannel(e.Data.Name, ModeratorChannel);
                            if (topPlayers.IsTop20(e.Data.LobbyID)) client.ForceJoinChannel(e.Data.Name, Top20Channel);
                            if (acc.Clan != null) client.ForceJoinChannel(e.Data.Name, acc.Clan.GetClanChannel(), acc.Clan.Password);
                            if (acc.Faction != null && acc.Level >= GlobalConst.FactionChannelMinLevel && acc.CanPlayerPlanetWars()) client.ForceJoinChannel(e.Data.Name, acc.Faction.Shortcut);
                        }
                        client.RequestUserIP(e.Data.Name);
                        client.RequestUserID(e.Data.Name);
                    }
                };

            this.client.UserIDRecieved += (sender, args) =>
                {
                    Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                using (var db = new ZkDataContext())
                                {
                                    var acc = Account.AccountByName(db, args.Name);
                                    var penalty = Punishment.GetActivePunishment(acc != null ? acc.AccountID : 0, null, args.ID, x => x.BanLobby, db);

                                    if (penalty != null)
                                        client.AdminKickFromLobby(args.Name,
                                                                  string.Format("Banned until {0} (ID match to {1}), reason: {2}", penalty.BanExpires, penalty.AccountByAccountID.Name, penalty.Reason)); ;

                                    if (acc != null && args.ID != 0)
                                    {
                                        var entry = acc.AccountUserIDs.FirstOrDefault(x => x.UserID == args.ID);
                                        if (entry == null)
                                        {
                                            entry = new AccountUserID { AccountID = acc.AccountID, UserID = args.ID, FirstLogin = DateTime.UtcNow };
                                            db.AccountUserIDs.InsertOnSubmit(entry);
                                        }
                                        entry.LoginCount++;
                                        entry.LastLogin = DateTime.UtcNow;
                                    }

                                    Account accAnteep = db.Accounts.FirstOrDefault(x => x.AccountID == 4490);
                                    bool isAnteepSmurf = accAnteep.AccountUserIDs.Any(x => x.UserID == args.ID);
                                    if (isAnteepSmurf)
                                    {
                                        client.Say(TasClient.SayPlace.Channel, ModeratorChannel, String.Format("Suspected Anteep smurf: {0} (ID match {1}) {2}", args.Name, args.ID,
                                            acc != null ? string.Format("{1}/Users/Detail/{0}", acc.AccountID, GlobalConst.BaseSiteUrl) : ""), false);
                                    }

                                    if (args.ID != 0 && args.ID < 1000)
                                    {
                                        client.Say(TasClient.SayPlace.Channel, ModeratorChannel, String.Format("Suspected Anteep smurf: {0} (too short userID {1}) {2}", args.Name, args.ID,
                                            acc != null ? string.Format("{1}/Users/Detail/{0}", acc.AccountID, GlobalConst.BaseSiteUrl) : ""), false);
                                    }

                                    db.SubmitChanges();
                                }
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError("Error getting user ID: {0}", ex);
                            }
                        });
                };

            this.client.UserIPRecieved += (sender, args) =>
                {
                    Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                Account acc = null;
                                using (var db = new ZkDataContext())
                                {
                                    acc = Account.AccountByName(db, args.Name);

                                    var penalty = Punishment.GetActivePunishment(acc != null ? acc.AccountID : 0, args.IP, null, x => x.BanLobby, db);
                                    if (penalty != null)
                                        client.AdminKickFromLobby(args.Name,
                                                                  string.Format("Banned until {0} (IP match to {1}), reason: {2}", penalty.BanExpires, penalty.AccountByAccountID.Name, penalty.Reason));
                                    if (acc != null)
                                    {
                                        var entry = acc.AccountIPs.FirstOrDefault(x => x.IP == args.IP);
                                        if (entry == null)
                                        {
                                            entry = new AccountIP { AccountID = acc.AccountID, IP = args.IP, FirstLogin = DateTime.UtcNow };
                                            db.AccountIPs.InsertOnSubmit(entry);
                                        }
                                        entry.LoginCount++;
                                        entry.LastLogin = DateTime.UtcNow;
                                    }
                                    db.SubmitChanges();
                                }

                                try
                                {
                                    if (acc == null || !acc.HasVpnException)
                                    {
                                        if (GlobalConst.VpnCheckEnabled)
                                        {
                                            // check user IP against http://dnsbl.tornevall.org
                                            // does not catch all smurfs
                                            // mostly false positives, do not use
                                            var reversedIP = string.Join(".", args.IP.Split('.').Reverse().ToArray());
                                            try
                                            {
                                                var resolved = Dns.GetHostEntry(string.Format("{0}.dnsbl.tornevall.org", reversedIP)).AddressList;
                                                if (resolved.Length > 0)
                                                {
                                                    client.Say(TasClient.SayPlace.Channel, ModeratorChannel, String.Format("User {0} {3} has IP {1} on dnsbl.tornevall.org ({2} result/s)",
                                                        args.Name, args.IP, resolved.Length, acc != null ? string.Format("{1}/Users/Detail/{0}", acc.AccountID, GlobalConst.BaseSiteUrl) : ""), false);
                                                    //client.AdminKickFromLobby(args.Name,
                                                    //                      "Connection using proxy or VPN is not allowed! (You can ask for exception). See http://dnsbl.tornevall.org/removal.php to get your IP removed from the blacklist.");
                                                }
                                            }
                                            catch (System.Net.Sockets.SocketException sockEx)
                                            {
                                                // not in database, do nothing
                                            }
                                        }
                                        using (var db = new ZkDataContext())
                                        {
                                            Account accAnteep = db.Accounts.FirstOrDefault(x => x.AccountID == 4490);
                                            bool isAnteepSmurf = accAnteep.AccountIPs.Any(x => x.IP == args.IP);
                                            if (isAnteepSmurf)
                                            {
                                                client.Say(TasClient.SayPlace.Channel, ModeratorChannel, String.Format("Suspected Anteep smurf: {0} (IP match {1}) {2}", args.Name, args.IP,
                                                acc != null ? string.Format("{1}/Users/Detail/{0}", acc.AccountID, GlobalConst.BaseSiteUrl) : ""), false);
                                            }
                                        }

                                        using (ZkDataContext db = new ZkDataContext())
                                        {
                                            for (int i = 0; i <= 1; i++)
                                            {
                                                var whois = new Whois();
                                                var data = whois.QueryByIp(args.IP, i == 1);

                                                if (!data.ContainsKey("netname")) data["netname"] = "UNKNOWN NETNAME";
                                                if (!data.ContainsKey("org-name")) data["org-name"] = "UNKNOWN ORG";
                                                if (!data.ContainsKey("abuse-mailbox")) data["abuse-mailbox"] = "no mailbox";
                                                if (!data.ContainsKey("notify")) data["notify"] = "no notify address";
                                                if (!data.ContainsKey("role")) data["role"] = "UNKNOWN ROLE";
                                                if (!data.ContainsKey("descr")) data["descr"] = "no description";
                                                if (!data.ContainsKey("remarks")) data["remarks"] = "no remarks";

                                                var blockedCompanies = db.BlockedCompanies.Select(x => x.CompanyName.ToLower()).ToList();
                                                var blockedHosts = db.BlockedHosts.Select(x => x.HostName).ToList();
                                                /*if (acc.Country == "MY")
                                                {
                                                    client.Say(TasClient.SayPlace.User, "KingRaptor", String.Format("USER {0}\nnetname: {1}\norgname: {2}\ndescr: {3}\nabuse-mailbox: {4}",
                                                        acc.Name, data["netname"], data["org-name"], data["descr"], data["abuse-mailbox"]), false);
                                                }*/
                                                if (blockedHosts.Any(x => data["abuse-mailbox"].Contains(x)) || (blockedHosts.Any(x => data["notify"].Contains(x))))
                                                {
                                                    client.AdminKickFromLobby(args.Name, "Connection using proxy or VPN is not allowed! (You can ask for exception)");
                                                }
                                                foreach (string company in blockedCompanies)
                                                {
                                                    if (data["netname"].ToLower().Contains(company) || data["org-name"].ToLower().Contains(company) || data["descr"].ToLower().Contains(company) || data["role"].ToLower().Contains(company) || data["remarks"].ToLower().Contains(company))
                                                    {
                                                        client.AdminKickFromLobby(args.Name, "Connection using proxy or VPN is not allowed! (You can ask for exception)");
                                                        break;
                                                    }
                                                }

                                                var hostname = Dns.GetHostEntry(args.IP).HostName;
                                                if (blockedHosts.Any(hostname.Contains))
                                                    client.AdminKickFromLobby(args.Name, "Connection using proxy or VPN is not allowed! (You can ask for exception)");
                                            }
                                        }
                                    }
                                }
                                catch (System.Net.Sockets.SocketException sockEx)
                                {
                                    // do nothing
                                }
                                catch (Exception ex)
                                {
                                    Trace.TraceError("VPN check error: {0}", ex);
                                    //client.Say(TasClient.SayPlace.Channel, ModeratorChannel, ex.ToString(), false);
                                }
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError("Error getting user IP: {0}", ex);
                                //client.Say(TasClient.SayPlace.User, "KingRaptor", ex.ToString(), false);
                            }
                        });
                };



            this.client.UserLobbyVersionRecieved += (s, e) =>
                {
                    using (var db = new ZkDataContext())
                    {
                        var acc = Account.AccountByName(db, e.Name);
                        if (acc != null)
                        {
                            acc.LobbyVersion = e.LobbyVersion;
                            db.SubmitAndMergeChanges();
                        }
                    }
                };

            this.client.BattleFound +=
                (s, e) => { if (e.Data.Founder.IsZkLobbyUser && !e.Data.Founder.IsBot) client.SetBotMode(e.Data.Founder.Name, true); };

            this.client.ChannelUserAdded += (sender, args) =>
                {
                    try
                    {
                        var channel = args.ServerParams[0];
                        var user = args.ServerParams[1];
                        if (channel == ModeratorChannel)
                        {
                            var u = client.ExistingUsers[user];
                            if (u.SpringieLevel <= 2 && !u.IsZeroKAdmin) client.ForceLeaveChannel(user, ModeratorChannel);
                        }
                        else if (channel == Top20Channel)
                        {
                            var u = client.ExistingUsers[user];
                            if (!topPlayers.IsTop20(u.LobbyID) && u.Name != client.UserName) client.ForceLeaveChannel(user, Top20Channel);
                        }
                        else
                        {
                            using (var db = new ZkDataContext())
                            {
                                var fac = db.Factions.FirstOrDefault(x => x.Shortcut == channel);
                                if (fac != null)
                                {
                                    // faction channel
                                    var u = client.ExistingUsers[user];
                                    var acc = db.Accounts.Find(u.LobbyID);
                                    if (acc == null || acc.FactionID != fac.FactionID || acc.Level < GlobalConst.FactionChannelMinLevel) client.ForceLeaveChannel(user, channel);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error procesisng channel user added: {0}", ex);
                    }
                };
            this.client.ChannelUserRemoved += (sender, args) =>
                {
                    try
                    {
                        var channel = args.ServerParams[0];
                        var user = args.ServerParams[1];
                        if (channel == ModeratorChannel)
                        {
                            var u = client.ExistingUsers[user];
                            if (u.SpringieLevel > 2 || u.IsZeroKAdmin) client.ForceJoinChannel(user, ModeratorChannel);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error procesisng channel user added: {0}", ex);
                    }
                };
        }


  


        public void SendLobbyMessage(Account account, string text)
        {
            User ex;
            if (client.ExistingUsers.TryGetValue(account.Name, out ex)) client.Say(TasClient.SayPlace.User, account.Name, text, false);
            else
            {
                var message = new LobbyMessage
                {
                    SourceLobbyID = client.MyUser != null ? client.MyUser.LobbyID : 0,
                    SourceName = client.UserName,
                    Created = DateTime.UtcNow,
                    Message = text,
                    TargetName = account.Name,
                    TargetLobbyID = account.AccountID
                };
                using (var db = new ZkDataContext())
                {
                    db.LobbyMessages.InsertOnSubmit(message);
                    db.SubmitChanges();
                }
            }
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
