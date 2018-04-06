namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DlcListForAccounts : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "PurchasedDlc", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Accounts", "PurchasedDlc");
        }
    }
}
