using PlasmaShared;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ZkData
{
    public class Autohost
    {
        public Autohost()
        {

        }

        public int AutohostID { get; set; }
        public MapSupportLevel MinimumMapSupportLevel { get; set; }
        public AutohostMode AutohostMode { get; set; }
        public int InviteMMPlayers { get; set; } = 0;
        public int MaxElo { get; set; } = int.MaxValue;
        public int MinElo { get; set; } = int.MinValue;
        public int MaxLevel { get; set; } = int.MaxValue;
        public int MinLevel { get; set; } = int.MinValue;
        public int MaxRank { get; set; } = int.MaxValue;
        public int MinRank { get; set; } = int.MinValue;
        public int MaxPlayers { get; set; }
        public int MaxEvenPlayers { get; set; }
        public bool CbalEnabled { get; set; } = true;
        [StringLength(255)]
        public string ModName { get; set; }
        [StringLength(255)]
        public string MapName { get; set; }
        public RatingCategory ApplicableRating { get; set; } = RatingCategory.Casual;

        [StringLength(200)]
        public string Title { get; set; }
    }
}
