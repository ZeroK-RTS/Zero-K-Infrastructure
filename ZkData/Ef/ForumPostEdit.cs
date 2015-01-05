using System;

namespace ZkData
{
    public class ForumPostEdit
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
