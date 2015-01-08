namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveLobbyTimeRankAddCpu : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "Cpu", c => c.Int(nullable: false, defaultValue:0));
            DropColumn("dbo.Accounts", "LobbyTimeRank");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Accounts", "LobbyTimeRank", c => c.Int(nullable: false));
            DropColumn("dbo.Accounts", "Cpu");
        }
    }
}
