namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ForumCategoryInSingleField : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ForumCategories", "ForumMode", c => c.Int(nullable: false));
            DropColumn("dbo.ForumCategories", "IsMissions");
            DropColumn("dbo.ForumCategories", "IsMaps");
            DropColumn("dbo.ForumCategories", "IsSpringBattles");
            DropColumn("dbo.ForumCategories", "IsClans");
            DropColumn("dbo.ForumCategories", "IsPlanets");
            DropColumn("dbo.ForumCategories", "IsNews");
            DropColumn("dbo.ForumCategories", "IsWiki");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ForumCategories", "IsWiki", c => c.Boolean(nullable: false));
            AddColumn("dbo.ForumCategories", "IsNews", c => c.Boolean(nullable: false));
            AddColumn("dbo.ForumCategories", "IsPlanets", c => c.Boolean(nullable: false));
            AddColumn("dbo.ForumCategories", "IsClans", c => c.Boolean(nullable: false));
            AddColumn("dbo.ForumCategories", "IsSpringBattles", c => c.Boolean(nullable: false));
            AddColumn("dbo.ForumCategories", "IsMaps", c => c.Boolean(nullable: false));
            AddColumn("dbo.ForumCategories", "IsMissions", c => c.Boolean(nullable: false));
            DropColumn("dbo.ForumCategories", "ForumMode");
        }
    }
}
