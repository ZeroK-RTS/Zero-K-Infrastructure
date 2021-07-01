namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDynConfMm1v1MinimumWinChance : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DynamicConfigs", "Mm1v1MinimumWinChance", c => c.Double(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "Mm1v1MinimumWinChance");
        }
    }
}
