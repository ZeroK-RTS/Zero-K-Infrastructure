// ReSharper disable RedundantUsingDirective
// ReSharper disable DoNotCallOverridableMethodsInConstructor
// ReSharper disable InconsistentNaming
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable PartialMethodWithSinglePart
// ReSharper disable RedundantNameQualifier

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
//using DatabaseGeneratedOption = System.ComponentModel.DataAnnotations.DatabaseGeneratedOption;

namespace ZkData
{
    // Punishment
    public partial class Punishment
    {
        public int PunishmentID { get; set; } // PunishmentID (Primary key)
        public int AccountID { get; set; } // AccountID
        public string Reason { get; set; } // Reason
        public DateTime Time { get; set; } // Time
        public DateTime? BanExpires { get; set; } // BanExpires
        public bool BanMute { get; set; } // BanMute
        public bool BanCommanders { get; set; } // BanCommanders
        public bool BanUnlocks { get; set; } // BanUnlocks
        public bool BanSite { get; set; } // BanSite
        public bool BanLobby { get; set; } // BanLobby
        public string BanIP { get; set; } // BanIP
        public bool BanForum { get; set; } // BanForum
        public long? UserID { get; set; } // UserID
        public int? CreatedAccountID { get; set; } // CreatedAccountID
        public bool DeleteInfluence { get; set; } // DeleteInfluence
        public bool DeleteXP { get; set; } // DeleteXP
        public bool SegregateHost { get; set; } // SegregateHost
        public bool SetRightsToZero { get; set; } // SetRightsToZero

        // Foreign keys
        public virtual Account AccountByAccountID { get; set; } // FK_Punishment_Account
        public virtual Account AccountByCreatedAccountID { get; set; } // FK_Punishment_Account1

        public Punishment()
        {
            BanMute = false;
            BanCommanders = false;
            BanUnlocks = false;
            BanSite = false;
            BanLobby = false;
            DeleteInfluence = false;
            DeleteXP = false;
            SegregateHost = false;
            SetRightsToZero = false;
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
