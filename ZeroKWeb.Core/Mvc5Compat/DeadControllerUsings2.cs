// Dead in ChartsController: named by a using, never mentioned again.
namespace System.Web.Helpers { internal static class DeadUsingMarker { } }

// EntityFramework.Extensions is NOT here, and nearly was. PlanetwarsAdminController's using
// looks dead by the same test - one occurrence, the using line - but the namespace supplies
// DbSet<T>.Delete() and IQueryable<T>.Update(), EF6's batch operations, which the controller
// calls six times without naming the namespace again. An empty shim would have let it compile
// and thrown at runtime. EF Core has ExecuteDelete/ExecuteUpdate, which are not the same API,
// so that controller needs porting rather than shimming.

// Dead in ForumPostIndexer: one using, and nothing from EF6's Infrastructure namespace -
// no DbEntityEntry, no DbChangeTracker, no DbPropertyValues. Checked by what the file uses,
// not by counting the name, which is the test that EntityFramework.Extensions failed.
namespace System.Data.Entity.Infrastructure { internal static class DeadUsingMarker { } }

// Dead in UsersController, checked by what the file uses rather than by counting the name:
// no SqlFunctions from the first, no RouteValueDictionary, RouteData or RouteTable from the
// second.
namespace System.Data.Entity.SqlServer { internal static class DeadUsingMarker { } }
namespace System.Web.Routing { internal static class DeadUsingMarker { } }

// Dead in MissionsController, checked by what the file uses rather than by counting the name:
// no HtmlTextWriter, no Page, nothing else from WebForms. It names the namespace once and then
// never touches it.
namespace System.Web.UI { internal static class DeadUsingMarker { } }

// Dead in MapsController, checked by what the file uses rather than by counting the name: no
// JavaScriptSerializer, no ScriptIgnore, nothing else from it. The controller serialises with
// XmlSerializer and Newtonsoft, both of which it names elsewhere.
namespace System.Web.Script.Serialization { internal static class DeadUsingMarker { } }

// Dead in SpotlightHandler, checked by what the file uses rather than by counting the name: the
// one cached thing goes through the site's own MemCache, not System.Web's Cache.
namespace System.Web.Caching { internal static class DeadUsingMarker { } }

// Dead in EnginesController, checked by what the file uses rather than by counting the name:
// no SortDirection, no Unit, nothing else from WebForms' control library.
namespace System.Web.UI.WebControls { internal static class DeadUsingMarker { } }
