namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPollIsvisible : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Polls", "IsVisible", c => c.Boolean(nullable: false, defaultValue:true));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Polls", "IsVisible");
        }
    }
}
