namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveSpringHashes : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ResourceSpringHashes", "ResourceID", "dbo.Resources");
            DropIndex("dbo.ResourceSpringHashes", new[] { "ResourceID" });
            DropTable("dbo.ResourceSpringHashes");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.ResourceSpringHashes",
                c => new
                    {
                        ResourceID = c.Int(nullable: false),
                        SpringVersion = c.String(nullable: false, maxLength: 50),
                        SpringHash = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.ResourceID, t.SpringVersion });
            
            CreateIndex("dbo.ResourceSpringHashes", "ResourceID");
            AddForeignKey("dbo.ResourceSpringHashes", "ResourceID", "dbo.Resources", "ResourceID", cascadeDelete: true);
        }
    }
}
