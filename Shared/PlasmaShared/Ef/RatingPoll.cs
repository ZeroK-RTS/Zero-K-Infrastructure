namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("RatingPoll")]
    public partial class RatingPoll
    {
        
        public RatingPoll()
        {
            AccountRatingVotes = new HashSet<AccountRatingVote>();
            Missions = new HashSet<Mission>();
            Missions1 = new HashSet<Mission>();
            Resources = new HashSet<Resource>();
            SpringBattles = new HashSet<SpringBattle>();
        }

        public int RatingPollID { get; set; }

        public double? Average { get; set; }

        public int Count { get; set; }

        
        public virtual ICollection<AccountRatingVote> AccountRatingVotes { get; set; }

        
        public virtual ICollection<Mission> Missions { get; set; }

        
        public virtual ICollection<Mission> Missions1 { get; set; }

        
        public virtual ICollection<Resource> Resources { get; set; }

        
        public virtual ICollection<SpringBattle> SpringBattles { get; set; }
    }
}
