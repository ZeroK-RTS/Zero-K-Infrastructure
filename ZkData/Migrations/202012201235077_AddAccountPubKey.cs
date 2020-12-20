namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAccountPubKey : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "LastPubKey", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Accounts", "LastPubKey");
        }
    }
}
