namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class IndexBattleStartTime : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.SpringBattles", "StartTime");
        }
        
        public override void Down()
        {
            DropIndex("dbo.SpringBattles", new[] { "StartTime" });
        }
    }
}
