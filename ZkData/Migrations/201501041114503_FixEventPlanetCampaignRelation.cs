namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixEventPlanetCampaignRelation : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.CampaignEvents", new[] { "CampaignID" });
            DropIndex("dbo.CampaignEvents", new[] { "PlanetID", "CampaignID" });
            RenameColumn(table: "dbo.CampaignEvents", name: "PlanetID", newName: "__mig_tmp__0");
            RenameColumn(table: "dbo.CampaignEvents", name: "CampaignID", newName: "PlanetID");
            RenameColumn(table: "dbo.CampaignEvents", name: "__mig_tmp__0", newName: "CampaignID");
            AlterColumn("dbo.CampaignEvents", "PlanetID", c => c.Int());
            AlterColumn("dbo.CampaignEvents", "CampaignID", c => c.Int(nullable: false));
            CreateIndex("dbo.CampaignEvents", "CampaignID");
            CreateIndex("dbo.CampaignEvents", new[] { "CampaignID", "PlanetID" });
        }
        
        public override void Down()
        {
            DropIndex("dbo.CampaignEvents", new[] { "CampaignID", "PlanetID" });
            DropIndex("dbo.CampaignEvents", new[] { "CampaignID" });
            AlterColumn("dbo.CampaignEvents", "CampaignID", c => c.Int());
            AlterColumn("dbo.CampaignEvents", "PlanetID", c => c.Int(nullable: false));
            RenameColumn(table: "dbo.CampaignEvents", name: "CampaignID", newName: "__mig_tmp__0");
            RenameColumn(table: "dbo.CampaignEvents", name: "PlanetID", newName: "CampaignID");
            RenameColumn(table: "dbo.CampaignEvents", name: "__mig_tmp__0", newName: "PlanetID");
            CreateIndex("dbo.CampaignEvents", new[] { "PlanetID", "CampaignID" });
            CreateIndex("dbo.CampaignEvents", "CampaignID");
        }
    }
}
