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
    // CommanderDecorationSlot
    public partial class CommanderDecorationSlot
    {
        public int CommanderDecorationSlotID { get; set; } // CommanderDecorationSlotID (Primary key)

        // Reverse navigation
        public virtual ICollection<CommanderDecoration> CommanderDecorations { get; set; } // Many to many mapping

        public CommanderDecorationSlot()
        {
            CommanderDecorations = new List<CommanderDecoration>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
