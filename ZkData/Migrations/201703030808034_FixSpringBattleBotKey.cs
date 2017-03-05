namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixSpringBattleBotKey : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.SpringBattleBots");
            AddColumn("dbo.SpringBattleBots", "SpringBattleBotID", c => c.Int(nullable: false, identity: true));
            AlterColumn("dbo.SpringBattleBots", "BotAI", c => c.String());
            AddPrimaryKey("dbo.SpringBattleBots", "SpringBattleBotID");
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.SpringBattleBots");
            AlterColumn("dbo.SpringBattleBots", "BotAI", c => c.String(nullable: false, maxLength: 128));
            DropColumn("dbo.SpringBattleBots", "SpringBattleBotID");
            AddPrimaryKey("dbo.SpringBattleBots", new[] { "SpringBattleID", "BotAI" });
        }
    }
}
