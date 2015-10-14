using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public class Word
    {
        [Key]
        public int WordID { get; set; }

        [Index(IsUnique = true)]
        [MaxLength(100)]
        public string Text { get; set; }

        public virtual ICollection<ForumPostWord> ForumPostWords { get; set; } = new List<ForumPostWord>();
    }
}