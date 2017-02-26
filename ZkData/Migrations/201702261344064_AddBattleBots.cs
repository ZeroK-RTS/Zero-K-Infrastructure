namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddBattleBots : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SpringBattleBots",
                c => new
                    {
                        SpringBattleID = c.Int(nullable: false),
                        BotAI = c.String(nullable: false, maxLength: 128),
                        BotName = c.String(),
                        IsInVictoryTeam = c.Boolean(nullable: false),
                        AllyNumber = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.SpringBattleID, t.BotAI })
                .ForeignKey("dbo.SpringBattles", t => t.SpringBattleID, cascadeDelete: true)
                .Index(t => t.SpringBattleID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.SpringBattleBots", "SpringBattleID", "dbo.SpringBattles");
            DropIndex("dbo.SpringBattleBots", new[] { "SpringBattleID" });
            DropTable("dbo.SpringBattleBots");
        }
    }
}
