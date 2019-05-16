namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDiscordNameDiscriminatorIDString : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Accounts", new[] { "DiscordID" });
            AddColumn("dbo.Accounts", "DiscordName", c => c.String());
            AddColumn("dbo.Accounts", "DiscordDiscriminator", c => c.String());
            AlterColumn("dbo.Accounts", "DiscordID", c => c.String());
            var indexName = "DiscordID";
            var tableName = "dbo.Accounts";
            var columnName = "DiscordID";

            Sql(string.Format(@"CREATE UNIQUE NONCLUSTERED INDEX {0} ON {1}({2}) WHERE {2} IS NOT NULL;", indexName, tableName, columnName));
        }
        
        public override void Down()
        {
            DropIndex("dbo.Accounts", new[] { "DiscordID" });
            AlterColumn("dbo.Accounts", "DiscordID", c => c.Decimal(precision: 38, scale: 0));
            DropColumn("dbo.Accounts", "DiscordDiscriminator");
            DropColumn("dbo.Accounts", "DiscordName");
            var indexName = "DiscordID";
            var tableName = "dbo.Accounts";
            var columnName = "DiscordID";

            Sql(string.Format(@"CREATE UNIQUE NONCLUSTERED INDEX {0} ON {1}({2}) WHERE {2} IS NOT NULL;", indexName, tableName, columnName));
        }
    }
}
