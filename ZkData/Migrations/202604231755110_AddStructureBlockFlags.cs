namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddStructureBlockFlags : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.StructureTypes", "EffectBlocksBombers", c => c.Boolean());
            AddColumn("dbo.StructureTypes", "EffectBlocksInvasion", c => c.Boolean());

            // HQ-class structures (those that win the game on capture) become immune to both attack vectors by default.
            Sql("UPDATE dbo.StructureTypes SET EffectBlocksBombers = 1, EffectBlocksInvasion = 1 WHERE OwnerChangeWinsGame = 1");
        }
        
        public override void Down()
        {
            DropColumn("dbo.StructureTypes", "EffectBlocksInvasion");
            DropColumn("dbo.StructureTypes", "EffectBlocksBombers");
        }
    }
}
