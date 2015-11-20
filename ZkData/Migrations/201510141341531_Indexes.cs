namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Indexes : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ForumPostWords",
                c => new
                    {
                        WordID = c.Int(nullable: false),
                        ForumPostID = c.Int(nullable: false),
                        Count = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.WordID, t.ForumPostID })
                .ForeignKey("dbo.ForumPosts", t => t.ForumPostID, cascadeDelete: true)
                .ForeignKey("dbo.Words", t => t.WordID, cascadeDelete: true)
                .Index(t => t.WordID)
                .Index(t => t.ForumPostID);
            
            CreateTable(
                "dbo.Words",
                c => new
                    {
                        WordID = c.Int(nullable: false, identity: true),
                        Text = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.WordID)
                .Index(t => t.Text, unique: true);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ForumPostWords", "WordID", "dbo.Words");
            DropForeignKey("dbo.ForumPostWords", "ForumPostID", "dbo.ForumPosts");
            DropIndex("dbo.Words", new[] { "Text" });
            DropIndex("dbo.ForumPostWords", new[] { "ForumPostID" });
            DropIndex("dbo.ForumPostWords", new[] { "WordID" });
            DropTable("dbo.Words");
            DropTable("dbo.ForumPostWords");
        }
    }
}
