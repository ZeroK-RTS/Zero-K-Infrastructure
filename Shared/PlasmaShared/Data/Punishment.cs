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
        public static Punishment GetActivePunishment(int? accountID, string ip, uint? userID, Expression<Func<Punishment,bool>> filter = null, ZkDataContext db = null)
        {
            if (ip == "") ip = null;
            if (accountID == 0) accountID = null;
            if (userID == 0) userID = null;

            if (db == null) db = new ZkDataContext();
            var ret = db.Punishments.Where(x => x.BanExpires > DateTime.UtcNow);    // don't use IsExpired because it has no supported translation to SQL
            if (filter != null) ret = ret.Where(filter);

            ret =
                ret.Where(
                    x => (accountID != null && x.AccountID == accountID) || (userID != null && x.UserID == userID) || (ip != null && x.BanIP == ip));
            return ret.OrderByDescending(x=> x.BanExpires).FirstOrDefault();
        }

        public bool IsExpired
        {
            get
            {
                return BanExpires < DateTime.UtcNow;
            }
        }
    }
}
