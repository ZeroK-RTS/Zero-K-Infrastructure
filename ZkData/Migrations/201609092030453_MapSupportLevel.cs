namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MapSupportLevel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Resources", "MapSupportLevel", c => c.Int(nullable: false));
            Sql("update dbo.Resources set MapSupportLevel=1 where MapIsSupported=1");
            Sql("update dbo.Resources set MapSupportLevel=2 where FeaturedOrder is not null");
            DropColumn("dbo.Resources", "MapIsSupported");
            DropColumn("dbo.Resources", "FeaturedOrder");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Resources", "FeaturedOrder", c => c.Single());
            AddColumn("dbo.Resources", "MapIsSupported", c => c.Boolean());
            DropColumn("dbo.Resources", "MapSupportLevel");
        }
    }
}
