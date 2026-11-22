using System;
using Microsoft.AspNetCore.Mvc.Filters;

// Global namespace, because Zero-K.info/AppCode/NoCache.cs declares it there and the
// controllers that carry [NoCache] are linked unmodified.

/// <summary>
/// The MVC 5 <c>NoCache</c> filter, reimplemented rather than stubbed.
///
/// Unlike the child-action shims next door, this one has an exact ASP.NET Core equivalent, so
/// there is nothing to defer and no reason to throw: the original sets response cache policy
/// through System.Web's HttpCachePolicy, which does not exist on .NET 9, but every call it
/// makes maps onto a response header.
///
///   SetCacheability(NoCache)      -> Cache-Control: no-cache, and Pragma: no-cache for HTTP/1.0
///   SetNoStore()                  -> Cache-Control: no-store
///   SetRevalidation(AllCaches)    -> Cache-Control: must-revalidate, proxy-revalidate
///   SetExpires(UtcNow.AddDays(-1)) and SetValidUntilExpires(false)
///                                 -> Expires, in the past
///
/// The Expires date is written from the same expression the original used rather than as a
/// fixed string, so the two builds send the same thing.
///
/// Four actions carry it, all of them in LobbyController and PlanetwarsController.
/// </summary>
public class NoCache : ActionFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext filterContext)
    {
        var headers = filterContext.HttpContext.Response.Headers;
        headers["Cache-Control"] = "no-cache, no-store, must-revalidate, proxy-revalidate";
        headers["Pragma"] = "no-cache";
        headers["Expires"] = DateTime.UtcNow.AddDays(-1).ToString("R");

        base.OnResultExecuting(filterContext);
    }
}
