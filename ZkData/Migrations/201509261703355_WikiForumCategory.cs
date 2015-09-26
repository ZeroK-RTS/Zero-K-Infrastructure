namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class WikiForumCategory : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ForumThreads", "WikiKey", c => c.String(maxLength: 100));
            AddColumn("dbo.ForumCategories", "IsWiki", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ForumCategories", "IsWiki");
            DropColumn("dbo.ForumThreads", "WikiKey");
        }
    }
}
