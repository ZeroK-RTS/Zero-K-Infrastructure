using System;

namespace System.Web.Mvc
{
    /// <summary>
    /// MVC 5's <c>[ValidateInput(false)]</c>, which switched off ASP.NET's request validation
    /// for an action so it could accept HTML - ForumController needs it for post bodies.
    ///
    /// ASP.NET Core has no request validation to switch off. So this attribute does nothing,
    /// and doing nothing is the faithful translation *for the actions that carry it*: they
    /// asked for input not to be rejected, and it is not rejected.
    ///
    /// The difference is on the actions that do NOT carry it. Under MVC 5 those were
    /// protected by the platform; under ASP.NET Core nothing rejects dangerous-looking input
    /// anywhere, so that protection is gone site-wide. That is not caused by this shim and
    /// cannot be fixed by it - it is the same ground as the Web.config finding in §4 of the
    /// modernization plan, where requestValidationMode="2.0" was already weakening it. Worth
    /// deciding about deliberately rather than inheriting.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ValidateInputAttribute : Attribute
    {
        public ValidateInputAttribute(bool enableValidation) => EnableValidation = enableValidation;

        public bool EnableValidation { get; }
    }
}
