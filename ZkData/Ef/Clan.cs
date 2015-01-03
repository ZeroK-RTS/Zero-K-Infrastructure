using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZkData
{
    public partial class Clan
    {
        
        public Clan()
        {
            AccountRoles = new HashSet<AccountRole>();
            ForumThreads = new HashSet<ForumThread>();
            PlanetOwnerHistories = new HashSet<PlanetOwnerHistory>();
            Events = new HashSet<Event>();
            Accounts = new HashSet<Account>();
            Polls = new HashSet<Poll>();
        }

        public int ClanID { get; set; }

        [Required]
        [StringLength(50)]
        public string ClanName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(20)]
        public string Password { get; set; }

        [StringLength(500)]
        public string SecretTopic { get; set; }

        [Required]
        [StringLength(6)]
        public string Shortcut { get; set; }

        public int? ForumThreadID { get; set; }

        public bool IsDeleted { get; set; }

        public int? FactionID { get; set; }


        public virtual ICollection<Account> Accounts { get; set; }
        public virtual ICollection<AccountRole> AccountRoles { get; set; }
        public virtual Faction Faction { get; set; }
        public virtual ForumThread ForumThread { get; set; }
        public virtual ICollection<ForumThread> ForumThreads { get; set; }
        public virtual ICollection<PlanetOwnerHistory> PlanetOwnerHistories { get; set; }
        public virtual ICollection<Event> Events { get; set; }
        public virtual ICollection<Poll> Polls { get; set; }
    }
}
