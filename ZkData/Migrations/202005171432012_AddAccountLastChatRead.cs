namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAccountLastChatRead : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "LastChatRead", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Accounts", "LastChatRead");
        }
    }
}
