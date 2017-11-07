namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddEffectPreventInfluenceDecay : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.StructureTypes", "EffectPreventInfluenceDecayBelow", c => c.Double());
        }
        
        public override void Down()
        {
            DropColumn("dbo.StructureTypes", "EffectPreventInfluenceDecayBelow");
        }
    }
}
