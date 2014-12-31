namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("ForumPostEdit")]
    public partial class ForumPostEdit
    {
        public int ForumPostEditID { get; set; }

        public int ForumPostID { get; set; }

        public int EditorAccountID { get; set; }

        public string OriginalText { get; set; }

        public string NewText { get; set; }

        public DateTime EditTime { get; set; }

        public virtual Account Account { get; set; }

        public virtual ForumPost ForumPost { get; set; }
    }
}
