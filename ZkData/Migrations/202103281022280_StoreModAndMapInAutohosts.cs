namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class StoreModAndMapInAutohosts : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Autohosts", "ModName", c => c.String(maxLength: 255));
            AddColumn("dbo.Autohosts", "MapName", c => c.String(maxLength: 255));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Autohosts", "MapName");
            DropColumn("dbo.Autohosts", "ModName");
        }
    }
}
