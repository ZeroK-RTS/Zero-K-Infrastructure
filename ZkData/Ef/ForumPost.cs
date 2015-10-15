using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZkData
{
    public class ForumPost
    {
        public int ForumPostID { get; set; }
        public int AuthorAccountID { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        [Required]
        public string Text { get; set; }
        public int ForumThreadID { get; set; }
        public int Upvotes { get; set; }
        public int Downvotes { get; set; }
        
        public virtual ICollection<AccountForumVote> AccountForumVotes { get; set; } = new HashSet<AccountForumVote>();
        public virtual ICollection<ForumPostEdit> ForumPostEdits { get; set; } = new HashSet<ForumPostEdit>();
        public virtual ICollection<ForumPostWord> ForumPostWords { get; set; } = new List<ForumPostWord>();
        public virtual ForumThread ForumThread { get; set; }
        public virtual Account Account { get; set; }
        
    }
}
