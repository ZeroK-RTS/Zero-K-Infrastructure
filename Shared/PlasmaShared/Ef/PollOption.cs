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
    // PollOption
    public partial class PollOption
    {
        public int OptionID { get; set; } // OptionID (Primary key)
        public int PollID { get; set; } // PollID
        public string OptionText { get; set; } // OptionText
        public int Votes { get; set; } // Votes

        // Reverse navigation
        public virtual ICollection<PollVote> PollVotes { get; set; } // PollVote.FK_PollVote_PollOption

        // Foreign keys
        public virtual Poll Poll { get; set; } // FK_PollOption_Poll

        public PollOption()
        {
            Votes = 0;
            PollVotes = new List<PollVote>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
