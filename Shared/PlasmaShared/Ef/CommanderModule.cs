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
    // CommanderModule
    public partial class CommanderModule
    {
        public int CommanderID { get; set; } // CommanderID (Primary key)
        public int ModuleUnlockID { get; set; } // ModuleUnlockID
        public int SlotID { get; set; } // SlotID (Primary key)

        // Foreign keys
        public virtual Commander Commander { get; set; } // FK_CommanderModule_Commander
        public virtual CommanderSlot CommanderSlot { get; set; } // FK_CommanderModule_CommanderSlot
        public virtual Unlock Unlock { get; set; } // FK_CommanderModule_Unlock
    }

}
