using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ZkData
{
    partial class Punishment
    {
        /// <summary>
        /// Finds active punishment
        /// </summary>
        /// <param name="accountID">0 is treated same as null</param>
        /// <param name="ip">"" is treated same as null</param>
        /// <param name="userID">0 is treated same as null</param>
        /// <param name="filter">additional filtering to punishments</param>
        /// <param name="db">db context to use</param>
        /// <returns></returns>

        public static bool DoIPv4sMatch(string ip1, string ip2)
        {
            string[] ip1Split = ip1.Split('.');
            string[] ip2Split = ip2.Split('.');
            if (ip1Split.Length != ip2Split.Length) return false;

            for (int i = 0; i < ip1Split.Length; i++)
            {
                if (ip1Split[i] != "*" && ip2Split[i] != "*")
                {
                    if (ip1Split[i] != ip2Split[i]) return false;
                }
            }
            return true;
        }

        public static Punishment GetActivePunishment(int? accountID, string ip, int? userID, Expression<Func<Punishment, bool>> filter = null, ZkDataContext db = null)
        {
            if (ip == "") ip = null;
            if (accountID == 0) accountID = null;
            if (userID == 0) userID = null;

            if (db == null) db = new ZkDataContext();
            var ret = db.Punishments.Where(x => x.BanExpires > DateTime.UtcNow);
            System.Collections.Generic.List<Punishment> ret2;
            if (filter != null) ret2 = ret.Where(filter).ToList();
            else ret2 = ret.ToList();

            ret2 =
                ret2.Where(
                    x => (accountID != null && x.AccountID == accountID) || (userID != null && x.UserID == userID) || (ip != null && x.BanIP != null && DoIPv4sMatch(x.BanIP, ip))).ToList();
            return ret2.OrderByDescending(x=> x.BanExpires).FirstOrDefault();
        }

    }
}
