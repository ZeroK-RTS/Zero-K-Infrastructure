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
    // PlanetStructure
    public partial class PlanetStructure
    {
        public int PlanetID { get; set; } // PlanetID (Primary key)
        public int StructureTypeID { get; set; } // StructureTypeID (Primary key)
        public int? OwnerAccountID { get; set; } // OwnerAccountID
        public int? ActivatedOnTurn { get; set; } // ActivatedOnTurn
        public EnergyPriority EnergyPriority { get; set; } // EnergyPriority
        public virtual bool IsActive { get; set; } // IsActive
        public int? TargetPlanetID { get; set; } // TargetPlanetID

        // Foreign keys
        public virtual Account Account { get; set; } // FK_PlanetStructure_Account
        public virtual Planet Planet { get; set; } // FK_PlanetStructure_Planet
        public virtual Planet PlanetByTargetPlanetID { get; set; } // FK_PlanetStructure_Planet1
        public virtual StructureType StructureType { get; set; } // FK_PlanetStructure_StructureType

        public PlanetStructure()
        {
            ActivatedOnTurn = 0;
            EnergyPriority = 0;
            IsActive = false;
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
