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

namespace PlasmaShared.Ef
{
    // Unlock
    public partial class Unlock
    {
        public int UnlockID { get; set; } // UnlockID (Primary key)
        public string Code { get; set; } // Code
        public string Name { get; set; } // Name
        public string Description { get; set; } // Description
        public int NeededLevel { get; set; } // NeededLevel
        public string LimitForChassis { get; set; } // LimitForChassis
        public int UnlockType { get; set; } // UnlockType
        public int? RequiredUnlockID { get; set; } // RequiredUnlockID
        public int MorphLevel { get; set; } // MorphLevel
        public int MaxModuleCount { get; set; } // MaxModuleCount
        public int? MetalCost { get; set; } // MetalCost
        public int XpCost { get; set; } // XpCost
        public int? MetalCostMorph2 { get; set; } // MetalCostMorph2
        public int? MetalCostMorph3 { get; set; } // MetalCostMorph3
        public int? MetalCostMorph4 { get; set; } // MetalCostMorph4
        public int? MetalCostMorph5 { get; set; } // MetalCostMorph5
        public int? KudosCost { get; set; } // KudosCost
        public bool? IsKudosOnly { get; set; } // IsKudosOnly

        // Reverse navigation
        public virtual CommanderDecorationIcon CommanderDecorationIcon { get; set; } // CommanderDecorationIcon.FK_CommanderDecorationIcon_Unlock
        public virtual ICollection<AccountUnlock> AccountUnlocks { get; set; } // Many to many mapping
        public virtual ICollection<Commander> Commanders { get; set; } // Commander.FK_Commander_Unlock
        public virtual ICollection<CommanderDecoration> CommanderDecorations { get; set; } // CommanderDecoration.FK_CommanderDecoration_Unlock
        public virtual ICollection<CommanderModule> CommanderModules { get; set; } // CommanderModule.FK_CommanderModule_Unlock
        public virtual ICollection<KudosPurchase> KudosPurchases { get; set; } // KudosPurchase.FK_KudosChange_Unlock
        public virtual ICollection<StructureType> StructureTypes { get; set; } // StructureType.FK_StructureType_Unlock
        public virtual ICollection<Unlock> Unlocks { get; set; } // Unlock.FK_Unlock_Unlock

        // Foreign keys
        public virtual Unlock Unlock_RequiredUnlockID { get; set; } // FK_Unlock_Unlock

        public Unlock()
        {
            NeededLevel = 0;
            UnlockType = 0;
            MorphLevel = 0;
            MaxModuleCount = 1;
            XpCost = 200;
            AccountUnlocks = new List<AccountUnlock>();
            Commanders = new List<Commander>();
            CommanderDecorations = new List<CommanderDecoration>();
            CommanderModules = new List<CommanderModule>();
            KudosPurchases = new List<KudosPurchase>();
            StructureTypes = new List<StructureType>();
            Unlocks = new List<Unlock>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
