using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ZkData;
using System.Data.Entity;
using Newtonsoft.Json;
using Ratings;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Linq.Translations;
using PlasmaShared;

namespace ZkData
{
    public class MapPollOption
    {

        [Key]
        public int MapPollOptionID { get; set; }

        [Required]
        public int ResourceID { get; set; }

        [ForeignKey(nameof(ResourceID))]
        public virtual Resource Resource { get; set; }

        [Required]
        public int MapPollID { get; set; }

        [ForeignKey(nameof(MapPollID))]
        public virtual MapPollOutcome MapPoll { get; set; }

        [Required]
        public int Votes { get; set; }
    }
}
