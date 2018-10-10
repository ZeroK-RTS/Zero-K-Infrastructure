namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddTourneyControl : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "IsTourneyController", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Accounts", "IsTourneyController");
        }
    }
}
