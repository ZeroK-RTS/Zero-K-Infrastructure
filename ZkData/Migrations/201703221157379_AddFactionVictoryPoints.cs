namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddFactionVictoryPoints : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factions", "VictoryPoints", c => c.Double(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Factions", "VictoryPoints");
        }
    }
}
