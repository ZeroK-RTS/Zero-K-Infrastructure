using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace ZkData
{
    public class Punishment :IEntityAfterChange
    {
        public int PunishmentID { get; set; }
        public int AccountID { get; set; }
        [Required]
        [StringLength(1000)]
        public string Reason { get; set; }
        public DateTime Time { get; set; }
        public DateTime? BanExpires { get; set; }
        public bool BanMute { get; set; }
        public bool BanCommanders { get; set; }
        public bool BanUnlocks { get; set; }
        public bool BanSite { get; set; }
        public bool BanLobby { get; set; }
        public bool BanSpecChat { get; set; }
        [StringLength(1000)]
        public string BanIP { get; set; }
        public bool BanForum { get; set; }
        public long? UserID { get; set; }
        public int? CreatedAccountID { get; set; }
        public bool DeleteInfluence { get; set; }
        public bool DeleteXP { get; set; }
        public bool SegregateHost { get; set; }

        public virtual Account AccountByAccountID { get; set; }
        public virtual Account AccountByCreatedAccountID { get; set; }


        /// <summary>
        /// Finds active punishment
        /// </summary>
        /// <param name="accountID">0 is treated same as null</param>
        /// <param name="ip">"" is treated same as null</param>
        /// <param name="userID">0 is treated same as null</param>
        /// <param name="filter">additional filtering to punishments</param>
        /// <param name="db">db context to use</param>
        /// <returns></returns>
        public static Punishment GetActivePunishment(int? accountID, string ip, long? userID, Expression<Func<Punishment, bool>> filter = null)
        {
            if (ip == "") ip = null;
            if (accountID == 0) accountID = null;
            if (userID == 0) userID = null;

            if (punishments == null) CachePunishments();
            
            var ret = punishments.Where(x => x.BanExpires > DateTime.UtcNow).AsQueryable();    // don't use IsExpired because it has no supported translation to SQL
            if (filter != null) ret = ret.Where(filter);

            ret =
                ret.Where(
                    x => (accountID != null && x.AccountID == accountID) || (userID != null && x.UserID == userID) || (ip != null && x.BanIP == ip));
            return ret.OrderByDescending(x => x.BanExpires).FirstOrDefault();
        }


        static object punishmentsLock = new object();

        private static void CachePunishments()
        {
            lock (punishmentsLock)
            {
                using (var db = new ZkDataContext()) punishments = db.Punishments.Where(x => x.BanExpires > DateTime.UtcNow).ToList();
            }
        }

        private static List<Punishment> punishments = null;

        [NotMapped]
        public bool IsExpired
        {
            get
            {
                return BanExpires < DateTime.UtcNow;
            }
        }
        public void AfterChange(ZkDataContext.EntityEntry entry)
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Deleted || entry.State == EntityState.Modified) CachePunishments();
        }
    }
}
