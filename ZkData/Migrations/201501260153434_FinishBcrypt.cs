using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ZkData.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FinishBcrypt : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Accounts", "Password", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Accounts", "Password", c => c.String(maxLength: 100, unicode: false));
        }
    }
}
