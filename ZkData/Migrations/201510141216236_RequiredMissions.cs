namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RequiredMissions : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "CanPlayMultiplayer", c => c.Boolean(nullable: false, defaultValue:true));
            AddColumn("dbo.Missions", "RequiredForMultiplayer", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Missions", "RequiredForMultiplayer");
            DropColumn("dbo.Accounts", "CanPlayMultiplayer");
        }
    }
}
