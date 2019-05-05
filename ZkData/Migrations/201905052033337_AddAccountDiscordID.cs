namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAccountDiscordID : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "DiscordID", c => c.Decimal(precision: 38, scale: 0));
            var indexName = "DiscordID";
            var tableName = "dbo.Accounts";
            var columnName = "DiscordID";

            Sql(string.Format(@"CREATE UNIQUE NONCLUSTERED INDEX {0} ON {1}({2}) WHERE {2} IS NOT NULL;", indexName, tableName, columnName));
        }
        
        public override void Down()
        {
            DropIndex("dbo.Accounts", new[] { "DiscordID" });
            DropColumn("dbo.Accounts", "DiscordID");
        }
    }
}
