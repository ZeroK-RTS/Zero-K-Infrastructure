namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddGameIdIndex : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.SpringBattles", "EngineGameID");
        }
        
        public override void Down()
        {
            DropIndex("dbo.SpringBattles", new[] { "EngineGameID" });
        }
    }
}
