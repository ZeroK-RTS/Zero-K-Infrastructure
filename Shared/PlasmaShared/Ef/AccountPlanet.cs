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
    // AccountPlanet
    public partial class AccountPlanet
    {
        public int PlanetID { get; set; } // PlanetID (Primary key)
        public int AccountID { get; set; } // AccountID (Primary key)
        public double AttackPoints { get; set; } // AttackPoints

        // Foreign keys
        public virtual Account Account { get; set; } // FK_PlayerPlanet_Player
        public virtual Planet Planet { get; set; } // FK_PlayerPlanet_Planet

        public AccountPlanet()
        {
            AttackPoints = 0;
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
