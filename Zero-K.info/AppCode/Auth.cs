using System;
using System.Web.Mvc;
using ZkData;

[Flags]
public enum AuthRole
{
	ZkAdmin = 1,
	LobbyAdmin = 2
}

public class AuthAttribute: AuthorizeAttribute
{
	public AuthRole Role { get; set; }

	protected override bool AuthorizeCore(System.Web.HttpContextBase httpContext)
	{
		
		//redirect if the user is not authenticated
		if (!httpContext.User.Identity.IsAuthenticated || !(httpContext.User is Account))
		{
			//use the current url for the redirect
			var redirectOnSuccess = httpContext.Request.Url.AbsolutePath;

			//send them off to the login page
			var helper = new UrlHelper(httpContext.Request.RequestContext);
			var loginUrl = helper.Action("NotLoggedIn", "Home", new { ReturnUrl = redirectOnSuccess });
			httpContext.Response.Redirect(loginUrl, true);
			return false;
		}
		else
		{
			if (Role > 0)
			{
				var us = httpContext.User;
				var isAuthorized = false;

				foreach (AuthRole option in Enum.GetValues(typeof(AuthRole))) if ((Role & option) > 0) if (us.IsInRole(option.ToString())) isAuthorized = true;
				if (!isAuthorized) throw new UnauthorizedAccessException("You are not authorized to view this page");
			}
		}
		return true;
	}
}