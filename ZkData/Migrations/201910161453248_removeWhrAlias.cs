namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class removeWhrAlias : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Accounts", "WhrAlias");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Accounts", "WhrAlias", c => c.Int(nullable: false));
        }
    }
}
