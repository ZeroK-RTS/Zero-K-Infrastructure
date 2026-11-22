using System.Web.Mvc;
using System.Web.Security;
using ZkData;

namespace ZeroKWeb
{
    /// <summary>
    /// The MVC 5 half of the sign-in twin pair. <c>FormsAuthentication</c> is a static over
    /// System.Web's pipeline and has no .NET 9 equivalent; the port signs in through ASP.NET
    /// Core's cookie authentication instead - see ZeroKWeb.Core/Mvc5Compat/AuthCookieCompat.cs.
    ///
    /// The seam takes the <see cref="Account"/> rather than its name, because that is what the
    /// port's ZkAuth.SignIn wants. Passing a name would have made the port look up the account
    /// again on a path the caller has already loaded it for.
    /// </summary>
    public static class AuthCookieCompat
    {
        public static void SignInCompat(this Controller controller, Account account)
        {
            FormsAuthentication.SetAuthCookie(account.Name, true);
        }

        public static void SignOutCompat(this Controller controller)
        {
            FormsAuthentication.SignOut();
        }
    }
}
