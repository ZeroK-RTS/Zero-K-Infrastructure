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
    // ContributionJar
    public partial class ContributionJar
    {
        public int ContributionJarID { get; set; } // ContributionJarID (Primary key)
        public string Name { get; set; } // Name
        public int GuarantorAccountID { get; set; } // GuarantorAccountID
        public string Description { get; set; } // Description
        public double TargetGrossEuros { get; set; } // TargetGrossEuros
        public bool IsDefault { get; set; } // IsDefault

        // Reverse navigation
        public virtual ICollection<Contribution> Contributions { get; set; } // Contribution.FK_Contribution_ContributionJar

        // Foreign keys
        public virtual Account Account { get; set; } // FK_ContributionJar_Account

        public ContributionJar()
        {
            IsDefault = true;
            Contributions = new List<Contribution>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
