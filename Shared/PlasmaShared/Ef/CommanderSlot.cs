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
    // CommanderSlot
    public partial class CommanderSlot
    {
        public int CommanderSlotID { get; set; } // CommanderSlotID (Primary key)
        public int MorphLevel { get; set; } // MorphLevel
        public int UnlockType { get; set; } // UnlockType

        // Reverse navigation
        public virtual ICollection<CommanderModule> CommanderModules { get; set; } // Many to many mapping

        public CommanderSlot()
        {
            MorphLevel = 1;
            CommanderModules = new List<CommanderModule>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
