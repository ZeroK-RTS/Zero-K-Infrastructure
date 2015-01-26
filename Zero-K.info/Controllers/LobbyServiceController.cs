using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Razor.Parser.SyntaxTree;
using LobbyClient;
using NightWatch;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class LobbyServiceController : AsyncController
    {
        static Account accAnteep;
        static LobbyServiceController()
        {
            using (var db = new ZkDataContext()) accAnteep = db.Accounts.Include(x => x.AccountUserIDs).Include(x => x.AccountUserIDs).FirstOrDefault(x => x.AccountID == 4490);
        }

        // GET: LobbyServer
        public async Task<JsonResult> Login(string login, string password, string lobby_name, long user_id, int cpu, string ip, string country)
        {
            var db = new ZkDataContext();
            var acc = await Task.Run(() => Account.AccountVerify(db, login, password));
            
            if (acc == null)
            {
                return new JsonResult()
                {
                    Data = new LoginResponse("Invalid username or password"),
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
            else
            {

                acc.LobbyVersion = lobby_name;
                acc.Country = country;
                acc.Cpu = cpu;
                acc.LastLogin = DateTime.UtcNow;

                await Task.Run(() => {
                    LogIP(db, acc, ip);
                    LogUserID(db, acc, user_id);
                });
                
                
                await db.SaveChangesAsync();

                var banPenalty = await Task.Run(()=>Punishment.GetActivePunishment(acc.AccountID, ip, user_id, x => x.BanLobby, db));

                if (banPenalty != null)
                {
                    return BlockLogin(string.Format("Banned until {0} (match to {1}), reason: {2}", banPenalty.BanExpires, banPenalty.AccountByAccountID.Name, banPenalty.Reason), acc, ip,user_id);
                }

                WarnAnteepSmurf(user_id, ip, acc);

                if (!acc.HasVpnException && GlobalConst.VpnCheckEnabled)
                {
                    // check user IP against http://dnsbl.tornevall.org
                    // does not catch all smurfs
                    // mostly false positives, do not use
                    var reversedIP = string.Join(".", ip.Split('.').Reverse().ToArray());
                    try
                    {
                        var resolved = (await Dns.GetHostEntryAsync(string.Format("{0}.dnsbl.tornevall.org", reversedIP))).AddressList;
                        if (resolved.Length > 0)
                        {
                            Global.Nightwatch.Tas.Say(TasClient.SayPlace.Channel, AuthService.ModeratorChannel, String.Format("User {0} {3} has IP {1} on dnsbl.tornevall.org ({2} result/s)", acc.Name, ip, resolved.Length, string.Format("{1}/Users/Detail/{0}", acc.AccountID, GlobalConst.BaseSiteUrl)), false);
                        }
                    }
                    catch (System.Net.Sockets.SocketException sockEx)
                    {
                        // not in database, do nothing
                    }
                }

                try
                {
                    if (!acc.HasVpnException)
                    {

                        for (int i = 0; i <= 1; i++)
                        {
                            var whois = new Whois();
                            var data = await whois.QueryByIp(ip, i == 1);

                            if (!data.ContainsKey("netname")) data["netname"] = "UNKNOWN NETNAME";
                            if (!data.ContainsKey("org-name")) data["org-name"] = "UNKNOWN ORG";
                            if (!data.ContainsKey("abuse-mailbox")) data["abuse-mailbox"] = "no mailbox";
                            if (!data.ContainsKey("notify")) data["notify"] = "no notify address";
                            if (!data.ContainsKey("role")) data["role"] = "UNKNOWN ROLE";
                            if (!data.ContainsKey("descr")) data["descr"] = "no description";
                            if (!data.ContainsKey("remarks")) data["remarks"] = "no remarks";

                            var blockedCompanies = await db.BlockedCompanies.Select(x => x.CompanyName.ToLower()).ToListAsync();
                            var blockedHosts = await db.BlockedHosts.Select(x => x.HostName).ToListAsync();
                            /*if (acc.Country == "MY")
                            {
                                client.Say(TasClient.SayPlace.User, "KingRaptor", String.Format("USER {0}\nnetname: {1}\norgname: {2}\ndescr: {3}\nabuse-mailbox: {4}",
                                    acc.Name, data["netname"], data["org-name"], data["descr"], data["abuse-mailbox"]), false);
                            }*/

                            bool blockedHost = blockedHosts.Any(x => data["abuse-mailbox"].Contains(x)) || (blockedHosts.Any(x => data["notify"].Contains(x)));

                            foreach (string company in blockedCompanies)
                            {
                                if (data["netname"].ToLower().Contains(company) || data["org-name"].ToLower().Contains(company) || data["descr"].ToLower().Contains(company) || data["role"].ToLower().Contains(company) || data["remarks"].ToLower().Contains(company))
                                {
                                    blockedHost = true;
                                    break;
                                }
                            }

                            var hostname = Dns.GetHostEntry(ip).HostName;
                            if (blockedHosts.Any(hostname.Contains)) blockedHost = true;

                            if (blockedHost) return BlockLogin("Connection using proxy or VPN is not allowed! (You can ask for exception)", acc, ip, user_id);
                        }
                    }
                }
                catch (System.Net.Sockets.SocketException sockEx)
                {
                }
                catch (Exception ex)
                {
                    Trace.TraceError("VPN check error: {0}", ex);
                }

                return new JsonResult() { Data = new LoginResponse(acc), JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
        }

        static void WarnAnteepSmurf(long user_id, string ip, Account acc)
        {
            if (accAnteep != null) {
                if (accAnteep.AccountUserIDs.Any(y => y.UserID == user_id)) {
                    Global.Nightwatch.Tas.Say(TasClient.SayPlace.Channel, AuthService.ModeratorChannel,
                        String.Format("Suspected Anteep smurf: {0} (ID match {1}) {2}", acc.Name, user_id,
                            string.Format("{1}/Users/Detail/{0}", acc.AccountID, GlobalConst.BaseSiteUrl)), false);
                }

                if (user_id > 0 && user_id < 1000) {
                    Global.Nightwatch.Tas.Say(TasClient.SayPlace.Channel, AuthService.ModeratorChannel,
                        String.Format("Suspected Anteep smurf: {0} (too short userID {1}) {2}", acc.Name, user_id,
                            string.Format("{1}/Users/Detail/{0}", acc.AccountID, GlobalConst.BaseSiteUrl)), false);
                }

                if (accAnteep.AccountIPs.Any(y => y.IP == ip)) {
                    Global.Nightwatch.Tas.Say(TasClient.SayPlace.Channel, AuthService.ModeratorChannel,
                        String.Format("Suspected Anteep smurf: {0} (IP match {1}) {2}", acc.Name, ip,
                            string.Format("{1}/Users/Detail/{0}", acc.AccountID, GlobalConst.BaseSiteUrl)), false);
                }
            }
        }

        public JsonResult BlockLogin(string reason, Account acc, string ip, long user_id)
        {
            Global.Nightwatch.Tas.Say(TasClient.SayPlace.Channel, AuthService.ModeratorChannel, string.Format("Login denied for {0} IP:{1} ID:{2} reason: {3}", acc.Name, ip, user_id, reason), true);

            return new JsonResult() { Data = new LoginResponse(reason), JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        static void LogUserID(ZkDataContext db, Account acc, long user_id)
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

        static void LogIP(ZkDataContext db, Account acc, string ip)
        {
            var entry = acc.AccountIPs.FirstOrDefault(x => x.IP == ip);
            if (entry == null)
            {
                entry = new AccountIP { AccountID = acc.AccountID, IP = ip, FirstLogin = DateTime.UtcNow };
                db.AccountIPs.InsertOnSubmit(entry);
            }
            entry.LoginCount++;
            entry.LastLogin = DateTime.UtcNow;
        }

        public class AccountForLobbyServer
        {
            public int AccountID;
            public bool IsBot;
            public string Access = "mod";

            public AccountForLobbyServer(Account fromDb)
            {
                AccountID = fromDb.AccountID;
                IsBot = fromDb.IsBot;
                Access = "user";
                if (fromDb.SpringieLevel > 2) Access = "mod";
                if (fromDb.IsZeroKAdmin || fromDb.IsLobbyAdministrator) Access = "admin";
            }
        }

        public class LoginResponse
        {
            public bool Ok;
            public string Reason;
            public AccountForLobbyServer Account;

            public LoginResponse(Account dbAccount)
            {
                Ok = true;
                Account = new AccountForLobbyServer(dbAccount);
            }

            public LoginResponse(string failureReason)
            {
                Ok = false;
                Reason = failureReason;
            }
        }

        public JsonResult Register(string login, string password, string country, string ip)
        {
            var db = new ZkDataContext();
            if (db.Accounts.Any(y=>y.Name == login)) return new JsonResult() {
                Data = new LoginResponse("Username already exists."), JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
            var acc = new Account() { Name = login, NewPassword = password, Country = country, };
            acc.SetPassword(password);
            db.Accounts.Add(acc);
            db.SaveChanges();

            return new JsonResult() { Data = new LoginResponse("Account registered successfully.") {Ok = true}, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

    }
}