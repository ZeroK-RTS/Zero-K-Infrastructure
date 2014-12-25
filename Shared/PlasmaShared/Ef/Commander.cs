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
    // Commander
    public partial class Commander
    {
        public int CommanderID { get; set; } // CommanderID (Primary key)
        public int AccountID { get; set; } // AccountID
        public int ProfileNumber { get; set; } // ProfileNumber
        public string Name { get; set; } // Name
        public int ChassisUnlockID { get; set; } // ChassisUnlockID

        // Reverse navigation
        public virtual ICollection<CommanderDecoration> CommanderDecorations { get; set; } // Many to many mapping
        public virtual ICollection<CommanderModule> CommanderModules { get; set; } // Many to many mapping

        // Foreign keys
        public virtual Account AccountByAccountID { get; set; } // FK_Commander_Account
        public virtual Unlock Unlock { get; set; } // FK_Commander_Unlock

        public Commander()
        {
            CommanderDecorations = new List<CommanderDecoration>();
            CommanderModules = new List<CommanderModule>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
