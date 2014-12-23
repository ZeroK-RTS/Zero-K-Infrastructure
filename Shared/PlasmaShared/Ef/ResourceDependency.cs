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
    // ResourceDependency
    public partial class ResourceDependency
    {
        public int ResourceID { get; set; } // ResourceID (Primary key)
        public string NeedsInternalName { get; set; } // NeedsInternalName (Primary key)

        // Foreign keys
        public virtual Resource Resource { get; set; } // FK_ResourceDependency_Resource
    }

}
