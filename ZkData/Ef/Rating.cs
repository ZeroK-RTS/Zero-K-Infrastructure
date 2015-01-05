using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public class Rating
    {
        public int RatingID { get; set; }
        public int AccountID { get; set; }
        public int? MissionID { get; set; }
        [Column("Rating")]
        public int? Rating1 { get; set; }
        public int? Difficulty { get; set; }

        public virtual Account Account { get; set; }
        public virtual Mission Mission { get; set; }
    }
}
