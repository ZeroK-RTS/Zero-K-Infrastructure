using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;

namespace ZkData
{
    public class CampaignPlanet
    {
        public int CampaignID { get; set; }
        public int PlanetID { get; set; }
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        public int MissionID { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsSkirmish { get; set; }
        [StringLength(4000)]
        public string Description { get; set; }
        [StringLength(4000)]
        public string DescriptionStory { get; set; }
        public bool StartsUnlocked { get; set; }
        public bool HideIfLocked { get; set; }
        [StringLength(100)]
        public string DisplayedMap { get; set; }

        public virtual ICollection<AccountCampaignProgress> AccountCampaignProgress { get; set; }
        public virtual Campaign Campaign { get; set; }
        public virtual ICollection<CampaignEvent> CampaignEvents { get; set; }
        public virtual ICollection<CampaignJournal> CampaignJournals { get; set; }
        public virtual ICollection<CampaignLink> CampaignLinksByPlanetToUnlock { get; set; }
        public virtual ICollection<CampaignLink> CampaignLinksByUnlockingPlanet { get; set; }
        public virtual Mission Mission { get; set; }
        public virtual ICollection<CampaignPlanetVar> CampaignPlanetVars { get; set; }

        public CampaignPlanet()
        {
            AccountCampaignProgress = new HashSet<AccountCampaignProgress>();
            CampaignEvents = new HashSet<CampaignEvent>();
            CampaignJournals = new HashSet<CampaignJournal>();
            CampaignLinksByPlanetToUnlock = new HashSet<CampaignLink>();
            CampaignLinksByUnlockingPlanet = new HashSet<CampaignLink>();
            CampaignPlanetVars = new HashSet<CampaignPlanetVar>();
        }

        public const double OverlayRatio = 2.25;

        public override string ToString()
        {
            return Name;
        }

        public string GetColor(Account viewer)
        {
            bool isUnlocked = IsUnlocked(viewer.AccountID);
            bool isCompleted = IsCompleted(viewer.AccountID);

            if (isCompleted)
            {
                return "#00FF88";
            }
            else if (isUnlocked)
            {
                return "#FFFFFF";
            }
            return "#808080";
        }

        public Rectangle PlanetOverlayRectangle(Campaign camp)
        {
            var db = new ZkDataContext();
            Resource map = db.Resources.FirstOrDefault(m => m.InternalName == Mission.Map);
            var w = map.PlanetWarsIconSize * OverlayRatio;
            var xp = (int)(X * camp.MapWidth);
            var yp = (int)(Y * camp.MapHeight);
            return new Rectangle((int)(xp - w / 2), (int)(yp - w / 2), (int)w, (int)w);
        }

        public Rectangle PlanetRectangle(Campaign camp)
        {
            var db = new ZkDataContext();
            Resource map = db.Resources.FirstOrDefault(m => m.InternalName == Mission.Map);
            var w = map.PlanetWarsIconSize;
            var xp = (int)(X * camp.MapWidth);
            var yp = (int)(Y * camp.MapHeight);
            return new Rectangle((int)(xp - w / 2), (int)(yp - w / 2), (int)w, (int)w);
        }

        public bool IsUnlocked(int accountID)
        {
            if (StartsUnlocked) return true;

            var db = new ZkDataContext();
            AccountCampaignProgress progress = db.AccountCampaignProgress.FirstOrDefault(x => x.AccountID == accountID && x.CampaignID == CampaignID && x.PlanetID == PlanetID);
            return (progress != null && progress.IsUnlocked);
        }

        public bool IsCompleted(int accountID)
        {
            var db = new ZkDataContext();
            AccountCampaignProgress progress = db.AccountCampaignProgress.FirstOrDefault(x => x.AccountID == accountID && x.CampaignID == CampaignID && x.PlanetID == PlanetID);
            return (progress != null && progress.IsCompleted);
        }
    }
}
