using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ZkData
{
    public class Clan:IValidatableObject
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

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!IsShortcutValid(Shortcut))
                yield return
                    new ValidationResult("Invalid shortcut - can only contain numbers and letters and must be at least one character long",
                        new[] { "Shortcut" });
        }

        public bool CanJoin(Account account)
        {
            if (account == null) return true;
            if (account.ClanID != null) return false;
            if (account.FactionID != null && account.FactionID != FactionID) return false;
            if (Accounts.Count() >= GlobalConst.MaxClanSkilledSize) return false;
            else return true;
        }


        public static bool IsShortcutValid(string text)
        {
            return text.All(Char.IsLetterOrDigit) && text.Length > 0 && text.Length <= 8;
        }

        

        public string GetImageUrl()
        {
            return string.Format("/img/clans/{0}.png", Shortcut);
        }

        public string GetBGImageUrl()
        {
            return string.Format("/img/clans/{0}_bg.png", Shortcut);
        }


        public static string ClanColor(Clan clan, int? myClanID = null)
        {
            if (clan == null || clan.Faction == null) return "";
            //if (clan.ClanID == myClanID) return "#00FFFF";
            return clan.Faction.Color;
        }

        public override string ToString()
        {
            return ClanName;
        }

        public string GetClanChannel()
        {
            return "clan_" + this.Shortcut;
        }
    }
}
