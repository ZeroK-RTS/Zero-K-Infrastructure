using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZkData
{
    public class Poll
    {
        
        public Poll()
        {
            PollOptions = new HashSet<PollOption>();
            PollVotes = new HashSet<PollVote>();
        }

        public int PollID { get; set; }
        [Required]
        [StringLength(500)]
        public string QuestionText { get; set; }
        public bool IsAnonymous { get; set; }
        public int? RoleTypeID { get; set; }
        public int? RoleTargetAccountID { get; set; }
        public bool RoleIsRemoval { get; set; }
        public int? RestrictFactionID { get; set; }
        public int? RestrictClanID { get; set; }
        public int? CreatedAccountID { get; set; }
        public DateTime? ExpireBy { get; set; }
        public bool IsHeadline { get; set; }

        public virtual Account AccountByRoleTargetAccountID { get; set; }
        public virtual Account CreatedAccount { get; set; }
        public virtual Faction Faction { get; set; }
        public virtual Clan Clan { get; set; }
        public virtual RoleType RoleType { get; set; }
        public virtual ICollection<PollOption> PollOptions { get; set; }
        public virtual ICollection<PollVote> PollVotes { get; set; }
    }
}
