// not-in-a-project: compiled by tools/ajax-ground-truth/capture.sh under mono, against MVC 5
// Prints the HTML that MVC 5's AjaxHelper actually emits, for the exact call shapes
// Zero-K.info uses. Run under mono against the real System.Web.Mvc 5.2.3 - see capture.sh.
//
// This exists because the alternative is transcribing markup from memory, and transcription
// has already put two defects into this port's helpers (a Math.Floor that should not be there,
// a dropped style attribute). The port's AjaxHelper is checked against this output rather than
// against anyone's recollection of it.

using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using System.Web.Mvc.Html;
using System.Web.Routing;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

public static class Capture
{
    public static void Main()
    {
        // Otherwise mono writes non-ASCII as '?' and the capture quietly lies about encoding,
        // which is the one thing this harness exists to get right.
        Console.OutputEncoding = new System.Text.UTF8Encoding(false);

        var routes = new RouteCollection();
        routes.MapRoute("Default", "{controller}/{action}/{id}",
            new { controller = "Home", action = "Index", id = UrlParameter.Optional });

        // Global.GetAjaxOptions("events"), which is how most of the site calls this.
        var siteOptions = new AjaxOptions
        {
            UpdateTargetId = "events",
            OnComplete = "GlobalPageInit($('#events'))",
            OnSuccess = "ReplaceHistory($('#events').find('form').serialize())",
        };

        // Planetwars/Events.cshtml, which spells its options out.
        var eventsOptions = new AjaxOptions
        {
            InsertionMode = InsertionMode.Replace,
            UpdateTargetId = "events",
            LoadingElementId = "ajaxScrollProgress",
        };

        // My/Commanders.cshtml and Poll/PollView.cshtml add a method.
        var postOptions = new AjaxOptions
        {
            UpdateTargetId = "com1",
            InsertionMode = InsertionMode.Replace,
            HttpMethod = "post",
            LoadingElementId = "busy",
        };

        // Encoding, which the port cannot guess: MVC 5 attribute-encodes with System.Web's rules
        // and ASP.NET Core's HtmlEncoder does not agree with them. Every character that might
        // differ is put through it here rather than reasoned about.
        var encodingOptions = new AjaxOptions
        {
            UpdateTargetId = "t",
            OnComplete = "a < b && c > d \" ' \u00fc \u00a9 \u4e2d",
        };
        Emit(routes, "encoding", (ajax, writer) => ajax.BeginForm("E", encodingOptions));

        Emit(routes, "BeginForm(action, routeValues, options)", (ajax, writer) =>
            ajax.BeginForm("Events", new { accountID = 42, partial = true, pageSize = 40 }, eventsOptions));

        Emit(routes, "BeginForm(action, options)", (ajax, writer) =>
            ajax.BeginForm("Index", siteOptions));

        Emit(routes, "BeginForm(action, controller, options)", (ajax, writer) =>
            ajax.BeginForm("Index", "Clans", siteOptions));

        Emit(routes, "BeginForm(action, controller, routeValues, options, htmlAttributes)", (ajax, writer) =>
            ajax.BeginForm("MatchMaker", "PlanetWars", null, siteOptions, new { id = "mmForm" }));

        Emit(routes, "BeginForm(action, routeValues, options) with method", (ajax, writer) =>
            ajax.BeginForm("CommanderProfile", new { profileNumber = 1 }, postOptions));

        // PostLink, the CSRF-safe link the site uses for state-changing actions. Same reason as
        // the Ajax shapes: its ASP.NET Core twin has to emit the same html, and MVC 5's TagBuilder
        // is not ASP.NET Core's - InnerHtml is a string on one and an IHtmlContentBuilder on the
        // other, and ToString(TagRenderMode) does not exist there at all.
        EmitHtml(routes, "PostLink(text, action)", html =>
            html.PostLink("Delete", "Delete").ToString());

        EmitHtml(routes, "PostLink(text, action, controller, routeValues)", html =>
            html.PostLink("Select", "SetDefault", "Planetwars", new { galaxyID = 7 }).ToString());

        EmitHtml(routes, "PostLink with cssClass and nicetitle", html =>
            html.PostLink("Delete", "Delete", null, new { id = 3 }, "js_confirm", "Really?").ToString());

        // The attribute lands between "class" and "nicetitle" because TagBuilder sorts, which is
        // the kind of thing only a capture settles.
        EmitHtml(routes, "PostLink with a confirm message", html =>
            html.PostLink("Activate", "Fire", null, new { id = 3 }, null, null,
                          "WARNING: it's gone after this. Proceed?").ToString());

        EmitHtml(routes, "PostLink encodes its text", html =>
            html.PostLink("a < b & c \" d ' e", "Act").ToString());

        EmitHtml(routes, "PostImageLink(src, height, action)", html =>
            html.PostImageLink("/img/x.png", 24, "Act", "Ctrl", new { id = 1 }).ToString());

        EmitHtml(routes, "PostImageLink with height 0 omits the attribute", html =>
            html.PostImageLink("/img/x.png", 0, "Act").ToString());

        EmitString(routes, "ActionLink(text, action, routeValues, options)", ajax =>
            ajax.ActionLink("Join", "MatchMakerJoin", new { planetID = 7, attackerFaction = "Dyn" },
                new AjaxOptions { UpdateTargetId = "matchMaker", InsertionMode = InsertionMode.Replace }).ToString());

        // EnumDropDownListFor, at fourteen call sites across seven views. ASP.NET Core dropped
        // it, so the port has to re-emit it, and every question the port would otherwise guess
        // at is asked here instead: what an option's VALUE is (the name or the number), whether
        // [Description] counts, whether [Display] counts, what a nullable enum adds, and where
        // the selected="selected" lands.
        //
        // [Description] is the one that matters most for Zero-K: PlanetWarsModes,
        // TreatyUnableToTradeMode, AutohostMode and Account.Level all carry it, and NONE of them
        // carry [Display]. If MVC 5 ignores it, the live site has been showing field names all
        // along and the port must show field names too - a "nicer" port would be a regression.
        var model = new EnumModel();
        EmitEnum(routes, "EnumDropDownListFor(plain enum)", model, m => m.Plain);
        EmitEnum(routes, "EnumDropDownListFor(enum with [Description])", model, m => m.Described);
        EmitEnum(routes, "EnumDropDownListFor(enum with [Display])", model, m => m.Displayed);
        EmitEnum(routes, "EnumDropDownListFor(nullable enum, null)", model, m => m.NullableEmpty);
        EmitEnum(routes, "EnumDropDownListFor(nullable enum, with a value)", model, m => m.NullableSet);
        EmitEnum(routes, "EnumDropDownListFor(non-default selected)", model, m => m.Selected);
        EmitEnumAttrs(routes, "EnumDropDownListFor(expression, htmlAttributes)", model, m => m.Plain,
            new { @class = "width-100" });

        // Declaration order and numeric order agree in every enum above, so none of them can
        // tell which one MVC 5 uses. This one disagrees on purpose.
        EmitEnum(routes, "EnumDropDownListFor(declaration order != numeric order)", model, m => m.Unordered);

        // The site's own Select, MultiSelectFor and EnumCheckboxesFor build their markup from
        // format strings that are plain to read in HtmlHelperExtensions.cs, so the markup is not
        // what needs capturing. What does is the pair of MVC 5 APIs they lean on and ASP.NET
        // Core does not have: what ExpressionHelper.GetExpressionText makes of a lambda, and
        // what ModelMetadata.FromLambdaExpression gives back as .Model. The port reaches those
        // through NameFor and a compiled expression, and whether that agrees is not obvious.
        // Server.HtmlEncode, which Shared/UserDetail.cshtml calls. MVC 5's is
        // HttpUtility.HtmlEncode; the port would use WebUtility.HtmlEncode, and the two are
        // only KNOWN to agree on ASCII punctuation - PostLink established that much. Whether
        // they agree on characters above 127 is the open question, and guessing it wrong would
        // mangle every commander name with an accent in it.
        Console.WriteLine("### HttpUtility.HtmlEncode");
        Console.WriteLine(HttpUtility.HtmlEncode("a < b & c \" d ' e > f \u00fc \u00a9 \u4e2d"));

        // Maps/Detail calls Html.DropDownList(name, EnumHelper.GetSelectList(type, value)).
        // Both halves are MVC 5 framework code the port has to reproduce, and one capture
        // settles them together: what GetSelectList makes of an enum, and what DropDownList
        // makes of the list. ASP.NET Core has a DropDownList of its own, and it is NOT the
        // same markup - so this is what the port is written against.
        EmitDropDown(routes, "DropDownList(EnumHelper.GetSelectList(enum, value))",
            typeof(Support), Support.Featured);
        EmitDropDown(routes, "DropDownList(EnumHelper.GetSelectList(enum, null))",
            typeof(Support), null);
        // Support starts at 0, so the line above cannot tell "selects the value 0" from
        // "selects the first item". Plain starts at 1 and separates them.
        EmitDropDown(routes, "DropDownList(EnumHelper.GetSelectList(enum starting at 1, null))",
            typeof(Plain), null);
        // ... and whether that synthesised entry is about the NULL or about the enum having no
        // zero member. Same enum, a real value.
        EmitDropDown(routes, "DropDownList(EnumHelper.GetSelectList(enum starting at 1, value))",
            typeof(Plain), Plain.Planetwars);

        var listModel = new ListModel { UserId = new List<int> { 4, 11 } };
        EmitExpression(routes, "ExpressionHelper.GetExpressionText(x => x.UserId)", listModel, m => m.UserId);
        EmitExpression(routes, "ExpressionHelper.GetExpressionText(x => x.Types)", listModel, m => m.Types);
    }

    // Mirrors the SHAPES the site's enums have, not any one of them: a non-zero-based enum
    // like RatingCategory, a [Description]-annotated one like PlanetWarsModes, a nullable one
    // like LaddersMapsModel.SupportLevel.
    public enum Plain { Casual = 1, MatchMaking = 2, Planetwars = 4 }

    public enum Described
    {
        [Description("offline")] AllOffline = 0,
        [Description("pre-game")] PreGame = 1,
        [Description("running")] Running = 2,
    }

    public enum Displayed
    {
        [Display(Name = "shown first")] First = 0,
        Second = 1,
    }

    public enum Support { None = 0, Supported = 1, Featured = 2, MatchMaker = 3 }

    public enum Unordered { Third = 30, First = 10, Second = 20 }

    public class ListModel
    {
        public IList<int> UserId { get; set; } = new List<int>();
        public IList<Support> Types { get; set; } = new List<Support> { Support.Featured };
    }

    public class EnumModel
    {
        public Plain Plain { get; set; } = Plain.Casual;
        public Described Described { get; set; } = Described.AllOffline;
        public Displayed Displayed { get; set; } = Displayed.First;
        public Support? NullableEmpty { get; set; }
        public Support? NullableSet { get; set; } = Support.Featured;
        public Plain Selected { get; set; } = Plain.Planetwars;
        public Unordered Unordered { get; set; } = Unordered.Second;
    }

    static void Emit(RouteCollection routes, string label, Func<AjaxHelper, TextWriter, MvcForm> call)
    {
        var writer = new StringWriter();
        var ajax = MakeHelper(routes, writer);
        using (call(ajax, writer)) { }
        Console.WriteLine("### " + label);
        Console.WriteLine(writer.ToString());
    }

    static void EmitString(RouteCollection routes, string label, Func<AjaxHelper, string> call)
    {
        var writer = new StringWriter();
        var ajax = MakeHelper(routes, writer);
        Console.WriteLine("### " + label);
        Console.WriteLine(call(ajax));
    }

    static void EmitHtml(RouteCollection routes, string label, Func<HtmlHelper, string> call)
    {
        var writer = new StringWriter();
        var html = MakeHtmlHelper(routes, writer);
        Console.WriteLine("### " + label);

        // The anti-forgery token's VALUE is random per request, so it can never be compared. Its
        // presence and its markup can, and that is what matters: a port that dropped the token
        // would turn every one of these links into a CSRF hole and still look right.
        Console.WriteLine(System.Text.RegularExpressions.Regex.Replace(
            call(html), "value=\"[^\"]{40,}\"", "value=\"TOKEN\""));
    }

    // Both halves at once: the name MVC 5 derives from the lambda, and what it hands back as
    // the model value - printed as its element count and contents so a null and an empty list
    // are not the same line.
    static void EmitDropDown(RouteCollection routes, string label, Type enumType, Enum value)
    {
        var writer = new StringWriter();
        var html = MakeHtmlHelper(routes, writer, new EnumModel());
        Console.WriteLine("### " + label);
        Console.WriteLine(html.DropDownList("mapSupportLevel", EnumHelper.GetSelectList(enumType, value)).ToString());
    }

    static void EmitExpression<T>(RouteCollection routes, string label, ListModel model,
                                  Expression<Func<ListModel, IList<T>>> expression)
    {
        var writer = new StringWriter();
        var html = MakeHtmlHelper(routes, writer, model);
        var name = ExpressionHelper.GetExpressionText(expression);
        var value = (IList<T>)ModelMetadata.FromLambdaExpression(expression, html.ViewData).Model;
        Console.WriteLine("### " + label);
        Console.WriteLine("name=" + name + " model="
            + (value == null ? "(null)" : "[" + string.Join(",", value) + "]"));
    }

    static void EmitEnum<TEnum>(RouteCollection routes, string label, EnumModel model,
                                Expression<Func<EnumModel, TEnum>> expression)
    {
        var writer = new StringWriter();
        Console.WriteLine("### " + label);
        Console.WriteLine(MakeHtmlHelper(routes, writer, model).EnumDropDownListFor(expression).ToString());
    }

    static void EmitEnumAttrs<TEnum>(RouteCollection routes, string label, EnumModel model,
                                     Expression<Func<EnumModel, TEnum>> expression, object htmlAttributes)
    {
        var writer = new StringWriter();
        Console.WriteLine("### " + label);
        Console.WriteLine(MakeHtmlHelper(routes, writer, model)
            .EnumDropDownListFor(expression, htmlAttributes).ToString());
    }

    static HtmlHelper<TModel> MakeHtmlHelper<TModel>(RouteCollection routes, TextWriter writer, TModel model)
    {
        var httpContext = new HttpContextWrapper(new HttpContext(
            new HttpRequest("", "http://localhost/", ""), new HttpResponse(TextWriter.Null)));
        var routeData = new RouteData();
        routeData.Values["controller"] = "Ladders";
        routeData.Values["action"] = "Full";
        routeData.Route = routes["Default"];

        var controllerContext = new ControllerContext(httpContext, routeData, new StubController());
        var viewData = new ViewDataDictionary<TModel>(model);
        var viewContext = new ViewContext(controllerContext, new StubView(), viewData,
            new TempDataDictionary(), writer);
        return new HtmlHelper<TModel>(viewContext, new StubDataContainer { ViewData = viewData }, routes);
    }

    static HtmlHelper MakeHtmlHelper(RouteCollection routes, TextWriter writer)
    {
        var httpContext = new HttpContextWrapper(new HttpContext(
            new HttpRequest("", "http://localhost/", ""), new HttpResponse(TextWriter.Null)));
        var routeData = new RouteData();
        routeData.Values["controller"] = "Planetwars";
        routeData.Values["action"] = "Index";
        routeData.Route = routes["Default"];

        var controllerContext = new ControllerContext(httpContext, routeData, new StubController());
        var viewData = new ViewDataDictionary();
        var viewContext = new ViewContext(controllerContext, new StubView(), viewData,
            new TempDataDictionary(), writer);
        return new HtmlHelper(viewContext, new StubDataContainer { ViewData = viewData }, routes);
    }

    static AjaxHelper MakeHelper(RouteCollection routes, TextWriter writer)
    {
        var httpContext = new HttpContextWrapper(new HttpContext(
            new HttpRequest("", "http://localhost/", ""), new HttpResponse(TextWriter.Null)));
        var routeData = new RouteData();
        routeData.Values["controller"] = "Planetwars";
        routeData.Values["action"] = "Index";

        var controllerContext = new ControllerContext(httpContext, routeData, new StubController());
        var viewData = new ViewDataDictionary();
        var viewContext = new ViewContext(controllerContext, new StubView(), viewData,
            new TempDataDictionary(), writer);
        // Zero-K.info/Web.config sets <add key="UnobtrusiveJavaScriptEnabled" value="true" />.
        // Without it MVC 5 emits the OLD Microsoft Ajax markup - onsubmit handlers calling
        // Sys.Mvc.AsyncForm - rather than the data-ajax attributes this site actually serves.
        // There is no Web.config here, so it is set explicitly. Getting this wrong produces a
        // capture that looks authoritative and describes a different site; the first run of this
        // harness did exactly that.
        viewContext.UnobtrusiveJavaScriptEnabled = true;

        return new AjaxHelper(viewContext, new StubDataContainer { ViewData = viewData }, routes);
    }

    class StubController : ControllerBase
    {
        protected override void ExecuteCore() { }
    }

    class StubView : IView
    {
        public void Render(ViewContext viewContext, TextWriter writer) { }
    }

    class StubDataContainer : IViewDataContainer
    {
        public ViewDataDictionary ViewData { get; set; }
    }
}
