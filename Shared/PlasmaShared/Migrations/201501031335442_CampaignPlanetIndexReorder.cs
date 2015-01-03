namespace PlasmaShared.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CampaignPlanetIndexReorder : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.CampaignLink", new[] { "PlanetToUnlockID", "CampaignID" }, "dbo.CampaignPlanet");
            DropForeignKey("dbo.CampaignLink", new[] { "UnlockingPlanetID", "CampaignID" }, "dbo.CampaignPlanet");
            DropIndex("dbo.CampaignLink", new[] { "PlanetToUnlockID", "CampaignID" });
            DropIndex("dbo.CampaignLink", new[] { "UnlockingPlanetID", "CampaignID" });
            DropPrimaryKey("dbo.CampaignLink");
            AddPrimaryKey("dbo.CampaignLink", new[] { "CampaignID", "PlanetToUnlockID", "UnlockingPlanetID" });
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.CampaignLink");
            AddPrimaryKey("dbo.CampaignLink", new[] { "PlanetToUnlockID", "UnlockingPlanetID", "CampaignID" });
            CreateIndex("dbo.CampaignLink", new[] { "UnlockingPlanetID", "CampaignID" });
            CreateIndex("dbo.CampaignLink", new[] { "PlanetToUnlockID", "CampaignID" });
            AddForeignKey("dbo.CampaignLink", new[] { "UnlockingPlanetID", "CampaignID" }, "dbo.CampaignPlanet", new[] { "CampaignID", "PlanetID" });
            AddForeignKey("dbo.CampaignLink", new[] { "PlanetToUnlockID", "CampaignID" }, "dbo.CampaignPlanet", new[] { "CampaignID", "PlanetID" });
        }
    }
}
