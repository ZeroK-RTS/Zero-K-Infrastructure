using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace ModStats
{
	public static class Global
	{
		public static ModStatsLinqDataContext Db
		{
			get
			{
				return DataContextFactory.GetScopedDataContext<ModStatsLinqDataContext>();
			}
		}

		public static void ExtractNameAndVersion(string fullName, out string name, out double version)
		{
			version = 0;
			name = fullName;
			var m = Regex.Match(fullName, "(.*[^0-9\\.]+)([0-9\\.]+)\\)*$");
			if (m.Success)
			{
				double.TryParse(m.Groups[2].Value, out version);
				name = m.Groups[1].Value;
			}
		}




	}
}
