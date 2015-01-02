using System;
using System.Diagnostics;
using System.Linq;
using ZkData;
using ZeroKWeb;

namespace ZkData
{
    public class AuthServiceClient
    {
        //static IAuthService channel;

        static CurrentLobbyStats cachedStats = new CurrentLobbyStats();
        static DateTime lastStatsCheck = DateTime.MinValue;
        static AuthServiceClient() {}

        public static CurrentLobbyStats GetLobbyStats()
        {
            if (DateTime.UtcNow.Subtract(lastStatsCheck).TotalMinutes < 2) return cachedStats;
            else
            {
                lastStatsCheck = DateTime.UtcNow;
                try
                {
                    cachedStats = Global.Nightwatch.Auth.GetCurrentStats();
                    var lastMonth = DateTime.Now.AddDays(-31);
                    cachedStats.UsersLastMonth = new ZkDataContext().SpringBattlePlayers.Where(x=>x.SpringBattle.StartTime > lastMonth).GroupBy(x=>x.AccountID).Count();
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error getting lobby stats: {0}", ex);
                }

                return cachedStats;
            }
        }

        public static void SendLobbyMessage(Account account, string text)
        {
            Global.Nightwatch.Auth.SendLobbyMessage(account, text);
        }

        public static Account VerifyAccountHashed(string login, string passwordHash)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(passwordHash)) return null;
            var db = new ZkDataContext();
#if DEBUG
            var acc = db.Accounts.FirstOrDefault(x => x.Name == login && x.LobbyID != null);
            
#else
            var acc = Account.AccountVerify(db, login, passwordHash);
#endif

            if (acc != null || Debugger.IsAttached) return acc;
            else return Global.Nightwatch.Auth.VerifyAccount(login, passwordHash);
        }

        public static Account VerifyAccountPlain(string login, string password)
        {
            return VerifyAccountHashed(login, Utils.HashLobbyPassword(password));
        }
    }
}