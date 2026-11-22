using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using ZkData;

namespace ZeroKWeb
{
    /// <summary>
    /// The ambient current-user object 46 views read, over ASP.NET Core's per-request state.
    ///
    /// MVC 5 let the application answer "who is signed in?" from a static, and Zero-K's views
    /// took it up thoroughly: Global.Account 87 times, IsModerator 31, AccountID 24. That is
    /// 69 references across 46 views, and it is the single largest thing standing between
    /// those views and compiling.
    ///
    /// **This shim deliberately keeps a pattern ASP.NET Core dropped on purpose.** Per-request
    /// state belongs to the request, and a static reachable from anywhere is why it was
    /// dropped. Keeping it is still the right move *for the port*: it changes one thing at a
    /// time, leaves 46 view files untouched, and leaves the MVC 5 build alone. Removing the
    /// ambient is a worthwhile second step, and a separate one - it means deciding what those
    /// 46 views read instead, and editing every one of them.
    ///
    /// The original reads HttpContext.Current.User and casts it to Account, because MVC 5
    /// allowed any IPrincipal. ASP.NET Core's User is a ClaimsPrincipal and cannot be an
    /// entity, so the account is carried in HttpContext.Items instead and put there by the
    /// authentication middleware the ported application will have. Until that exists this
    /// answers null, which is exactly what the original does for an anonymous request.
    /// </summary>
    public static class Global
    {
        public const int AjaxScrollCount = 40;

        private static IHttpContextAccessor accessor;

        /// <summary>Called once at startup. The static is the point - see the class note.</summary>
        public static void Configure(IHttpContextAccessor httpContextAccessor) => accessor = httpContextAccessor;

        /// <summary>Where the request's account lives, in place of MVC 5's custom IPrincipal.</summary>
        public const string AccountItemKey = "ZkAccount";

        private static HttpContext Context => accessor?.HttpContext;

        /// <summary>
        /// The same request, for the System.Web.HttpContext.Current shim next door. Exposed
        /// rather than duplicating the accessor, so there is one place that knows where the
        /// ambient request comes from.
        /// </summary>
        public static HttpContext AmbientHttpContext => Context;

        public static Account Account => Context?.Items != null && Context.Items.TryGetValue(AccountItemKey, out var a)
            ? a as Account
            : null;

        public static bool IsAccountAuthorized => Account != null;

        public static int AccountID => IsAccountAuthorized ? Account.AccountID : 0;

        public static Clan Clan => Account?.Clan;

        public static int ClanID => IsAccountAuthorized && Clan != null ? Clan.ClanID : 0;

        public static int FactionID => IsAccountAuthorized && Account.Faction != null ? Account.FactionID ?? 0 : 0;

        public static bool IsModerator => IsAccountAuthorized && Account?.AdminLevel >= AdminLevel.Moderator;

        public static bool IsSuperAdmin => IsAccountAuthorized && Account?.AdminLevel >= AdminLevel.SuperAdmin;

        public static bool IsTourneyController =>
            IsAccountAuthorized && (Account?.AdminLevel >= AdminLevel.Moderator || Account?.IsTourneyController == true);

        public static bool IsLobbyAccess => Context?.Request?.Cookies[GlobalConst.LobbyAccessCookieName] != null;

        /// <summary>
        /// MVC 5 built a UrlHelper from the ambient request; ASP.NET Core builds one from an
        /// ActionContext through a factory. Several view helpers call this to turn
        /// ("Detail", "Clans", new { id }) into a URL, and the call shape is identical on both,
        /// so only the construction differs.
        ///
        /// The ActionContext is built here rather than taken from IActionContextAccessor, which
        /// would need registering in every host that links these helpers. Routing is read off
        /// the request when it is there, so links resolve against the real route table.
        /// </summary>
        public static IUrlHelper UrlHelper()
        {
            var http = Context;
            if (http == null) return null;
            var factory = http.RequestServices?.GetService(typeof(IUrlHelperFactory)) as IUrlHelperFactory;
            if (factory == null) return null;
            var routeData = http.GetRouteData() ?? new RouteData();
            return factory.GetUrlHelper(new ActionContext(http, routeData, new ActionDescriptor()));
        }

        /// <summary>
        /// The lobby server, through Phase 1's seam - the crossable half of it, which is all
        /// this project can see. Null, because no lobby server is attached to a port harness.
        ///
        /// Null is the faithful answer when nothing is configured. The real Global sets this
        /// at application start and leaves it null when the server is not running, and the call
        /// sites already handle that: PlanetwarsEventCreator guards with
        /// `if (Global.LobbyApi != null)` before every notification it sends. Those paths skip
        /// the notification here, which is exactly what they do on a site with no lobby server.
        ///
        /// Unguarded call sites will throw, loudly, which is the right failure: it says the port
        /// has reached code that genuinely needs a running lobby server, rather than quietly
        /// pretending one answered.
        /// </summary>
        public static ZkLobbyServer.ILobbyServerApi LobbyApi => lobbyApi.Value;

        /// <summary>
        /// Built once, on first use, because it reads MiscVars and that is a database call.
        /// </summary>
        static readonly Lazy<ZkLobbyServer.ILobbyServerApi> lobbyApi =
            new Lazy<ZkLobbyServer.ILobbyServerApi>(CreateLobbyApi);

        /// <summary>
        /// A client for the lobby server when one is configured, and null when none is.
        ///
        /// **This is the switch Phase 1 built and nobody had thrown.** The note that used to be
        /// here said "nothing constructs a RemoteLobbyServerApi", and gave the reason: the shared
        /// statics had not been answered, so a switch could not safely be flipped. They have been.
        ///
        /// Null still means what it meant - no lobby server is reachable, and the call sites that
        /// guard for it skip their notifications exactly as they do on a site without one. What
        /// has changed is that setting the LobbyApiUrl MiscVar now makes this a real client
        /// instead of nothing, which is what lets the website and the server be two processes.
        ///
        /// It does NOT start a server of its own, and cannot: this is the .NET 9 build, where
        /// ZkLobbyServer is not referenced at all. Only the transport is linked - the caller's
        /// half, not the listener's.
        /// </summary>
        static ZkLobbyServer.ILobbyServerApi CreateLobbyApi()
        {
            var url = MiscVar.GetValue(ZkLobbyServer.Api.LobbyApiConfiguration.UrlKey);
            if (!ZkLobbyServer.Api.LobbyApiConfiguration.IsRemote(url)) return null;

            return ZkLobbyServer.Api.LobbyApiConfiguration.CreateClient(
                url,
                MiscVar.GetValue(ZkLobbyServer.Api.LobbyApiConfiguration.SecretKey),
                ZkLobbyServer.Api.LobbyApiConfiguration.AllowInsecureTransport(
                    MiscVar.GetValue(ZkLobbyServer.Api.LobbyApiProtocol.AllowInsecureKey)));
        }

        /// <summary>
        /// The PayPal IPN handler, which ContributionsController.Ipn hands each notification to.
        ///
        /// Constructed here, as the real Global does at application start - the payment still has to
        /// be recorded and verified, and that half needs nothing but the database and an outbound
        /// request to PayPal.
        ///
        /// **What it cannot do is announce.** The real Global wires Error and NewContribution to
        /// LobbyApi.GhostSay, and LobbyApi is null here (see above). Rather than attach handlers that
        /// would throw on a payment - losing the contribution to protect a chat message - both events
        /// are traced, saying plainly that the announcement did not happen and why. A contribution
        /// that is recorded but unannounced is a stated gap; one that is dropped is a defect.
        ///
        /// This becomes real when the port constructs a RemoteLobbyServerApi - the Phase 1 work in
        /// Zero-K.info/HOSTING.md - and is one of the things that will say whether that is wired up.
        /// </summary>
        public static PayPalInterface PayPalInterface { get; } = CreatePayPalInterface();

        static PayPalInterface CreatePayPalInterface()
        {
            var paypal = new PayPalInterface();

            paypal.Error += e => System.Diagnostics.Trace.TraceError(
                "PayPal error, NOT announced to zkdev because this host has no lobby server: {0}", e);

            paypal.NewContribution += c => System.Diagnostics.Trace.TraceInformation(
                "New contribution of {0:F2} EUR recorded, NOT announced to zk because this host has no lobby server",
                c.Euros);

            return paypal;
        }

        /// <summary>
        /// The forum's full-text indexer. The real Global constructs one at application
        /// start; nothing starts an application here, so this is created on first use and
        /// never indexes anything the site would not. ForumController only ever calls into
        /// it to say a post changed.
        /// </summary>
        public static ForumPostIndexer ForumPostIndexer { get; } = new ForumPostIndexer();

        /// <summary>
        /// The parsed-BBCode cache. The real Global constructs one at application start; nothing
        /// starts an application here, so it is created on first use. It is a plain dictionary
        /// keyed by post id and edit time, so an empty one behaves exactly like a cold one.
        /// </summary>
        public static ForumPostCache ForumPostCache { get; } = new ForumPostCache();

        /// <summary>
        /// MVC 5's Global.MapPath, which resolved a ~/ path against the site root. The same answer
        /// Mvc5Server.MapPath gives a view; this is for linked code that has no view to ask.
        /// IncludeFile and IncludeWiki read files from disk through it.
        /// </summary>
        public static string MapPath(string virtualPath)
        {
            var environment = Context?.RequestServices?.GetService(
                typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment)) as Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
            var root = environment?.WebRootPath ?? System.IO.Directory.GetCurrentDirectory();
            return System.IO.Path.Combine(root, (virtualPath ?? "").TrimStart('~', '/', '\\').Replace('/', System.IO.Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Whether this visitor arrived inside the web lobby, which _SiteLayout.cshtml:114 reads
        /// to decide whether to draw the site menu at all. This answered a hardcoded false, so the
        /// port drew the menu for a player who had asked not to see it - the web lobby embeds the
        /// site, and the menu is chrome it does not want.
        ///
        /// **Not ASP.NET Core session, and that is a decision rather than an oversight.** On
        /// MVC 5 Global.asax copies ?weblobby= into Session and this read it back. Reproducing
        /// that means AddDistributedMemoryCache, AddSession and UseSession - a server-side store
        /// behind one boolean, for a site whose Web.config configures no &lt;sessionState&gt; and
        /// whose ONLY use of session is this flag. An in-memory store also quietly assumes one
        /// instance, which is an odd thing to introduce to a port whose direction is containers.
        ///
        /// A non-persistent cookie holds the same thing with the same lifetime: set when the web
        /// lobby opens the site, carried for the rest of that browsing session, gone when the
        /// browser closes. It is client-supplied, which session state is not - and that changes
        /// nothing here, because the value it replaces came from a query string the client wrote.
        /// Forging it hides your own menu.
        ///
        /// If anything else ever needs session state, this is the decision to revisit, and it is
        /// three lines in Program.cs to do so.
        /// </summary>
        public static bool IsWebLobbyAccess => Context?.Items.ContainsKey(WebLobbyItemKey) == true;

        /// <summary>The cookie that carries the flag between requests.</summary>
        public const string WebLobbyCookie = "weblobby";

        /// <summary>
        /// Where the middleware puts the answer for THIS request. Reading the item rather than the
        /// cookie matters on one request: the one the web lobby actually opens, which carries
        /// ?weblobby= and no cookie yet, and whose layout would otherwise be the single page that
        /// still draws the menu.
        /// </summary>
        public const string WebLobbyItemKey = "zk.weblobby";

        /// <summary>
        /// The map registrar. A tripwire - see Mvc5Compat/AutoRegistratorCompat.cs for why it is
        /// one rather than a shim, and which single action reaches for it.
        /// </summary>
        public static global::AutoRegistrator.AutoRegistrator AutoRegistrator
            => new global::AutoRegistrator.AutoRegistrator();

        /// <summary>The Steam depot builder - see Mvc5Compat/AutoRegistratorCompat.cs.</summary>
        public static global::AutoRegistrator.SteamDepotGenerator SteamDepotGenerator
            => new global::AutoRegistrator.SteamDepotGenerator();

        private static readonly object awardCalculatorLock = new object();
        private static AwardCalculator awardCalculator;

        /// <summary>
        /// The monthly awards table, recomputed on a 30-minute timer.
        ///
        /// <c>AwardCalculator</c> itself is LINKED from Zero-K.info/AppCode/LadderCalculator.cs
        /// and compiles unmodified on both stacks - its three suspicious usings
        /// (EntityFramework.Extensions, ZkLobbyServer, Ratings) turn out to be dead, checked by
        /// what the file uses rather than by counting the name. It needed one production edit,
        /// <c>Database.CommandTimeout</c> to <c>SetCommandTimeoutCompat</c>, which is the
        /// extension-property move DbCompat already exists for.
        ///
        /// One behaviour difference, deliberate and named: MVC 5 builds this in
        /// Application_Start and calls RecomputeNow() there, so the site does not finish
        /// starting until a 600-second-timeout query has run. Here it is built on first use.
        /// The first visitor to /Ladders waits for that query instead of the process doing so,
        /// and every visitor after either one sees the same table. The port has no
        /// Application_Start to put it in, and a host that blocks on the database before it can
        /// serve a page is not a property worth carrying over.
        /// </summary>
        public static AwardCalculator AwardCalculator
        {
            get
            {
                if (awardCalculator != null) return awardCalculator;
                lock (awardCalculatorLock)
                {
                    if (awardCalculator == null)
                    {
                        var created = new AwardCalculator();
                        created.RecomputeNow();
                        awardCalculator = created;
                    }
                }
                return awardCalculator;
            }
        }

        /// <summary>
        /// The site's own AjaxOptions factory, copied from Zero-K.info/AppCode/Global.cs rather
        /// than linked - that file needs System.Web and cannot compile here. Sixteen of the
        /// eighteen Ajax views go through it, so the two strings below decide most of the site's
        /// AJAX markup and are duplicated literals that can drift.
        ///
        /// Duplicated rather than split out because splitting Global.cs is a production edit with
        /// a much wider blast radius than this one type; when Global.cs is ported, this goes.
        /// </summary>
        public static System.Web.Mvc.Ajax.AjaxOptions GetAjaxOptions(string targetID, bool updateHistory = true)
        {
            var ret = new System.Web.Mvc.Ajax.AjaxOptions
            {
                UpdateTargetId = targetID,
                OnComplete = string.Format("GlobalPageInit($('#{0}'))", targetID),
            };
            if (updateHistory) ret.OnSuccess = string.Format("ReplaceHistory($('#{0}').find('form').serialize())", targetID);
            return ret;
        }
    }
}
