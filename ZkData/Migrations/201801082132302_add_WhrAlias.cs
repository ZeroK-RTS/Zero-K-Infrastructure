namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class add_WhrAlias : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "WhrAlias", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Accounts", "WhrAlias");
        }
    }
}
