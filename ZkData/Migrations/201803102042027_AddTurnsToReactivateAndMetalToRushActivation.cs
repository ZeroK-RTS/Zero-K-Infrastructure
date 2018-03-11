namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddTurnsToReactivateAndMetalToRushActivation : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.StructureTypes", "TurnsToReactivate", c => c.Int());
            AddColumn("dbo.StructureTypes", "MetalToRushActivation", c => c.Int());
            Sql("update StructureTypes set TurnsToReactivate = TurnsToActivate * 2 where turnstoactivate <> null");
            Sql("update StructureTypes set MetalToRushActivation = Cost where turnstoactivate <> null");
        }
        
        public override void Down()
        {
            DropColumn("dbo.StructureTypes", "MetalToRushActivation");
            DropColumn("dbo.StructureTypes", "TurnsToReactivate");
        }
    }
}
