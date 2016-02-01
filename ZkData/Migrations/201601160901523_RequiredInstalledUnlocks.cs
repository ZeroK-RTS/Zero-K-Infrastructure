namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RequiredInstalledUnlocks : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Unlocks", "RequiredInstalledUnlockIDs", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Unlocks", "RequiredInstalledUnlockIDs");
        }
    }
}
