namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SpringBattleIsCompetitive : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SpringBattles", "IsCompetitive", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.SpringBattles", "IsCompetitive");
        }
    }
}
