using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
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

        [NotMapped]
        public string ImageRelativeUrl
        {
            get { if (ImageExtension == null) return null; return string.Format("/img/news/{0}{1}", NewsID, ImageExtension); }
        }

        [NotMapped]
        public string ThumbRelativeUrl
        {
            get { if (ImageExtension == null) return null; return string.Format("/img/news/{0}_thumb{1}", NewsID, ImageExtension); }
        }

    }
}
