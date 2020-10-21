namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAutohostApplicableRating : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Autohosts", "ApplicableRating", c => c.Int(nullable: false, defaultValue: 1));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Autohosts", "ApplicableRating");
        }
    }
}
