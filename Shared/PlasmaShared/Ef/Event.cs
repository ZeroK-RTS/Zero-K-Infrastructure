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
    // Event
    public partial class Event
    {
        public int EventID { get; set; } // EventID (Primary key)
        public string Text { get; set; } // Text
        public DateTime Time { get; set; } // Time
        public int Turn { get; set; } // Turn
        public string PlainText { get; set; } // PlainText

        // Reverse navigation
        public virtual ICollection<Account> Accounts { get; set; } // Many to many mapping
        public virtual ICollection<Clan> Clans { get; set; } // Many to many mapping
        public virtual ICollection<Faction> Factions { get; set; } // Many to many mapping
        public virtual ICollection<Planet> Planets { get; set; } // Many to many mapping
        public virtual ICollection<SpringBattle> SpringBattles { get; set; } // Many to many mapping

        public Event()
        {
            Accounts = new List<Account>();
            Clans = new List<Clan>();
            Factions = new List<Faction>();
            Planets = new List<Planet>();
            SpringBattles = new List<SpringBattle>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
