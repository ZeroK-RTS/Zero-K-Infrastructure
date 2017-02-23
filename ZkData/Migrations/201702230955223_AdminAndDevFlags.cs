namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AdminAndDevFlags : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "DevLevel", c => c.Int(nullable: false));
            AddColumn("dbo.Accounts", "SpecialNote", c => c.String(maxLength: 200));
            AddColumn("dbo.Accounts", "AdminLevel", c => c.Int(nullable: false));
            Sql("update dbo.Accounts set AdminLevel = 1 where IsZeroKAdmin = 1");
            DropColumn("dbo.Accounts", "IsZeroKAdmin");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Accounts", "IsZeroKAdmin", c => c.Boolean(nullable: false));
            Sql("update dbo.Accounts set IsZeroKAdmin = 1 where AdminLevel > 0");
            DropColumn("dbo.Accounts", "AdminLevel");
            DropColumn("dbo.Accounts", "SpecialNote");
            DropColumn("dbo.Accounts", "DevLevel");
        }
    }
}
