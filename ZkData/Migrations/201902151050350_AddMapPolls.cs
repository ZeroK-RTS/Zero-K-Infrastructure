namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMapPolls : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.MapPollOptions",
                c => new
                    {
                        MapPollOptionID = c.Int(nullable: false, identity: true),
                        ResourceID = c.Int(nullable: false),
                        MapPollID = c.Int(nullable: false),
                        Votes = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.MapPollOptionID)
                .ForeignKey("dbo.MapPollOutcomes", t => t.MapPollID, cascadeDelete: true)
                .ForeignKey("dbo.Resources", t => t.ResourceID, cascadeDelete: true)
                .Index(t => t.ResourceID)
                .Index(t => t.MapPollID);
            
            CreateTable(
                "dbo.MapPollOutcomes",
                c => new
                    {
                        MapPollID = c.Int(nullable: false, identity: true),
                    })
                .PrimaryKey(t => t.MapPollID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.MapPollOptions", "ResourceID", "dbo.Resources");
            DropForeignKey("dbo.MapPollOptions", "MapPollID", "dbo.MapPollOutcomes");
            DropIndex("dbo.MapPollOptions", new[] { "MapPollID" });
            DropIndex("dbo.MapPollOptions", new[] { "ResourceID" });
            DropTable("dbo.MapPollOutcomes");
            DropTable("dbo.MapPollOptions");
        }
    }
}
