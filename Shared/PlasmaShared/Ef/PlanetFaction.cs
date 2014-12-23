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
    // PlanetFaction
    public partial class PlanetFaction
    {
        public int PlanetID { get; set; } // PlanetID (Primary key)
        public int FactionID { get; set; } // FactionID (Primary key)
        public double Influence { get; set; } // Influence
        public int Dropships { get; set; } // Dropships
        public DateTime? DropshipsLastAdded { get; set; } // DropshipsLastAdded

        // Foreign keys
        public virtual Faction Faction { get; set; } // FK_PlanetFactionInfluence_Faction
        public virtual Planet Planet { get; set; } // FK_PlanetFactionInfluence_Planet

        public PlanetFaction()
        {
            Influence = 0;
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
