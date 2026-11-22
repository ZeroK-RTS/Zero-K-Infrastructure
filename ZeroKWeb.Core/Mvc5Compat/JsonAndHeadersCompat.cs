using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace System.Web.Mvc
{
    /// <summary>
    /// MVC 5 refused to answer a GET with JSON unless the action said
    /// <c>JsonRequestBehavior.AllowGet</c>. It was a defence against JSON hijacking, a 2000s
    /// attack that relied on browsers letting a <c>&lt;script&gt;</c> tag read a JSON array
    /// cross-origin. Browsers closed that hole and ASP.NET Core dropped the switch, so
    /// <c>Json(x)</c> here already does what <c>Json(x, AllowGet)</c> did there.
    ///
    /// The enum and the overload exist so the 20-odd call sites that spell it - all of
    /// MapsController's one and AutocompleteController's nine, with more behind them - compile
    /// unedited. <c>DenyGet</c> is declared because the enum has two members, and is not
    /// honoured: nothing in this site passes it, and an overload that started returning 500 for
    /// a value nobody uses would be inventing behaviour rather than porting it.
    /// </summary>
    /// <summary>
    ///     **Not an enum, and the reason matters.** It was one, with an extension method
    ///     <c>Json(this Controller, object, JsonRequestBehavior)</c> beside it, and every call site
    ///     compiled. None of them called it: ASP.NET Core's <c>Controller</c> already declares
    ///     <c>Json(object value, object serializerSettings)</c>, and an instance method always beats
    ///     an extension method when both are applicable - an enum value is an <c>object</c>, so the
    ///     built-in won every time.
    ///
    ///     The call therefore passed <c>AllowGet</c> as the SERIALIZER SETTINGS, and every
    ///     autocomplete endpoint answered 500 at runtime: *"Property 'JsonResult.SerializerSettings'
    ///     must be an instance of type 'System.Text.Json.JsonSerializerOptions'"*. Nothing caught it
    ///     because nothing had ever requested one of those URLs.
    ///
    ///     So the shim stops fighting overload resolution and joins it: these are the settings now,
    ///     and they are null, which is what <c>Json(data)</c> passes and means "use the serializer
    ///     this application is configured with". The call sites are unchanged, MVC 5 still sees its
    ///     own enum, and the built-in overload does the right thing instead of throwing.
    /// </summary>
    public static class JsonRequestBehavior
    {
        public static readonly JsonSerializerOptions AllowGet = null;
        public static readonly JsonSerializerOptions DenyGet = null;
    }

    /// <summary>
    /// MVC 5's <c>HttpResponseBase.AddHeader</c>. MapsController's CORS filter calls it once.
    /// ASP.NET Core spells it <c>Headers.Append</c>, and this is an extension METHOD onto a
    /// method, so no call site has to move - unlike Server.MapPath, which was a property.
    /// </summary>
    public static class HttpResponseHeaderCompat
    {
        public static void AddHeader(this Microsoft.AspNetCore.Http.HttpResponse response, string name, string value)
            => response.Headers.Append(name, value);
    }
}
