namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddExtraGalaxyColumns : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Galaxies", "Ended", c => c.DateTime());
            AddColumn("dbo.Galaxies", "EndMessage", c => c.String());
            AlterColumn("dbo.Galaxies", "MatchMakerState", c => c.String(unicode: false));
            DropColumn("dbo.Galaxies", "TurnStarted");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Galaxies", "TurnStarted", c => c.DateTime());
            AlterColumn("dbo.Galaxies", "MatchMakerState", c => c.String(unicode: false, storeType: "text"));
            DropColumn("dbo.Galaxies", "EndMessage");
            DropColumn("dbo.Galaxies", "Ended");
        }
    }
}
