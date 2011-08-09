using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PlasmaShared;
using ZkData;

namespace SpringAccountReader
{
  class Program
  {
    static DateTime? ConvertTimestamp(double timestamp)
    {
      if (timestamp <= 0) return null;
      var converted = new DateTime(1970, 1, 1, 0, 0, 0, 0);
      var newDateTime = converted.AddSeconds(timestamp);
      if (newDateTime == DateTime.MinValue) return null;
      return newDateTime.ToUniversalTime();
    }

    static void Main(string[] args)
    {
      var db = new ZkDataContext();

      var dict = db.Accounts.ToDictionary(x => x.AccountID);

      var path = args.Length > 0 ? args[0] : @"accounts.txt";
      using (var r = new StreamReader(path)) {
        string line;
        while ((line = r.ReadLine()) != null) {
          Account ac = null;
          try {
            var parts = line.Split(' ');
            if (parts.Length < 9) {
              Trace.TraceError("Faulty line: ", line);
              continue;
            }

            var name = parts[0];
            var pass = parts[1];
            var flags = parts[2];
            //var cookie = int.Parse(parts[3]);
            var lastLogin = ConvertTimestamp(double.Parse(parts[4]) / 1000);
            var lastIP = parts[5];
            var registered = ConvertTimestamp(double.Parse(parts[6]) / 1000);
            var country = parts[7];
            var id = int.Parse(parts[8]);

            Account de = null;
            dict.TryGetValue(id, out de);

            Console.WriteLine(string.Format("{0} {1}", id, name));
            if (de == null || name != de.Name || pass != de.Password || registered != de.FirstLogin ) {
              if (de == null) {
                ac = new Account();
                db.Accounts.InsertOnSubmit(ac);
              } else ac = db.Accounts.SingleOrDefault(x => x.LobbyID == id);

              ac.LobbyID = id;
              ac.Name = name;
              //ac.Flags = flags;
              ac.Password = pass;
              //ac.UserCookie = cookie;
              if (lastLogin.HasValue) ac.LastLogin = lastLogin.Value;
              //ac.LastIP = lastIP;
              if (registered.HasValue) ac.FirstLogin = registered.Value;
              if (ac.LastLogin == DateTime.MinValue) ac.LastLogin = registered ?? DateTime.UtcNow;
              //ac.Created = registered;
              ac.Country = country;
              Console.Write(" CHANGED!");
              db.SubmitChanges();
            }
          } catch (Exception e) {
            Console.WriteLine("Problem importing line: {0}: {1}", line, e);
            db = new ZkDataContext();
          }
        }
      }
    }
  }
}