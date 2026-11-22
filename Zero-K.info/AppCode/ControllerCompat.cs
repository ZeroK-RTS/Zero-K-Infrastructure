using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web.Mvc;

namespace ZeroKWeb
{
    /// <summary>
    /// Controller members that MVC 5 and ASP.NET Core spell differently, behind one name - the
    /// same move as HttpCompat, DbCompat and HtmlCompat.
    ///
    /// MVC 5 puts <c>Server</c> on the controller; ASP.NET Core has no such property, and C# has
    /// no extension properties, so <c>Server.MapPath(x)</c> cannot be shimmed where it stands.
    /// The call site has to move instead, which is what the ten edits in ClansController,
    /// NewsController and LobbyNewsController are.
    ///
    /// When the port completes, this file goes and the twin in ZeroKWeb.Core stays.
    /// </summary>
    public static class ControllerCompat
    {
        public static string MapPath(this Controller controller, string virtualPath)
            => controller.Server.MapPath(virtualPath);
    }
    /// <summary>
    /// The MVC 5 half of ZeroKWeb.Core/Mvc5Compat/RequestBodyCompat.cs. Both members already exist
    /// here as properties, so these only give them a name an extension method can carry - a member
    /// always beats a same-named extension, which is why the port cannot simply supply InputStream.
    /// </summary>
    public static class RequestBodyCompat
    {
        public static Stream RequestInputStream(this Controller controller)
        {
            return controller.Request.InputStream;
        }

        public static IEnumerable<string> AllKeysCompat(this NameValueCollection form)
        {
            return form.AllKeys;
        }

        /// <summary>
        /// The twin of the ASP.NET Core BorrowController. Note it sets ControllerContext, which
        /// DependencyResolver does not: a controller resolved this way has none, and SubmitPost -
        /// the only thing this is used for - reads Request. See the note in the Core half.
        /// </summary>
        /// <summary>
        /// The MVC 5 half of the Core ReadIpnRequest. Params is read BEFORE BinaryRead, which is the
        /// order the call site used before it moved in here - ASP.NET buffers the entity body and the
        /// two reads interact, so the order is preserved rather than tidied.
        /// </summary>
        public static NameValueCollection ReadIpnRequest(this Controller controller, out byte[] raw)
        {
            var values = controller.Request.Params;
            raw = controller.Request.BinaryRead(controller.Request.ContentLength);
            return values;
        }

        public static T BorrowController<T>(this Controller caller) where T : Controller
        {
            var borrowed = (T)DependencyResolver.Current.GetService(typeof(T));
            borrowed.ControllerContext = caller.ControllerContext;
            return borrowed;
        }
    }

}
