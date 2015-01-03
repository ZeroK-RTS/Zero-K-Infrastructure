using System.Data.Entity.Migrations;

namespace ZkData.Migrations
{
    public partial class CampaignPlanetIndexReorder2 : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.CampaignLink", new[] { "CampaignID", "PlanetToUnlockID" });
            CreateIndex("dbo.CampaignLink", new[] { "CampaignID", "UnlockingPlanetID" });
            AddForeignKey("dbo.CampaignLink", new[] { "CampaignID", "PlanetToUnlockID" }, "dbo.CampaignPlanet", new[] { "CampaignID", "PlanetID" });
            AddForeignKey("dbo.CampaignLink", new[] { "CampaignID", "UnlockingPlanetID" }, "dbo.CampaignPlanet", new[] { "CampaignID", "PlanetID" });
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.CampaignLink", new[] { "CampaignID", "UnlockingPlanetID" }, "dbo.CampaignPlanet");
            DropForeignKey("dbo.CampaignLink", new[] { "CampaignID", "PlanetToUnlockID" }, "dbo.CampaignPlanet");
            DropIndex("dbo.CampaignLink", new[] { "CampaignID", "UnlockingPlanetID" });
            DropIndex("dbo.CampaignLink", new[] { "CampaignID", "PlanetToUnlockID" });
        }
    }
}
