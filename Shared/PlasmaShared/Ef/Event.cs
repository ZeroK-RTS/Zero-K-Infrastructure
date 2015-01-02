namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Event")]
    public partial class Event
    {
        
        public Event()
        {
            Accounts = new HashSet<Account>();
            Clans = new HashSet<Clan>();
            Factions = new HashSet<Faction>();
            Planets = new HashSet<Planet>();
            SpringBattles = new HashSet<SpringBattle>();
        }

        public int EventID { get; set; }

        [Required]
        [StringLength(4000)]
        public string Text { get; set; }

        public DateTime Time { get; set; }

        public int Turn { get; set; }

        [StringLength(4000)]
        public string PlainText { get; set; }

        
        public virtual ICollection<Account> Accounts { get; set; }

        
        public virtual ICollection<Clan> Clans { get; set; }

        
        public virtual ICollection<Faction> Factions { get; set; }

        
        public virtual ICollection<Planet> Planets { get; set; }

        
        public virtual ICollection<SpringBattle> SpringBattles { get; set; }
    }
}
