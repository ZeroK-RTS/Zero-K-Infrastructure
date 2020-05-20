namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixNonUniqueInstallIDs : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.AccountUserIDs");
            Sql("UPDATE dbo.AccountUserIDs SET InstallID = '' WHERE InstallID IS NULL");
            AlterColumn("dbo.AccountUserIDs", "InstallID", c => c.String(nullable: false, maxLength: 50));
            AddPrimaryKey("dbo.AccountUserIDs", new[] { "AccountID", "UserID", "InstallID" });
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.AccountUserIDs");
            AlterColumn("dbo.AccountUserIDs", "InstallID", c => c.String());
            AddPrimaryKey("dbo.AccountUserIDs", new[] { "AccountID", "UserID" });
        }
    }
}
