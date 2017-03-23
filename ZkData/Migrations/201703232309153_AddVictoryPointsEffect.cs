namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddVictoryPointsEffect : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TreatyEffectTypes", "EffectGiveVictoryPoints", c => c.Boolean());
        }
        
        public override void Down()
        {
            DropColumn("dbo.TreatyEffectTypes", "EffectGiveVictoryPoints");
        }
    }
}
