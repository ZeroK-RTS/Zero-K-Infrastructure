namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class EloRename : DbMigration
    {
        public override void Up()
        {
            RenameColumn("dbo.Accounts", "Elo1v1", "EloMm");
            RenameColumn("dbo.Accounts", "Elo1v1Weight", "EloMmWeight");
        }
        
        public override void Down()
        {
            RenameColumn("dbo.Accounts", "EloMm", "Elo1v1");
            RenameColumn("dbo.Accounts", "EloMmWeight", "Elo1v1Weight");
        }
    }
}
