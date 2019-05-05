namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAccountDiscordID : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "DiscordID", c => c.Decimal(precision: 38, scale: 0));
            CreateIndex("dbo.Accounts", "DiscordID", unique: true);
        }
        
        public override void Down()
        {
            DropIndex("dbo.Accounts", new[] { "DiscordID" });
            DropColumn("dbo.Accounts", "DiscordID");
        }
    }
}
