namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLogoutTime : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "LastLogout", c => c.DateTime(nullable: false));
            DropColumn("dbo.LobbyChatHistories", "WasRead");
        }
        
        public override void Down()
        {
            AddColumn("dbo.LobbyChatHistories", "WasRead", c => c.Boolean(nullable: false));
            DropColumn("dbo.Accounts", "LastLogout");
        }
    }
}
