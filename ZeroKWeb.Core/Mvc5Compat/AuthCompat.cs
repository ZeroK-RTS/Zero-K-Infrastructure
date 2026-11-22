using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ZeroKWeb;
using ZkData;

/// <summary>
/// The site's [Auth] attribute, ported.
///
/// The original (Zero-K.info/AppCode/Auth.cs) extends MVC 5's AuthorizeAttribute and does two
/// things in OnAuthorization: send an unauthenticated visitor to Home/NotLoggedIn with a
/// ReturnUrl, and, when a Role is named, answer 403 unless the account holds it. ASP.NET Core
/// has no AuthorizeAttribute to extend with that shape, so the logic is written against
/// IAuthorizationFilter instead - the same two decisions, in the same order.
///
/// This is the third time authorization has come up in this port, and the first time it is
/// implemented rather than deferred. The two earlier appearances stay as they were, for
/// reasons that have not changed: Global still reads the account out of HttpContext.Items
/// because nothing populates it yet, and child actions still throw rather than run an action
/// whose filters would be skipped. What this attribute does is make a linked controller's
/// declarations mean what they say.
///
/// It fails closed. With no authentication middleware, Global.Account is null, so every
/// [Auth] action redirects - which is what the original does for an anonymous visitor, not a
/// special case invented here.
///
/// Deliberately NOT global-namespace-free: the original is in the global namespace, which is
/// how ModsController and every other controller find it without a using.
/// </summary>
// MVC 5's AuthorizeAttribute, which the original derives from, is declared AllowMultiple = true,
// and MapBansController.Update relies on it - it carries [Auth] twice, once above its doc comment
// and once below. Without this the port fails with CS0579 on a file MVC 5 compiles happily. The
// shim mirrors the original rather than the source being cleaned up, because a duplicate filter
// runs twice and produces the same answer, while a shim that is stricter than the framework it
// stands in for silently narrows what the port can accept.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public class AuthAttribute : Attribute, IAuthorizationFilter
{
    public AdminLevel Role { get; set; }

    public void OnAuthorization(AuthorizationFilterContext filterContext)
    {
        var account = Global.Account;
        if (account == null)
        {
            var request = filterContext.HttpContext.Request;
            var redirectOnSuccess = request.PathBase + request.Path + request.QueryString;
            filterContext.Result = new RedirectToActionResult("NotLoggedIn", "Home",
                new { ReturnUrl = redirectOnSuccess.ToString() });
            return;
        }

        if (Role <= 0) return;

        // Account.IsInRole parses the name back to an AdminLevel and compares with >=, so a
        // Moderator satisfies a Role of Moderator and anything below it. The original walks
        // every flag in the enum and accepts if any matches; kept, flag for flag.
        var authorized = Enum.GetValues(typeof(AdminLevel))
            .Cast<AdminLevel>()
            .Where(option => (Role & option) > 0)
            .Any(option => account.IsInRole(option.ToString()));

        if (!authorized)
            filterContext.Result = new StatusCodeResult(403);
    }
}
