using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Web.Mvc;
using EntityFramework.Extensions;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.Render
{
    /// <summary>
    /// Renders a real Zero-K view on .NET 9 with a real model, and checks the HTML.
    ///
    ///     ZK_CONNECTION_STRING=...zk_test... dotnet run
    ///
    /// The view is Maps/MapTags.cshtml, chosen because it is the plainest of the ones that
    /// compile: it takes a ZkData.Resource, sets its own layout, calls no helper from the
    /// unported web project, and its entire output is decided by the model. So what this
    /// proves is the seam, not the view - a row read through EF Core reaches the Razor
    /// engine on .NET 9 and comes back as HTML that depends on the row's values.
    /// </summary>
    public static class Program
    {
        public static async Task<int> Main()
        {
            if (string.IsNullOrEmpty(ZkDataContext.ConnectionString))
            {
                Console.Error.WriteLine("Set ZK_CONNECTION_STRING first - see db/README.md.");
                return 2;
            }

            List<Resource> maps;
            using (var db = new ZkDataContext())
            {
                // Several maps, not one: a view that ignored its model entirely would still
                // satisfy a single-row check if the expected strings happened to be in the
                // template. Different rows have to produce different HTML.
                maps = db.Resources.AsNoTracking()
                    .Where(r => r.MapHills != null && r.MapWaterLevel != null)
                    .OrderBy(r => r.ResourceID)
                    .Take(5)
                    .ToList();
            }

            if (maps.Count == 0)
            {
                Console.Error.WriteLine("no map with terrain data - load the fixture first");
                return 2;
            }

            var failures = 0;
            var rendered = new List<string>();

            foreach (var map in maps)
            {
                var html = await Render("Maps/MapTags", map);
                rendered.Add(html);

                Console.WriteLine(map.InternalName + "  water=" + map.MapWaterLevel
                                  + " hills=" + map.MapHills + " ffa=" + (map.MapIsFfa == true));
                failures += Check(html.Contains("sea" + map.MapWaterLevel + ".png"),
                    "  water tag sea" + map.MapWaterLevel + ".png");
                failures += Check(html.Contains("hill" + map.MapHills + ".png"),
                    "  hills tag hill" + map.MapHills + ".png");
                failures += Check(html.Contains("ffa.png") == (map.MapIsFfa == true),
                    "  ffa tag present exactly when the row says so");
                failures += Check(!html.Contains("@"), "  no unprocessed Razor markers survived");
            }

            // The view is a function of the row, so rows that differ must render differently -
            // and rows that agree on every field the view reads must render identically. All
            // five fields, not the three that are obvious: leaving MapIsSpecial and
            // MapIsAssymetrical out of this key made the check fail against a correct view,
            // which is the right failure for the wrong reason.
            var distinctRows = maps
                .Select(m => string.Join("/", m.MapWaterLevel, m.MapHills, m.MapIsFfa, m.MapIsSpecial, m.MapIsAssymetrical))
                .Distinct().Count();
            var distinctHtml = rendered.Select(h => h.Trim()).Distinct().Count();
            Console.WriteLine();
            failures += Check(distinctHtml == distinctRows,
                distinctRows + " distinct terrain combinations produced " + distinctHtml + " distinct renderings");

            failures += CheckPortedHelpers();
            failures += CheckBatchOperations();
            failures += CheckChildActionTripwire();
            failures += await CheckViewComponents();
            failures += CheckAjaxMarkup();
            failures += CheckPostLinkMarkup();
            failures += CheckEnumDropDownMarkup();
            failures += CheckExpressionSubstitutes();
            failures += CheckEveryCapturedShapeIsCovered();

            Console.WriteLine();
            if (failures == 0)
            {
                Console.WriteLine("Zero-K views render on .NET 9, from rows read through EF Core.");
                return 0;
            }
            Console.WriteLine(failures + " check(s) failed.");
            return 1;
        }


        /// <summary>
        /// The child-action shims throw, and this proves it.
        ///
        /// Four views compile only because ZeroKWeb.Core/Mvc5Compat/ChildActionCompat.cs
        /// supplies signatures for Html.Action and Html.RenderAction, which ASP.NET Core
        /// removed. The whole safety of that arrangement rests on those signatures FAILING
        /// when reached, because three of the seven actions they stand in for carry [Auth] -
        /// a shim that quietly rendered one to an anonymous visitor would be worse than a
        /// view that does not compile.
        ///
        /// "It throws" is not something a compiler can check and not something the view
        /// inventory can see, so it is checked here. A future edit that made one of these
        /// return empty content instead - which would look like progress, and would move
        /// views into `compiles` - fails this.
        ///
        /// The helper argument is null on purpose: these must throw before touching it.
        /// </summary>
        private static int CheckChildActionTripwire()
        {
            Console.WriteLine();
            var failures = 0;
            var cases = new (string What, Action Call)[]
            {
                ("Action(action)", () => ((IHtmlHelper)null).Action("MatchMaker")),
                ("Action(action, routeValues)", () => ((IHtmlHelper)null).Action("Events", new { partial = true })),
                ("Action(action, controller)", () => ((IHtmlHelper)null).Action("Ladder", "Planetwars")),
                ("Action(action, controller, routeValues)", () => ((IHtmlHelper)null).Action("Events", "Planetwars", new { partial = true })),
                ("RenderAction(action)", () => ((IHtmlHelper)null).RenderAction("MatchMaker")),
                ("RenderAction(action, routeValues)", () => ((IHtmlHelper)null).RenderAction("CommanderProfile", new { profileNumber = 1 })),
                ("RenderAction(action, controller)", () => ((IHtmlHelper)null).RenderAction("ChatNotification", "Lobby")),
                ("RenderAction(action, controller, routeValues)", () => ((IHtmlHelper)null).RenderAction("Index", "Poll", new { pollID = 1 })),
            };

            foreach (var (what, call) in cases)
            {
                string outcome;
                try
                {
                    call();
                    outcome = "returned without throwing";
                }
                catch (NotSupportedException e)
                {
                    outcome = e.Message.Contains("child actions") && e.Message.Contains("[Auth]")
                        ? null
                        : "threw NotSupportedException, but the message no longer explains why: " + e.Message;
                }
                catch (Exception e)
                {
                    outcome = "threw " + e.GetType().Name + " rather than NotSupportedException";
                }
                failures += Check(outcome == null, "  " + what + " refuses to render" + (outcome == null ? "" : " - " + outcome));
            }

            // Every call site in the repository must bind to one of the overloads above. This
            // is the count that, when it was wrong, had four views reporting CS1929/CS1503/CS1501
            // for a gap in the shim rather than for anything about child actions.
            failures += Check(cases.Length == 8, "  all eight overloads are covered");
            return failures;
        }

        /// <summary>
        /// The ported view helpers, against the exact HTML their MVC 5 originals emit.
        ///
        /// These were rewritten rather than linked - MvcHtmlString and HtmlHelper do not
        /// exist on .NET 9 - so they can drift from the originals in the HTML the site is
        /// made of, invisibly. Transcribing ten of them by eye produced two defects
        /// (a Math.Floor that should not be there, a style attribute dropped), so the
        /// expected strings below are written out in full rather than computed: a check that
        /// derives its expectation the same way as the code cannot catch a mistake in both.
        /// </summary>
        private static int CheckPortedHelpers()
        {
            Console.WriteLine();
            Console.WriteLine("ported view helpers, against their MVC 5 output:");

            // PrintLines calls helper.Encode, so these need a real one rather than null.
            var html = BuildServices().GetRequiredService<Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper>();
            var failures = 0;

            failures += Same("PrintEnergy(12.7)", html.PrintEnergy(12.7),
                "<span>12<img src='/img/luaui/energy.png' class='icon20'/></span>");
            failures += Same("PrintMetal(12.7)", html.PrintMetal(12.7),
                "<span style='color:#00FFFF;'>12<img src='/img/luaui/ibeam.png' class='icon20'/></span>");
            failures += Same("PrintBombers(12.7)", html.PrintBombers(12.7),
                "<span>12.7<img src='/img/fleets/neutral.png' class='icon20'/></span>");
            failures += Same("PrintWarps(12.7)", html.PrintWarps(12.7),
                "<span>12.7<img src='/img/warpcore.png' class='icon20'/></span>");
            failures += Same("PrintEnergy(null)", html.PrintEnergy(null),
                "<span>0<img src='/img/luaui/energy.png' class='icon20'/></span>");
            failures += Same("PrintMetal((Account)null)", html.PrintMetal((Account)null), null);
            failures += Same("PrintLines(a\\nb)", html.PrintLines("a\nb"), "a<br/>b");
            failures += Same("PrintLines(list)", html.PrintLines(new object[] { 1, 2 }), "1<br/>2<br/>");
            // M&#252;ller, not Müller: HttpUtility.HtmlEncode escapes non-ASCII too, so this is
            // what MVC 5 emits. Verified by running both encoders under mono side by side -
            // an earlier commit claimed MVC 5 wrote it through, and that was wrong.
            failures += Same("PrintLines(Müller)", html.PrintLines("Müller"), "M&#252;ller");
            failures += Same("Stars(RedStarSmall, 3.5)", html.Stars(StarType.RedStarSmall, 3.5),
                "<span class='RedStarSmall' style='width:49px'></span><span style='width:21px'></span>");
            failures += Same("Stars(RedSkull, null)", html.Stars(StarType.RedSkull, null),
                "<span class='WhiteSkull' style='width:70px' title='No votes'></span>");
            // FactionColor returns empty for no faction, and this overload does NOT substitute
            // a default the way PrintAccount and PrintClan do - so an empty colour is correct.
            failures += Same("PrintInfluence(null, 25)", html.PrintInfluence((Faction)null, 25.0),
                "<span style='color:'>25 (25%)</span>");
            failures += Same("PrintBadges((Account)null)", html.PrintBadges(null), "");

            // The seven entity helpers, pinned before they move to shared code.
            //
            // These are ~150 lines duplicated between Zero-K.info/AppCode/HtmlHelperExtensions.cs
            // and this project's Ported copy, and the event feed's HTML is a third caller. The
            // expectations below were RECORDED by running the current implementation, not written
            // from reading it - so they are what the site emits today, and a move that changes a
            // byte says so.
            failures += PrintEntityHelpers();
            return failures;
        }

        /// <summary>Synthetic entities, distinctive enough that a swapped field shows up.</summary>
        private static int PrintEntityHelpers()
        {
            // A contextualized helper, because these build links: PrintAccount asks the helper for
            // a UrlHelper to make the clan link, and an uncontextualized one throws. That is the
            // dependency the whole move is about - the event feed has no view context at all.
            var provider = BuildServices();
            var httpContext = new DefaultHttpContext { RequestServices = provider };
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("localhost");
            httpContext.SetEndpoint(new Microsoft.AspNetCore.Http.Endpoint(
                _ => Task.CompletedTask, Microsoft.AspNetCore.Http.EndpointMetadataCollection.Empty, "record"));
            PublishAmbient(provider, httpContext);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
            var writer0 = new StringWriter();
            var viewContext = new ViewContext(actionContext, new FakeView(), viewData,
                new TempDataDictionary(httpContext, provider.GetRequiredService<ITempDataProvider>()),
                writer0, new HtmlHelperOptions());
            var html = provider.GetRequiredService<Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper>();
            ((IViewContextAware)html).Contextualize(viewContext);

            var faction = new Faction { FactionID = 3, Name = "Dynasty", Shortcut = "Dyn", Color = "#FF8800" };
            var clan = new Clan { ClanID = 7, ClanName = "Test Clan", Shortcut = "TC", FactionID = 3, Faction = faction };
            var account = new Account
            {
                AccountID = 42, Name = "player42", Country = "CZ", Level = 5,
                Clan = clan, ClanID = 7, Faction = faction, FactionID = 3,
            };
            var planet = new Planet
            {
                PlanetID = 11, Name = "Testworld", OwnerFactionID = 3, Faction = faction,
                Resource = new Resource { ResourceID = 5, MapPlanetWarsIcon = "icon.png" },
            };

            // RECORDED from the current implementation, not transcribed from reading it.
            //
            // What this does NOT pin is the URLs: this harness has no route table, so every
            // href comes out empty - see the note on InvokeViewComponent. It pins the flags,
            // ranks, colours, icons and structure around them, which is everything the move to
            // shared code touches, because the move passes URL generation through rather than
            // reimplementing it.
            var failures = 0;
            foreach (var probe in new (string What, Microsoft.AspNetCore.Html.IHtmlContent Produced, string Expected)[]
            {
                ("PrintAccount", html.PrintAccount(account), "<img src='/img/flags/CZ.png' class='flag' height='11' width='16' alt='CZ'/><img src='/img/ranks/1_0.png'  class='icon16' alt='rank' /><a href='' nicetitle='$clan$7'><img src='/img/clans/TC.png' width='16'/></a><a href='/Users/Detail/42' style='color:#FF8800' nicetitle='$user$42'>player42</a>"),
                ("PrintAccount(no links)", html.PrintAccount(account, makeLinks: false), "<img src='/img/flags/CZ.png' class='flag' height='11' width='16' alt='CZ'/><img src='/img/ranks/1_0.png'  class='icon16' alt='rank' /><img src='/img/clans/TC.png' width='16'/>player42"),
                ("PrintAccount(null)", html.PrintAccount(null), "Nobody"),
                ("PrintClan", html.PrintClan(clan), "<a href='' nicetitle='$clan$7'><img src='/img/clans/TC.png' width='16'><span style='color:#FF8800'>TC</span></a>"),
                ("PrintClan(null)", html.PrintClan(null), "<a href=''>No Clan</a>"),
                ("PrintFaction", html.PrintFaction(faction), "<a href='' nicetitle='$faction$3'><img src='/img/factions/Dyn.png'/></a>"),
                ("PrintFaction(null)", html.PrintFaction(null), ""),
                ("PrintPlanet", html.PrintPlanet(planet), "<a href='' title='$planet$11' style='color:#FF8800'><img src='/img/planets/icon.png' width='8'>Testworld</a>"),
                ("PrintPlanet(null)", html.PrintPlanet(null), "?"),
            })
            {
                failures += Same("  " + probe.What, probe.Produced, probe.Expected);
            }
            return failures;
        }

        private static int Same(string what, Microsoft.AspNetCore.Html.IHtmlContent produced, string expected)
        {
            string actual = null;
            if (produced != null)
            {
                using (var writer = new StringWriter())
                {
                    produced.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
                    actual = writer.ToString();
                }
            }
            return CheckEqual(actual, expected, what);
        }

        /// <summary>
        /// Makes the request this harness built the ambient one, the way an ASP.NET Core
        /// application does through middleware. Global reads it, and so does the
        /// System.Web.HttpContext.Current shim that UniGrid's constructor goes through.
        /// </summary>
        private static void PublishAmbient(IServiceProvider provider, DefaultHttpContext httpContext)
        {
            var accessor = provider.GetRequiredService<IHttpContextAccessor>();
            accessor.HttpContext = httpContext;
            ZeroKWeb.Global.Configure(accessor);
        }

        /// <summary>
        /// PostLink, against markup captured from MVC 5 itself - the same arrangement as the Ajax
        /// helper, and for a harder case: MVC 5's TagBuilder and ASP.NET Core's share a name and
        /// little else, so this is a rewrite rather than a relocation.
        ///
        /// Two things the capture could not carry, both stated rather than quietly skipped:
        ///
        /// - **The URL.** The capture ran without a route table, so every `action` came out
        ///   empty; it is supplied here, exactly as the Ajax check does.
        /// - **The anti-forgery token.** MVC 5's reads web.config, which mono cannot host, and it
        ///   is random per request anyway. The captured file has the slot empty; what is checked
        ///   here is that the port emits something where MVC 5 emitted a token, which is asserted
        ///   separately below.
        /// </summary>
        private static int CheckPostLinkMarkup()
        {
            Console.WriteLine();
            Console.WriteLine("PostLink markup, against MVC 5 captured under mono:");

            var path = FindUpwards(Path.Combine("tools", "ajax-ground-truth", "expected.txt"));
            if (path == null)
            {
                Console.WriteLine("   FAIL  the capture is missing - run tools/ajax-ground-truth/capture.sh --update");
                return 1;
            }

            var expected = new Dictionary<string, string>();
            string label = null;
            foreach (var line in File.ReadAllLines(path))
            {
                if (line.StartsWith("### ")) { label = line.Substring(4); continue; }
                if (label != null && line.Length > 0) { expected[label] = line; label = null; }
            }

            var cases = new (string Label, Func<string> Build)[]
            {
                ("PostLink(text, action)",
                 () => System.Web.Mvc.PostLinkExtensions.Tag("form", Form(""), Button("Delete", null, null))),

                ("PostLink(text, action, controller, routeValues)",
                 () => System.Web.Mvc.PostLinkExtensions.Tag("form", Form(""), Button("Select", null, null))),

                ("PostLink with cssClass and nicetitle",
                 () => System.Web.Mvc.PostLinkExtensions.Tag("form", Form(""), Button("Delete", "js_confirm", "Really?"))),

                ("PostLink with a confirm message",
                 () => System.Web.Mvc.PostLinkExtensions.Tag("form", Form(""),
                     Button("Activate", null, null, "WARNING: it's gone after this. Proceed?"))),

                ("PostLink encodes its text",
                 () => System.Web.Mvc.PostLinkExtensions.Tag("form", Form(""),
                     Button(System.Net.WebUtility.HtmlEncode("a < b & c \" d ' e"), null, null))),

                ("PostImageLink(src, height, action)",
                 () => System.Web.Mvc.PostLinkExtensions.Tag("form", Form(""),
                     Button(Img("/img/x.png", 24), null, null))),

                ("PostImageLink with height 0 omits the attribute",
                 () => System.Web.Mvc.PostLinkExtensions.Tag("form", Form(""),
                     Button(Img("/img/x.png", 0), null, null))),
            };

            var failures = 0;
            foreach (var (caseLabel, build) in cases)
            {
                if (!expected.TryGetValue(caseLabel, out var want))
                {
                    Console.WriteLine("   FAIL    no captured line for " + caseLabel);
                    failures++;
                    continue;
                }
                CapturedShapesChecked.Add(caseLabel);
                failures += CheckEqual(build(), want, "  " + caseLabel);
            }
            return failures;
        }

        /// <summary>Mirrors Capture.cs's ListModel; see the note on the enums below.</summary>
        private class CapturedListModel
        {
            public IList<int> UserId { get; set; } = new List<int>();
            public IList<CapturedSupport> Types { get; set; } = new List<CapturedSupport> { CapturedSupport.Featured };
        }

        /// <summary>
        /// The two MVC 5 APIs that MultiSelectFor and EnumCheckboxesFor lean on, against the
        /// capture of what MVC 5 answers.
        ///
        /// Those helpers' markup is copied verbatim from HtmlHelperExtensions.cs and can be
        /// diffed against it, so it is not what needed capturing. The substitution underneath is:
        /// the port uses <c>NameFor</c> where MVC 5 used
        /// <c>ExpressionHelper.GetExpressionText</c>, and a compiled expression where MVC 5 used
        /// <c>ModelMetadata.FromLambdaExpression(...).Model</c>. Neither substitute is obviously
        /// equal - NameFor prepends the field prefix, and the prefix is only empty because every
        /// call site is at the top level.
        ///
        /// This builds a real IHtmlHelper&lt;T&gt; out of DI, the same way the view-component
        /// check builds a real IViewComponentHelper, and compares its answers to the recorded
        /// ones.
        /// </summary>
        private static int CheckExpressionSubstitutes()
        {
            Console.WriteLine();
            Console.WriteLine("expression name and model value, against MVC 5 captured under mono:");

            var path = FindUpwards(Path.Combine("tools", "ajax-ground-truth", "expected.txt"));
            if (path == null)
            {
                Console.WriteLine("   FAIL  the capture is missing - run tools/ajax-ground-truth/capture.sh --update");
                return 1;
            }

            var expected = new Dictionary<string, string>();
            string label = null;
            foreach (var line in File.ReadAllLines(path))
            {
                if (line.StartsWith("### ")) { label = line.Substring(4); continue; }
                if (label != null && line.Length > 0) { expected[label] = line; label = null; }
            }

            var provider = BuildServices();
            var httpContext = new DefaultHttpContext { RequestServices = provider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var model = new CapturedListModel { UserId = new List<int> { 4, 11 } };
            var viewData = new ViewDataDictionary<CapturedListModel>(
                new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model };

            var failures = 0;
            using (var writer = new StringWriter())
            {
                var viewContext = new ViewContext(actionContext, new FakeView(), viewData,
                    new TempDataDictionary(httpContext, provider.GetRequiredService<ITempDataProvider>()),
                    writer, new HtmlHelperOptions());
                var html = provider.GetRequiredService<IHtmlHelper<CapturedListModel>>();
                ((IViewContextAware)html).Contextualize(viewContext);

                var cases = new (string Label, Func<string> Build)[]
                {
                    // Server.HtmlEncode, which Shared/UserDetail.cshtml calls on a commander
                    // name. Not WebUtility.HtmlEncode, which is what this used to be: HttpUtility
                    // encodes 160-255 and leaves everything above 255 alone, so a CJK name passes
                    // through, and WebUtility would have turned it into a run of &#20013;.
                    ("HttpUtility.HtmlEncode",
                     () => new ZeroKWeb.Compat.Mvc5Server(null).HtmlEncode(
                         "a < b & c \" d ' e > f \u00fc \u00a9 \u4e2d")),

                    ("ExpressionHelper.GetExpressionText(x => x.UserId)",
                     () => "name=" + html.NameFor(x => x.UserId) + " model=["
                           + string.Join(",", ((Expression<Func<CapturedListModel, IList<int>>>)(x => x.UserId))
                               .Compile()(model)) + "]"),

                    ("ExpressionHelper.GetExpressionText(x => x.Types)",
                     () => "name=" + html.NameFor(x => x.Types) + " model=["
                           + string.Join(",", ((Expression<Func<CapturedListModel, IList<CapturedSupport>>>)(x => x.Types))
                               .Compile()(model)) + "]"),
                };

                foreach (var (caseLabel, build) in cases)
                {
                    if (!expected.TryGetValue(caseLabel, out var want))
                    {
                        Console.WriteLine("   FAIL    no captured line for " + caseLabel);
                        failures++;
                        continue;
                    }
                    CapturedShapesChecked.Add(caseLabel);
                    failures += CheckEqual(build(), want, "  " + caseLabel);
                }
            }
            return failures;
        }

        /// <summary>
        /// EnumDropDownListFor, against the same capture - and the one shape in it that spans
        /// several lines, because MVC 5 puts Environment.NewLine after every option.
        ///
        /// The enums below MIRROR the ones in tools/ajax-ground-truth/Capture.cs rather than
        /// sharing them: that file compiles under mono against .NET Framework and this one does
        /// not, so there is nothing to share. They can drift, and if they do the comparison
        /// fails loudly rather than quietly comparing something else - a renamed member or a
        /// changed number shows up as a diff on the very next run.
        ///
        /// What this drives is Render(), not the public helper, for the same reason the PostLink
        /// check drives Tag(): building an IHtmlHelper&lt;T&gt; outside a view needs the whole
        /// view engine. Everything the capture settled - declaration order, [Display] over
        /// [Description], numeric values, the nullable empty option, where selected lands - is
        /// inside Render(). NameFor, IdFor and the ModelState lookup are not, and are not
        /// claimed here.
        /// </summary>
        private enum CapturedPlain { Casual = 1, MatchMaking = 2, Planetwars = 4 }

        private enum CapturedDescribed
        {
            [System.ComponentModel.Description("offline")] AllOffline = 0,
            [System.ComponentModel.Description("pre-game")] PreGame = 1,
            [System.ComponentModel.Description("running")] Running = 2,
        }

        private enum CapturedDisplayed
        {
            [System.ComponentModel.DataAnnotations.Display(Name = "shown first")] First = 0,
            Second = 1,
        }

        private enum CapturedSupport { None = 0, Supported = 1, Featured = 2, MatchMaker = 3 }

        private enum CapturedUnordered { Third = 30, First = 10, Second = 20 }

        private static int CheckEnumDropDownMarkup()
        {
            Console.WriteLine();
            Console.WriteLine("EnumDropDownListFor markup, against MVC 5 captured under mono:");

            var path = FindUpwards(Path.Combine("tools", "ajax-ground-truth", "expected.txt"));
            if (path == null)
            {
                Console.WriteLine("   FAIL  the capture is missing - run tools/ajax-ground-truth/capture.sh --update");
                return 1;
            }

            // Multi-line blocks, unlike every other shape in this file: a label owns every line
            // up to the next label.
            var expected = new Dictionary<string, string>();
            string label = null;
            var block = new List<string>();
            foreach (var line in File.ReadAllLines(path))
            {
                if (line.StartsWith("### "))
                {
                    if (label != null) expected[label] = string.Join("\n", block);
                    label = line.Substring(4);
                    block.Clear();
                    continue;
                }
                if (label != null) block.Add(line);
            }
            if (label != null) expected[label] = string.Join("\n", block);

            var provider = BuildServices();
            var httpContext = new DefaultHttpContext { RequestServices = provider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var viewData = new ViewDataDictionary<CapturedListModel>(
                new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = new CapturedListModel() };
            var writer = new StringWriter();
            var viewContext = new ViewContext(actionContext, new FakeView(), viewData,
                new TempDataDictionary(httpContext, provider.GetRequiredService<ITempDataProvider>()),
                writer, new HtmlHelperOptions());
            var html = provider.GetRequiredService<IHtmlHelper<CapturedListModel>>();
            ((IViewContextAware)html).Contextualize(viewContext);

            var cases = new (string Label, Func<string> Build)[]
            {
                ("EnumDropDownListFor(plain enum)",
                 () => Enum_(typeof(CapturedPlain), false, "Plain", "1", null)),

                ("EnumDropDownListFor(enum with [Description])",
                 () => Enum_(typeof(CapturedDescribed), false, "Described", "0", null)),

                ("EnumDropDownListFor(enum with [Display])",
                 () => Enum_(typeof(CapturedDisplayed), false, "Displayed", "0", null)),

                ("EnumDropDownListFor(nullable enum, null)",
                 () => Enum_(typeof(CapturedSupport), true, "NullableEmpty", null, null)),

                ("EnumDropDownListFor(nullable enum, with a value)",
                 () => Enum_(typeof(CapturedSupport), true, "NullableSet", "2", null)),

                ("EnumDropDownListFor(non-default selected)",
                 () => Enum_(typeof(CapturedPlain), false, "Selected", "4", null)),

                ("EnumDropDownListFor(expression, htmlAttributes)",
                 () => Enum_(typeof(CapturedPlain), false, "Plain", "1", new { @class = "width-100" })),

                ("EnumDropDownListFor(declaration order != numeric order)",
                 () => Enum_(typeof(CapturedUnordered), false, "Unordered", "20", null)),

                // Does ASP.NET Core's own DropDownList emit MVC 5's markup? Asked, not
                // assumed - the list comes from the port's EnumHelper either way, so this
                // isolates the rendering.
                ("DropDownList(EnumHelper.GetSelectList(enum, value))",
                 () => Rendered(html.DropDownList("mapSupportLevel",
                     System.Web.Mvc.EnumHelper.GetSelectList(typeof(CapturedSupport), CapturedSupport.Featured)))),

                ("DropDownList(EnumHelper.GetSelectList(enum, null))",
                 () => Rendered(html.DropDownList("mapSupportLevel",
                     System.Web.Mvc.EnumHelper.GetSelectList(typeof(CapturedSupport), null)))),

                ("DropDownList(EnumHelper.GetSelectList(enum starting at 1, null))",
                 () => Rendered(html.DropDownList("mapSupportLevel",
                     System.Web.Mvc.EnumHelper.GetSelectList(typeof(CapturedPlain), null)))),

                ("DropDownList(EnumHelper.GetSelectList(enum starting at 1, value))",
                 () => Rendered(html.DropDownList("mapSupportLevel",
                     System.Web.Mvc.EnumHelper.GetSelectList(typeof(CapturedPlain), CapturedPlain.Planetwars)))),
            };

            var failures = 0;
            foreach (var (caseLabel, build) in cases)
            {
                if (!expected.TryGetValue(caseLabel, out var want))
                {
                    Console.WriteLine("   FAIL    no captured block for " + caseLabel);
                    failures++;
                    continue;
                }
                CapturedShapesChecked.Add(caseLabel);
                failures += CheckEqual(build(), want, "  " + caseLabel);
            }
            return failures;
        }

        /// <summary>
        /// The capture was written on Linux under mono, so its option separator is LF; so is
        /// this process's Environment.NewLine. Normalising rather than assuming keeps the
        /// comparison honest if either side ever runs on Windows - the separator would then be
        /// CRLF on BOTH, which is the property that matters, and not a diff.
        /// </summary>
        private static string Rendered(Microsoft.AspNetCore.Html.IHtmlContent content)
        {
            using (var writer = new StringWriter())
            {
                content.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
                return writer.ToString().Replace("\r\n", "\n").TrimEnd('\n');
            }
        }

        private static string Enum_(Type enumType, bool isNullable, string name, string selected, object attributes)
        {
            var markup = System.Web.Mvc.EnumDropDownListExtensions.Render(
                enumType, isNullable, name, name, selected, attributes);
            return markup.Replace("\r\n", "\n").TrimEnd('\n');
        }

        private static SortedDictionary<string, string> Form(string url) =>
            new SortedDictionary<string, string>(StringComparer.Ordinal)
                { ["method"] = "post", ["action"] = url, ["class"] = "postlink" };

        private static string Button(string inner, string cssClass, string nicetitle, string confirmMessage = null)
        {
            var attributes = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["type"] = "submit",
                ["class"] = string.IsNullOrEmpty(cssClass) ? "postlink-button" : cssClass + " postlink-button",
            };
            if (!string.IsNullOrEmpty(nicetitle)) attributes["nicetitle"] = nicetitle;
            if (!string.IsNullOrEmpty(confirmMessage)) attributes["data-confirm"] = confirmMessage;
            return System.Web.Mvc.PostLinkExtensions.Tag("button", attributes, inner);
        }

        private static string Img(string src, int height)
        {
            var attributes = new SortedDictionary<string, string>(StringComparer.Ordinal)
                { ["alt"] = "", ["src"] = src };
            if (height > 0) attributes["height"] = height.ToString();
            return System.Web.Mvc.PostLinkExtensions.Tag("img", attributes, null, selfClosing: true);
        }

        /// <summary>Finds a repo-relative file by walking up from the binary.</summary>
        private static string FindUpwards(string relative)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, relative);
                if (File.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }
            return null;
        }

        /// <summary>Byte comparison, with both sides printed on a mismatch.</summary>
        private static int CheckEqual(string actual, string expected, string what)
        {
            var ok = actual == expected;
            Console.WriteLine((ok ? "   ok    " : "   FAIL  ") + what);
            if (!ok) Console.WriteLine("           wanted " + (expected ?? "(nothing)") + "\n           got    " + (actual ?? "(nothing)"));
            return ok ? 0 : 1;
        }


        /// <summary>
        /// EF6's batch Delete and Update, translated onto EF Core's ExecuteDelete and
        /// ExecuteUpdate, against the real database.
        ///
        /// This is checked rather than assumed because the alternative was an empty namespace
        /// that compiled and would have thrown - and because these run one SQL statement over
        /// every matching row, which is the kind of thing worth being sure about before an
        /// admin page uses it to clear a galaxy. Everything here happens inside a transaction
        /// that is rolled back.
        /// </summary>
        private static int CheckBatchOperations()
        {
            Console.WriteLine();
            Console.WriteLine("EF6 batch operations over EF Core:");

            var failures = 0;
            using (var db = new ZkDataContext())
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var before = db.Accounts.Count(a => a.Level != 4242);

                    // Update: a member initialiser with a constant, the shape PlanetwarsAdmin uses.
                    var updated = db.Accounts.Where(a => a.Level != 4242).Update(a => new Account { Level = 4242 });
                    db.ChangeTracker.Clear();
                    failures += Check(updated == before, "Update set " + updated + " rows, matching the query");
                    failures += Check(db.Accounts.Count(a => a.Level == 4242) == before,
                        "every row the query matched actually changed in the database");

                    // Update with null, which the unassign-factions path relies on.
                    db.Accounts.Where(a => a.Level == 4242).Update(a => new Account { FactionID = null });
                    db.ChangeTracker.Clear();
                    failures += Check(!db.Accounts.Any(a => a.FactionID != null), "a null assignment reaches the database");

                    // Delete: filtered, then the whole set.
                    var doomed = db.AccountRatings.Count();
                    var deleted = db.AccountRatings.Where(r => r.AccountID > 0).Delete();
                    db.ChangeTracker.Clear();
                    failures += Check(deleted == doomed, "Delete removed " + deleted + " rows, matching the query");
                    failures += Check(db.AccountRatings.Count() == 0, "the rows are gone");
                }
                finally
                {
                    transaction.Rollback();
                }
            }

            using (var db = new ZkDataContext())
            {
                failures += Check(db.Accounts.Any(a => a.Level != 4242) && db.AccountRatings.Any(),
                    "the fixture is left as it was found");
            }
            return failures;
        }

        private static int Check(bool ok, string what)
        {
            Console.WriteLine((ok ? "   ok    " : "   FAIL  ") + what);
            return ok ? 0 : 1;
        }

        /// <summary>
        /// The Razor view engine on its own - no Kestrel, no routing, no request. Everything
        /// here is the minimum the engine insists on before it will execute a view, which is
        /// itself worth knowing: this is the machinery the ASP.NET Core port has to stand up.
        /// </summary>

        /// <summary>The minimum ASP.NET Core insists on before it will execute a view.</summary>
        /// <summary>
        /// The ported Ajax helper against markup captured from MVC 5 itself.
        ///
        /// The expected strings are not written here by hand. tools/ajax-ground-truth/capture.sh
        /// runs the real System.Web.Mvc 5.2.3 under mono and records what it emits, and this
        /// compares the port's output to that file byte for byte.
        ///
        /// That process has already paid for itself: the first capture came back with
        /// `onsubmit="Sys.Mvc.AsyncForm.handleSubmit(...)"`, the pre-unobtrusive Microsoft Ajax
        /// markup, because the capture harness had no Web.config and so defaulted
        /// UnobtrusiveJavaScriptEnabled to false. Zero-K.info/Web.config sets it true. A
        /// transcription from memory would have produced data-ajax attributes and been right by
        /// luck, or Sys.Mvc and been wrong with confidence.
        ///
        /// The action URL is supplied rather than generated: URL generation is MVC's own routing,
        /// not this shim's, and the harness has no route table. Everything else - which
        /// attributes appear, their ALPHABETICAL order, the '#' prefixes, the &amp;#39; encoding -
        /// is this file's responsibility and is compared exactly.
        /// </summary>
        private static int CheckAjaxMarkup()
        {
            Console.WriteLine();
            Console.WriteLine("Ajax markup, against MVC 5 captured under mono:");

            // Walked up from the binary rather than taken from the working directory, which is
            // not the repository root when this runs under tools/render-view.sh.
            var path = FindUpwards(Path.Combine("tools", "ajax-ground-truth", "expected.txt"));
            if (path == null)
            {
                Console.WriteLine("   FAIL  tools/ajax-ground-truth/expected.txt not found - run "
                                  + "tools/ajax-ground-truth/capture.sh --update");
                return 1;
            }

            var expected = new Dictionary<string, string>();
            string label = null;
            foreach (var line in File.ReadAllLines(path))
            {
                if (line.StartsWith("### ")) { label = line.Substring(4); continue; }
                if (label != null && line.Length > 0) { expected[label] = line; label = null; }
            }

            var siteOptions = new System.Web.Mvc.Ajax.AjaxOptions
            {
                UpdateTargetId = "events",
                OnComplete = "GlobalPageInit($(\'#events\'))",
                OnSuccess = "ReplaceHistory($(\'#events\').find(\'form\').serialize())",
            };

            var cases = new (string Label, string Url, Func<string, string> Build)[]
            {
                ("BeginForm(action, routeValues, options)",
                 "/Planetwars/Events?accountID=42&partial=True&pageSize=40",
                 url => System.Web.Mvc.AjaxCompat.BuildTag("form", url, "action",
                     new System.Web.Mvc.Ajax.AjaxOptions
                     {
                         InsertionMode = System.Web.Mvc.Ajax.InsertionMode.Replace,
                         UpdateTargetId = "events",
                         LoadingElementId = "ajaxScrollProgress",
                     }, null, "")),

                ("BeginForm(action, options)", "/Planetwars",
                 url => System.Web.Mvc.AjaxCompat.BuildTag("form", url, "action", siteOptions, null, "")),

                ("BeginForm(action, controller, options)", "/Clans",
                 url => System.Web.Mvc.AjaxCompat.BuildTag("form", url, "action", siteOptions, null, "")),

                ("BeginForm(action, controller, routeValues, options, htmlAttributes)", "/PlanetWars/MatchMaker",
                 url => System.Web.Mvc.AjaxCompat.BuildTag("form", url, "action", siteOptions,
                     new Dictionary<string, object> { { "id", "mmForm" } }, "")),

                ("BeginForm(action, routeValues, options) with method", "/Planetwars/CommanderProfile?profileNumber=1",
                 url => System.Web.Mvc.AjaxCompat.BuildTag("form", url, "action",
                     new System.Web.Mvc.Ajax.AjaxOptions
                     {
                         UpdateTargetId = "com1",
                         InsertionMode = System.Web.Mvc.Ajax.InsertionMode.Replace,
                         HttpMethod = "post",
                         LoadingElementId = "busy",
                     }, null, "")),

                ("encoding", "/Planetwars/E",
                 url => System.Web.Mvc.AjaxCompat.BuildTag("form", url, "action",
                     new System.Web.Mvc.Ajax.AjaxOptions
                     {
                         UpdateTargetId = "t",
                         OnComplete = "a < b && c > d \" ' \u00fc \u00a9 \u4e2d",
                     }, null, "")),

                ("ActionLink(text, action, routeValues, options)",
                 "/Planetwars/MatchMakerJoin?planetID=7&attackerFaction=Dyn",
                 url => System.Web.Mvc.AjaxCompat.BuildTag("a", url, "href",
                     new System.Web.Mvc.Ajax.AjaxOptions
                     {
                         UpdateTargetId = "matchMaker",
                         InsertionMode = System.Web.Mvc.Ajax.InsertionMode.Replace,
                     }, null, "Join")),
            };

            var failures = 0;
            foreach (var (caseLabel, url, build) in cases)
            {
                if (!expected.TryGetValue(caseLabel, out var want))
                {
                    Console.WriteLine("   FAIL    no captured line for " + caseLabel);
                    failures++;
                    continue;
                }
                CapturedShapesChecked.Add(caseLabel);
                failures += CheckEqual(build(url), want, "  " + caseLabel);
            }

            return failures;
        }

        /// <summary>
        /// Every shape in the capture is checked by SOMETHING. Each check records the labels it
        /// consumed and this compares the union to the file, so a shape captured and then never
        /// compared fails rather than sitting there looking like coverage.
        ///
        /// It used to be one assertion inside the Ajax check - `cases.Length == expected.Count` -
        /// which was true until the file gained six PostLink shapes that the Ajax check does not
        /// own, and then said the Ajax check was broken.
        /// </summary>
        private static readonly HashSet<string> CapturedShapesChecked = new HashSet<string>();

        private static int CheckEveryCapturedShapeIsCovered()
        {
            Console.WriteLine();
            var path = FindUpwards(Path.Combine("tools", "ajax-ground-truth", "expected.txt"));
            if (path == null) return 0;

            var captured = File.ReadAllLines(path)
                .Where(line => line.StartsWith("### "))
                .Select(line => line.Substring(4))
                .ToList();

            var missed = captured.Where(label => !CapturedShapesChecked.Contains(label)).ToList();
            return Check(missed.Count == 0,
                "  every captured shape is checked (" + CapturedShapesChecked.Count + " of "
                + captured.Count + ")" + (missed.Count == 0 ? "" : " - missing " + string.Join(", ", missed)));
        }

        /// <summary>
        /// A view component, invoked the way a diverged view invokes one.
        ///
        /// This is the check the child-action work has been missing. ForumPostList compiles and
        /// cannot be run - Forum/PostList.cshtml does not build yet - so up to now "a view
        /// component works on .NET 9" was an assumption. PlanetwarsLadder is the first that can
        /// be exercised: Planetwars/Ladder.cshtml compiles, and Planetwars/Ladder carries no
        /// filters.
        ///
        /// It goes through the real IViewComponentHelper by NAME, which is what
        /// `@await Component.InvokeAsync("PlanetwarsLadder")` compiles into - not by calling
        /// Invoke() directly, which would prove the query and skip everything that makes a view
        /// component a view component: discovery, view resolution, the ViewComponentResult.
        ///
        /// The fixture has no Galaxies and no Factions, and the component's query needs both, so
        /// the rows are made here inside a transaction that is rolled back. They are synthetic
        /// and deliberately identifiable - the assertions look for names this method wrote.
        /// </summary>
        private static async Task<int> CheckViewComponents()
        {
            Console.WriteLine();
            Console.WriteLine("view components:");

            var failures = 0;

            // COMMITTED, not a rolled-back transaction, and that is forced: the component opens
            // its own ZkDataContext, so it is a different connection and cannot see uncommitted
            // rows. The first attempt used a transaction and the component reported "Sequence
            // contains no elements" from db.Galaxies.First - it was reading the real database
            // while the rows sat in another connection's transaction.
            //
            // So the setup is written, used, and undone by hand below. Every original value is
            // captured first and restored in the finally. If this process is killed between the
            // two, the fixture is left dirty - reload it with db/load-fixture.sh.
            int factionID = 0, galaxyID = 0;
            SpringBattle battle = null;
            AutohostMode originalMode = default;
            DateTime originalStart = default;
            var originalPlayers = new List<(int AccountID, bool IsSpectator)>();
            var originalAccounts = new List<(int AccountID, int? FactionID, DateTime LastLogin)>();

            try
            {
                var names = new List<string>();
                using (var db = new ZkDataContext())
                {
                    var faction = new Faction
                    {
                        Name = "RenderCheckFaction", Shortcut = "RCF", Color = "#123456",
                        Metal = 0, Bombers = 0, Dropships = 0, Warps = 0,
                        EnergyDemandLastTurn = 0, EnergyProducedLastTurn = 0,
                        VictoryPoints = 0, IsDeleted = false,
                    };
                    db.Factions.Add(faction);

                    // WinnerFactionID so Galaxy.cshtml takes its "PlanetWars ended" branch, which
                    // is the one that invokes the ladder component. MiscVar.PlanetWarsMode reads
                    // AllOffline with no row in the database, which is the case that branch sits
                    // under, so nothing else needs seeding.
                    var galaxy = new Galaxy
                    {
                        IsDefault = true, IsDirty = false, Started = DateTime.UtcNow.AddDays(-2),
                        Width = 100, Height = 100, Turn = 0, AttackerSideCounter = 0,
                        // The navigation, not the id: the faction has not been saved yet, so its
                        // FactionID is still 0 and the foreign key would be rejected.
                        WinnerFaction = faction, EndMessage = "RenderCheckEndMessage",
                    };
                    db.Galaxies.Add(galaxy);
                    db.SaveChanges();
                    factionID = faction.FactionID;
                    galaxyID = galaxy.GalaxyID;

                    // An existing battle becomes a PlanetWars battle, which is cheaper than
                    // building one and keeps the player rows real.
                    battle = db.SpringBattles.OrderBy(b => b.SpringBattleID).First();
                    originalMode = battle.Mode;
                    originalStart = battle.StartTime;
                    battle.Mode = AutohostMode.Planetwars;
                    battle.StartTime = DateTime.UtcNow.AddDays(-1);

                    var players = db.SpringBattlePlayers
                        .Where(x => x.SpringBattleID == battle.SpringBattleID)
                        .OrderBy(x => x.AccountID).Take(2).ToList();
                    if (players.Count < 2)
                    {
                        Console.WriteLine("   FAIL  the fixture battle has fewer than two players");
                        return 1;
                    }

                    foreach (var player in players)
                    {
                        originalPlayers.Add((player.AccountID, player.IsSpectator));
                        player.IsSpectator = false;

                        var account = db.Accounts.Single(a => a.AccountID == player.AccountID);
                        originalAccounts.Add((account.AccountID, account.FactionID, account.LastLogin));
                        account.FactionID = factionID;
                        account.LastLogin = DateTime.UtcNow;
                        names.Add(account.Name);
                    }
                    db.SaveChanges();
                }

                // Ladder.cshtml calls acct.GetRating(RatingCategory.Planetwars), which indexes
                // RatingSystems.whr - empty until Init() runs, and its "unknown category"
                // fallback indexes the same empty dictionary, so the failure is a
                // KeyNotFoundException rather than a default rating.
                //
                // Init() fills that dictionary on its first line and then starts the WHR pass in
                // a background task. Only the first part matters here: with the pass unfinished,
                // GetPlayerRating reads AccountRatings from the database, which the fixture has.
                Ratings.RatingSystems.Init();

                var html = await InvokeViewComponent("PlanetwarsLadder");

                failures += Check(html.Contains("Top players"), "  the component rendered its view");
                failures += Check(html.Contains("RenderCheckFaction"),
                    "  the faction from the database reached the HTML");
                foreach (var name in names)
                {
                    failures += Check(html.Contains(name), "  " + name + " is in the ladder");
                }
                failures += Check(!html.Contains("@"), "  no unprocessed Razor markers survived");

                failures += await CheckEventsComponent();
                failures += await CheckMatchMakerAuthorization();
                failures += await CheckDivergedGalaxy(galaxyID, names);
                failures += await CheckForumPostList();
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    foreach (var original in originalAccounts)
                    {
                        var account = db.Accounts.Single(a => a.AccountID == original.AccountID);
                        account.FactionID = original.FactionID;
                        account.LastLogin = original.LastLogin;
                    }
                    foreach (var original in originalPlayers)
                    {
                        var player = db.SpringBattlePlayers.Single(
                            x => x.SpringBattleID == battle.SpringBattleID && x.AccountID == original.AccountID);
                        player.IsSpectator = original.IsSpectator;
                    }
                    if (battle != null)
                    {
                        var stored = db.SpringBattles.Single(b => b.SpringBattleID == battle.SpringBattleID);
                        stored.Mode = originalMode;
                        stored.StartTime = originalStart;
                    }
                    if (galaxyID != 0) db.Galaxies.Remove(db.Galaxies.Single(g => g.GalaxyID == galaxyID));
                    db.SaveChanges();
                    if (factionID != 0) db.Factions.Remove(db.Factions.Single(f => f.FactionID == factionID));
                    db.SaveChanges();
                }
            }

            using (var db = new ZkDataContext())
            {
                failures += Check(
                    !db.Factions.Any(f => f.Name == "RenderCheckFaction") && !db.Galaxies.Any()
                    && db.SpringBattles.Single(b => b.SpringBattleID == battle.SpringBattleID).Mode == originalMode,
                    "  the fixture is left as it was found");
            }
            return failures;
        }

        /// <summary>
        /// PlanetwarsEvents, the child action with six callers.
        ///
        /// Unlike ForumPostList this one can be run: Planetwars/Events.cshtml compiles as of the
        /// Ajax helper. So this renders the same partial six views ask for, through the same
        /// component pipeline, and checks that a row written here comes back in the HTML.
        ///
        /// It also checks the Ajax markup that the helper produces IN A REAL VIEW rather than in
        /// isolation - Events.cshtml opens with Ajax.BeginForm, and the data-ajax attributes in
        /// the output are the byte-compared ones arriving through Razor.
        ///
        /// Runs inside the ladder check's setup block, so the Events row it writes is cleaned up
        /// by the same finally.
        /// </summary>
        private static async Task<int> CheckEventsComponent()
        {
            Console.WriteLine();
            var failures = 0;
            var marker = "RenderCheckEvent-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            int eventID;
            using (var db = new ZkDataContext())
            {
                var ev = new Event { Text = marker, Time = DateTime.UtcNow, Turn = 0 };
                db.Events.Add(ev);
                db.SaveChanges();
                eventID = ev.EventID;
            }

            try
            {
                var html = await InvokeViewComponent("PlanetwarsEvents", new { partial = true, pageSize = 40 });

                failures += Check(html.Contains(marker), "  the event row reached the HTML");
                    failures += Check(html.Contains("data-ajax=\"true\""),
                    "  Ajax.BeginForm emitted data-ajax through a real view");
                failures += Check(html.Contains("data-ajax-update=\"#events\"")
                                  && html.Contains("data-ajax-loading=\"#ajaxScrollProgress\""),
                    "  the view's own AjaxOptions reached the attributes");
                failures += Check(html.Contains("<div id='events'>"), "  the partial rendered its body");
                failures += Check(!html.Contains("@"), "  no unprocessed Razor markers survived");
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var ev = db.Events.FirstOrDefault(e => e.EventID == eventID);
                    if (ev != null) { db.Events.Remove(ev); db.SaveChanges(); }
                }
            }
            return failures;
        }

        /// <summary>
        /// PlanetwarsMatchMaker refuses an anonymous viewer, and refuses EARLY.
        ///
        /// It is the first of the seven child actions carrying [Auth], and a view component
        /// inherits nothing from a filter pipeline, so the check lives in the component itself.
        /// That makes it code, and code that is not exercised is a claim rather than a fact.
        ///
        /// Two assertions, and the second is the one with teeth:
        ///
        /// - An anonymous viewer gets empty content. Global.Account is null here because nothing
        ///   populates HttpContext.Items, which is exactly the state an anonymous request is in.
        /// - It does not throw. Global.LobbyApi is null in every harness, so a component that
        ///   evaluated IsPlanetWarsMatchMakerRunning before checking the account would die with a
        ///   NullReferenceException. Passing this proves the gate comes first - reordering those
        ///   two lines fails here rather than shipping a page that renders matchmaking state to
        ///   whoever asks.
        ///
        /// The PERMIT path is not checked and cannot be: it needs an authenticated account, which
        /// waits on authentication middleware, and a running lobby server, which no harness has.
        /// This component is verified to refuse and unverified to allow.
        /// </summary>
        private static async Task<int> CheckMatchMakerAuthorization()
        {
            Console.WriteLine();
            var failures = 0;
            string html;
            try
            {
                html = await InvokeViewComponent("PlanetwarsMatchMaker");
            }
            catch (Exception e)
            {
                Console.WriteLine("   FAIL    the [Auth] gate runs before Global.LobbyApi - threw "
                                  + e.GetType().Name + ": " + e.Message);
                return 1;
            }

            failures += Check(html.Length == 0, "  an anonymous viewer gets empty content");
            failures += Check(!html.Contains("Match maker"),
                "  it did not fall through to the matchmaker body");
            return failures;
        }

        /// <summary>
        /// The diverged Galaxy.cshtml, rendered with its three view components live.
        ///
        /// This is what the whole child-action exercise was for. PortedViews/Planetwars/Galaxy
        /// replaces three @Html.Action calls with @await Component.InvokeAsync, and until now
        /// every component had been exercised on its own. Here they run where they will actually
        /// run: inside a view, activated by Razor, from one model.
        ///
        /// Two of the three appear in the output. The third, MatchMaker, is skipped by the VIEW -
        /// its call sits behind `Global.IsAccountAuthorized &amp;&amp; CanPlayerPlanetWars()`, so an
        /// anonymous render never reaches it. That is the same belt-and-braces shape as
        /// TopMenu.cshtml guarding ChatNotification, and it means the component's own [Auth]
        /// check is a second line of defence rather than the only one.
        /// </summary>
        private static async Task<int> CheckDivergedGalaxy(int galaxyID, List<string> ladderNames)
        {
            Console.WriteLine();
            var failures = 0;

            var marker = "RenderCheckGalaxyEvent-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            int eventID;
            using (var db = new ZkDataContext())
            {
                var ev = new Event { Text = marker, Time = DateTime.UtcNow, Turn = 0 };
                db.Events.Add(ev);
                db.SaveChanges();
                eventID = ev.EventID;
            }

            try
            {
                // The context stays open across the render. With lazy-loading proxies the model
                // is a GalaxyProxy, and reading Model.WinnerFaction after the context is disposed
                // throws from inside LazyLoader rather than returning null - so an entity handed
                // to a view now has to outlive nothing. This is the second consequence of
                // UseLazyLoadingProxies to surface in a harness, after the validation one.
                using (var db = new ZkDataContext())
                {
                    var galaxy = db.Galaxies.Single(g => g.GalaxyID == galaxyID);
                    var html = await Render("Planetwars/Galaxy", galaxy);

                    failures += Check(html.Contains("PlanetWars ended"), "  the diverged Galaxy rendered");
                    failures += Check(html.Contains("Top players"),
                        "  the ladder COMPONENT ran inside the view");
                    foreach (var name in ladderNames)
                        failures += Check(html.Contains(name), "  " + name + " came through the ladder component");
                    failures += Check(html.Contains(marker),
                        "  the events COMPONENT ran inside the view");
                failures += Check(html.Contains("data-ajax=\"true\""),
                        "  the events component's Ajax form reached the page");
                    failures += Check(!html.Contains("Match maker"),
                        "  the view gated MatchMaker out for an anonymous render");
                    failures += Check(!html.Contains("@"), "  no unprocessed Razor markers survived");
                }
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var ev = db.Events.FirstOrDefault(e => e.EventID == eventID);
                    if (ev != null) { db.Events.Remove(ev); db.SaveChanges(); }
                }
            }
            return failures;
        }

        /// <summary>
        /// ForumPostList, the first view component written and the last one to be run.
        ///
        /// It was written when PortedViews was introduced, as the reference for the divergence
        /// mechanism, and could not be exercised: Forum/PostList.cshtml did not compile, because
        /// it calls GridHelpers. That is what the grid work cleared, so this closes the gap - all
        /// four written components now run, not three.
        ///
        /// It is also the only one whose CALLER is diverged: Shared/CommentList.cshtml is the
        /// view that invokes it, and PortedViews holds the .NET 9 copy. What is checked here is
        /// the component; the diverged Galaxy check covers a diverged view invoking components.
        ///
        /// The fixture has 22 forum categories and no threads or posts, so a thread and a post
        /// are written here and removed afterwards - committed rather than in a transaction,
        /// because the component opens its own ZkDataContext and cannot see another connection's
        /// uncommitted rows.
        /// </summary>
        private static async Task<int> CheckForumPostList()
        {
            Console.WriteLine();
            var marker = "RenderCheckPost-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var title = "RenderCheckThread-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            // The post carries a [poll] tag, which is the one child-action call site that was
            // linked C# rather than a view: ForumParser/Tags/PollTag.cs. A BBCode tag cannot
            // invoke a view component, so it now loads the poll and renders PollView directly -
            // and this is what proves that path, through the forum parser, in a real render.
            var question = "RenderCheckPoll-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            int threadID;
            int pollID;
            using (var db = new ZkDataContext())
            {
                var poll = new Poll
                {
                    QuestionText = question,
                    IsAnonymous = false,
                    IsHeadline = false,
                    IsVisible = true,
                    RoleIsRemoval = false,
                };
                db.Polls.Add(poll);
                db.SaveChanges();
                pollID = poll.PollID;
            }

            using (var db = new ZkDataContext())
            {
                var category = db.ForumCategories.OrderBy(c => c.ForumCategoryID).First();
                var author = db.Accounts.OrderBy(a => a.AccountID).First();
                var thread = new ForumThread
                {
                    Title = title,
                    Created = DateTime.UtcNow,
                    LastPost = DateTime.UtcNow,
                    ForumCategoryID = category.ForumCategoryID,
                    CreatedAccountID = author.AccountID,
                    LastPostAccountID = author.AccountID,
                    PostCount = 1,
                    ViewCount = 0,
                    IsLocked = false,
                    IsPinned = false,
                };
                thread.ForumPosts.Add(new ForumPost
                {
                    Text = marker + " [poll]" + pollID + "[/poll]",
                    Created = DateTime.UtcNow,
                    AuthorAccountID = author.AccountID,
                    Upvotes = 0,
                    Downvotes = 0,
                });
                db.ForumThreads.Add(thread);
                db.SaveChanges();
                threadID = thread.ForumThreadID;
            }

            var failures = 0;
            try
            {
                var html = await InvokeViewComponent("ForumPostList", new { threadID });

                // One row, for the one seeded post: the query ran and the grid walked it.
                var rows = System.Text.RegularExpressions.Regex.Matches(html, "<tr class=\"(odd|even)\">").Count;
                failures += Check(rows == 1, "  the grid rendered one row for the seeded post (" + rows + ")");
                failures += Check(html.Contains("<div id='gposts'>") || html.Contains("id=\"gposts\""),
                    "  PostList.cshtml rendered its body");
                failures += Check(html.Contains("grid_table"),
                    "  the grid partials rendered inside it");
                failures += Check(html.Contains("data-ajax=\"true\""),
                    "  its Ajax form reached the page");
                failures += Check(!html.Contains("@"), "  no unprocessed Razor markers survived");

                // And what is NOT there, stated rather than left to be discovered. The row's cell
                // is empty: PostList renders each post with Html.DisplayFor, which looks for
                // Views/Shared/DisplayTemplates/ForumPost.cshtml - a view that does not compile
                // yet (CS1061) and is therefore not in this harness's set. ASP.NET Core falls back
                // to a default display rather than failing, so the page renders and says nothing.
                //
                // Asserted as absent on purpose: when that template starts compiling this check
                // fails, which is the reminder to turn it into an assertion that the text IS there.
                // Was asserted as ABSENT until DisplayTemplates/ForumPost.cshtml compiled, precisely
                // so that it would fail when the gap closed and force this line to be written.
                // It did, and this is that line.
                failures += Check(html.Contains(marker), "  the post's TEXT rendered, through its display template");
                failures += Check(html.Contains(question),
                    "  the [poll] tag rendered PollView, without a child action");
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var thread = db.ForumThreads.FirstOrDefault(t => t.ForumThreadID == threadID);
                    // ForumPosts cascade on delete, so the post goes with the thread.
                    if (thread != null) { db.ForumThreads.Remove(thread); db.SaveChanges(); }
                    var poll = db.Polls.FirstOrDefault(x => x.PollID == pollID);
                    if (poll != null) { db.Polls.Remove(poll); db.SaveChanges(); }
                }
            }
            return failures;
        }

        /// <summary>
        /// Invokes a view component by name through the MVC infrastructure, exactly as
        /// <c>@await Component.InvokeAsync("Name")</c> does from a view.
        /// </summary>
        private static async Task<string> InvokeViewComponent(string name, object arguments = null)
        {
            var provider = BuildServices();
            var tempDataProvider = provider.GetRequiredService<ITempDataProvider>();
            var httpContext = new DefaultHttpContext { RequestServices = provider };

            // An endpoint has to be present for MVC to pick the LinkGenerator-based UrlHelper
            // rather than the router-based one. There is no route table here, so Url.Action
            // returns null and hrefs come out empty - URL GENERATION IS NOT EXERCISED by this
            // check, only that the view runs and the model reaches the HTML.
            httpContext.SetEndpoint(new Microsoft.AspNetCore.Http.Endpoint(
                _ => Task.CompletedTask, Microsoft.AspNetCore.Http.EndpointMetadataCollection.Empty, "render-harness"));
            // A real request always has these; a bare DefaultHttpContext does not, and
            // Mvc5Request.Url builds "{Scheme}://{Host}..." out of them - which threw
            // UriFormatException the moment a view rendered that reads Request.Url, namely the
            // forum post display template.
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("localhost");

            PublishAmbient(provider, httpContext);

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());

            using (var writer = new StringWriter())
            {
                var viewContext = new ViewContext(actionContext, new FakeView(), viewData,
                    new TempDataDictionary(httpContext, tempDataProvider), writer, new HtmlHelperOptions());

                var helper = provider.GetRequiredService<IViewComponentHelper>();
                ((IViewContextAware)helper).Contextualize(viewContext);
                var content = arguments == null
                    ? await helper.InvokeAsync(name)
                    : await helper.InvokeAsync(name, arguments);

                using (var componentWriter = new StringWriter())
                {
                    content.WriteTo(componentWriter, System.Text.Encodings.Web.HtmlEncoder.Default);
                    return componentWriter.ToString();
                }
            }
        }

        /// <summary>A ViewContext needs an IView; a component does not use it.</summary>
        private sealed class FakeView : Microsoft.AspNetCore.Mvc.ViewEngines.IView
        {
            public string Path => "/none";
            public Task RenderAsync(ViewContext context) => Task.CompletedTask;
        }

        private static ServiceProvider BuildServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var environment = new HostingEnvironmentStub();
            services.AddSingleton<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(environment);
            services.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(environment);
            services.AddSingleton(new Microsoft.Extensions.ObjectPool.DefaultObjectPoolProvider());
            // AddApplicationPart: a console app is not an MVC application, so nothing registers
            // this assembly as a place to look for controllers or view components. Without it
            // InvokeAsync("PlanetwarsLadder") reports that no such component exists, which reads
            // like the component is wrong rather than undiscovered.
            // Routing, so IUrlHelper resolves at all. Views call @Url.Action; without this the
            // factory hands back the router-based UrlHelper, which throws "Could not find an
            // IRouter associated with the ActionContext".
            services.AddRouting();
            // The ambient request, which Global and the System.Web.HttpContext.Current shim both
            // read. UniGrid's constructor takes its page number and sort column from it - a grid
            // is built inside a view and there is nothing to pass it - so without this every
            // grid-bearing view dies with a NullReferenceException in the constructor.
            services.AddHttpContextAccessor();
            services.AddMvcCore().AddRazorViewEngine().AddViews()
                .AddApplicationPart(typeof(Program).Assembly);
            services.AddSingleton<System.Diagnostics.DiagnosticSource>(new System.Diagnostics.DiagnosticListener("zk"));
            services.AddSingleton(new System.Diagnostics.DiagnosticListener("zk"));
            return services.BuildServiceProvider();
        }

        private static async Task<string> Render(string viewPath, object model)
        {
            var provider = BuildServices();
            var engine = provider.GetRequiredService<IRazorViewEngine>();
            var tempDataProvider = provider.GetRequiredService<ITempDataProvider>();

            var result = engine.GetView(null, "/Views/" + viewPath + ".cshtml", isMainPage: true);
            if (!result.Success)
                throw new InvalidOperationException("view not found: " + viewPath + "; looked in "
                                                    + string.Join(", ", result.SearchedLocations));

            var httpContext = new DefaultHttpContext { RequestServices = provider };
            // See InvokeViewComponent: without an endpoint, @Url.Action gets the router-based
            // UrlHelper and throws. Galaxy.cshtml calls it; MapTags.cshtml does not, which is why
            // this was not needed until a second view was rendered.
            httpContext.SetEndpoint(new Microsoft.AspNetCore.Http.Endpoint(
                _ => Task.CompletedTask, Microsoft.AspNetCore.Http.EndpointMetadataCollection.Empty, "render-harness"));
            // A real request always has these; a bare DefaultHttpContext does not, and
            // Mvc5Request.Url builds "{Scheme}://{Host}..." out of them - which threw
            // UriFormatException the moment a view rendered that reads Request.Url, namely the
            // forum post display template.
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("localhost");

            PublishAmbient(provider, httpContext);

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model,
            };

            using (var writer = new StringWriter())
            {
                var viewContext = new ViewContext(actionContext, result.View, viewData,
                    new TempDataDictionary(httpContext, tempDataProvider), writer, new HtmlHelperOptions());
                await result.View.RenderAsync(viewContext);
                return writer.ToString();
            }
        }

        private sealed class HostingEnvironmentStub : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
        {
            public string EnvironmentName { get; set; } = "Development";
            public string ApplicationName { get; set; } = typeof(Program).Assembly.GetName().Name;
            public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();
            public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; }
                = new Microsoft.Extensions.FileProviders.NullFileProvider();
            public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
                = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Directory.GetCurrentDirectory());
        }

        private sealed class DiagnosticSourceStub { }
    }
}
