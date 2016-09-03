using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZkData;

namespace MigrateDatabase
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var db = new ZkDataContext(true);
                var acc = db.Accounts.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    }
}
