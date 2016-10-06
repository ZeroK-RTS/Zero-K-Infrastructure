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
using System.Threading.Tasks;
using LobbyClient;
using MaxMind.Db;
using MaxMind.GeoIP2;
using ZkData;

namespace ZkLobbyServer
{
    public class LoginChecker
    {
        private const int MaxConnectionAttempts = 60;
        private const int MaxConnectionAttemptsMinutes = 60;

        private static string[] ipWhitelist = new string[] { "127.0.0.1", "86.61.217.155" };
        private readonly IGeoIP2Provider geoIP;

        private readonly ZkLobbyServer state;
        private ConcurrentDictionary<string, int> connectionAttempts = new ConcurrentDictionary<string, int>();

        private DateTime lastConLogReset = DateTime.UtcNow;

        public LoginChecker(ZkLobbyServer state, string geoipPath)
        {
            this.state = state;
            geoIP = new DatabaseReader(Path.Combine(geoipPath, "GeoLite2-Country.mmdb"), FileAccessMode.Memory);
        }

        public async Task<LoginReturn> Login(Login login, string ip)
        {
            var userID = login.UserID;
            var lobbyVersion = login.LobbyVersion;

            using (var db = new ZkDataContext())
            {
                if (!VerifyIp(ip)) return new LoginReturn(LoginResponse.Code.Banned, "Too many conneciton attempts");

                var acc =
                    db.Accounts.Include(x => x.Clan)
                        .Include(x => x.Faction)
                        .Include(x => x.AccountUserIDs)
                        .Include(x => x.AccountIPs)
                        .FirstOrDefault(x => x.Name == login.Name);
                if (acc == null)
                {
                    LogIpFailure(ip);
                    return new LoginReturn(LoginResponse.Code.InvalidName, "Invalid user name");
                }
                if (!acc.VerifyPassword(login.PasswordHash))
                {
                    LogIpFailure(ip);
                    return new LoginReturn(LoginResponse.Code.InvalidPassword, "Invalid password");
                }

                var ret = new LoginReturn(LoginResponse.Code.Ok, null);
                var user = ret.User;
                
                acc.Country = ResolveCountry(ip);
                if ((acc.Country == null) || string.IsNullOrEmpty(acc.Country)) acc.Country = "unknown";
                acc.LobbyVersion = lobbyVersion;
                acc.LastLogin = DateTime.UtcNow;

                user.ClientType = login.ClientType;
                user.LobbyVersion = login.LobbyVersion;
                UpdateUserFromAccount(user, acc);

                LogIP(db, acc, ip);

                LogUserID(db, acc, userID);

                await db.SaveChangesAsync();

                var banMute = Punishment.GetActivePunishment(acc.AccountID, ip, userID, x => x.BanMute);
                if (banMute != null) user.BanMute = true;

                var banSpecChat = Punishment.GetActivePunishment(acc.AccountID, ip, userID, x => x.BanSpecChat);
                if (banSpecChat != null) user.BanSpecChat = true;

                var banPenalty = Punishment.GetActivePunishment(acc.AccountID, ip, userID, x => x.BanLobby);

                if (banPenalty != null)
                    return
                        BlockLogin(
                            $"Banned until {banPenalty.BanExpires} (match to {banPenalty.AccountByAccountID.Name}), reason: {banPenalty.Reason}",
                            acc,
                            ip,
                            userID);


                if (!acc.HasVpnException && GlobalConst.VpnCheckEnabled) if (HasVpn(ip, acc, db)) return BlockLogin("Connection using proxy or VPN is not allowed! (You can ask for exception)", acc, ip, userID);

                return ret;
            }
        }

        public static void UpdateUserFromAccount(User user, Account acc)
        {
            user.Name = acc.Name;
            user.DisplayName = acc.SteamName;
            user.Avatar = acc.Avatar;
            user.Level = acc.Level;
            user.EffectiveMmElo = (int)acc.EffectiveMmElo;
            user.CompetitiveRank = acc.CompetitiveRank;
            user.SteamID = (ulong?)acc.SteamID;
            user.IsAdmin = acc.IsZeroKAdmin;
            user.IsBot = acc.IsBot;
            user.Country = acc.Country;
            user.Faction = acc.Faction != null ? acc.Faction.Shortcut : null;
            user.Clan = acc.Clan != null ? acc.Clan.Shortcut : null;
            user.AccountID = acc.AccountID;

            var banMute = Punishment.GetActivePunishment(acc.AccountID, "", 0, x => x.BanMute);
            if (banMute != null) user.BanMute = true;
            // note: we do not do "else = false" because this just checks accountID (there can still be active bans per IP or userID)

            var banSpecChat = Punishment.GetActivePunishment(acc.AccountID, "", 0, x => x.BanSpecChat);
            if (banSpecChat != null) user.BanSpecChat = true;
        }


        private LoginReturn BlockLogin(string reason, Account acc, string ip, long user_id)
        {
            LogIpFailure(ip);
            var str = $"Login denied for {acc.Name} IP:{ip} ID:{user_id} reason: {reason}";
            Talk(str);
            Trace.TraceInformation(str);
            return new LoginReturn(LoginResponse.Code.Banned, reason);
        }

        private static bool CheckMask(IPAddress address, IPAddress mask, IPAddress target)
        {
            if (mask == null) return false;

            var ba = address.GetAddressBytes();
            var bm = mask.GetAddressBytes();
            var bb = target.GetAddressBytes();

            if ((ba.Length != bm.Length) || (bm.Length != bb.Length)) return false;

            for (var i = 0; i < ba.Length; i++)
            {
                int m = bm[i];

                var a = ba[i] & m;
                var b = bb[i] & m;

                if (a != b) return false;
            }

            return true;
        }

        private bool HasVpn(string ip, Account acc, ZkDataContext db)
        {
            // check user IP against http://dnsbl.tornevall.org
            // does not catch all smurfs
            // mostly false positives, do not use
            var reversedIP = string.Join(".", ip.Split('.').Reverse().ToArray());
            try
            {
                var resolved = Dns.GetHostEntry(string.Format("{0}.dnsbl.tornevall.org", reversedIP)).AddressList;
                if (resolved.Length > 0)
                {
                    Talk(string.Format("User {0} {3} has IP {1} on dnsbl.tornevall.org ({2} result/s)",
                        acc.Name,
                        ip,
                        resolved.Length,
                        string.Format("{1}/Users/Detail/{0}", acc.AccountID, GlobalConst.BaseSiteUrl)));
                    return true;
                }
            }
            catch (SocketException sockEx)
            {
                // not in database, do nothing
            }

            try
            {
                //for (int i = 0; i <= 1; i++) {
                for (var i = 1; i <= 1; i++)
                {
                    var whois = new Whois();
                    var data = whois.QueryByIp(ip, i == 1);

                    if (!data.ContainsKey("netname")) data["netname"] = "UNKNOWN NETNAME";
                    if (!data.ContainsKey("org-name")) data["org-name"] = "UNKNOWN ORG";
                    if (!data.ContainsKey("abuse-mailbox")) data["abuse-mailbox"] = "no mailbox";
                    if (!data.ContainsKey("notify")) data["notify"] = "no notify address";
                    if (!data.ContainsKey("role")) data["role"] = "UNKNOWN ROLE";
                    if (!data.ContainsKey("descr")) data["descr"] = "no description";
                    if (!data.ContainsKey("remarks")) data["remarks"] = "no remarks";

                    var blockedCompanies = db.BlockedCompanies.Select(x => x.CompanyName.ToLower()).ToList();
                    var blockedHosts = db.BlockedHosts.Select(x => x.HostName).ToList();

                    //Trace.TraceInformation($"VPN check for USER {acc.Name}\nnetname: {data["netname"]}\norgname: {data["org-name"]}\ndescr: {data["descr"]}\nabuse-mailbox: {data["abuse-mailbox"]}", false);

                    if (blockedHosts.Any(x => data["abuse-mailbox"].Contains(x)) || blockedHosts.Any(x => data["notify"].Contains(x))) return true;

                    foreach (var company in blockedCompanies)
                        if (data["netname"].ToLower().Contains(company) || data["org-name"].ToLower().Contains(company) ||
                            data["descr"].ToLower().Contains(company) || data["role"].ToLower().Contains(company) ||
                            data["remarks"].ToLower().Contains(company)) return true;

                    // this can throw a SocketException, so make sure we block login already if we ought to
                    try
                    {
                        var hostname = Dns.GetHostEntry(ip)?.HostName;
                        if (blockedHosts.Any(hostname.Contains)) return true;
                    }
                    catch (SocketException) { }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("VPN check error for user {0}: {1}", acc.Name, ex);
            }
            return false;
        }

        private static bool IsLanIP(string ip)
        {
            var address = IPAddress.Parse(ip);
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var iface in interfaces)
            {
                var properties = iface.GetIPProperties();
                foreach (var ifAddr in properties.UnicastAddresses)
                    if ((ifAddr.IPv4Mask != null) && (ifAddr.Address.AddressFamily == AddressFamily.InterNetwork) &&
                        CheckMask(ifAddr.Address, ifAddr.IPv4Mask, address)) return true;
            }
            return false;
        }


        private static void LogIP(ZkDataContext db, Account acc, string ip)
        {
            if (IsLanIP(ip)) return;
            var entry = acc.AccountIPs.FirstOrDefault(x => x.IP == ip);
            if (entry == null)
            {
                entry = new AccountIP { AccountID = acc.AccountID, IP = ip, FirstLogin = DateTime.UtcNow };
                db.AccountIPs.InsertOnSubmit(entry);
            }
            entry.LoginCount++;
            entry.LastLogin = DateTime.UtcNow;
        }

        private void LogIpFailure(string ip)
        {
            connectionAttempts.AddOrUpdate(ip, (ipStr) => 1, (ipStr, count) => count + 1);
        }

        private static void LogUserID(ZkDataContext db, Account acc, long user_id)
        {
            if (user_id != 0)
            {
                var entry = acc.AccountUserIDs.FirstOrDefault(x => x.UserID == user_id);
                if (entry == null)
                {
                    entry = new AccountUserID { AccountID = acc.AccountID, UserID = user_id, FirstLogin = DateTime.UtcNow };
                    db.AccountUserIDs.InsertOnSubmit(entry);
                }
                entry.LoginCount++;
                entry.LastLogin = DateTime.UtcNow;
            }
        }

        private string ResolveCountry(string ip)
        {
            if (IsLanIP(ip)) return "CZ";
            else
                try
                {
                    return geoIP.Country(ip).Country.IsoCode;
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("{0} Unable to resolve country", this);
                    return "??";
                }
        }

        private void Talk(string text)
        {
            ConnectedUser cli;
            if (state.ConnectedUsers.TryGetValue(GlobalConst.NightwatchName, out cli)) cli.Process(new Say { IsEmote = true, Place = SayPlace.Channel, Target = "zkadmin", Text = text });
        }

        private bool VerifyIp(string ip)
        {
            if (ipWhitelist.Contains(ip)) return true;
            if (DateTime.UtcNow.Subtract(lastConLogReset).TotalMinutes > MaxConnectionAttemptsMinutes)
            {
                connectionAttempts = new ConcurrentDictionary<string, int>();
                lastConLogReset = DateTime.UtcNow;
                return true;
            }

            int entry;
            if (connectionAttempts.TryGetValue(ip, out entry) && (entry > MaxConnectionAttempts))
            {
                Trace.TraceInformation("Blocking IP {0} due to too many connection attempts", ip);
                return false;
            }
            return true;
        }


        public class LoginReturn
        {
            public LoginResponse LoginResponse = new LoginResponse();
            public User User = new User();

            public LoginReturn(LoginResponse.Code code, string reason)
            {
                LoginResponse.ResultCode = code;
                LoginResponse.Reason = reason;
            }
        }
    }
}