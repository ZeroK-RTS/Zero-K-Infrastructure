namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDynamicConfig : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DynamicConfigs",
                c => new
                    {
                        Key = c.Int(nullable: false),
                        MmBanSecondsIncrease = c.Int(nullable: false),
                        MmBanSecondsMax = c.Int(nullable: false),
                        MmBanReset = c.Int(nullable: false),
                        MmStartingWidth = c.Double(nullable: false),
                        MmWidthGrowth = c.Double(nullable: false),
                        MmWidthGrowthTime = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.Key);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.DynamicConfigs");
        }
    }
}
