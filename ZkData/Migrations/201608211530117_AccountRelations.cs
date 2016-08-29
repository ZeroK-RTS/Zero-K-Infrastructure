namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AccountRelations : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AccountRelations",
                c => new
                    {
                        OwnerAccountID = c.Int(nullable: false),
                        TargetAccountID = c.Int(nullable: false),
                        Relation = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.OwnerAccountID, t.TargetAccountID })
                .ForeignKey("dbo.Accounts", t => t.TargetAccountID)
                .ForeignKey("dbo.Accounts", t => t.OwnerAccountID, cascadeDelete: true)
                .Index(t => t.OwnerAccountID)
                .Index(t => t.TargetAccountID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AccountRelations", "OwnerAccountID", "dbo.Accounts");
            DropForeignKey("dbo.AccountRelations", "TargetAccountID", "dbo.Accounts");
            DropIndex("dbo.AccountRelations", new[] { "TargetAccountID" });
            DropIndex("dbo.AccountRelations", new[] { "OwnerAccountID" });
            DropTable("dbo.AccountRelations");
        }
    }
}
