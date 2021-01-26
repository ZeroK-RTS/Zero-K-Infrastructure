namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddGameModeShortcutUniqueIndex : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.GameModes", new[] { "ShortName" });
            CreateIndex("dbo.GameModes", "ShortName", unique: true);
        }
        
        public override void Down()
        {
            DropIndex("dbo.GameModes", new[] { "ShortName" });
            CreateIndex("dbo.GameModes", "ShortName");
        }
    }
}
