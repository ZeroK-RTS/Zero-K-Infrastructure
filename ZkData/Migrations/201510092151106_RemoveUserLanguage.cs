namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveUserLanguage : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Accounts", "Language");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Accounts", "Language", c => c.String(maxLength: 2));
        }
    }
}
