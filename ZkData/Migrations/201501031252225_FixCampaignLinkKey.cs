using System.Data.Entity.Migrations;

namespace ZkData.Migrations
{
    public partial class FixCampaignLinkKey : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.CampaignLink");
            AddPrimaryKey("dbo.CampaignLink", new[] { "PlanetToUnlockID", "UnlockingPlanetID", "CampaignID" });
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.CampaignLink");
            AddPrimaryKey("dbo.CampaignLink", new[] { "PlanetToUnlockID", "UnlockingPlanetID" });
        }
    }
}
