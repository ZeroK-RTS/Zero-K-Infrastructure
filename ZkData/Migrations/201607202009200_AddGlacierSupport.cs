namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddGlacierSupport : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SpringBattles", "GlacierArchiveID", c => c.String(maxLength: 250));
            CreateIndex("dbo.SpringBattles", "ReplayFileName");
        }
        
        public override void Down()
        {
            DropIndex("dbo.SpringBattles", new[] { "ReplayFileName" });
            DropColumn("dbo.SpringBattles", "GlacierArchiveID");
        }
    }
}
