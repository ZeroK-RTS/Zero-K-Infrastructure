namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class WikiKeyIndex : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.ForumThreads", "WikiKey");
        }
        
        public override void Down()
        {
            DropIndex("dbo.ForumThreads", new[] { "WikiKey" });
        }
    }
}
