using System;
using System.Web;
using System.Web.Mvc;

public class NoCache: ActionFilterAttribute
{
  public override void OnResultExecuting(ResultExecutingContext filterContext)
  {
    var cache = filterContext.HttpContext.Response.Cache;
    cache.SetExpires(DateTime.UtcNow.AddDays(-1));
    cache.SetValidUntilExpires(false);
    cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
    cache.SetCacheability(HttpCacheability.NoCache);
    cache.SetNoStore();

    base.OnResultExecuting(filterContext);
  }
}