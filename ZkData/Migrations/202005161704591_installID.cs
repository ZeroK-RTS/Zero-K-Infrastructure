namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class installID : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AccountUserIDs", "InstallID", c => c.String());
            AddColumn("dbo.Punishments", "InstallID", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Punishments", "InstallID");
            DropColumn("dbo.AccountUserIDs", "InstallID");
        }
    }
}
