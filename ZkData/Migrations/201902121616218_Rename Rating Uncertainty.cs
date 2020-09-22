namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RenameRatingUncertainty : DbMigration
    {
        public override void Up()
        {
            RenameColumn("dbo.AccountRatings", "Uncertainty", "EloStdev");
        }
        
        public override void Down()
        {
            RenameColumn("dbo.AccountRatings", "EloStdev", "Uncertainty");
        }
    }
}
