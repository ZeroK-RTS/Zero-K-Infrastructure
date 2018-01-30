namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class HideCountry : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "HideCountry", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Accounts", "HideCountry");
        }
    }
}
