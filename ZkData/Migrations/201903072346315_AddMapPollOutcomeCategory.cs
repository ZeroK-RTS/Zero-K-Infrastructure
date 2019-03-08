namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMapPollOutcomeCategory : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.MapPollOutcomes", "Category", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.MapPollOutcomes", "Category");
        }
    }
}
