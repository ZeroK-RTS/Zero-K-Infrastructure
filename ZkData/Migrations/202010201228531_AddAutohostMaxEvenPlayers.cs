namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAutohostMaxEvenPlayers : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Autohosts", "MaxEvenPlayers", c => c.Int(nullable: false));
            AddColumn("dbo.DynamicConfigs", "MaximumStatLimitedBattlePlayers", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "MaximumStatLimitedBattlePlayers");
            DropColumn("dbo.Autohosts", "MaxEvenPlayers");
        }
    }
}
