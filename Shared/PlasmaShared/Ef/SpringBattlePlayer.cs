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
    // SpringBattlePlayer
    public partial class SpringBattlePlayer
    {
        public int SpringBattleID { get; set; } // SpringBattleID (Primary key)
        public int AccountID { get; set; } // AccountID (Primary key)
        public bool IsSpectator { get; set; } // IsSpectator
        public bool IsInVictoryTeam { get; set; } // IsInVictoryTeam
        public string CommanderType { get; set; } // CommanderType
        public int? LoseTime { get; set; } // LoseTime
        public int AllyNumber { get; set; } // AllyNumber
        public int Rank { get; set; } // Rank
        public float? EloChange { get; set; } // EloChange
        public int? XpChange { get; set; } // XpChange
        public int? Influence { get; set; } // Influence

        // Foreign keys
        public virtual Account Account { get; set; } // FK_SpringBattlePlayer_Account
        public virtual SpringBattle SpringBattle { get; set; } // FK_SpringBattlePlayer_SpringBattle
    }

}
