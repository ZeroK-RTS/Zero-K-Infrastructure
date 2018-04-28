namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class StoreOnlyKudoExistence : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "HasKudos", c => c.Boolean(nullable: false));
            DropColumn("dbo.Accounts", "Kudos");
            Sql("update dbo.Accounts set HasKudos = 1 where (select count(*) from Contributions where accounts.AccountID = Contributions.AccountID) > 0");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Accounts", "Kudos", c => c.Int(nullable: false));
            DropColumn("dbo.Accounts", "HasKudos");
        }
    }
}
