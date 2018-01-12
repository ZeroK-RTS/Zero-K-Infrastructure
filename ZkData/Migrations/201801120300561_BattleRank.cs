namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class BattleRank : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SpringBattles", "Rank", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.SpringBattles", "Rank");
        }
    }
}
