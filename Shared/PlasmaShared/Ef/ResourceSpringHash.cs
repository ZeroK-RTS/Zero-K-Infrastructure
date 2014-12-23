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
    // ResourceSpringHash
    public partial class ResourceSpringHash
    {
        public int ResourceID { get; set; } // ResourceID (Primary key)
        public string SpringVersion { get; set; } // SpringVersion (Primary key)
        public int SpringHash { get; set; } // SpringHash

        // Foreign keys
        public virtual Resource Resource { get; set; } // FK_ResourceSpringHash_Resource
    }

}
