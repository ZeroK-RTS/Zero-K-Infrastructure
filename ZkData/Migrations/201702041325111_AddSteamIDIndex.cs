namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSteamIDIndex : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.Accounts", "SteamID");
        }
        
        public override void Down()
        {
            DropIndex("dbo.Accounts", new[] { "SteamID" });
        }
    }
}
