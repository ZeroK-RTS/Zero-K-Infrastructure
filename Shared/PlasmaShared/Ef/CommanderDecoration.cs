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
    // CommanderDecoration
    public partial class CommanderDecoration
    {
        public int CommanderID { get; set; } // CommanderID (Primary key)
        public int DecorationUnlockID { get; set; } // DecorationUnlockID
        public int SlotID { get; set; } // SlotID (Primary key)

        // Foreign keys
        public virtual Commander Commander { get; set; } // FK_CommanderDecoration_Commander
        public virtual CommanderDecorationSlot CommanderDecorationSlot { get; set; } // FK_CommanderDecoration_CommanderDecorationSlot
        public virtual Unlock Unlock { get; set; } // FK_CommanderDecoration_Unlock
    }

}
