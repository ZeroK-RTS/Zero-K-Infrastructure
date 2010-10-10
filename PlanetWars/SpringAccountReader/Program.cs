using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ServiceData;

namespace SpringAccountReader
{
	class Program
	{
		static DateTime? ConvertTimestamp(double timestamp)
		{
			if (timestamp <= 0) return null;
			var converted = new DateTime(1970, 1, 1, 0, 0, 0, 0);
			var newDateTime = converted.AddSeconds(timestamp);
			return newDateTime.ToLocalTime();
		}

		static void Main(string[] args)
		{
			var db = new DbDataContext();

			var dict = db.SpringAccounts.ToDictionary(x => x.SpringAccountID);

			var path = args.Length > 0 ? args[0] : @"accounts.txt";
			using (var r = new StreamReader(path)) {
				string line;
				while ((line = r.ReadLine()) != null) {
					try {
						var parts = line.Split(' ');
						if (parts.Length < 9) {
							Trace.TraceError("Faulty line: ", line);
							continue;
						}

						var name = parts[0];
						var pass = parts[1];
						var flags = parts[2];
						var cookie = int.Parse(parts[3]);
						var lastLogin = ConvertTimestamp(double.Parse(parts[4])/1000);
						var lastIP = parts[5];
						var registered = ConvertTimestamp(double.Parse(parts[6])/1000);
						var country = parts[7];
						var id = int.Parse(parts[8]);

						SpringAccount de = null;
						dict.TryGetValue(id, out de);
						SpringAccount ac = null;

						Console.WriteLine(string.Format("{0} {1}", id, name));
						if (de == null || name != de.Name || pass != de.Password || cookie != de.UserCookie || lastIP != de.LastIP) {
							if (de == null) {
								ac = new SpringAccount();
								db.SpringAccounts.InsertOnSubmit(ac);
							}
							else ac = db.SpringAccounts.SingleOrDefault(x => x.SpringAccountID == id);

							ac.SpringAccountID = id;
							ac.Name = name;
							ac.Flags = flags;
							ac.Password = pass;
							ac.UserCookie = cookie;
							ac.LastLogin = lastLogin;
							ac.LastIP = lastIP;
							ac.Created = registered;
							ac.LastCountry = country;
							Console.Write(" CHANGED!");
							db.SubmitChanges();
						}
					} catch (Exception e) {
						Console.WriteLine("Problem importing line: {0}: {1}", line, e);
						db = new DbDataContext();
					}
				}
			}
		}
	}
}