namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddTreatyDbValuesForGuaranteeAndUnableToTradeMode : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.FactionTreaties", "ProposingFactionGuarantee", c => c.Int());
            AddColumn("dbo.FactionTreaties", "AcceptingFactionGuarantee", c => c.Int());
            AddColumn("dbo.FactionTreaties", "TreatyUnableToTradeMode", c => c.Int(nullable: false));
            AddColumn("dbo.PlanetStructures", "ActivationTurnCounter", c => c.Int());
            AddColumn("dbo.PlanetStructures", "TurnsToActivateOverride", c => c.Int());
            DropColumn("dbo.PlanetStructures", "ActivatedOnTurn");
        }
        
        public override void Down()
        {
            AddColumn("dbo.PlanetStructures", "ActivatedOnTurn", c => c.Int());
            DropColumn("dbo.PlanetStructures", "TurnsToActivateOverride");
            DropColumn("dbo.PlanetStructures", "ActivationTurnCounter");
            DropColumn("dbo.FactionTreaties", "TreatyUnableToTradeMode");
            DropColumn("dbo.FactionTreaties", "AcceptingFactionGuarantee");
            DropColumn("dbo.FactionTreaties", "ProposingFactionGuarantee");
        }
    }
}
