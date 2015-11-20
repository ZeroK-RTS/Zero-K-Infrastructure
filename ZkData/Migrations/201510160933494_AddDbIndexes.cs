namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDbIndexes : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.Missions", "Name", unique: true);
            CreateIndex("dbo.Resources", "InternalName", unique: true);
        }
        
        public override void Down()
        {
            DropIndex("dbo.Resources", new[] { "InternalName" });
            DropIndex("dbo.Missions", new[] { "Name" });
        }
    }
}
