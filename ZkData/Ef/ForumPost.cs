using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZkData
{
    public class ForumPost
    {
        
        public ForumPost()
        {
            Created = DateTime.UtcNow;
            
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
        
        public virtual ICollection<AccountForumVote> AccountForumVotes { get; set; }
        public virtual ICollection<ForumPostEdit> ForumPostEdits { get; set; }
        public virtual ForumThread ForumThread { get; set; }
        public virtual Account Account { get; set; }
    }
}
