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
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using MaxMind.Db;
using MaxMind.GeoIP2;
using ZkData;
using Ratings;
using PlasmaShared;

namespace ZkLobbyServer
{
    public class LoginChecker
    {
        private const int MaxConnectionAttempts = 60;
        private const int MaxConnectionAttemptsMinutes = 60;
        private static readonly int MaxConcurrentLogins = Environment.ProcessorCount * 2;

        private static string[] ipWhitelist = { "127.0.0.1", "86.61.217.155", "78.45.34.102" };
        private readonly IGeoIP2Provider geoIP;

        private readonly ZkLobbyServer server;
        private ConcurrentDictionary<string, int> connectionAttempts = new ConcurrentDictionary<string, int>();

        private DateTime lastConLogReset = DateTime.UtcNow;


        private SemaphoreSlim semaphore = new SemaphoreSlim(MaxConcurrentLogins);

        public LoginChecker(ZkLobbyServer server, string geoipPath)
        {
            this.server = server;
            geoIP = new DatabaseReader(Path.Combine(geoipPath, "GeoLite2-Country.mmdb"), FileAccessMode.Memory);
        }

        public async Task<LoginCheckerResponse> DoLogin(Login login, string ip, List<ulong> dlc)
        {
            var limit = MiscVar.ZklsMaxUsers;
            if (limit > 0 && server.ConnectedUsers.Count >= limit) return new LoginCheckerResponse(LoginResponse.Code.ServerFull);
            await semaphore.WaitAsync();
            try
            {
                var userID = login.UserID;
                var installID = login.InstallID;
                var lobbyVersion = login.LobbyVersion;

                using (var db = new ZkDataContext())
                {
                    if (!VerifyIp(ip)) return new LoginCheckerResponse(LoginResponse.Code.BannedTooManyConnectionAttempts);

                    SteamWebApi.PlayerInfo info = null;
                    if (!string.IsNullOrEmpty(login.SteamAuthToken))
                    {
                        info = await server.SteamWebApi.VerifyAndGetAccountInformation(login.SteamAuthToken);

                        if (info == null)
                        {
                            LogIpFailure(ip);
                            return new LoginCheckerResponse(LoginResponse.Code.InvalidSteamToken);
                        }
                    }

                    Account accBySteamID = null;
                    Account accByLogin = null;
                    if (info != null) accBySteamID = db.Accounts.Include(x => x.Clan).Include(x => x.Faction).FirstOrDefault(x => x.SteamID == info.steamid);
                    if (!string.IsNullOrEmpty(login.Name))
                    {
                        accByLogin = db.Accounts.Include(x => x.Clan).Include(x => x.Faction).FirstOrDefault(x => x.Name == login.Name) ?? db.Accounts.Include(x => x.Clan).Include(x => x.Faction).FirstOrDefault(x => x.Name.Equals(login.Name, StringComparison.CurrentCultureIgnoreCase));
                    }

                    if (accBySteamID == null)
                    {
                        if (accByLogin == null)
                        {
                            LogIpFailure(ip);
                            if (!string.IsNullOrEmpty(login.Name)) return new LoginCheckerResponse(LoginResponse.Code.InvalidName);
                            else return new LoginCheckerResponse(LoginResponse.Code.SteamNotLinkedAndLoginMissing);
                        }

                        if (string.IsNullOrEmpty(login.PasswordHash) || !accByLogin.VerifyPassword(login.PasswordHash))
                        {
                            LogIpFailure(ip);
                            return new LoginCheckerResponse(LoginResponse.Code.InvalidPassword);
                        }
                    }
                    var acc = accBySteamID ?? accByLogin;

                    var ret = new LoginCheckerResponse(LoginResponse.Code.Ok);
                    ret.LoginResponse.Name = acc.Name;
                    var user = ret.User;

                    acc.Country = ResolveCountry(ip);
                    if ((acc.Country == null) || string.IsNullOrEmpty(acc.Country)) acc.Country = "??";
                    acc.LobbyVersion = lobbyVersion;
                    acc.LastLogin = DateTime.UtcNow;
                    if (info != null)
                    {
                        if (db.Accounts.Any(x => x.SteamID == info.steamid && x.Name != acc.Name))
                        {
                            LogIpFailure(ip);
                            return new LoginCheckerResponse(LoginResponse.Code.SteamLinkedToDifferentAccount);
                        }
                        acc.SteamID = info.steamid;
                        acc.SteamName = info.personaname;
                    }

                    user.LobbyVersion = login.LobbyVersion;
                    user.IpAddress = ip;

                    acc.VerifyAndAddDlc(dlc);

                    UpdateUserFromAccount(user, acc);
                    LogIP(db, acc, ip);
                    LogUserID(db, acc, userID, installID);

                    if (String.IsNullOrEmpty(installID) && !acc.HasVpnException)
                    {
                        await server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("{0} just logged in with an unsupported lobby https://zero-k.info/Users/AdminUserDetail/{1}", acc.Name, acc.AccountID));
                    }

                    db.SaveChanges();

                    ret.LoginResponse.SessionToken = Guid.NewGuid().ToString(); // create session token

                    var banPenalty = Punishment.GetActivePunishment(acc.AccountID, ip, userID, installID, x => x.BanLobby);

                    if (banPenalty != null)
                        return
                            BlockLogin(
                                $"Banned until {banPenalty.BanExpires} (match to {banPenalty.AccountByAccountID.Name}), reason: {banPenalty.Reason}",
                                acc,
                                ip,
                                userID,
                                installID);

                    if (!acc.HasVpnException && GlobalConst.VpnCheckEnabled) if (HasVpn(ip, acc, db)) return BlockLogin("Connection using proxy or VPN is not allowed! (You can ask for exception)", acc, ip, userID, installID);

                    return ret;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<RegisterResponse> DoRegister(Register register, string ip)
        {
            if (!Account.IsValidLobbyName(register.Name)) return new RegisterResponse(RegisterResponse.Code.NameHasInvalidCharacters);

            if (server.ConnectedUsers.ContainsKey(register.Name)) return new RegisterResponse(RegisterResponse.Code.AlreadyConnected);

            if (string.IsNullOrEmpty(register.PasswordHash) && string.IsNullOrEmpty(register.SteamAuthToken)) return new RegisterResponse(RegisterResponse.Code.MissingBothPasswordAndToken);

            if (!VerifyIp(ip)) return new RegisterResponse(RegisterResponse.Code.BannedTooManyAttempts);

            var banPenalty = Punishment.GetActivePunishment(null, ip, register.UserID, register.InstallID, x => x.BanLobby);
            if (banPenalty != null) return new RegisterResponse(RegisterResponse.Code.Banned) {BanReason =  banPenalty.Reason};

            SteamWebApi.PlayerInfo info = null;
            if (!string.IsNullOrEmpty(register.SteamAuthToken))
            {
                info = await server.SteamWebApi.VerifyAndGetAccountInformation(register.SteamAuthToken);
                if (info == null) return new RegisterResponse(RegisterResponse.Code.InvalidSteamToken);
            }

            using (var db = new ZkDataContext())
            {
                var registerName = register.Name.ToUpper();
                var existingByName = db.Accounts.FirstOrDefault(x => x.Name.ToUpper() == registerName);
                if (existingByName != null)
                {
                    if (info != null && existingByName.SteamID == info.steamid) return new RegisterResponse(RegisterResponse.Code.AlreadyRegisteredWithThisSteamToken);

                    if (info == null && !string.IsNullOrEmpty(register.PasswordHash) && existingByName.VerifyPassword(register.PasswordHash)) return new RegisterResponse(RegisterResponse.Code.AlreadyRegisteredWithThisPassword);

                    return new RegisterResponse(RegisterResponse.Code.NameAlreadyTaken);
                }

                var acc = new Account() { Name = register.Name };
                acc.SetPasswordHashed(register.PasswordHash);
                acc.SetName(register.Name);
                acc.SetAvatar();
                acc.Email = register.Email;
                if (info != null)
                {
                    var existingBySteam = db.Accounts.FirstOrDefault(x => x.SteamID == info.steamid);
                    if (existingBySteam != null)
                        return new RegisterResponse(RegisterResponse.Code.SteamAlreadyRegistered);

                    acc.SteamID = info.steamid;
                    acc.SteamName = info.personaname;
                } else if (string.IsNullOrEmpty(register.PasswordHash))
                {
                    return new RegisterResponse(RegisterResponse.Code.InvalidPassword);
                }
                LogIP(db, acc, ip);
                LogUserID(db, acc, register.UserID, register.InstallID);
                db.Accounts.Add(acc);
                db.SaveChanges();
                var smurfs = acc.GetSmurfs().Where(a => a.PunishmentsByAccountID.Any(x => x.BanExpires > DateTime.UtcNow));
                if (smurfs.Any())
                {
                    await server.GhostChanSay(GlobalConst.ModeratorChannel, string.Format("Smurf Alert! {0} might be a smurf of {1}. Check https://zero-k.info/Users/AdminUserDetail/{2}", acc.Name, smurfs.OrderByDescending(x => x.Level).First().Name, acc.AccountID));
                }
            }
            return new RegisterResponse(RegisterResponse.Code.Ok);
        }

        public void LogIpFailure(string ip)
        {
            connectionAttempts.AddOrUpdate(ip, (ipStr) => 1, (ipStr, count) => count + 1);
        }


        public static void UpdateUserFromAccount(User user, Account acc)
        {
            user.Name = acc.Name;
            user.DisplayName = acc.SteamName;
            user.Avatar = acc.Avatar;
            user.Level = acc.Level;
            user.Rank = acc.Rank;
            user.EffectiveMmElo = (int)Math.Round(Math.Min(acc.GetRating(RatingCategory.MatchMaking).LadderElo, acc.GetRating(RatingCategory.MatchMaking).RealElo));
            user.EffectiveElo = (int)Math.Round(acc.GetRating(RatingCategory.Casual).LadderElo);
            user.RawMmElo = (int)Math.Round(acc.GetRating(RatingCategory.MatchMaking).RealElo);
            user.SteamID = acc.SteamID?.ToString();
            user.IsAdmin = acc.AdminLevel >= AdminLevel.Moderator;
            user.IsBot = acc.IsBot;
            user.Country = acc.HideCountry ? "??" : acc.Country;
            user.Faction = acc.Faction != null ? acc.Faction.Shortcut : null;
            user.Clan = acc.Clan != null ? acc.Clan.Shortcut : null;
            user.AccountID = acc.AccountID;
            user.Badges = acc.GetBadges().Select(x => x.ToString()).ToList();
            user.Icon = acc.GetIconName();
            if (user.Badges.Count == 0) user.Badges = null; // slight optimization for data transfer
            Interlocked.Increment(ref user.SyncVersion);

            user.BanMute = Punishment.GetActivePunishment(acc.AccountID, user.IpAddress, 0, null, x => x.BanMute) != null;
            user.BanVotes = Punishment.GetActivePunishment(acc.AccountID, user.IpAddress, 0, null, x => x.BanVotes) != null;
            user.BanSpecChat = Punishment.GetActivePunishment(acc.AccountID, user.IpAddress, 0, null, x => x.BanSpecChat) != null;
        }

        public bool VerifyIp(string ip)
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


        private LoginCheckerResponse BlockLogin(string reason, Account acc, string ip, long user_id, string installID)
        {
            LogIpFailure(ip);
            var str = $"Login denied for {acc.Name} IP:{ip} UserID:{user_id} InstallID:{installID} reason: {reason}";
            Talk(str);
            Trace.TraceInformation(str);
            
            var ret = new LoginCheckerResponse(LoginResponse.Code.Banned);
            ret.LoginResponse.BanReason = reason;
            return ret;
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

        private static void LogUserID(ZkDataContext db, Account acc, long user_id, string installID)
        {
            if (user_id != 0)
            {
                installID = installID ?? "";
                var entry = acc.AccountUserIDs.FirstOrDefault(x => x.UserID == user_id && x.InstallID == installID);
                if (entry == null)
                {
                    entry = new AccountUserID { AccountID = acc.AccountID, UserID = user_id, FirstLogin = DateTime.UtcNow, InstallID = installID };
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
                    return geoIP.Country(ip)?.Country?.IsoCode ?? "??";
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
            if (server.ConnectedUsers.TryGetValue(GlobalConst.NightwatchName, out cli)) cli.Process(new Say { IsEmote = true, Place = SayPlace.Channel, Target = "zkadmin", Text = text });
        }


        public class LoginCheckerResponse
        {
            public LoginResponse LoginResponse = new LoginResponse();
            public User User = new User();

            public LoginCheckerResponse(LoginResponse.Code code)
            {
                LoginResponse.ResultCode = code;
            }
        }
    }
}
