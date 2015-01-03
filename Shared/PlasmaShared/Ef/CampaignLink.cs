namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("CampaignLink")]
    public partial class CampaignLink
    {
        public int PlanetToUnlockID { get; set; }

        public int UnlockingPlanetID { get; set; }

        public int CampaignID { get; set; }

        public virtual Campaign Campaign { get; set; }

        public virtual CampaignPlanet PlanetToUnlock { get; set; }

        public virtual CampaignPlanet UnlockingPlanet { get; set; }
    }
}
