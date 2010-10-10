using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace ModelBase
{
	public class ModelBasePrincipal:IPrincipal {
		public bool IsInRole(string role)
		{
			return true;
		}

		public IIdentity Identity {get;protected set;}


		public ModelBasePrincipal(User user)
		{
			Identity = new ModelBaseIdentity(user);
		}
	}

	public class ModelBaseIdentity:IIdentity
	{
		public User User { get; protected set; }
		public string Name
		{
			get { return User.Login; }
		}

		public string AuthenticationType
		{
			get { return "default"; }
		}

		public bool IsAuthenticated
		{
			get { return true; }
		}

		public ModelBaseIdentity(User user)
		{
			User = user;
		}
	}


}
