using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace ZkData
{
	partial class Account: IPrincipal, IIdentity
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
				List<string> aliases = null;
				if (!string.IsNullOrEmpty(Aliases)) aliases = new List<string>(Aliases.Split(','));
				else aliases = new List<string>();

				if (!aliases.Contains(Name)) aliases.Add(Name);
				Aliases = string.Join(",", aliases.ToArray());
			}
		}

		public string AuthenticationType { get { return "LobbyServer"; } }
		public bool IsAuthenticated { get { return true; } }

		public bool IsInRole(string role)
		{
			if (role == "admin" || role == "moderator") return IsLobbyAdministrator;
      if (role == "user") return true;
      else return string.IsNullOrEmpty(role);
		}

		public IIdentity Identity { get { return this; } }
	}
}