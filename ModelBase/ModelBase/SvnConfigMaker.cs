using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace ModelBase
{
	public class SvnConfigMaker
	{
		private string savePath;

		public SvnConfigMaker(string savePath)
		{
			this.savePath = savePath;
		}

		public SvnConfigMaker()
		{
			savePath = ConfigurationManager.AppSettings["SvnConfigPath"];
		}


		public void Generate()
		{
			var authz = new StringBuilder();
			var passwd = new StringBuilder();


			passwd.Append("[users]\n");
			passwd.Append("admin = sasl\n");
			foreach (var user in from x in Global.Db.Users where !x.IsDeleted select x) {
				passwd.AppendFormat("{0} = {1}\n", user.Login, user.PasswordText);
			}

			authz.Append("[/]\n");
			authz.Append("* = r\n");
			authz.Append("admin = rw\n");
			foreach (var user in from x in Global.Db.Users where !x.IsDeleted && x.IsAdmin select x)
			{
				authz.AppendFormat("{0} = rw\n", user.Login);
			}

			foreach (var user in from x in Global.Db.Users where !x.IsDeleted && !x.IsAdmin select x)
			{
				authz.AppendFormat("\n[/{0}]\n", user.Login);
				authz.AppendFormat("{0} = rw\n", user.Login);
			}

			File.WriteAllText(savePath + "authz", authz.ToString());
			File.WriteAllText(savePath + "passwd", passwd.ToString());
		}

	}
}
