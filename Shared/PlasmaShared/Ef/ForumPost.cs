namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("ForumPost")]
    public partial class ForumPost
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ForumPost()
        {
            AccountForumVotes = new HashSet<AccountForumVote>();
            ForumPostEdits = new HashSet<ForumPostEdit>();
        }

        public int ForumPostID { get; set; }

        public int AuthorAccountID { get; set; }

        public DateTime Created { get; set; }

        [Required]
        public string Text { get; set; }

        public int ForumThreadID { get; set; }

        public int Upvotes { get; set; }

        public int Downvotes { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<AccountForumVote> AccountForumVotes { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ForumPostEdit> ForumPostEdits { get; set; }
    }
}
