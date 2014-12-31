namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("PollOption")]
    public partial class PollOption
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PollOption()
        {
            PollVotes = new HashSet<PollVote>();
        }

        [Key]
        public int OptionID { get; set; }

        public int PollID { get; set; }

        [Required]
        [StringLength(200)]
        public string OptionText { get; set; }

        public int Votes { get; set; }

        public virtual Poll Poll { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PollVote> PollVotes { get; set; }
    }
}
