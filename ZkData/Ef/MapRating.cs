using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public class MapRating
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ResourceID { get; set; }
        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AccountID { get; set; }
        public int Rating { get; set; }

        public virtual Account Account { get; set; }
        public virtual Resource Resource { get; set; }
    }
}
