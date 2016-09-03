namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveAhConfig : DbMigration
    {
        public override void Up()
        {
            DropTable("dbo.AutohostConfigs");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.AutohostConfigs",
                c => new
                    {
                        AutohostConfigID = c.Int(nullable: false, identity: true),
                        ClusterNode = c.String(nullable: false, maxLength: 50),
                        Login = c.String(nullable: false, maxLength: 50),
                        Password = c.String(nullable: false, maxLength: 50),
                        MaxPlayers = c.Int(nullable: false),
                        Welcome = c.String(maxLength: 200),
                        AutoSpawn = c.Boolean(nullable: false),
                        AutoUpdateRapidTag = c.String(maxLength: 50),
                        SpringVersion = c.String(maxLength: 50),
                        CommandLevels = c.String(maxLength: 500),
                        Map = c.String(maxLength: 100),
                        Mod = c.String(maxLength: 100),
                        Title = c.String(maxLength: 100),
                        JoinChannels = c.String(maxLength: 100),
                        BattlePassword = c.String(maxLength: 50),
                        AutohostMode = c.Int(nullable: false),
                        MinToStart = c.Int(),
                        MaxToStart = c.Int(),
                        MaxEloDifference = c.Int(),
                        MinLevel = c.Int(),
                        MinElo = c.Int(),
                        MaxLevel = c.Int(),
                        MaxElo = c.Int(),
                    })
                .PrimaryKey(t => t.AutohostConfigID);
            
        }
    }
}
