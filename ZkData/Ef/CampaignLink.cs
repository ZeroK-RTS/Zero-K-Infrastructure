namespace ZkData
{
    public class CampaignLink
    {
        public int PlanetToUnlockID { get; set; }
        public int UnlockingPlanetID { get; set; }
        public int CampaignID { get; set; }

        public virtual Campaign Campaign { get; set; }
        public virtual CampaignPlanet PlanetToUnlock { get; set; }
        public virtual CampaignPlanet UnlockingPlanet { get; set; }
    }
}
