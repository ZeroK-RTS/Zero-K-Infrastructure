using System.Linq;

namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPasswordBcrypt : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Accounts", "PasswordBcrypt", c => c.String(maxLength: 150));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Accounts", "PasswordBcrypt");
        }
    }
}
