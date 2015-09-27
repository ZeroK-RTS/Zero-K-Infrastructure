namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class WikiPosts : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ForumThreads", "WikiKey", c => c.String(maxLength: 100));
            AddColumn("dbo.ForumCategories", "IsWiki", c => c.Boolean(nullable: false));
            CreateIndex("dbo.ForumThreads", "Title");
            CreateIndex("dbo.ForumThreads", "WikiKey");
        }
        
        public override void Down()
        {
            DropIndex("dbo.ForumThreads", new[] { "WikiKey" });
            DropIndex("dbo.ForumThreads", new[] { "Title" });
            DropColumn("dbo.ForumCategories", "IsWiki");
            DropColumn("dbo.ForumThreads", "WikiKey");
        }
    }
}
