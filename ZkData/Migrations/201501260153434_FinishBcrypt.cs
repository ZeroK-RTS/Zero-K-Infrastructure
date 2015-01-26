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

            List<Account> list;
            do
            {
                using (var db = new ZkDataContext(false))
                {
                    list = db.Accounts.Where(x => x.PasswordBcrypt == null && x.Password != null).Take(1000).ToList();
                    list.AsParallel().ForAll(x =>
                    {
                        x.PasswordBcrypt = BCrypt.Net.BCrypt.HashPassword(x.Password);
                    });
                    db.SaveChanges();
                }
            } while (list.Count > 0);
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Accounts", "Password", c => c.String(maxLength: 100, unicode: false));
        }
    }
}
