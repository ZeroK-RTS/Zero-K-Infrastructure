using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceData
{
	partial class SpringAccount
	{

		public static string HashPassword(string password) {
			var md5 = System.Security.Cryptography.MD5.Create();
			var data = md5.ComputeHash(Encoding.ASCII.GetBytes(password));
			return Convert.ToBase64String(data);
		}
	}

}
