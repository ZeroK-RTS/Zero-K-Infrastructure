namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class News
    {
        public int NewsID { get; set; }

        public DateTime Created { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Column(TypeName = "text")]
        [Required]
        public string Text { get; set; }

        public int AuthorAccountID { get; set; }

        public DateTime HeadlineUntil { get; set; }

        public int ForumThreadID { get; set; }

        public int? SpringForumPostID { get; set; }

        [StringLength(50)]
        public string ImageExtension { get; set; }

        [StringLength(50)]
        public string ImageContentType { get; set; }

        public int? ImageLength { get; set; }

        public virtual Account Account { get; set; }

        public virtual ForumThread ForumThread { get; set; }
    }
}
