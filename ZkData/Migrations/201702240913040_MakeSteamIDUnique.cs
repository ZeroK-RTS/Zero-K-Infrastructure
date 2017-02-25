namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MakeSteamIDUnique : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Accounts", new[] { "SteamID" });
            Sql("update dbo.Accounts set SteamID=null where SteamID is not null and adminlevel=0");

            var indexName = "SteamID";
            var tableName = "dbo.Accounts";
            var columnName = "SteamID";

            Sql(string.Format(@"CREATE UNIQUE NONCLUSTERED INDEX {0} ON {1}({2}) WHERE {2} IS NOT NULL;", indexName, tableName, columnName));
        }
        
        public override void Down()
        {
            DropIndex("dbo.Accounts", new[] { "SteamID" });
            CreateIndex("dbo.Accounts", "SteamID");
        }
    }
}
