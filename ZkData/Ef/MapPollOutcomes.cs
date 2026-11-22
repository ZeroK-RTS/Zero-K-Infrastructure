using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ZkData;
using Newtonsoft.Json;
using Ratings;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PlasmaShared;

namespace ZkData
{
    public class MapPollOutcome
    {
        [Key]
        public int MapPollID { get; set; }

        public MapRatings.Category Category { get; set; }
        
        public virtual List<MapPollOption> MapPollOptions { get; set; }
    }
}
