namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPopularMapsFraction : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.DynamicConfigs", "MapVoteFractionOfPopularMaps", c => c.Double(nullable: false, defaultValue: 0.5));
        }
        
        public override void Down()
        {
            DropColumn("dbo.DynamicConfigs", "MapVoteFractionOfPopularMaps");
        }
    }
}
