namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveWikiKey : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.ForumThreads", new[] { "WikiKey" });
            CreateIndex("dbo.ForumThreads", "Title");
            DropColumn("dbo.ForumThreads", "WikiKey");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ForumThreads", "WikiKey", c => c.String(maxLength: 100));
            DropIndex("dbo.ForumThreads", new[] { "Title" });
            CreateIndex("dbo.ForumThreads", "WikiKey");
        }
    }
}
