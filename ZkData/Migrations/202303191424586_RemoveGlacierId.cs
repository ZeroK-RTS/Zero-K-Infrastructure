namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveGlacierId : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.SpringBattles", "GlacierArchiveID");
        }
        
        public override void Down()
        {
            AddColumn("dbo.SpringBattles", "GlacierArchiveID", c => c.String(maxLength: 250));
        }
    }
}
