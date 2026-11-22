using System.Web;

namespace ZeroKWeb
{
    /// <summary>
    /// Call sites that MVC 5 and ASP.NET Core spell differently, behind one name - the same
    /// move as ZkData/DbCompat.cs, and for the same reason.
    ///
    /// MVC 5 puts the client address on the request as <c>UserHostAddress</c>; ASP.NET Core
    /// keeps it on the connection. C# has no extension properties, so the call site has to
    /// move rather than the API. This is that move, made once, with a twin in
    /// ZeroKWeb.Core/Mvc5Compat so a linked controller compiles against either framework.
    ///
    /// When the port completes, this file goes and the twin stays.
    /// </summary>
    public static class HttpCompat
    {
        public static string UserHostAddressCompat(this HttpRequestBase request) => request?.UserHostAddress;
    }
}
