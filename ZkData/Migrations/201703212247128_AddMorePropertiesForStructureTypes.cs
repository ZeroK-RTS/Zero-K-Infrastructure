namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMorePropertiesForStructureTypes : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.StructureTypes", "EffectVictoryPointProduction", c => c.Double());
            AddColumn("dbo.StructureTypes", "EffectDisconnectedMetalMalus", c => c.Double());
            AddColumn("dbo.StructureTypes", "EffectDistanceMetalBonusMultiplier", c => c.Double());
            AddColumn("dbo.StructureTypes", "EffectDistanceMetalBonusMax", c => c.Double());
            AddColumn("dbo.StructureTypes", "EffectDistanceMetalBonusMin", c => c.Double());
            AddColumn("dbo.StructureTypes", "IsIngameEvacuable", c => c.Boolean(nullable: false));
            AddColumn("dbo.StructureTypes", "OwnerChangeWinsGame", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.StructureTypes", "OwnerChangeWinsGame");
            DropColumn("dbo.StructureTypes", "IsIngameEvacuable");
            DropColumn("dbo.StructureTypes", "EffectDistanceMetalBonusMin");
            DropColumn("dbo.StructureTypes", "EffectDistanceMetalBonusMax");
            DropColumn("dbo.StructureTypes", "EffectDistanceMetalBonusMultiplier");
            DropColumn("dbo.StructureTypes", "EffectDisconnectedMetalMalus");
            DropColumn("dbo.StructureTypes", "EffectVictoryPointProduction");
        }
    }
}
