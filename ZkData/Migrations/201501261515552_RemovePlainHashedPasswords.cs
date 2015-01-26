namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovePlainHashedPasswords : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Accounts", "Password");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Accounts", "Password", c => c.String(maxLength: 100));
        }
    }
}
