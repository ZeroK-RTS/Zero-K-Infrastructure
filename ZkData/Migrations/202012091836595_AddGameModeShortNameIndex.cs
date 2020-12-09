namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddGameModeShortNameIndex : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.GameModes", "ShortName");
        }
        
        public override void Down()
        {
            DropIndex("dbo.GameModes", new[] { "ShortName" });
        }
    }
}
