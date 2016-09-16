namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMatchMakerFlagToBattles : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SpringBattles", "IsMatchMaker", c => c.Boolean(nullable: false));
            DropColumn("dbo.SpringBattles", "TeamsTitle");
            DropColumn("dbo.SpringBattles", "IsFfa");
            DropColumn("dbo.SpringBattles", "RatingPollID");
        }
        
        public override void Down()
        {
            AddColumn("dbo.SpringBattles", "RatingPollID", c => c.Int());
            AddColumn("dbo.SpringBattles", "IsFfa", c => c.Boolean(nullable: false));
            AddColumn("dbo.SpringBattles", "TeamsTitle", c => c.String(maxLength: 250));
            DropColumn("dbo.SpringBattles", "IsMatchMaker");
        }
    }
}
