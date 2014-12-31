namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Poll")]
    public partial class Poll
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
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

        public virtual Account Account { get; set; }

        public virtual Account Account1 { get; set; }

        public virtual Faction Faction { get; set; }

        public virtual RoleType RoleType { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PollOption> PollOptions { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PollVote> PollVotes { get; set; }
    }
}
