using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using LobbyClient;
using System.Data.Entity;
using MaxMind.Db;
using MaxMind.GeoIP2;
using ZkData;

namespace ZkLobbyServer
{
    public class LoginChecker
    {
        readonly IGeoIP2Provider geoIP;

        readonly ZkLobbyServer state;

        public LoginChecker(ZkLobbyServer state, string geoipPath)
        {
            this.state = state;
            geoIP = new DatabaseReader(Path.Combine(geoipPath,"GeoLite2-Country.mmdb"), FileAccessMode.Memory);
        }

        DateTime lastConLogReset = DateTime.UtcNow;
        ConcurrentDictionary<string, int> connectionAttempts = new ConcurrentDictionary<string, int>();
        const int MaxConnectionAttempts = 60;
        const int MaxConnectionAttemptsMinutes = 60;

        private bool VerifyIp(string ip)
        {
            if (DateTime.UtcNow.Subtract(lastConLogReset).TotalMinutes > MaxConnectionAttemptsMinutes)
            {
                connectionAttempts = new ConcurrentDictionary<string, int>();
                lastConLogReset = DateTime.UtcNow;
                return true;
            }

            int entry;
            if (connectionAttempts.TryGetValue(ip, out entry) && entry > MaxConnectionAttempts)
            {
                Trace.TraceInformation("Blocking IP {0} due to too many connection attempts", ip);
                return false;
            }
            return true;
        }

        private void LogIpFailure(string ip)
        {
            connectionAttempts.AddOrUpdate(ip, (ipStr) => 1, (ipStr, count) => count + 1);
        }


        public LoginResponse Login(Login login, string ip, out User user)
        {
            user = new User();
            long userID = login.UserID;
            string lobbyVersion = login.LobbyVersion;

            using (var db = new ZkDataContext())
            {
                if (!VerifyIp(ip)) return new LoginResponse() {ResultCode = LoginResponse.Code.Banned, Reason = "Too many conneciton attempts"};

                Account acc = db.Accounts.Include(x => x.Clan).Include(x => x.Faction).FirstOrDefault(x => x.Name == login.Name);
                if (acc == null)
                {
                    LogIpFailure(ip);
                    return new LoginResponse { ResultCode = LoginResponse.Code.InvalidName };
                }
                if (!acc.VerifyPassword(login.PasswordHash))
                {
                    LogIpFailure(ip);
                    return new LoginResponse { ResultCode = LoginResponse.Code.InvalidPassword };
                }
                
                acc.Country = ResolveCountry(ip);
                if (acc.Country == null || String.IsNullOrEmpty(acc.Country)) acc.Country = "unknown";
                acc.LobbyVersion = lobbyVersion;
                acc.LastLogin = DateTime.UtcNow;

                user.ClientType = login.ClientType;
                user.LobbyVersion = login.LobbyVersion;
                UpdateUserFromAccount(user, acc);

                LogIP(db, acc, ip);

                LogUserID(db, acc, userID);

                db.SaveChanges();


                

                var banMute = Punishment.GetActivePunishment(acc.AccountID, ip, userID, x => x.BanMute, db);
                if (banMute != null) user.BanMute = true;

                var banSpecChat = Punishment.GetActivePunishment(acc.AccountID, ip, userID, x => x.BanSpecChat, db);
                if (banSpecChat != null) user.BanSpecChat = true;
                

                Punishment banPenalty = Punishment.GetActivePunishment(acc.AccountID, ip, userID, x => x.BanLobby, db);

                if (banPenalty != null) {
                    return
                        BlockLogin(
                            string.Format("Banned until {0} (match to {1}), reason: {2}", banPenalty.BanExpires, banPenalty.AccountByAccountID.Name,
                                banPenalty.Reason), acc, ip, userID);
                }

                Account accAnteep = db.Accounts.FirstOrDefault(x => x.AccountID == 4490);
                if (accAnteep != null) {
                    if (accAnteep.AccountUserIDs.Any(y => y.UserID == userID)) {
                        Talk(String.Format("Suspected Anteep smurf: {0} (ID match {1}) {2}", acc.Name, userID,
                            string.Format("{1}/Users/Detail/{0}", acc.AccountID, GlobalConst.BaseSiteUrl)));
                    }

                    if (userID > 0 && userID < 1000) {
                        Talk(String.Format("Suspected Anteep smurf: {0} (too short userID {1}) {2}", acc.Name, userID,
                            string.Format("{1}/Users/Detail/{0}", acc.AccountID, GlobalConst.BaseSiteUrl)));
                    }

                    if (accAnteep.AccountIPs.Any(y => y.IP == ip)) {
                        Talk(String.Format("Suspected Anteep smurf: {0} (IP match {1}) {2}", acc.Name, ip,
                            string.Format("{1}/Users/Detail/{0}", acc.AccountID, GlobalConst.BaseSiteUrl)));
                    }
                }

                if (!acc.HasVpnException && GlobalConst.VpnCheckEnabled) {
                    // check user IP against http://dnsbl.tornevall.org
                    // does not catch all smurfs
                    // mostly false positives, do not use
                    string reversedIP = string.Join(".", ip.Split('.').Reverse().ToArray());
                    try {
                        IPAddress[] resolved = Dns.GetHostEntry(string.Format("{0}.dnsbl.tornevall.org", reversedIP)).AddressList;
                        if (resolved.Length > 0) {
                            Talk(String.Format("User {0} {3} has IP {1} on dnsbl.tornevall.org ({2} result/s)", acc.Name, ip, resolved.Length,
                                string.Format("{1}/Users/Detail/{0}", acc.AccountID, GlobalConst.BaseSiteUrl)));
                        }
                    } catch (SocketException sockEx) {
                        // not in database, do nothing
                    }
                }

                try {
                    if (!acc.HasVpnException) {
                        //for (int i = 0; i <= 1; i++) {
                        for (int i = 1; i <= 1; i++)
                        {
                            var whois = new Whois();
                            Dictionary<string, string> data = whois.QueryByIp(ip, i == 1);

                            if (!data.ContainsKey("netname")) data["netname"] = "UNKNOWN NETNAME";
                            if (!data.ContainsKey("org-name")) data["org-name"] = "UNKNOWN ORG";
                            if (!data.ContainsKey("abuse-mailbox")) data["abuse-mailbox"] = "no mailbox";
                            if (!data.ContainsKey("notify")) data["notify"] = "no notify address";
                            if (!data.ContainsKey("role")) data["role"] = "UNKNOWN ROLE";
                            if (!data.ContainsKey("descr")) data["descr"] = "no description";
                            if (!data.ContainsKey("remarks")) data["remarks"] = "no remarks";

                            List<string> blockedCompanies = db.BlockedCompanies.Select(x => x.CompanyName.ToLower()).ToList();
                            List<string> blockedHosts = db.BlockedHosts.Select(x => x.HostName).ToList();
                            
                            //Trace.TraceInformation($"VPN check for USER {acc.Name}\nnetname: {data["netname"]}\norgname: {data["org-name"]}\ndescr: {data["descr"]}\nabuse-mailbox: {data["abuse-mailbox"]}", false);

                            bool shouldBlock = blockedHosts.Any(x => data["abuse-mailbox"].Contains(x)) ||
                                               (blockedHosts.Any(x => data["notify"].Contains(x)));

                            foreach (string company in blockedCompanies) {
                                if (data["netname"].ToLower().Contains(company) || data["org-name"].ToLower().Contains(company) ||
                                    data["descr"].ToLower().Contains(company) || data["role"].ToLower().Contains(company) ||
                                    data["remarks"].ToLower().Contains(company)) {
                                    shouldBlock = true;
                                    break;
                                }
                            }

                            if (shouldBlock) return BlockLogin("Connection using proxy or VPN is not allowed! (You can ask for exception)", acc, ip, userID);

                            // this can throw a SocketException, so make sure we block login already if we ought to
                            try
                            {
                                string hostname = Dns.GetHostEntry(ip)?.HostName;
                                if (blockedHosts.Any(hostname.Contains)) shouldBlock = true;


                                if (shouldBlock) return BlockLogin("Connection using proxy or VPN is not allowed! (You can ask for exception)", acc, ip, userID);
                            }
                            catch (SocketException) {}
                        }
                    }
                } catch (Exception ex) {
                    Trace.TraceError("VPN check error for user {0}: {1}", acc.Name, ex);
                }

                return new LoginResponse { ResultCode = LoginResponse.Code.Ok };

            }
        }

        string ResolveCountry(string ip)
        {
            if (IsLanIP(ip)) return "CZ";
            else {
                try {
                    return geoIP.Country(ip).Country.IsoCode;
                } catch (Exception ex) {
                    Trace.TraceWarning("{0} Unable to resolve country", this);
                    return  "??";
                }
            }
        }


        LoginResponse BlockLogin(string reason, Account acc, string ip, long user_id)
        {
            LogIpFailure(ip);
            var str = $"Login denied for {acc.Name} IP:{ip} ID:{user_id} reason: {reason}";
            Talk(str);
            Trace.TraceInformation(str);
            return new LoginResponse { Reason = reason, ResultCode = LoginResponse.Code.Banned };
        }

        static bool CheckMask(IPAddress address, IPAddress mask, IPAddress target)
        {
            if (mask == null) return false;

            byte[] ba = address.GetAddressBytes();
            byte[] bm = mask.GetAddressBytes();
            byte[] bb = target.GetAddressBytes();

            if (ba.Length != bm.Length || bm.Length != bb.Length) return false;

            for (int i = 0; i < ba.Length; i++) {
                int m = bm[i];

                int a = ba[i] & m;
                int b = bb[i] & m;

                if (a != b) return false;
            }

            return true;
        }

        static bool IsLanIP(string ip)
        {
            IPAddress address = IPAddress.Parse(ip);
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface iface in interfaces) {
                IPInterfaceProperties properties = iface.GetIPProperties();
                foreach (UnicastIPAddressInformation ifAddr in properties.UnicastAddresses) {
                    if (ifAddr.IPv4Mask != null && ifAddr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        CheckMask(ifAddr.Address, ifAddr.IPv4Mask, address)) return true;
                }
            }
            return false;
        }


        static void LogIP(ZkDataContext db, Account acc, string ip)
        {
            if (IsLanIP(ip)) return;
            AccountIP entry = acc.AccountIPs.FirstOrDefault(x => x.IP == ip);
            if (entry == null) {
                entry = new AccountIP { AccountID = acc.AccountID, IP = ip, FirstLogin = DateTime.UtcNow };
                db.AccountIPs.InsertOnSubmit(entry);
            }
            entry.LoginCount++;
            entry.LastLogin = DateTime.UtcNow;
        }

        static void LogUserID(ZkDataContext db, Account acc, long user_id)
        {
            if (user_id != 0) {
                AccountUserID entry = acc.AccountUserIDs.FirstOrDefault(x => x.UserID == user_id);
                if (entry == null) {
                    entry = new AccountUserID { AccountID = acc.AccountID, UserID = user_id, FirstLogin = DateTime.UtcNow };
                    db.AccountUserIDs.InsertOnSubmit(entry);
                }
                entry.LoginCount++;
                entry.LastLogin = DateTime.UtcNow;
            }
        }

        public static void UpdateUserFromAccount(User user, Account acc)
        {
            user.Name = acc.Name;
            user.DisplayName = acc.SteamName;
            user.Avatar = acc.Avatar;
            user.Level = acc.Level;
            user.EffectiveElo = (int)acc.EffectiveElo;
            user.Effective1v1Elo = (int)acc.EffectiveMmElo;
            user.SteamID = (ulong?)acc.SteamID;
            user.IsAdmin = acc.IsZeroKAdmin;
            user.IsBot = acc.IsBot;
            user.Country = acc.Country;
            user.Faction = acc.Faction != null ? acc.Faction.Shortcut : null;
            user.Clan = acc.Clan != null ? acc.Clan.Shortcut : null;
            user.AccountID = acc.AccountID;


            var banMute = Punishment.GetActivePunishment(acc.AccountID, "", 0, x => x.BanMute, null);
            if (banMute != null) user.BanMute = true;
            // note: we do not do "else = false" because this just checks accountID (there can still be active bans per IP or userID)

            var banSpecChat = Punishment.GetActivePunishment(acc.AccountID, "", 0, x => x.BanSpecChat, null);
            if (banSpecChat != null) user.BanSpecChat = true;
        }

        void Talk(string text)
        {
            ConnectedUser cli;
            if (state.ConnectedUsers.TryGetValue(GlobalConst.NightwatchName, out cli)) cli.Process(new Say { IsEmote = true, Place = SayPlace.Channel, Target = "zkadmin", Text = text });
        }
    }
}
