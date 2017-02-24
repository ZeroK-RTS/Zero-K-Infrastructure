namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MakeSteamIDUnique : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Accounts", new[] { "SteamID" });
            Sql("update dbo.Accounts set SteamID=null where SteamID is not null and adminlevel>0");
            CreateIndex("dbo.Accounts", "SteamID", unique: true);
        }
        
        public override void Down()
        {
            DropIndex("dbo.Accounts", new[] { "SteamID" });
            CreateIndex("dbo.Accounts", "SteamID");
        }
    }
}
