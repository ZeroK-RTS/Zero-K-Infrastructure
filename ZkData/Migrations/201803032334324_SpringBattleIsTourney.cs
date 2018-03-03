namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SpringBattleIsTourney : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SpringBattles", "IsTourney", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.SpringBattles", "IsTourney");
        }
    }
}
