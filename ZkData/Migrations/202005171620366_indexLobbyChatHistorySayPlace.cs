namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class indexLobbyChatHistorySayPlace : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.LobbyChatHistories", "SayPlace");
        }
        
        public override void Down()
        {
            DropIndex("dbo.LobbyChatHistories", new[] { "SayPlace" });
        }
    }
}
