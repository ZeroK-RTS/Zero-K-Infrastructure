using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;

namespace ZkData
{
	partial class Account
	{
	
		partial void OnCreated()
		{
			FirstLogin = DateTime.UtcNow;
			Elo = 1500;
			EloWeight = 1;
		}

		partial void OnNameChanging(string value)
		{
			if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(value))
			{
				var aliases = new List<string>(Aliases.Split(','));
				if (!aliases.Contains(Name)) aliases.Add(Name);
				Aliases = string.Join(",", aliases);
			}
		}
	}
}
