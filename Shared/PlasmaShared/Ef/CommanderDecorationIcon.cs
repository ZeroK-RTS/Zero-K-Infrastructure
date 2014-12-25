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
    // CommanderDecorationIcon
    public partial class CommanderDecorationIcon
    {
        public int DecorationUnlockID { get; set; } // DecorationUnlockID (Primary key)
        public int IconPosition { get; set; } // IconPosition
        public int IconType { get; set; } // IconType

        // Foreign keys
        public virtual Unlock Unlock { get; set; } // FK_CommanderDecorationIcon_Unlock
    }

}
