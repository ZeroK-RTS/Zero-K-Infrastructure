namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCbalanceToggle : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Autohosts", "CbalEnabled", c => c.Boolean(nullable: false, defaultValue: true));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Autohosts", "CbalEnabled");
        }
    }
}
