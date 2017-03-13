namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveNewLastRead : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Accounts", "LastNewsRead");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Accounts", "LastNewsRead", c => c.DateTime());
        }
    }
}
