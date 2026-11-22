using Microsoft.AspNetCore.Mvc;
using ZeroKWeb.Compat;
using ZkData;

namespace ZeroKWeb
{
    /// <summary>
    /// The .NET 9 half of the sign-in twin pair; the MVC 5 one is
    /// Zero-K.info/AppCode/AuthCookieCompat.cs and calls FormsAuthentication.
    ///
    /// This is the same ZkAuth the harness's own sign-in check already exercises, so HomeController
    /// signing a visitor in goes through the cookie scheme, the middleware that turns the cookie
    /// back into an Account, and the ban check - not a second, parallel notion of being signed in.
    ///
    /// Blocking on the Task is deliberate and confined: the two call sites are in a synchronous
    /// ActionResult that returns a redirect immediately afterwards, and SignInAsync on the cookie
    /// handler only writes a Set-Cookie header - it does no IO.
    /// </summary>
    public static class AuthCookieCompat
    {
        public static void SignInCompat(this Controller controller, Account account)
            => ZkAuth.SignIn(controller.HttpContext, account).GetAwaiter().GetResult();

        public static void SignOutCompat(this Controller controller)
            => ZkAuth.SignOut(controller.HttpContext).GetAwaiter().GetResult();
    }
}
