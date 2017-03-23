namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddWinnerToGalaxy : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Galaxies", "WinnerFactionID", c => c.Int());
            CreateIndex("dbo.Galaxies", "WinnerFactionID");
            AddForeignKey("dbo.Galaxies", "WinnerFactionID", "dbo.Factions", "FactionID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Galaxies", "WinnerFactionID", "dbo.Factions");
            DropIndex("dbo.Galaxies", new[] { "WinnerFactionID" });
            DropColumn("dbo.Galaxies", "WinnerFactionID");
        }
    }
}
