using System;
using System.Net;
using System.Web.Mvc;
using ZeroKWeb;
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

    public override void OnAuthorization(AuthorizationContext filterContext)
    {
        base.OnAuthorization(filterContext);
        var httpContext = filterContext.HttpContext;
        //redirect if the user is not authenticated
        if (!httpContext.User.Identity.IsAuthenticated || !(httpContext.User is Account))
        {
            //use the current url for the redirect
            var redirectOnSuccess = httpContext.Request.Url.PathAndQuery;

            //send them off to the login page
            var helper = Global.UrlHelper();
            var loginUrl = helper.Action("NotLoggedIn", "Home", new { ReturnUrl = redirectOnSuccess });
            filterContext.Result = new RedirectResult(loginUrl, false);
        }
        else
        {
            if (Role > 0)
            {
                var us = httpContext.User;
                var isAuthorized = false;

                foreach (AuthRole option in Enum.GetValues(typeof(AuthRole))) if ((Role & option) > 0) if (us.IsInRole(option.ToString())) isAuthorized = true;
                if (!isAuthorized)
                    filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden, "You are not authorized to view this page");
            }
        }
    }
}