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
    // RoleTypeHierarchy
    public partial class RoleTypeHierarchy
    {
        public int MasterRoleTypeID { get; set; } // MasterRoleTypeID (Primary key)
        public int SlaveRoleTypeID { get; set; } // SlaveRoleTypeID (Primary key)
        public bool CanAppoint { get; set; } // CanAppoint
        public bool CanRecall { get; set; } // CanRecall

        // Foreign keys
        public virtual RoleType RoleType_MasterRoleTypeID { get; set; } // FK_RoleTypeHierarchy_RoleType
        public virtual RoleType RoleType_SlaveRoleTypeID { get; set; } // FK_RoleTypeHierarchy_RoleType1

        public RoleTypeHierarchy()
        {
            CanAppoint = true;
            CanRecall = true;
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
