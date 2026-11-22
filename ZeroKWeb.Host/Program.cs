using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZeroKWeb.Compat;
using ZkData;

namespace ZeroKWeb.Host
{
    /// <summary>
    /// Serves a Zero-K view over HTTP on .NET 9, through routing and a controller.
    ///
    ///     ZK_CONNECTION_STRING=...zk_test... dotnet run             request it once and check
    ///     ZK_CONNECTION_STRING=...zk_test... dotnet run -- --serve  leave it running
    ///
    /// The check is the default because this exists to be verified, not admired.
    /// </summary>
    public static class Program
    {
        private const string Url = "http://127.0.0.1:5199";

        public static async Task<int> Main(string[] args)
        {
            if (string.IsNullOrEmpty(ZkDataContext.ConnectionString))
            {
                Console.Error.WriteLine("Set ZK_CONNECTION_STRING first - see db/README.md.");
                return 2;
            }

            // Seeding a PlanetWars round, for tools/stack.sh. These do their work and stop; no
            // web application is built, because the rows have to exist BEFORE the lobby server
            // starts and the site is not involved in that.
            if (args.Contains("--seed-planetwars-round")) return SeedPlanetWarsRound();
            if (args.Contains("--remove-planetwars-round")) return RemovePlanetWarsRound();

            // The site's static content - img/, Scripts/, Styles/ - lives in Zero-K.info, and
            // the layout reads it through Server.MapPath. Without this the layout throws on
            // Directory.GetFiles("~/img/screenshots"), which is a missing web root rather than
            // anything to do with the port.
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                WebRootPath = FindSiteRoot(),
            });
            builder.Logging.SetMinimumLevel(LogLevel.Warning);
            // 127.0.0.1 for the checks, which request exactly that. A container has to bind
            // 0.0.0.0 instead or nothing outside it can connect, so ZK_HOST_URLS overrides - but
            // ONLY when serving. Honouring it during the checks would move the server without
            // moving what they ask for, and they would fail for a reason that is not about the
            // site.
            var serving = args.Contains("--serve");
            var urls = serving
                ? Environment.GetEnvironmentVariable("ZK_HOST_URLS") ?? Url
                : Url;
            builder.WebHost.UseUrls(urls);

            // Upload size, which Web.config sets for IIS and nothing set here.
            //
            //     <httpRuntime maxRequestLength="5000000" />          ASP.NET, in KILOBYTES
            //     <requestLimits maxAllowedContentLength="500000000" />   IIS, in BYTES
            //
            // The smaller of those binds, so the live site accepts roughly 477MB. Kestrel defaults
            // to 30,000,000 bytes and the multipart form reader to 128MB, so without this the port
            // answered 413 to anything over ~30MB - measured, not assumed. Map archives are
            // routinely larger than that, and UploadResource is how they arrive.
            //
            // Both limits are needed and they are different things: Kestrel's is the outer one on
            // the request body, FormOptions' applies when a multipart form is parsed, which is the
            // shape an actual upload arrives in.
            const long uploadLimit = 500_000_000;
            builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = uploadLimit);
            builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(
                options => options.MultipartBodyLengthLimit = uploadLimit);
            // The HttpPostedFileBase binder. Without it an upload action compiles and always
            // receives null - see ZeroKWeb.Core/Mvc5Compat/HttpPostedFileCompat.cs. Inserted at 0
            // so it is consulted before the built-in providers, none of which know the type.
            builder.Services.AddControllersWithViews(options =>
                options.ModelBinderProviders.Insert(0, new System.Web.HttpPostedFileBinderProvider()));
            // Web.config: <globalization uiCulture="en" culture="en-US" />. Nothing here pinned it,
            // so formatting followed whatever locale the process happened to start in - invariant
            // in the container, and whatever LANG says on a real host. A server in a de_DE locale
            // would render 1234,56 and 29.09.2026 where the site running today renders 1234.56 and
            // 9/29/2026, on every page, with nothing failing.
            //
            // Set as the thread defaults rather than through UseRequestLocalization: <globalization>
            // does not negotiate from Accept-Language unless it says culture="auto", and this one
            // does not, so a fixed default is the same behaviour and not a new one.
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = new System.Globalization.CultureInfo("en-US");
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = new System.Globalization.CultureInfo("en");

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddZkAuthentication();

            var app = builder.Build();

            // ZK_RENDERED_VIEWS: append the path of every Razor view that actually RENDERS.
            //
            // 123 views compile. That is checked, and it is not the same claim as any of them
            // having executed - News/Index.cshtml compiled for weeks and threw on its first
            // request. This is how the difference gets counted rather than guessed at: run the
            // harness with the variable set and diff what comes out against the inventory.
            //
            // Off unless the variable is set, so it costs a request nothing in normal runs.
            var renderedLog = Environment.GetEnvironmentVariable("ZK_RENDERED_VIEWS");
            if (!string.IsNullOrEmpty(renderedLog))
            {
                var seen = new System.Collections.Concurrent.ConcurrentDictionary<string, byte>();
                var listener = app.Services.GetRequiredService<System.Diagnostics.DiagnosticListener>();
                listener.Subscribe(new RenderedViewRecorder(seen, renderedLog));
            }

            // The ambient Global the views read. Nothing signs anyone in yet, so it answers
            // the same as it does for an anonymous request - see Mvc5Compat/GlobalCompat.cs.
            ZeroKWeb.Global.Configure(app.Services.GetRequiredService<IHttpContextAccessor>());

            // **Phase 1 coupling, in a shape the InProcess count does not see.**
            //
            // The website reads Ratings.RatingSystems and Ratings.MapRatings as STATICS, in
            // eight files - ChartsController, HomeController, AdminController,
            // PlanetwarsAdminController, WhrController, HtmlHelperExtensions.Portable,
            // Ladders/Ladders.cshtml and Factions/FactionBox.cshtml - and nothing in the website
            // ever fills them. The only caller of either Init() is ZkLobbyServer.ZkLobbyServer,
            // which the live site starts IN ITS OWN PROCESS. So these pages work today because
            // the lobby server happens to share their memory.
            //
            // That is not an API call, so it never appeared in the count of LobbyApi.InProcess
            // uses that Phase 1 has been tracking, and moving the lobby server out will break
            // /Ladders, /Ladders/Maps, /Charts and the rating shown beside every player - with a
            // KeyNotFoundException, not a default rating, because the "unknown category"
            // fallback indexes the same empty dictionary.
            //
            // CreateRatingSystems, not Init: this process READS ratings, it does not compute them.
            // Init would start a WHR pass over every battle in the database here as well as in the
            // lobby server - two processes doing the same expensive work and disagreeing about the
            // answer. The reader half creates the systems and stops, and each one then serves
            // GetPlayerRating out of the AccountRatings table, which is the rating of record.
            //
            // This is the shape the website wants once the lobby server is elsewhere, so the
            // harness runs it rather than the shape that only works when they share a process.
            Ratings.RatingSystems.CreateRatingSystems();
            Ratings.MapRatings.Init();

            // The site's own error log. Global.StartApplication does exactly this line, and it is
            // the only thing anywhere that writes LogEntries - the table Admin/TraceLogs reads.
            // Without it the port's Trace.TraceError calls went nowhere and that page was
            // permanently empty, which no check noticed because the page renders either way.
            //
            // The constructor also drops entries older than 14 days, which is the retention the
            // live site has always had and is left exactly as it was.
            //
            // NOT installed when the lobby server's own standalone process traces: that one adds a
            // ConsoleTraceListener instead, so in the split architecture its traces go to
            // `docker logs` rather than to this table. Before Phase 1 it ran in this process and
            // was caught by this listener. See HOSTING.md - restoring that is one line and a
            // decision about write volume, not a port question.
            System.Diagnostics.Trace.Listeners.Add(new ZkLobbyServer.ZkServerTraceListener());

            // HarnessController is in this project, so whatever this project is deployed as serves
            // it. That is fine while this is only a test host and is not fine for a minute longer
            // than that: /Harness/Whoami names the signed-in account and its admin level,
            // /Harness/Throw makes the site answer 500 on demand, /Harness/Upload takes a file,
            // /Harness/Token hands out an anti-forgery token and /Harness/Slow sleeps for as long
            // as it is asked to. None of them is reachable by accident, and all of them are
            // reachable by anyone who guesses the path.
            //
            // So they are OFF when this is serving, and on only for the two things that need them:
            // this process running its own checks, and the site container in tools/stack.sh, which
            // the lobby probe asks /Harness/Whoami to prove single sign-on worked.
            //
            // 404 rather than 403, because 403 tells you the endpoint is there.
            var harnessEndpoints = !serving
                                   || Environment.GetEnvironmentVariable("ZK_HARNESS_ENDPOINTS") == "1";
            if (!harnessEndpoints)
                app.Use(async (context, next) =>
                {
                    if (context.Request.Path.StartsWithSegments("/Harness"))
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }
                    await next();
                });

            // Order matters and is not interchangeable: UseAuthentication populates
            // HttpContext.User from the cookie, UseZkAccount turns that name into an Account and
            // publishes it where Global reads it, and UseAuthorization runs [Auth] against the
            // result. Putting UseZkAccount first leaves every [Auth] page redirecting a signed-in
            // visitor, which looks exactly like a broken cookie.
            app.UseAuthentication();

            // Global.asax does this at the top of PostAuthenticateRequest, so it sits here, in the
            // same place in the pipeline:
            //
            //     if (DateTime.UtcNow.Subtract(lastPollCheck).TotalMinutes > 60)
            //     {
            //         PollController.AutoClosePolls(); // this is silly here, should be a
            //         lastPollCheck = DateTime.UtcNow; // seaprate timer/thread
            //     }
            //
            // Without it a headline poll with an expiry NEVER closes on this host. That is not
            // cosmetic: AutoClosePolls is what counts the votes on a PlanetWars role election,
            // grants or removes the role, writes the event and PMs the winner, and then deletes
            // the poll. An election that has ended simply stays open, forever, and the vote has
            // no effect.
            //
            // The author's own comment says this belongs on a timer, and it does. It is ported as
            // the thing it is rather than the thing it should be, because a hosted service would
            // also run it on a site with no traffic, which is a behaviour change and somebody
            // else's call to make.
            app.Use(async (context, next) =>
            {
                PollAutoClose.TickIfDue();
                await next();
            });

            app.UseZkAccount();
            app.UseAuthorization();

            // Global.asax's OnPostAcquireRequestState, which fires just after the one above. It
            // carries ?weblobby= for the rest of the browsing session, and _SiteLayout reads it to
            // decide whether to draw the site menu at all - the web lobby embeds this site, and
            // the menu is chrome it does not want.
            app.UseWebLobbyFlag();

            // Home/Index as the default, like the MVC 5 route table - not Forum. With Forum as the
            // default, Html.ActionLink("Forum index", "Index") renders as "/" because routing elides
            // the defaults, which is correct and makes the link impossible to tell apart from a
            // broken one. The first version of this check asserted the wrong URL for that reason.
            // What MVC 5's MvcApplication_Error does, and nothing more.
            //
            // On 4.8 an unhandled request exception reaches Global.asax, which writes it with
            // Trace.TraceError - and Global.StartApplication has put a ZkServerTraceListener on
            // Trace.Listeners, so it lands in the log the Admin/TraceLogs page reads. ASP.NET Core
            // logs through ILogger instead, so on the port those exceptions went to Kestrel's
            // console and nowhere the site can see.
            //
            // That comment used to end here, saying the Trace call was the half that belongs to
            // serving a request - which was true and was also only half the job. The LISTENER was
            // never installed, so every one of these traces went nowhere: LogEntries stayed empty
            // and Admin/TraceLogs was a working page with nothing to show. It is installed below,
            // before any of this can fire.
            //
            // The "does not implement IController" filter is copied verbatim: on 4.8 that message
            // is what a request for a missing controller produces, and it was noise worth dropping.
            // It is kept so this reproduces the original rather than improving on it.
            //
            // The exception is RETHROWN. MVC 5 leaves it for customErrors to turn into a page, and
            // choosing what this host shows instead would be inventing behaviour, not porting it.
            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("was not found or does not implement IController"))
                        System.Diagnostics.Trace.TraceError(ex.ToString());
                    throw;
                }
            });

            // The seven routes Global.asax.RegisterRoutes declares BEFORE the default one, and
            // that order is the whole point: each of these would otherwise be swallowed by
            // {controller}/{action}/{id}, which binds a third segment to `id` and nothing else.
            //
            // Every one of them was a 404 or a 500 on this host until now, measured:
            //
            //     /Wiki/Commander                404   action "Commander" does not exist
            //     /p/zero-k/wiki/Commander       404   three literal segments match nothing
            //     /Missions/File/5011            500   binds id, the action takes name, Single() throws
            //     /Missions/Img/5011             404
            //     /Replays/test.sdfz             404   action "test.sdfz" does not exist
            //     /Static/UnitGuide              404
            //
            // These are not obscure: /Replays/{name} is how a replay is downloaded and
            // /Missions/File/{name} is how a mission is. The harness reached the Static page as
            // /Static?name=UnitGuide and the mission file as /Missions/File?name=5011 - both work,
            // because the actions take those parameters by name, and both quietly worked AROUND
            // the missing route rather than finding it.
            //
            // UrlParameter.Optional becomes {node?}. The literal "p/zero-k/wiki" route is a legacy
            // path and is kept because it is in Global.asax; removing it is a separate decision.
            app.MapControllerRoute("WikiPage", "Wiki/{node?}", new { controller = "Wiki", action = "Index" });
            app.MapControllerRoute("WikiPage2", "p/zero-k/wiki/{node?}", new { controller = "Wiki", action = "Index" });
            // Dead on BOTH stacks and kept anyway: there is no Img action on MissionsController,
            // so this route resolves to nothing and 404s wherever it is registered. Reproduced
            // because the port's list is meant to be Global.asax's list, which is a property
            // somebody can check; deleting it here would make the two differ for a reason that
            // then has to be remembered.
            app.MapControllerRoute("MissionImage", "Missions/Img/{name?}", new { controller = "Missions", action = "Img" });
            app.MapControllerRoute("MissionFile", "Missions/File/{name?}", new { controller = "Missions", action = "File" });
            app.MapControllerRoute("ReplayFile", "Replays/{name?}", new { controller = "Replays", action = "Download" });
            app.MapControllerRoute("StaticFile", "Static/{name?}", new { controller = "Static", action = "Index" });
            app.MapControllerRoute("RedeemCode", "Contributions/Redeem/{code?}",
                new { controller = "Contributions", action = "Redeem" });

            app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

            // The OTHER route table, and NOTHING is registered for it here on purpose.
            //
            // Application_Start calls GlobalConfiguration.Configure(WebApiConfig.Register), which
            // on .NET Framework builds a second, independent pipeline: Web API's own routes, its
            // own controller base, its own table. Auditing RegisterRoutes found seven missing MVC
            // routes and said nothing about this one, because it is a different table in a
            // different file - and the port served no /api surface at all. The site serves
            //
            //     POST /api/whr/battles
            //
            // through WhrController, and nothing in this repository calls it, so the consumer is
            // outside the repo and would have found out at cutover.
            //
            // What fixes it is LINKING that controller (port-sources.props) with an ApiController
            // base to compile against - see ZeroKWeb.Core/Mvc5Compat/ApiControllerCompat.cs. No
            // routing call is needed: MapControllerRoute above already builds the controller
            // endpoint data source, and that includes attribute-routed actions, so
            // [Route("api/whr/battles")] is live the moment the class exists.
            //
            // An app.MapControllers() was written here first, on the assumption that attribute
            // routing needed turning on. Removing it changed nothing - the endpoint still
            // answered 200 - while unlinking the controller took it to 404. So the line was
            // deleted rather than kept for looks.
            //
            // WebApiConfig's other line, the api/{controller}/{id} convention route, is
            // deliberately NOT reproduced. On .NET Framework it can only ever match an
            // ApiController, because the Web API table is a separate table. ASP.NET Core has one
            // controller model, so the same template here would put EVERY controller on the site
            // under /api/ - /api/Home/Index would reach HomeController, which the live site does
            // not do. Reproducing the line would reproduce the text and not the behaviour.

            // content/ is the fifth, and it is the one a GAME CLIENT follows rather than a
            // browser. ResourceLinkProvider hands out {BaseSiteUrl}/content/{maps|games}/{file}
            // as a download link whenever that file is on the site's disk - so the site
            // advertises these URLs itself, and until this they answered 404 on the port while
            // IIS served them.
            //
            // Map and game archives also need their media type spelled out. Web.config does it
            // for IIS:
            //
            //     <mimeMap fileExtension=".sd7" mimeType="application/octet-stream" />
            //     <mimeMap fileExtension=".sdz" mimeType="application/octet-stream" />
            //
            // ASP.NET Core's FileExtensionContentTypeProvider knows neither, and
            // ServeUnknownFileTypes is false by default - which does not mean "serve it with no
            // type", it means 404. So the same two extensions are added here and no others:
            // .sdp is scanned by PlasmaResourceChecker but has no mimeMap either, so IIS does
            // not serve it today and neither should this. Reproduce, do not improve.
            var archives = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            archives.Mappings[".sd7"] = "application/octet-stream";
            archives.Mappings[".sdz"] = "application/octet-stream";

            foreach (var assets in new[] { "img", "Scripts", "Styles", GlobalConst.ResourceFolder, "content" })
            {
                // CREATED rather than skipped, and that is a fix rather than a convenience. The
                // mapping is decided once at startup, so skipping a directory that is not there
                // yet means it stays unserved for the life of the process - and content/ and
                // Resources/ are written by AutoRegistrator ON a deployment, after the site is
                // already running. A deploy in that order advertised download links that answered
                // 404 until somebody restarted the site, with nothing to say why.
                //
                // It costs a checkout nothing, which is what the previous comment here was
                // protecting: git does not track empty directories, so creating them leaves no
                // untracked noise. PhysicalFileProvider throws if its root is missing, which is
                // why the old code skipped rather than mapped.
                var directory = System.IO.Path.Combine(FindSiteRoot(), assets);
                System.IO.Directory.CreateDirectory(directory);

                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(directory),
                    RequestPath = "/" + assets,
                    ContentTypeProvider = archives,
                });
            }

            if (serving)
            {
                // Static content, and ONLY these three directories. WebRootPath above is the site
                // SOURCE tree - Zero-K.info - so a plain UseStaticFiles over it would serve
                // Web.config and every .cs file in the project. Named folders, mapped one at a
                // time, is the difference between serving a site and publishing its source.
                //
                // The bundles stay serve-only, and that part IS deliberate: registering them sets
                // Bundles.BuiltPath, which changes the page from eleven script tags to one per
                // bundle - and eleven is what the harness asserts. The asset directories moved
                // above this block, because "the checks assert HTML" stopped being true the moment
                // /content/maps had something worth asserting that is not HTML.
                // The built bundles, when tools/build-assets.mjs has produced them. Without them
                // the page lists eleven scripts individually, which is the developer answer and
                // what the harness asserts; with them it is one tag per bundle, which is what the
                // real bundler does and what the container ships.
                // Next to the binary only, which is the container's layout - NOT the repository's
                // build/ directory. That fallback was here for one commit and made the harness
                // non-deterministic: it would serve bundles on a machine that had run the asset
                // build and individual files on one that had not, so the same check meant
                // different things in two places. The container is where the bundled path is
                // exercised; ZK_BUNDLES overrides for anyone who wants it locally.
                var built = Environment.GetEnvironmentVariable("ZK_BUNDLES")
                            ?? System.IO.Path.Combine(AppContext.BaseDirectory, "bundles");

                if (System.IO.Directory.Exists(built))
                {
                    app.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(built),
                        RequestPath = "/bundles",
                    });
                    System.Web.Optimization.Bundles.BuiltPath = "/bundles";
                    Console.WriteLine("serving built bundles from " + built);
                }

                // Resources is the fourth because it is content the site PUBLISHES, not content
                // it displays: {BaseSiteUrl}/Resources/{name}.metadata.xml.gz is where the game
                // client downloads map and game metadata from, and now where a lobby server in
                // another process falls back to when it cannot read the website's disk. IIS
                // serves it as static content today; nothing here did.
                //
                // The directory is not in the repository - AutoRegistrator writes it on a
                // deployment - and the loop below skips what is not there, so this costs a
                // checkout nothing.
                // LAST, after every UseStaticFiles - see the note at the other call below.
                app.UseDosProtection();

                Console.WriteLine("serving on " + urls + " - try /Home/NotLoggedIn, /Tourney, /Harness/ForumPath/3");
                await app.RunAsync();
                return 0;
            }

            // The rate limiter goes in LAST, after every UseStaticFiles, and that position IS the
            // static-file exemption: Global.asax spells it `HttpContext.Handler == null`, which is
            // how a file IIS serves without a managed handler presents there, and here a static
            // file is simply answered before this is reached.
            //
            // It has to be said twice because the serving branch above ends in RunAsync and never
            // comes back. Registering it once, higher up, would have put it in FRONT of the three
            // directories only the serving branch maps - img, css and Resources - and rate-limited
            // them. Fifteen parallel requests is one ordinary page's worth of images.
            app.UseDosProtection();

            await app.StartAsync();
            try
            {
                return await CheckItServes();
            }
            finally
            {
                await app.StopAsync();
            }
        }


        /// <summary>
        /// An upload actually arrives.
        ///
        /// This is the check the HttpPostedFileBase decision rests on. A wrapper type alone
        /// compiles and ASP.NET Core has nothing that produces it, so every upload action would
        /// receive null and silently do nothing - which is exactly why the type was left
        /// unshimmed for most of this port. So: POST a real multipart form and read back what the
        /// action saw.
        ///
        /// It asserts the name, the length and THE BYTES. Name and length alone would pass for a
        /// binder that produced a wrapper around an empty stream, which is the failure closest to
        /// the one being guarded against.
        /// </summary>
        /// <summary>
        /// The PayPal IPN read, over real HTTP.
        ///
        /// This is the check ContributionsController rests on, and it exists because the defect it
        /// guards against produces no error. ImportIpnPayment takes the parsed fields AND the raw
        /// bytes, and VerifyRequest posts those bytes back to PayPal - so if reading the form
        /// consumes the body, as it does in ASP.NET Core, the literal translation of the MVC 5 call
        /// site records the contribution and then asks PayPal to verify nothing. Every payment would
        /// be stamped VERIFICATION FAILED on a site that logged no error at all.
        ///
        /// So both halves are asserted from ONE request: the exact bytes, and the fields parsed out
        /// of those same bytes afterwards. Asserting either alone would pass while the other was
        /// broken, which is the whole shape of this bug.
        /// </summary>
        private static async Task<int> CheckIpnBodyRead()
        {
            Console.WriteLine();
            Console.WriteLine("paypal ipn:");

            var failures = 0;

            // A real IPN body shape, including a repeated key and one that needs URL decoding.
            const string body = "txn_id=8KM12345&payment_status=Completed&item_name=ZK_ID_42_JAR_1&mc_gross=5.00&payer_email=a%40b.c";
            var expected = string.Concat(Array.ConvertAll(System.Text.Encoding.ASCII.GetBytes(body), b => b.ToString("X2")));

            using (var client = new HttpClient())
            {
                var content = new StringContent(body, System.Text.Encoding.ASCII, "application/x-www-form-urlencoded");
                var response = await client.PostAsync(Url + "/Harness/Ipn?probe=query", content);
                var read = await response.Content.ReadAsStringAsync();

                failures += Check(response.StatusCode == System.Net.HttpStatusCode.OK,
                    "  the IPN read was reached (" + (int)response.StatusCode + ")");

                // The one that matters: the bytes are still there AFTER the form was parsed.
                failures += Check(read.Contains("rawlength=" + body.Length),
                    "  the raw body is the full length, not consumed by the form read");
                failures += Check(read.Contains("raw=" + expected),
                    "  the raw body is byte for byte what PayPal would have to verify");

                failures += Check(read.Contains("txn_id=8KM12345"),
                    "  the fields parsed out of the same body");
                failures += Check(read.Contains("payer_email=a@b.c"),
                    "  percent-encoding was decoded, as Request.Params did");
                failures += Check(read.Contains("probe=query"),
                    "  the query string is merged in, as MVC 5's Params did");
            }

            return failures;
        }

        /// <summary>
        /// /MissionService, the JSON endpoint that exists so the mission editor can stop needing
        /// WCF - requested for the first time on .NET 9.
        ///
        /// Compiling proved the controller's types; this proves the path. One POST exercises the
        /// route, the raw body read (the same RequestInputStream the IPN handler uses), dispatch by
        /// request class name through CommandJsonSerializer, an EF Core query, and serialization of
        /// the response.
        ///
        /// **The fixture has no missions**, so the list assertion deliberately claims only that the
        /// envelope is right - an empty Missions list is exactly what this database should produce,
        /// and saying more would be the "empty list behind a 200" this harness exists to distrust.
        /// The second request is what carries the weight: a mission that does not exist must come
        /// back as a DeleteMissionResponse carrying Error, because WCF turned that exception into a
        /// fault and this endpoint turns it into a field. A 500 would mean the difference was lost.
        /// </summary>
        private static async Task<int> CheckMissionServiceJson()
        {
            Console.WriteLine();
            Console.WriteLine("/MissionService (json):");

            var failures = 0;

            using (var client = new HttpClient())
            {
                var list = await client.PostAsync(Url + "/MissionService",
                    new StringContent("ListMissionInfosRequest {}", System.Text.Encoding.UTF8, "text/plain"));
                var listBody = await list.Content.ReadAsStringAsync();

                failures += Check(list.StatusCode == System.Net.HttpStatusCode.OK,
                    "  the endpoint answered (" + (int)list.StatusCode + ")");
                failures += Check(listBody.StartsWith("ListMissionInfosResponse"),
                    "  dispatched by request class, and answered with the matching response");
                failures += Check(listBody.Contains("Missions"),
                    "  the response envelope carries its Missions field");

                // The header, which six assertions on the body do not cover. MissionServiceController
                // returns Content(response, "application/json"); nothing said so until /ContentService
                // got the same assertion and the omission here became obvious.
                failures += Check(list.Content.Headers.ContentType?.MediaType == "application/json",
                    "  as application/json (" + (list.Content.Headers.ContentType?.MediaType ?? "none") + ")");

                var missing = await client.PostAsync(Url + "/MissionService",
                    new StringContent(
                        "DeleteMissionRequest {\"MissionID\":999999,\"Delete\":true,\"Author\":\"nobody\",\"Password\":\"x\"}",
                        System.Text.Encoding.UTF8, "text/plain"));
                var missingBody = await missing.Content.ReadAsStringAsync();

                failures += Check(missing.StatusCode == System.Net.HttpStatusCode.OK,
                    "  a failing operation is still a 200 (" + (int)missing.StatusCode + ")");
                failures += Check(missingBody.StartsWith("DeleteMissionResponse"),
                    "  and answers with its own response type");
                failures += Check(missingBody.Contains("No such mission found"),
                    "  the exception came back in Error, as WCF's fault did");
            }

            return failures;
        }

        private static async Task<int> CheckUploadBinding()
        {
            Console.WriteLine();
            Console.WriteLine("uploads:");

            var failures = 0;
            var bytes = new byte[] { 0x5A, 0x4B, 0x00, 0xFF, 0x10, 0x20 };

            using (var client = new HttpClient())
            {
                using (var form = new MultipartFormDataContent())
                {
                    var file = new ByteArrayContent(bytes);
                    file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                    form.Add(file, "upload", "probe.bin");

                    var response = await client.PostAsync(Url + "/Harness/Upload", form);
                    var body = await response.Content.ReadAsStringAsync();

                    failures += Check(response.StatusCode == System.Net.HttpStatusCode.OK,
                        "  the upload action was reached (" + (int)response.StatusCode + ")");
                    failures += Check(body.Contains("name=probe.bin"),
                        "  the file name was bound");
                    failures += Check(body.Contains("length=" + bytes.Length),
                        "  ContentLength is the real length");
                    failures += Check(body.Contains("bytes=5A4B00FF1020"),
                        "  InputStream carried the actual bytes");
                }

                // No file at all: MVC 5 handed the action null, and two call sites test for it.
                using (var empty = new MultipartFormDataContent())
                {
                    empty.Add(new StringContent("x"), "somethingelse");
                    var body = await (await client.PostAsync(Url + "/Harness/Upload", empty)).Content.ReadAsStringAsync();
                    failures += Check(body.Contains("null"),
                        "  an absent file binds to null, as MVC 5 did");
                }
            }
            return failures;
        }

        /// <summary>
        /// Signing in, end to end: the real password check, a real cookie, and [Auth] telling the
        /// difference between "not signed in" and "not allowed".
        ///
        /// This is the check that says identity works, and it is worth being precise about what
        /// it proves. It does NOT use a back door - the password goes through ZkAuth.Verify,
        /// which is Account.AccountVerify and a BCrypt comparison, and the account is recovered
        /// from the cookie by the same middleware a browser goes through.
        ///
        /// The fixture stores PasswordBcrypt as NULL for every account, so one is set here with
        /// Account.SetPasswordPlain - production code - and put back afterwards.
        ///
        /// The three status codes are the point:
        ///   anonymous     -> 302, redirected to NotLoggedIn
        ///   signed in     -> 403 on a page whose [Auth] names a Role the account lacks
        ///   signed in     -> Whoami names the account
        /// A port that recognised nobody would give 302 for all three, and a port that ignored
        /// roles would give 200.
        /// </summary>
        private static async Task<int> CheckSignIn()
        {
            Console.WriteLine();
            Console.WriteLine("signing in:");

            const string password = "harness-check-password";
            string name;
            int accountID;
            string originalHash;
            AdminLevel originalAdminLevel;

            using (var db = new ZkDataContext())
            {
                var account = db.Accounts.OrderBy(a => a.AccountID).First();
                name = account.Name;
                accountID = account.AccountID;
                originalHash = account.PasswordBcrypt;

                // The role assertion below reads 403 as "recognised, then refused". That only
                // holds if the account HAS no role, and `ZkData.Core -- grant` can have given it
                // one - which is exactly what happened while testing by hand, turning the 403
                // into a 200 and the check into a puzzle. The check owns this state now.
                originalAdminLevel = account.AdminLevel;
                account.AdminLevel = AdminLevel.None;

                account.SetPasswordPlain(password);
                db.SaveChanges();
            }

            var failures = 0;
            try
            {
                var handler = new HttpClientHandler { UseCookies = true, CookieContainer = new System.Net.CookieContainer(), AllowAutoRedirect = false };
                using (var client = new HttpClient(handler))
                {
                    var anonymous = await client.GetAsync(Url + "/Charts");
                    failures += Check(anonymous.StatusCode == System.Net.HttpStatusCode.Redirect,
                        "  an anonymous request to an [Auth] page is redirected (" + (int)anonymous.StatusCode + ")");

                    var wrong = await client.PostAsync(Url + "/Harness/Login", new FormUrlEncodedContent(
                        new[] { new KeyValuePair<string, string>("login", name),
                                new KeyValuePair<string, string>("password", password + "-wrong") }));
                    failures += Check((await wrong.Content.ReadAsStringAsync()).Contains("Invalid login"),
                        "  a wrong password is refused");

                    var signIn = await client.PostAsync(Url + "/Harness/Login", new FormUrlEncodedContent(
                        new[] { new KeyValuePair<string, string>("login", name),
                                new KeyValuePair<string, string>("password", password) }));
                    failures += Check(signIn.StatusCode == System.Net.HttpStatusCode.Redirect,
                        "  the right password signs in");

                    // How long that cookie is good for. Web.config says timeout="2880" - minutes,
                    // so two days - and the port had 30 days, which nobody had written down as a
                    // decision. A stolen cookie outliving the Framework site's by fifteen times is
                    // not something a status code would ever show.
                    var authCookie = signIn.Headers.TryGetValues("Set-Cookie", out var setCookies)
                        ? setCookies.FirstOrDefault(x => x.StartsWith("ZkAuth="))
                        : null;
                    failures += Check(authCookie != null, "  and sets the auth cookie");
                    if (authCookie != null)
                    {
                        var expires = System.Text.RegularExpressions.Regex.Match(authCookie, @"expires=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        var lifetime = expires.Success && DateTime.TryParse(expires.Groups[1].Value,
                                           System.Globalization.CultureInfo.InvariantCulture,
                                           System.Globalization.DateTimeStyles.AdjustToUniversal, out var when)
                            ? when - DateTime.UtcNow
                            : (TimeSpan?)null;
                        failures += Check(lifetime != null && lifetime.Value.TotalHours > 47 && lifetime.Value.TotalHours < 49,
                            "  and it lasts the 48 hours Web.config asks for, not longer ("
                            + (lifetime?.TotalHours.ToString("0.0") ?? "no expiry") + "h)");
                    }

                    var whoami = await (await client.GetAsync(Url + "/Harness/Whoami")).Content.ReadAsStringAsync();
                    failures += Check(whoami.Contains("signed in as " + name),
                        "  the cookie comes back as an Account (" + whoami.Trim() + ")");

                    // The layout, signed in. TopMenu calls a child action for authenticated
                    // visitors only, so this page was 200 anonymous and 500 signed in until that
                    // call became a view component - on EVERY page, because TopMenu is the layout.
                    // Anonymous rendering cannot catch that, which is why it is asserted here.
                    var layout = await client.GetAsync(Url + "/Home/NotLoggedIn");
                    var layoutHtml = await layout.Content.ReadAsStringAsync();
                    failures += Check(layout.StatusCode == System.Net.HttpStatusCode.OK,
                        "  a page with the layout still renders when signed in ("
                        + (int)layout.StatusCode + ")");
                    failures += Check(layoutHtml.Contains("menu"),
                        "  TopMenu rendered for a signed-in visitor");

                    // 403, not 302: the account is recognised and then found to lack the Role.
                    var authorized = await client.GetAsync(Url + "/Charts");
                    failures += Check(authorized.StatusCode == System.Net.HttpStatusCode.Forbidden,
                        "  a signed-in request is refused by ROLE, not by identity ("
                        + (int)authorized.StatusCode + ")");

                    // The ban check. Global.asax writes three lines and calls Response.End() for a
                    // site-banned account; nothing about the port would have complained if that
                    // had been left out of the middleware, and a banned account would simply have
                    // browsed. So it is exercised rather than trusted.
                    int punishmentID;
                    using (var db = new ZkDataContext())
                    {
                        var punishment = new Punishment
                        {
                            AccountID = accountID,
                            BanSite = true,
                            BanExpires = DateTime.UtcNow.AddHours(1),
                            Reason = "harness ban check",
                            CreatedAccountID = accountID,
                            // NOT NULL, and a default DateTime is 0001-01-01, which SQL Server's
                            // datetime cannot hold - it starts at 1753.
                            Time = DateTime.UtcNow,
                        };
                        db.Punishments.Add(punishment);
                        db.SaveChanges();
                        punishmentID = punishment.PunishmentID;
                    }
                    try
                    {
                        var banned = await client.GetAsync(Url + "/Harness/Whoami");
                        var bannedBody = await banned.Content.ReadAsStringAsync();
                        failures += Check(bannedBody.Contains("You are banned!"),
                            "  a site-banned account is stopped, with the reason");
                        failures += Check(!bannedBody.Contains("signed in as"),
                            "  and does not reach the page it asked for");
                    }
                    finally
                    {
                        using (var db = new ZkDataContext())
                        {
                            var punishment = db.Punishments.FirstOrDefault(x => x.PunishmentID == punishmentID);
                            if (punishment != null) { db.Punishments.Remove(punishment); db.SaveChanges(); }
                        }
                    }

                    await client.GetAsync(Url + "/Harness/Logout");
                    var after = await (await client.GetAsync(Url + "/Harness/Whoami")).Content.ReadAsStringAsync();
                    failures += Check(after.Contains("not signed in"), "  signing out takes it away again");
                }
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var account = db.Accounts.Single(a => a.AccountID == accountID);
                    account.PasswordBcrypt = originalHash;
                    account.AdminLevel = originalAdminLevel;
                    db.SaveChanges();
                }
            }
            return failures;
        }

        /// <summary>The real site's folder, found by walking up from the binary.</summary>
        private static string FindSiteRoot()
        {
            var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = System.IO.Path.Combine(dir.FullName, "Zero-K.info");
                if (System.IO.Directory.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            throw new System.IO.DirectoryNotFoundException("could not find Zero-K.info above " + AppContext.BaseDirectory);
        }

        private static async Task<int> CheckItServes()
        {
            int category;
            string expectedTitle;
            using (var db = new ZkDataContext())
            {
                var deepest = db.ForumCategories.AsNoTracking()
                    .Where(x => x.ParentForumCategoryID != null)
                    .OrderBy(x => x.ForumCategoryID)
                    .FirstOrDefault()
                    ?? db.ForumCategories.AsNoTracking().OrderBy(x => x.ForumCategoryID).First();
                category = deepest.ForumCategoryID;
                expectedTitle = deepest.Title;
            }

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(Url + "/Harness/ForumPath/" + category);
                var html = await response.Content.ReadAsStringAsync();

                Console.WriteLine("GET /Harness/ForumPath/" + category + " -> " + (int)response.StatusCode);
                Console.WriteLine();
                Console.WriteLine(html.Trim());
                Console.WriteLine();

                var failures = 0;
                failures += Check(response.IsSuccessStatusCode, "the request succeeded");
                failures += Check(html.Contains(expectedTitle),
                    "the category came out of the database and into the HTML (" + expectedTitle + ")");
                // Html.ActionLink is why this project exists: it needs routing to produce a URL.
                //
                // /Harness, not /Forum. ForumPath.cshtml writes ActionLink("Forum index", "Index")
                // with no controller named, so it resolves against the AMBIENT one - which is this
                // harness controller, not Forum. That changed when the real ForumController was
                // linked into this project and the harness's had to stop sharing its name.
                //
                // What the check is for is unaffected: the link still goes through routing rather
                // than being a literal, and "Index" is elided as the route default, which is the
                // behaviour that made a Forum default route indistinguishable from a broken link.
                failures += Check(html.Contains("href=\"/Harness\""),
                    "Html.ActionLink resolved through routing to the ambient controller");
                failures += Check(!html.Contains("@"), "no unprocessed Razor markers survived");

                failures += await CheckItServesAPage(client);
                failures += await CheckSignIn();
                failures += await CheckUploadBinding();
                failures += await CheckIpnBodyRead();
                failures += await CheckMissionServiceJson();
                failures += await CheckLadders(client);
                failures += await CheckResumableDownload(client);
                failures += await CheckSelectHelpers(client);
                failures += await CheckDivergedViews(client);
                failures += await CheckMaps(client);
                failures += await CheckSteamAndRealLogon();
                failures += await CheckEngines();
                failures += await CheckPagesNobodyHadRequested(client);
                failures += await CheckPlanetwarsActionSurface();
                failures += await CheckClanActionSurface();
                failures += await CheckFormPostsAreGuarded();
                failures += await CheckPagesOnlyASignedInVisitorSees();
                failures += await CheckPagesOnlyAnAdminSees();
                failures += await CheckTheNewsFeed();
                failures += await CheckContentService();
                failures += await CheckMissionDownloads();
                failures += await CheckReplayDownload();
                failures += await CheckGameModeAndInfolog();
                failures += await CheckContentArchivesAreServed();
                failures += await CheckUploadSizeLimit();
                failures += await CheckGlobalAsaxRoutes();
                failures += await CheckPollAutoClose();
                failures += await CheckRateLimit();
                failures += await CheckWebLobbyFlag();
                failures += await CheckWebApi();
                failures += await CheckTooltipsAndUnvisitedPages();
                failures += await CheckAjaxLayoutAndLobbyViews();
                failures += await CheckGetSurface();
                failures += await CheckClosedGetWrites();
                failures += await CheckIpnVerification();
                failures += await CheckForumPostToken();
                failures += await CheckCampaignTooltip();
                failures += await CheckTransactionScopes();
                failures += await CheckNewsFeed();
                failures += await CheckEventMarkup();
                failures += await CheckPlanetWarsMatchMaker();

                Console.WriteLine();
                if (failures == 0)
                {
                    Console.WriteLine("a Zero-K page is served over HTTP on .NET 9, layout and all.");
                    return 0;
                }
                Console.WriteLine(failures + " check(s) failed.");
                return 1;
            }
        }


        /// <summary>
        /// Planetwars/PwMatchMaker.cshtml, the last PlanetWars view that had never rendered.
        ///
        /// It is not blocked on a row the way Galaxy and Planet were. The action is [Auth] and
        /// opens with <c>Global.LobbyApi.IsPlanetWarsMatchMakerRunning</c>, so with no lobby
        /// server attached it cannot reach the view at all - and this harness deliberately runs
        /// without one, which is the property the null guards in Planet.cshtml and Galaxy.cshtml
        /// exist to keep.
        ///
        /// So it SKIPS rather than fails when there is no lobby server, and runs for real when
        /// there is: tools/stack.sh sets the LobbyApiUrl MiscVar and starts an actual lobby
        /// server beside the site, and the site container runs these same checks when it is given
        /// no --serve. A skip that printed nothing would be the worst of both - this says which
        /// of the two runs happened, so "it passed" cannot quietly mean "it was not attempted".
        ///
        /// A real server always answers: PlanetWarsMatchMaker is constructed unconditionally in
        /// the ZkLobbyServer constructor, and GenerateLobbyCommand returns a Clear command rather
        /// than null when PlanetWars is not running. So the view renders with no options, and the
        /// assertion is deliberately about the view's own markup rather than about a vote: the
        /// options loop needs a PlanetWars round in progress, which is not something a check can
        /// seed, and claiming otherwise would be the same mistake the galaxy links nearly were.
        /// </summary>
        private static async Task<int> CheckPlanetWarsMatchMaker()
        {
            if (ZeroKWeb.Global.LobbyApi == null)
            {
                Console.WriteLine("   ....  /Planetwars/MatchMaker needs a lobby server and none is "
                                  + "configured - run tools/stack.sh, which starts one");
                return 0;
            }

            return await AsModerator(async client =>
            {
                var response = await client.GetAsync(Url + "/Planetwars/MatchMaker");
                var html = await response.Content.ReadAsStringAsync();

                // The action answers Content("Match maker offline") when the lobby server says the
                // matchmaker is not running, and that is a 200 with no view behind it. Asserting
                // the status alone would pass on exactly that.
                var failures = Check(response.IsSuccessStatusCode && html.Contains("id=\"matchMaker\""),
                    "/Planetwars/MatchMaker rendered its view against a real lobby server ("
                    + (int)response.StatusCode + ", " + html.Length + " bytes)");

                int planetID, factionID;
                using (var db = new ZkDataContext())
                {
                    var planet = db.Planets.FirstOrDefault(x => x.Name == "Harness Planet");
                    var faction = db.Factions.FirstOrDefault(x => x.Name == "Harness Faction");
                    if (planet == null || faction == null)
                    {
                        Console.WriteLine("   ....  and its options loop needs a seeded round; "
                                          + "tools/stack.sh seeds one before the lobby server starts");
                        return failures;
                    }
                    planetID = planet.PlanetID;
                    factionID = faction.FactionID;
                }

                // The view renders with no options at all when PlanetWars is not running -
                // GenerateLobbyCommand answers a Clear command rather than null - so the check
                // above passes on a page whose foreach never runs. This is the part that makes
                // the loop execute: one attack option, over the same API the lobby server
                // publishes to everyone else.
                ZeroKWeb.Global.LobbyApi.AddPlanetWarsAttackOption(planetID, factionID);

                var withOption = await client.GetAsync(Url + "/Planetwars/MatchMaker");
                var optionHtml = await withOption.Content.ReadAsStringAsync();

                failures += Check(optionHtml.Contains("Harness Planet"),
                    "and with an attack option on it, the options loop renders the planet ("
                    + optionHtml.Length + " bytes)");
                failures += Check(optionHtml.Contains("[HARN]"),
                    "with the attacking faction beside it");
                // Only the loop's CanSelectForBattle branch emits this, and only for a viewer in
                // the attacking faction - which the seed put them in.
                failures += Check(optionHtml.Contains("MatchMakerJoin"),
                    "and a Join form, which is the branch a viewer outside the faction never sees");

                return failures;
            });
        }

        /// <summary>
        /// HomeController's own Logon, which is the site's real sign-in page - the one visitors
        /// use, as opposed to /Harness/Login, which this harness wrote for itself.
        ///
        /// Two paths, and neither existed on the port until HomeController was linked:
        ///
        /// **Password.** Goes through SteamOpenId.TryCompleteLogin first (which must answer null
        /// for an ordinary post, or every password login would go asking Steam about itself), then
        /// this.SignInCompat, which is the twin that replaces FormsAuthentication with the cookie
        /// scheme the rest of the port already uses. The proof it is the SAME notion of signed-in
        /// is that /Harness/Whoami - which reads Global.Account, set by the middleware from the
        /// cookie - recognises the visitor afterwards.
        ///
        /// **Steam.** Only as far as the redirect: the assertion coming back cannot be exercised
        /// without Steam, and is covered instead by Tests.Portable against a stubbed provider.
        /// </summary>
        private static async Task<int> CheckSteamAndRealLogon()
        {
            Console.WriteLine();
            Console.WriteLine("the site's own sign-in:");
            var failures = 0;

            const string password = "harness-home-password";
            string name;
            string originalHash;
            using (var db = new ZkDataContext())
            {
                var account = db.Accounts.OrderBy(a => a.AccountID).First();
                name = account.Name;
                originalHash = account.PasswordBcrypt;
                account.SetPasswordPlain(password);
                db.SaveChanges();
            }

            try
            {
                var handler = new HttpClientHandler
                {
                    UseCookies = true,
                    CookieContainer = new System.Net.CookieContainer(),
                    AllowAutoRedirect = false
                };
                using (var client = new HttpClient(handler))
                {
                    // The login CSRF, closed. A link carrying somebody else's credentials used to
                    // sign the visitor in as them; now the GET half refuses to look at them.
                    var byLink = await client.GetAsync(Url + "/Home/Logon?login=" + Uri.EscapeDataString(name)
                        + "&password=" + Uri.EscapeDataString(password) + "&zklogin=1");
                    var byLinkBody = await byLink.Content.ReadAsStringAsync();
                    failures += Check(byLinkBody.Contains("Sign in by submitting the login form"),
                        "a GET carrying credentials does not sign anyone in (" + Summarize(byLink, byLinkBody) + ")");
                    var afterLink = await (await client.GetAsync(Url + "/Harness/Whoami")).Content.ReadAsStringAsync();
                    failures += Check(afterLink.Contains("not signed in"),
                        "and left nobody signed in (" + afterLink.Trim() + ")");

                    // POST alone is not the protection: a form on someone else's page can POST.
                    var untokened = await client.PostAsync(Url + "/Home/Logon", new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("login", name),
                        new KeyValuePair<string, string>("password", password),
                        new KeyValuePair<string, string>("zklogin", "1"),
                    }));
                    failures += Check(untokened.StatusCode == System.Net.HttpStatusCode.BadRequest,
                        "a POST without an anti-forgery token is refused (" + (int)untokened.StatusCode + ")");

                    var token = await (await client.GetAsync(Url + "/Harness/Token")).Content.ReadAsStringAsync();
                    var signIn = await client.PostAsync(Url + "/Home/Logon", new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("__RequestVerificationToken", token),
                        new KeyValuePair<string, string>("login", name),
                        new KeyValuePair<string, string>("password", password),
                        new KeyValuePair<string, string>("zklogin", "1"),
                    }));
                    failures += Check(signIn.StatusCode == System.Net.HttpStatusCode.Redirect,
                        "a password login through the site's own Logon redirects (" + (int)signIn.StatusCode + ")");

                    var whoami = await (await client.GetAsync(Url + "/Harness/Whoami")).Content.ReadAsStringAsync();
                    failures += Check(whoami.Contains("signed in as " + name),
                        "SignInCompat wrote the same cookie the rest of the port reads (" + whoami.Trim() + ")");

                    // No zklogin: the Steam branch. A 302 to Steam, built by the port's own
                    // protocol code rather than DotNetOpenAuth.
                    // A FRESH token. ASP.NET Core binds the anti-forgery token to the identity, so
                    // the one fetched while anonymous stops validating the moment SignInCompat runs
                    // - the first version of this check reused it and got a 400 that looked like a
                    // broken Steam branch.
                    var signedInToken = await (await client.GetAsync(Url + "/Harness/Token")).Content.ReadAsStringAsync();
                    var steam = await client.PostAsync(Url + "/Home/Logon", new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("__RequestVerificationToken", signedInToken),
                        new KeyValuePair<string, string>("login", name),
                    }));
                    var location = steam.Headers.Location?.ToString() ?? "";
                    failures += Check(steam.StatusCode == System.Net.HttpStatusCode.Redirect,
                        "no zklogin redirects to Steam (" + (int)steam.StatusCode + ")");
                    failures += Check(location.StartsWith("https://steamcommunity.com/openid/login?"),
                        "and it goes to Steam's OpenID endpoint");
                    failures += Check(location.Contains("openid.mode=checkid_setup"),
                        "asking for checkid_setup");
                    failures += Check(location.Contains(Uri.EscapeDataString("http://specs.openid.net/auth/2.0/identifier_select")),
                        "with identifier_select, so Steam names the account rather than us");
                    failures += Check(location.Contains(Uri.EscapeDataString(Url + "/Home/Logon")),
                        "and a return_to on this host");
                }
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var account = db.Accounts.OrderBy(a => a.AccountID).First();
                    account.PasswordBcrypt = originalHash;
                    db.SaveChanges();
                }
            }

            return failures;
        }

        /// <summary>
        /// The Maps pages, which carry three claims that only a request can settle.
        ///
        /// **System.Drawing.Common on Linux.** ZkData.UnitSyncLib.Map declares Image and Bitmap
        /// members, so ZkData.Core references a package that is Windows-only at RUNTIME. The
        /// argument for referencing it anyway is that the members are [XmlIgnore] and nothing
        /// reads them, so the assembly never has to do anything. Detail deserialises a real Map
        /// through XmlSerializer, which is where that argument is either true or a 500.
        ///
        /// **The AutoRegistrator tripwire stays out of the way.** Eight of the nine actions must
        /// work with the registrar absent; only UploadResource may throw.
        ///
        /// **ResourceLinkProvider and its ExecuteSqlCommandCompat.** Detail calls RefreshLinks on
        /// every content file on every view, which runs the raw UPDATE that moved from EF6's
        /// ExecuteSqlCommand to EF Core's ExecuteSqlRaw.
        /// </summary>
        private static async Task<int> CheckMaps(HttpClient client)
        {
            Console.WriteLine();
            Console.WriteLine("maps:");
            var failures = 0;

            var index = await client.GetAsync(Url + "/Maps");
            failures += Check(index.IsSuccessStatusCode, "/Maps was served (" + (int)index.StatusCode + ")");

            var detail = await client.GetAsync(Url + "/Maps/Detail/1");
            var html = await detail.Content.ReadAsStringAsync();
            failures += Check(detail.IsSuccessStatusCode,
                "/Maps/Detail/1 was served (" + (int)detail.StatusCode + ")"
                + " - Map deserialised, and RefreshLinks ran its raw UPDATE");
            failures += Check(html.Contains("test_map_1"), "the resource reached the page");

            // BoolSelect, the four-argument Select and Html.DropDownList are all behind
            // @if (Global.IsModerator), so an anonymous request cannot see them - the first
            // version of this check asserted them anyway and failed for that reason. This owns
            // the role state the way CheckSignIn does, rather than trusting the fixture: a
            // `ZkData.Core -- grant` by hand leaves the local database ahead of fixture.sql,
            // and a check that depends on which one you have is not a check.
            failures += await AsModerator(async client2 =>
            {
                var moderator = await client2.GetAsync(Url + "/Maps/Detail/1");
                var modHtml = await moderator.Content.ReadAsStringAsync();
                var inner = 0;
                inner += Check(moderator.IsSuccessStatusCode,
                    "/Maps/Detail/1 as a moderator (" + (int)moderator.StatusCode + ")");
                inner += Check(modHtml.Contains("<select name='assymetrical'>"),
                    "BoolSelect rendered");
                inner += Check(modHtml.Contains("<select name='sea'>"),
                    "the four-argument Select rendered");
                inner += Check(modHtml.Contains("<select id=\"mapSupportLevel\" name=\"mapSupportLevel\">"),
                    "Html.DropDownList over EnumHelper.GetSelectList rendered");
                // NOT the blanket !Contains("@") the other pages use. This page legitimately
                // serves one, in a lobby command inside a javascript: URL -
                // chat/battle@select_map:1 - so the blanket version failed on correct output.
                // These are the shapes that would actually mean Razor did not run.
                inner += Check(!System.Text.RegularExpressions.Regex.IsMatch(modHtml, @"@(Html\.|Url\.|Model\b|\(|\{|if\b|foreach\b)"),
                    "no unprocessed Razor markers survived");
                return inner;
            });

            return failures;
        }

        /// <summary>
        /// The Engines list, which is the last controller to link and the one whose recorded
        /// blocker - SharpCompress - turned out not to be the blocker at all.
        ///
        /// Moderator-gated at the controller, so it goes through AsModerator. Index reads the
        /// engine directory off disk through this.MapPath, which is the compat pair, and the
        /// view builds a UniGrid with a templated delegate that had to be moved above its use.
        ///
        /// MakeDefault is NOT exercised and cannot be: it needs the content service, the Steam
        /// depot builder and a lobby server. All three throw, by design.
        /// </summary>
        private static async Task<int> CheckEngines()
        {
            Console.WriteLine();
            Console.WriteLine("engines:");

            return await AsModerator(async client =>
            {
                var response = await client.GetAsync(Url + "/Engines");
                var html = await response.Content.ReadAsStringAsync();
                var failures = 0;
                failures += Check(response.IsSuccessStatusCode, "/Engines was served (" + (int)response.StatusCode + ")");
                failures += Check(html.Contains("Engine list"), "Page.Title reached the layout");
                failures += Check(html.Contains("Upload a new engine"), "the view's own content is there");
                failures += Check(!System.Text.RegularExpressions.Regex.IsMatch(html, @"@(Html\.|Url\.|Model\b|\(|\{|if\b|foreach\b)"),
                    "no unprocessed Razor markers survived");
                return failures;
            });
        }

        /// <summary>
        /// What a URL can reach on PlanetwarsController, and what it has to prove to get there.
        ///
        /// **Three of these assert a 404, which is the point.** `CreateLink` and
        /// `GenerateGalaxyImage` were written as helpers - one caller each, nothing in any view or
        /// script pointing at them, and a comment on CreateLink's caller saying the pair was never
        /// to be entered from outside. All of that was true and none of it mattered, because they
        /// were `public`, and under the {controller}/{action}/{id} route both stacks map, a public
        /// method on a controller is an action. CreateLink checked only that two planets shared a
        /// galaxy, so anyone logged in could link any two planets and change who can attack whom.
        ///
        /// A 404 is a good assertion here precisely because it does not depend on game state: the
        /// action is gone from the routing table, so no galaxy, planet or structure has to exist
        /// for the check to mean something.
        ///
        /// The last two need state, and they are the interesting pair. ActivateTargetedStructure
        /// had no authorization check at all - Planet.cshtml drew its Activate link only when
        /// CanSetStructureTarget said so, and a link the template declines to draw is not a check.
        /// So a structure is seeded on an enemy faction's planet and a signed-in outsider asks for
        /// it to be fired. Then the SAME structure is handed to that account and asked for again,
        /// which must get past the gate - otherwise "refused" would prove nothing, since a check
        /// that refuses everybody looks identical from outside.
        /// </summary>
        private static async Task<int> CheckPlanetwarsActionSurface()
        {
            Console.WriteLine();
            Console.WriteLine("planetwars action surface:");
            var failures = 0;

            // No cookie at all. RunSetPlanetOwners carried no [Auth] of any kind while running a
            // turn handler that writes planet ownership and inserts events.
            using (var anonymous = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
            {
                var response = await anonymous.GetAsync(Url + "/Planetwars/RunSetPlanetOwners");
                failures += Check(response.StatusCode == System.Net.HttpStatusCode.Redirect,
                    "  RunSetPlanetOwners turns an anonymous request away (" + (int)response.StatusCode + ")");
            }

            failures += await AsPlanetwarsOutsider(async (client, seed) =>
            {
                var inner = 0;

                var link = await client.GetAsync(Url + "/Planetwars/CreateLink?planetID="
                    + seed.SourcePlanetID + "&structureTypeID=" + seed.StructureTypeID
                    + "&targetID=" + seed.TargetPlanetID);
                inner += Check(link.StatusCode == System.Net.HttpStatusCode.NotFound,
                    "  CreateLink is not reachable by URL (" + (int)link.StatusCode + ")");

                var galaxyImage = await client.GetAsync(Url + "/Planetwars/GenerateGalaxyImage?galaxyID=" + seed.GalaxyID);
                inner += Check(galaxyImage.StatusCode == System.Net.HttpStatusCode.NotFound,
                    "  GenerateGalaxyImage is not reachable by URL (" + (int)galaxyImage.StatusCode + ")");

                // MatchMakerJoin was an Ajax.ActionLink, which is a GET: a crafted link signed the
                // player up to a PlanetWars battle they had not chosen. It writes no rows - it
                // calls the lobby server - so tools/check-get-writes.py cannot see it, and this is
                // the only thing that does.
                var join = Url + "/Planetwars/MatchMakerJoin?planetID=" + seed.SourcePlanetID + "&attackerFaction=HARNO";
                var joinByLink = await client.GetAsync(join);
                inner += Check(joinByLink.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
                    "  MatchMakerJoin is not reachable by GET (" + (int)joinByLink.StatusCode + ")");

                var joinUntokened = await client.PostAsync(join, new FormUrlEncodedContent(
                    new KeyValuePair<string, string>[0]));
                inner += Check(joinUntokened.StatusCode == System.Net.HttpStatusCode.BadRequest,
                    "  and a POST without an anti-forgery token is refused ("
                    + (int)joinUntokened.StatusCode + ")");

                var owners = await client.GetAsync(Url + "/Planetwars/RunSetPlanetOwners");
                inner += Check(owners.StatusCode == System.Net.HttpStatusCode.Forbidden,
                    "  RunSetPlanetOwners refuses a signed-in player by role (" + (int)owners.StatusCode + ")");

                // The actions themselves, now that they are POST-only and token-checked.
                //
                // ConfiscateStructure is the one driven here because its authorization check is
                // the first thing in the method and the seeded planet is not this account's, so a
                // request that gets all the way through answers "Planet not yours" and writes
                // nothing. That makes the accepted case safe to assert as an ACCEPTED case rather
                // than merely a non-error.
                var confiscate = Url + "/Planetwars/ConfiscateStructure?planetID="
                    + seed.SourcePlanetID + "&structureTypeID=" + seed.StructureTypeID;

                // The crafted link. This is what an <a href> left open: a GET the browser makes
                // with the player's cookies because someone else's page asked it to.
                var asLink = await client.GetAsync(confiscate);
                inner += Check(asLink.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
                    "  a state-changing action is not reachable by GET (" + (int)asLink.StatusCode + ")");

                // The crafted form. POST alone is not the protection - any page can POST.
                var untokened = await client.PostAsync(confiscate, new FormUrlEncodedContent(
                    new KeyValuePair<string, string>[0]));
                inner += Check(untokened.StatusCode == System.Net.HttpStatusCode.BadRequest,
                    "  and a POST without an anti-forgery token is refused (" + (int)untokened.StatusCode + ")");

                // The site's own form. Without this the two checks above would pass just as well
                // on an action that had stopped working altogether.
                var token = await (await client.GetAsync(Url + "/Harness/Token")).Content.ReadAsStringAsync();
                var tokened = await client.PostAsync(confiscate, new FormUrlEncodedContent(
                    new[] { new KeyValuePair<string, string>("__RequestVerificationToken", token) }));
                var tokenedBody = await tokened.Content.ReadAsStringAsync();
                inner += Check(tokened.StatusCode == System.Net.HttpStatusCode.OK,
                    "  while a POST that carries one is accepted (" + (int)tokened.StatusCode + ")");
                inner += Check(tokenedBody.Contains("Planet not yours"),
                    "  and reaches the action's own check (" + Summarize(tokened, tokenedBody) + ")");

                // Not 400: the token was accepted and the action ran. The NullReferenceException
                // logged just above this line is that, and it is expected. It cannot SUCCEED here -
                // MatchMakerJoin calls Global.LobbyApi, and this harness has no lobby server, so
                // it dies inside the action - but "reached the action" is the half that says the
                // check is a guard rather than a wall. Asserting the two refusals alone would pass
                // just as well on an action that had stopped existing.
                var joinTokenTest = await (await client.GetAsync(Url + "/Harness/Token")).Content.ReadAsStringAsync();
                var joinTokened = await Post(client, join, joinTokenTest);
                inner += Check(joinTokened.StatusCode != System.Net.HttpStatusCode.BadRequest,
                    "  while a POST that carries one gets past the token check ("
                    + (int)joinTokened.StatusCode + ", no lobby server to finish the job)");

                // The hole. An active structure, aimed, on a planet belonging to a faction this
                // account is not in, owned by nobody it has any claim through.
                var activate = Url + "/Planetwars/ActivateTargetedStructure?planetID="
                    + seed.SourcePlanetID + "&structureTypeID=" + seed.StructureTypeID;
                var refused = await Post(client, activate, token);
                var refusedBody = await refused.Content.ReadAsStringAsync();
                inner += Check(refusedBody.Contains("Cannot activate this structure"),
                    "  an outsider cannot fire someone else's structure ("
                    + Summarize(refused, refusedBody) + ")");

                // The control. Same URL, same account - the structure is simply theirs now, and
                // deactivated so that getting past the gate stops at the next guard instead of
                // running a PlanetWars turn. "inactive" is the reply from AFTER the check, so it
                // says the gate opened; without this, a check that refused everyone would pass.
                using (var db = new ZkDataContext())
                {
                    var structure = db.PlanetStructures.Single(x => x.PlanetID == seed.SourcePlanetID
                        && x.StructureTypeID == seed.StructureTypeID);
                    structure.OwnerAccountID = seed.AccountID;
                    structure.IsActive = false;
                    db.SaveChanges();
                }

                var allowed = await Post(client, activate, token);
                var allowedBody = await allowed.Content.ReadAsStringAsync();
                inner += Check(!allowedBody.Contains("Cannot activate this structure"),
                    "  and the owner of the same structure is not refused ("
                    + Summarize(allowed, allowedBody) + ")");
                inner += Check(allowedBody.Contains("is inactive"),
                    "  reaching the guard that comes after the check");

                return inner;
            });

            return failures;
        }

        /// <summary>A POST carrying the anti-forgery token, which is what the site's own forms send.</summary>
        private static Task<HttpResponseMessage> Post(HttpClient client, string url, string token)
            => client.PostAsync(url, new FormUrlEncodedContent(
                new[] { new KeyValuePair<string, string>("__RequestVerificationToken", token) }));

        /// <summary>
        /// The clan actions, which are the same shape of hole in a different controller.
        ///
        /// Their authorization was never in question - KickPlayerFromClan checks clan rights or
        /// moderator, and JoinClan checks CanJoin. They were simply GETs, so a link was enough to
        /// make a clan leader expel someone, or to put a player in a clan and, with it, a faction.
        ///
        /// KickPlayerFromClan is the one driven here because its first check is "No such person",
        /// so an accountID nobody has gives an accepted request that changes nothing. The negative
        /// assertions need a positive one beside them or they would pass on an action that had
        /// stopped working.
        /// </summary>
        private static async Task<int> CheckClanActionSurface()
        {
            Console.WriteLine();
            Console.WriteLine("clan actions:");

            return await AsModerator(async client =>
            {
                var failures = 0;

                int unusedAccountID;
                using (var db = new ZkDataContext()) unusedAccountID = db.Accounts.Max(a => a.AccountID) + 1000;
                var kick = Url + "/Clans/KickPlayerFromClan?accountID=" + unusedAccountID;

                var asLink = await client.GetAsync(kick);
                failures += Check(asLink.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
                    "  KickPlayerFromClan is not reachable by GET (" + (int)asLink.StatusCode + ")");

                var untokened = await client.PostAsync(kick, new FormUrlEncodedContent(
                    new KeyValuePair<string, string>[0]));
                failures += Check(untokened.StatusCode == System.Net.HttpStatusCode.BadRequest,
                    "  and a POST without an anti-forgery token is refused (" + (int)untokened.StatusCode + ")");

                var token = await (await client.GetAsync(Url + "/Harness/Token")).Content.ReadAsStringAsync();
                var tokened = await Post(client, kick, token);
                var tokenedBody = await tokened.Content.ReadAsStringAsync();
                failures += Check(tokened.StatusCode == System.Net.HttpStatusCode.OK,
                    "  while a POST that carries one is accepted (" + (int)tokened.StatusCode + ")");
                failures += Check(tokenedBody.Contains("No such person"),
                    "  and reaches the action's own check (" + Summarize(tokened, tokenedBody) + ")");

                // LeaveClan takes no parameters at all, which is what made it the easiest of the
                // three to fire from someone else's page: a bare href was the whole attack.
                var leave = await client.GetAsync(Url + "/Clans/LeaveClan");
                failures += Check(leave.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
                    "  LeaveClan is not reachable by GET (" + (int)leave.StatusCode + ")");

                // JoinClan changes the player's FACTION along with their clan, which is why it is
                // asserted rather than left to the two above.
                var join = await client.GetAsync(Url + "/Clans/JoinClan?id=1");
                failures += Check(join.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
                    "  JoinClan is not reachable by GET (" + (int)join.StatusCode + ")");

                return failures;
            });
        }

        /// <summary>
        /// The forms that were not links: a POST with no token on either end.
        ///
        /// Writing tools/check-antiforgery.py turned up 23 of these across the site, and
        /// ChangePassword is the one worth an end-to-end assertion - a page that can silently
        /// change someone's password when they visit it is the worst thing on the list.
        ///
        /// The accepted case sends the RIGHT old password with two new ones that disagree, so it
        /// reaches the action's own validation and returns before touching the account. Sending a
        /// wrong old password would have worked too, and would have called LobbyApi.LogIpFailure
        /// on the way - which has no server here.
        /// </summary>
        private static async Task<int> CheckFormPostsAreGuarded()
        {
            Console.WriteLine();
            Console.WriteLine("form posts:");

            return await AsModerator(async client =>
            {
                var failures = 0;
                var url = Url + "/Users/ChangePassword";

                // The link-shaped half. MarkAllAsRead was an ActionLink: a GET that wrote
                // ForumLastRead rows for every category the visitor could see. It is the one
                // driven here because it needs no arguments and its write is idempotent, so the
                // accepted case can run for real instead of stopping at a guard.
                var markRead = Url + "/Forum/MarkAllAsRead";
                var markReadByLink = await client.GetAsync(markRead);
                failures += Check(markReadByLink.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
                    "  MarkAllAsRead is not reachable by GET (" + (int)markReadByLink.StatusCode + ")");

                var asLink = await client.GetAsync(url);
                failures += Check(asLink.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
                    "  ChangePassword is not reachable by GET (" + (int)asLink.StatusCode + ")");

                var untokened = await client.PostAsync(url, new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("oldPassword", ModeratorPassword),
                    new KeyValuePair<string, string>("newPassword", "harness-new-password"),
                    new KeyValuePair<string, string>("newPassword2", "harness-new-password"),
                }));
                failures += Check(untokened.StatusCode == System.Net.HttpStatusCode.BadRequest,
                    "  and a POST without an anti-forgery token is refused (" + (int)untokened.StatusCode + ")");

                var token = await (await client.GetAsync(Url + "/Harness/Token")).Content.ReadAsStringAsync();
                var tokened = await client.PostAsync(url, new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("__RequestVerificationToken", token),
                    new KeyValuePair<string, string>("oldPassword", ModeratorPassword),
                    new KeyValuePair<string, string>("newPassword", "one"),
                    new KeyValuePair<string, string>("newPassword2", "another"),
                }));
                var body = await tokened.Content.ReadAsStringAsync();
                failures += Check(tokened.StatusCode == System.Net.HttpStatusCode.OK,
                    "  while a POST that carries one is accepted (" + (int)tokened.StatusCode + ")");
                failures += Check(body.Contains("New passwords do not match"),
                    "  and reaches the action's own check (" + Summarize(tokened, body) + ")");

                // Maps/Rate, which the stars called with $.get - so a crafted link set somebody
                // else's rating of a map. This one can be driven all the way through: the fixture
                // has Resources, and the row it writes is visible afterwards.
                int resourceID, accountID;
                int? ratingSum, ratingCount;
                using (var db = new ZkDataContext())
                {
                    var resource = db.Resources.OrderBy(r => r.ResourceID).First();
                    resourceID = resource.ResourceID;
                    ratingSum = resource.MapRatingSum;
                    ratingCount = resource.MapRatingCount;
                    accountID = db.Accounts.OrderBy(a => a.AccountID).First().AccountID;
                }
                var rate = Url + "/Maps/Rate?id=" + resourceID + "&rating=4";

                var rateByLink = await client.GetAsync(rate);
                failures += Check(rateByLink.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
                    "  Maps/Rate is not reachable by GET (" + (int)rateByLink.StatusCode + ")");

                var rateUntokened = await client.PostAsync(rate, new FormUrlEncodedContent(
                    new KeyValuePair<string, string>[0]));
                failures += Check(rateUntokened.StatusCode == System.Net.HttpStatusCode.BadRequest,
                    "  and a POST without an anti-forgery token is refused ("
                    + (int)rateUntokened.StatusCode + ")");

                var rated = await Post(client, rate, token);
                failures += Check(rated.StatusCode == System.Net.HttpStatusCode.OK,
                    "  while a POST that carries one is accepted (" + (int)rated.StatusCode + ")");
                try
                {
                    using (var db = new ZkDataContext())
                    {
                        var stored = db.MapRatings.SingleOrDefault(
                            x => x.ResourceID == resourceID && x.AccountID == accountID);
                        failures += Check(stored != null && stored.Rating == 4,
                            "  and the rating really was written (" + (stored == null ? "no row" : stored.Rating.ToString()) + ")");
                    }
                }
                finally
                {
                    using (var db = new ZkDataContext())
                    {
                        db.MapRatings.RemoveRange(db.MapRatings.Where(
                            x => x.ResourceID == resourceID && x.AccountID == accountID));
                        var resource = db.Resources.Single(r => r.ResourceID == resourceID);
                        resource.MapRatingSum = ratingSum;
                        resource.MapRatingCount = ratingCount;
                        db.SaveChanges();
                    }
                }

                // Poll/NominateRole, which was a link whose onclick pasted a prompt() answer onto
                // its own href - so a crafted link started a poll in the visitor's name.
                //
                // Driven to a definite answer rather than to success: a RoleType is seeded so the
                // lookup finds one, and the account's Level is dropped below MinLevelForForumVote
                // so the action stops at its own message instead of creating a poll.
                int roleTypeID;
                int originalLevel;
                using (var db = new ZkDataContext())
                {
                    var roleType = new RoleType { Name = "Harness Role", Description = "for the harness", IsVoteable = true };
                    db.RoleTypes.Add(roleType);
                    var account = db.Accounts.Single(a => a.AccountID == accountID);
                    originalLevel = account.Level;
                    account.Level = 0;
                    db.SaveChanges();
                    roleTypeID = roleType.RoleTypeID;
                }
                try
                {
                    var nominate = Url + "/Poll/NominateRole?roleTypeID=" + roleTypeID;

                    var nominateByLink = await client.GetAsync(nominate);
                    failures += Check(nominateByLink.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
                        "  NominateRole is not reachable by GET (" + (int)nominateByLink.StatusCode + ")");

                    var nominateUntokened = await client.PostAsync(nominate, new FormUrlEncodedContent(
                        new[] { new KeyValuePair<string, string>("text", "vote me") }));
                    failures += Check(nominateUntokened.StatusCode == System.Net.HttpStatusCode.BadRequest,
                        "  and a POST without an anti-forgery token is refused ("
                        + (int)nominateUntokened.StatusCode + ")");

                    var nominated = await client.PostAsync(nominate, new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("__RequestVerificationToken", token),
                        new KeyValuePair<string, string>("text", "vote me"),
                    }));
                    var nominatedBody = await nominated.Content.ReadAsStringAsync();
                    failures += Check(nominatedBody.Contains("You need to be level"),
                        "  while a POST that carries one reaches the action's own check ("
                        + Summarize(nominated, nominatedBody) + ")");
                }
                finally
                {
                    using (var db = new ZkDataContext())
                    {
                        db.Polls.RemoveRange(db.Polls.Where(x => x.RoleTypeID == roleTypeID));
                        db.SaveChanges();
                        db.RoleTypes.RemoveRange(db.RoleTypes.Where(x => x.RoleTypeID == roleTypeID));
                        db.Accounts.Single(a => a.AccountID == accountID).Level = originalLevel;
                        db.SaveChanges();
                    }
                }

                // Forum/VotePost. The +N / -N controls were <a href>, so a crafted link voted on
                // the visitor's behalf.
                //
                // Driven all the way through, because this PR changed what the action RETURNS -
                // it used to redirect, and now answers "" so ZkVote can tell a taken vote from a
                // refusal. If that contract broke, jQuery would follow a redirect and hand the JS
                // a whole HTML page, which it would show the voter in an alert box. Asserting the
                // 405 and the 400 alone would not notice.
                int threadID, postID, authorID;
                int authorUpvotes;
                int? voterVotesAvailable;
                using (var db = new ZkDataContext())
                {
                    var category = db.ForumCategories.OrderBy(c => c.ForumCategoryID).First();
                    var author = db.Accounts.Where(a => a.AccountID != accountID).OrderBy(a => a.AccountID).First();
                    authorID = author.AccountID;

                    // A vote is not only a row: it moves the author's karma and spends one of the
                    // voter's votes. Both are put back below, or the fixture drifts a little on
                    // every run of this harness.
                    authorUpvotes = author.ForumTotalUpvotes;
                    voterVotesAvailable = db.Accounts.Single(a => a.AccountID == accountID).VotesAvailable;
                    var thread = new ForumThread
                    {
                        Title = "Harness thread",
                        ForumCategoryID = category.ForumCategoryID,
                        CreatedAccountID = authorID,
                        Created = DateTime.UtcNow,
                        LastPost = DateTime.UtcNow,
                        PostCount = 1,
                    };
                    db.ForumThreads.Add(thread);
                    db.SaveChanges();
                    threadID = thread.ForumThreadID;

                    var post = new ForumPost
                    {
                        ForumThreadID = threadID,
                        AuthorAccountID = authorID,
                        Text = "harness post",
                        Created = DateTime.UtcNow,
                    };
                    db.ForumPosts.Add(post);
                    db.SaveChanges();
                    postID = post.ForumPostID;
                }
                try
                {
                    var vote = Url + "/Forum/VotePost?forumPostID=" + postID + "&delta=1";

                    var voteByLink = await client.GetAsync(vote);
                    failures += Check(voteByLink.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
                        "  VotePost is not reachable by GET (" + (int)voteByLink.StatusCode + ")");

                    var voteUntokened = await client.PostAsync(vote, new FormUrlEncodedContent(
                        new KeyValuePair<string, string>[0]));
                    failures += Check(voteUntokened.StatusCode == System.Net.HttpStatusCode.BadRequest,
                        "  and a POST without an anti-forgery token is refused ("
                        + (int)voteUntokened.StatusCode + ")");

                    var voted = await Post(client, vote, token);
                    var votedBody = await voted.Content.ReadAsStringAsync();
                    failures += Check(voted.StatusCode == System.Net.HttpStatusCode.OK && votedBody == "",
                        "  while a POST that carries one answers empty, which is ZkVote's \"it worked\" ("
                        + (int)voted.StatusCode + ", " + votedBody.Length + " bytes)");

                    using (var db = new ZkDataContext())
                        failures += Check(db.ForumPosts.Single(x => x.ForumPostID == postID).Upvotes == 1,
                            "  and the vote really was counted");
                }
                finally
                {
                    using (var db = new ZkDataContext())
                    {
                        db.AccountForumVotes.RemoveRange(db.AccountForumVotes.Where(x => x.ForumPostID == postID));
                        db.SaveChanges();
                        db.ForumPosts.RemoveRange(db.ForumPosts.Where(x => x.ForumThreadID == threadID));
                        db.SaveChanges();
                        db.ForumLastReads.RemoveRange(db.ForumLastReads.Where(x => x.ForumCategoryID == null));
                        db.ForumThreads.RemoveRange(db.ForumThreads.Where(x => x.ForumThreadID == threadID));
                        db.Accounts.Single(a => a.AccountID == authorID).ForumTotalUpvotes = authorUpvotes;
                        db.Accounts.Single(a => a.AccountID == accountID).VotesAvailable = voterVotesAvailable;
                        db.SaveChanges();
                    }
                }

                // PlanetwarsAdmin. Its Index was the page AND its form handler, branching on
                // which submit button had a value - so a link could switch PlanetWars off or purge
                // a galaxy. The three writes are their own POST actions now.
                foreach (var write in new[] { "SetMode", "SetFutureMode", "Purge", "ResetRatings" })
                {
                    var byLink = await client.GetAsync(Url + "/PlanetwarsAdmin/" + write);
                    failures += Check(byLink.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
                        "  PlanetwarsAdmin/" + write + " is not reachable by GET (" + (int)byLink.StatusCode + ")");
                }

                var futureMode = Url + "/PlanetwarsAdmin/SetFutureMode";
                var futureUntokened = await client.PostAsync(futureMode, new FormUrlEncodedContent(
                    new KeyValuePair<string, string>[0]));
                failures += Check(futureUntokened.StatusCode == System.Net.HttpStatusCode.BadRequest,
                    "  and a POST without an anti-forgery token is refused (" + (int)futureUntokened.StatusCode + ")");

                // SetFutureMode is the one driven here because it writes two MiscVars and nothing
                // else; Purge is on the same page and deletes a galaxy's worth of rows.
                var nextMode = MiscVar.PlanetWarsNextMode;
                var nextModeTime = MiscVar.PlanetWarsNextModeTime;
                try
                {
                    var future = await Post(client, futureMode, token);
                    failures += Check(future.StatusCode == System.Net.HttpStatusCode.OK,
                        "  while a POST that carries one is accepted (" + (int)future.StatusCode + ")");
                }
                finally
                {
                    MiscVar.PlanetWarsNextMode = nextMode;
                    MiscVar.PlanetWarsNextModeTime = nextModeTime;
                }

                // The page itself, and the second bug the split fixed: Html.PostLink renders a
                // <form>, and all of them sat inside the one big form. A form cannot nest inside
                // another, so the browser dropped them and their buttons submitted to Index.
                var adminPage = await client.GetAsync(Url + "/PlanetwarsAdmin");
                var adminHtml = await adminPage.Content.ReadAsStringAsync();
                failures += Check(adminPage.StatusCode == System.Net.HttpStatusCode.OK,
                    "  the admin page is served (" + (int)adminPage.StatusCode + ")");
                failures += Check(adminHtml.Contains("SetMode") && adminHtml.Contains("Purge"),
                    "  with a form per button rather than one form and three named submits");
                failures += Check(NoNestedForms(adminHtml),
                    "  and no form inside another, so the PostLink buttons survive");

                // My/CommanderProfile, the last of them. Commanders.cshtml rendered itself by
                // calling this action through Html.RenderAction - the same method that deletes
                // commanders and saves modules - so the write ran on every page load and a link
                // reached it. The read is its own action now, which is what the view calls.
                var profile = Url + "/My/CommanderProfile?profileNumber=1";

                var profileByLink = await client.GetAsync(profile);
                failures += Check(profileByLink.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
                    "  My/CommanderProfile is not reachable by GET (" + (int)profileByLink.StatusCode + ")");

                var profileUntokened = await client.PostAsync(profile, new FormUrlEncodedContent(
                    new KeyValuePair<string, string>[0]));
                failures += Check(profileUntokened.StatusCode == System.Net.HttpStatusCode.BadRequest,
                    "  and a POST without an anti-forgery token is refused ("
                    + (int)profileUntokened.StatusCode + ")");

                var profilePosted = await Post(client, profile, token);
                failures += Check(profilePosted.StatusCode == System.Net.HttpStatusCode.OK,
                    "  while a POST that carries one is accepted (" + (int)profilePosted.StatusCode + ")");

                // The read half, which is what the page renders through. Without it the split
                // would have taken the page's own markup away with the hole.
                var profileRead = await client.GetAsync(Url + "/My/CommanderProfileView?profileNumber=1");
                failures += Check(profileRead.StatusCode == System.Net.HttpStatusCode.OK,
                    "  and the render-only half still answers a GET (" + (int)profileRead.StatusCode + ")");

                using (var db = new ZkDataContext())
                    failures += Check(!db.Commanders.Any(x => x.AccountID == accountID),
                        "  with no commander left behind by either");

                var markReadPosted = await Post(client, markRead, token);
                failures += Check(markReadPosted.StatusCode == System.Net.HttpStatusCode.Redirect,
                    "  while MarkAllAsRead still works when posted with one ("
                    + (int)markReadPosted.StatusCode + ")");

                return failures;
            });
        }

        /// <summary>
        /// True when no &lt;form&gt; opens while another is still open.
        ///
        /// The HTML parser does not nest forms: it drops the inner start tag and leaves its
        /// children behind, so the inner buttons quietly become the outer form's. Nothing about
        /// that shows up as an error - the page renders, the button is there, and it posts to the
        /// wrong action.
        /// </summary>
        private static bool NoNestedForms(string html)
        {
            var depth = 0;
            foreach (System.Text.RegularExpressions.Match token in
                     System.Text.RegularExpressions.Regex.Matches(html, "<form\\b|</form>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                if (token.Value.StartsWith("</")) depth--;
                else if (++depth > 1) return false;
            }
            return depth == 0;
        }

        private static string Summarize(HttpResponseMessage response, string body)
        {
            var text = body.Replace("\r", " ").Replace("\n", " ").Trim();
            if (text.Length > 60) text = text.Substring(0, 60) + "...";
            return text.Length > 0 ? text : (int)response.StatusCode + " " + response.Headers.Location;
        }

        /// <summary>A galaxy the signed-in account has nothing to do with.</summary>
        private sealed class PlanetwarsSeed
        {
            public int AccountID;
            public int GalaxyID;
            public int SourcePlanetID;
            public int TargetPlanetID;
            public int StructureTypeID;
        }

        /// <summary>
        /// Signs in as an ordinary player, in a faction, and builds a two-planet galaxy belonging
        /// to a different faction with an armed structure on it. Everything is removed afterwards.
        ///
        /// The account needs a faction because CanSetStructureTarget's first line refuses anyone
        /// without one. A factionless account would be turned away for a reason that has nothing to
        /// do with ownership, and the check would pass while the hole was wide open.
        /// </summary>
        private static async Task<int> AsPlanetwarsOutsider(Func<HttpClient, PlanetwarsSeed, Task<int>> body)
        {
            const string password = "harness-planetwars-password";
            string name;
            string originalHash;
            AdminLevel originalAdminLevel;
            int? originalFactionID;
            var seed = new PlanetwarsSeed();
            int ourFactionID, theirFactionID;

            using (var db = new ZkDataContext())
            {
                var account = db.Accounts.OrderBy(a => a.AccountID).First();
                name = account.Name;
                seed.AccountID = account.AccountID;
                originalHash = account.PasswordBcrypt;
                originalAdminLevel = account.AdminLevel;
                originalFactionID = account.FactionID;
                account.AdminLevel = AdminLevel.None;
                account.SetPasswordPlain(password);

                var ours = new Faction { Name = "Harness Ours", Shortcut = "HARNO", Color = "#101010" };
                var theirs = new Faction { Name = "Harness Theirs", Shortcut = "HARNT", Color = "#202020" };
                db.Factions.Add(ours);
                db.Factions.Add(theirs);
                db.SaveChanges();
                ourFactionID = ours.FactionID;
                theirFactionID = theirs.FactionID;

                account.FactionID = ourFactionID;

                // IsDefault stays false: Planetwars/Index does Single(x => x.IsDefault), and a
                // second default galaxy would break an unrelated page for as long as this runs.
                var galaxy = new Galaxy { ImageName = "harness.jpg", Width = 100, Height = 100, Turn = 1 };
                db.Galaxies.Add(galaxy);
                db.SaveChanges();
                seed.GalaxyID = galaxy.GalaxyID;

                var source = new Planet { Name = "Harness Source", GalaxyID = galaxy.GalaxyID, X = 0.25, Y = 0.25, TeamSize = 2, OwnerFactionID = theirFactionID };
                var target = new Planet { Name = "Harness Target", GalaxyID = galaxy.GalaxyID, X = 0.75, Y = 0.75, TeamSize = 2, OwnerFactionID = theirFactionID };
                db.Planets.Add(source);
                db.Planets.Add(target);

                // Every Effect* left null, so firing this does nothing to the database even if a
                // regression lets the request through.
                var structureType = new StructureType { Name = "Harness Emitter", IsSingleUse = false };
                db.StructureTypes.Add(structureType);
                db.SaveChanges();

                seed.SourcePlanetID = source.PlanetID;
                seed.TargetPlanetID = target.PlanetID;
                seed.StructureTypeID = structureType.StructureTypeID;

                db.PlanetStructures.Add(new PlanetStructure
                {
                    PlanetID = source.PlanetID,
                    StructureTypeID = structureType.StructureTypeID,
                    IsActive = true,
                    TargetPlanetID = target.PlanetID,
                });
                db.SaveChanges();
            }

            try
            {
                var handler = new HttpClientHandler
                {
                    UseCookies = true,
                    CookieContainer = new System.Net.CookieContainer(),
                    AllowAutoRedirect = false
                };
                using (var client = new HttpClient(handler))
                {
                    await client.PostAsync(Url + "/Harness/Login", new FormUrlEncodedContent(
                        new[] { new KeyValuePair<string, string>("login", name),
                                new KeyValuePair<string, string>("password", password) }));
                    return await body(client, seed);
                }
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var account = db.Accounts.Single(a => a.AccountID == seed.AccountID);
                    account.PasswordBcrypt = originalHash;
                    account.AdminLevel = originalAdminLevel;
                    account.FactionID = originalFactionID;
                    db.SaveChanges();

                    // Children first: the structure points at both planets, and a planet points at
                    // the galaxy. Events are swept because a regression that lets the request
                    // through inserts one, and leaving it behind would make the next run's
                    // deletions fail on a foreign key instead of reporting the real failure.
                    db.PlanetStructures.RemoveRange(db.PlanetStructures.Where(x => x.PlanetID == seed.SourcePlanetID));
                    db.SaveChanges();
                    db.Events.RemoveRange(db.Events.Where(e => e.Planets.Any(p => p.GalaxyID == seed.GalaxyID)));
                    db.Links.RemoveRange(db.Links.Where(x => x.GalaxyID == seed.GalaxyID));
                    db.SaveChanges();
                    db.Planets.RemoveRange(db.Planets.Where(x => x.GalaxyID == seed.GalaxyID));
                    db.StructureTypes.RemoveRange(db.StructureTypes.Where(x => x.StructureTypeID == seed.StructureTypeID));
                    db.SaveChanges();
                    db.Galaxies.RemoveRange(db.Galaxies.Where(x => x.GalaxyID == seed.GalaxyID));
                    db.Factions.RemoveRange(db.Factions.Where(x => x.FactionID == ourFactionID || x.FactionID == theirFactionID));
                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Runs a block signed in as a moderator, and puts the account back afterwards.
        /// Same arrangement as CheckSignIn, and for the same reason: the check owns the role.
        /// </summary>
        /// <summary>The password AsModerator sets, named so a check inside it can send it back.</summary>
        private const string ModeratorPassword = "harness-moderator-password";

        private static Task<int> AsModerator(Func<HttpClient, Task<int>> body)
        {
            return AsAccount(AdminLevel.Moderator, body);
        }

        /// <summary>
        /// Signs in as the first account at the level asked for, hands the body a client carrying
        /// the cookie, and puts the password and admin level back afterwards. The level is SET
        /// rather than assumed: `ZkData.Core -- grant` can have left the account with one, which
        /// turned a 403 assertion into a 200 and the check into a puzzle, so this owns that state.
        /// </summary>
        private static async Task<int> AsAccount(AdminLevel level, Func<HttpClient, Task<int>> body)
        {
            const string password = ModeratorPassword;
            string name;
            string originalHash;
            AdminLevel originalAdminLevel;

            using (var db = new ZkDataContext())
            {
                var account = db.Accounts.OrderBy(a => a.AccountID).First();
                name = account.Name;
                originalHash = account.PasswordBcrypt;
                originalAdminLevel = account.AdminLevel;
                account.AdminLevel = level;
                account.SetPasswordPlain(password);
                db.SaveChanges();
            }

            try
            {
                var handler = new HttpClientHandler
                {
                    UseCookies = true,
                    CookieContainer = new System.Net.CookieContainer(),
                    AllowAutoRedirect = false
                };
                using (var client = new HttpClient(handler))
                {
                    await client.PostAsync(Url + "/Harness/Login", new FormUrlEncodedContent(
                        new[] { new KeyValuePair<string, string>("login", name),
                                new KeyValuePair<string, string>("password", password) }));

                    // If the cookie did not take, every page below answers 302 or 403 and each
                    // line fails with a number that does not say why. Said once, here.
                    var whoami = await (await client.GetAsync(Url + "/Harness/Whoami")).Content.ReadAsStringAsync();
                    if (!whoami.Contains("signed in as " + name))
                        return Check(false, "  could not sign in, so none of this ran (" + whoami.Trim() + ")");

                    return await body(client);
                }
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var account = db.Accounts.OrderBy(a => a.AccountID).First();
                    account.AdminLevel = originalAdminLevel;
                    account.PasswordBcrypt = originalHash;
                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// A galaxy with two linked planets, for the PlanetWars pages.
        ///
        /// The fixture has no galaxies and no planets, and four actions open with
        /// `Galaxies.Single(g =&gt; g.IsDefault)` or `Planets.Single(...)` - so /Planetwars,
        /// /Planetwars/Minimap, /Planetwars/Ladder and /Planetwars/Planet all threw before they
        /// reached a view.
        ///
        /// **Two planets and a link, not one planet.** Galaxy.cshtml draws the map's links as
        /// native SVG, and all of that - the gradient defs, GalaxyMapGeometry.Link, the rotate
        /// transform - sits inside `foreach (var link in Model.Links)`. A galaxy with one planet
        /// renders the page with that loop running zero times, which would report the view as
        /// rendered while the half of it that was rewritten from Raphael had never run.
        ///
        /// ImageName is set because /Planetwars composes the galaxy JPEG on first request from
        /// `File.ReadAllBytes(MapPath("/img/galaxies/" + gal.ImageName))`. A null there reads a
        /// directory rather than a file.
        ///
        /// The planets point at real Resources from the fixture rather than seeded ones: the
        /// views read `planet.Resource.MapPlanetWarsIcon` and the map's name, and composing the
        /// map reads that icon off disk.
        /// </summary>
        private static async Task<int> WithGalaxy(Func<int, int, int, Task<int>> body)
        {
            // A round seeded from outside this process - tools/stack.sh does it before the lobby
            // server starts - is ALREADY the default galaxy. Seeding a second one makes
            // Galaxies.Single(x => x.IsDefault) throw, and that opens /Planetwars, /Home and the
            // site's own Logon, so eight unrelated checks turn into 500s. Reuse it, and leave it
            // alone afterwards: it belongs to whoever seeded it.
            var seeded = FindSeededGalaxy();
            if (seeded != null) return await body(seeded.Value.GalaxyID, seeded.Value.PlanetID, seeded.Value.OtherPlanetID);

            var (galaxyID, planetID, otherPlanetID) = SeedGalaxyRows();

            try
            {
                return await body(galaxyID, planetID, otherPlanetID);
            }
            finally
            {
                RemoveGalaxyRows(galaxyID);
            }
        }

        /// <summary>The seeded round, if some other process left one behind. By name; ids do not
        /// survive the gap between two processes.</summary>
        private static (int GalaxyID, int PlanetID, int OtherPlanetID)? FindSeededGalaxy()
        {
            using (var db = new ZkDataContext())
            {
                var planet = db.Planets.FirstOrDefault(x => x.Name == "Harness Planet");
                var other = db.Planets.FirstOrDefault(x => x.Name == "Harness Neighbour");
                if (planet == null || other == null) return null;
                return (planet.GalaxyID, planet.PlanetID, other.PlanetID);
            }
        }

        /// <summary>
        /// A PlanetWars round the lobby server in the NEXT process will find already running.
        ///
        /// Everything here has to be in the database before ZkLobbyServer starts, and that is not
        /// a preference: PlanetWarsMatchMaker's constructor returns immediately when there is no
        /// default galaxy, leaving its faction list null, and it caches the factions it does find.
        /// MiscVar caches per process too. A round seeded afterwards is a round the matchmaker
        /// never hears about, and the failure is silence - AddAttackOption catches, traces and
        /// returns.
        ///
        /// The planets stay neutral and the faction owns nothing, which is what makes them
        /// attackable without seeding a whole game state; see SeedGalaxyRows.
        ///
        /// Removal is by NAME rather than by id, because the process that removes is not the one
        /// that seeded and ids do not survive the gap.
        /// </summary>
        private static int SeedPlanetWarsRound()
        {
            RemovePlanetWarsRound();

            var (galaxyID, planetID, otherPlanetID) = SeedGalaxyRows();

            using (var db = new ZkDataContext())
            {
                var faction = NewHarnessFaction();
                db.Factions.Add(faction);
                db.SaveChanges();

                // The viewer is in the attacking faction, so the options loop renders its Join
                // form as well as the option: CanSelectForBattle gates that on the player's own
                // faction, and an option nobody can join renders one branch of the loop short.
                var account = db.Accounts.OrderBy(a => a.AccountID).First();
                account.FactionID = faction.FactionID;
                db.SaveChanges();

                Console.WriteLine("seeded galaxy " + galaxyID + ", planets " + planetID + " and "
                                  + otherPlanetID + ", faction " + faction.FactionID);
            }

            MiscVar.PlanetWarsMode = PlanetWarsModes.Running;
            Console.WriteLine("PlanetWars mode is now Running");
            return 0;
        }

        private static int RemovePlanetWarsRound()
        {
            using (var db = new ZkDataContext())
            {
                foreach (var account in db.Accounts.Where(a => a.Faction.Name == "Harness Faction").ToList())
                    account.FactionID = null;
                db.SaveChanges();

                foreach (var galaxyID in db.Planets.Where(x => x.Name == "Harness Planet")
                             .Select(x => x.GalaxyID).Distinct().ToList())
                    RemoveGalaxyRows(galaxyID);

                db.Factions.RemoveRange(db.Factions.Where(f => f.Name == "Harness Faction"));

                // The ROW, not the value. MiscVar.SetValue writes AllOffline where there was
                // nothing before, and the fixture has no planetWarsMode row at all - the getter
                // answers AllOffline for a missing one. Setting it back would behave identically
                // and still leave the fixture a row heavier than it was found.
                db.MiscVars.RemoveRange(db.MiscVars.Where(v => v.VarName == "planetWarsMode"));
                db.SaveChanges();
            }

            return 0;
        }

        /// <summary>The faction the harness seeds, wherever it seeds one.</summary>
        private static Faction NewHarnessFaction() => new Faction
        {
            Name = "Harness Faction", Shortcut = "HARN", Color = "#3366cc",
            Metal = 0, Bombers = 0, Dropships = 0, Warps = 0,
            EnergyDemandLastTurn = 0, EnergyProducedLastTurn = 0,
            VictoryPoints = 0, IsDeleted = false,
        };

        /// <summary>
        /// The default galaxy, two linked planets on it, and nothing else. Shared by WithGalaxy,
        /// which wraps a body in it, and by --seed-planetwars-round, which has to leave it behind
        /// for a lobby server in another process to read. Two implementations of the same rows
        /// would drift, and the one that drifted would be the one nobody ran.
        ///
        /// The planets are left NEUTRAL - no OwnerFactionID - on purpose, and that is what makes
        /// a round possible at all: Planet.CheckLinkAttack allows an attack outright when the
        /// planet has no faction and the attacker owns no planets, so a seeded faction can be
        /// given an attack option without also seeding treaties, structures and dropships.
        /// </summary>
        private static (int GalaxyID, int PlanetID, int OtherPlanetID) SeedGalaxyRows()
        {
            using (var db = new ZkDataContext())
            {
                var resources = db.Resources.OrderBy(r => r.ResourceID).Take(2).ToList();

                var galaxy = new Galaxy
                {
                    IsDefault = true, IsDirty = false, Started = DateTime.UtcNow.AddDays(-1),
                    ImageName = "galaxy1.jpg",
                    Width = 1000, Height = 1000, Turn = 1, AttackerSideCounter = 0,
                };
                db.Galaxies.Add(galaxy);
                db.SaveChanges();
                var galaxyID = galaxy.GalaxyID;

                // Apart on both axes, so the link between them has a length and an angle. Two
                // planets sharing a Y would leave GalaxyMapGeometry.Link rotating by zero, and
                // zero is the one angle that looks right however wrong the arithmetic is.
                var planetID = AddPlanet(db, galaxyID, "Harness Planet", 0.35, 0.40, resources[0].ResourceID);
                var otherPlanetID = AddPlanet(db, galaxyID, "Harness Neighbour", 0.65, 0.60,
                    resources[resources.Count > 1 ? 1 : 0].ResourceID);

                db.Links.Add(new Link { GalaxyID = galaxyID, PlanetID1 = planetID, PlanetID2 = otherPlanetID });
                db.SaveChanges();
                return (galaxyID, planetID, otherPlanetID);
            }
        }

        private static void RemoveGalaxyRows(int galaxyID)
        {
            using (var db = new ZkDataContext())
            {
                db.Links.RemoveRange(db.Links.Where(l => l.GalaxyID == galaxyID));
                db.SaveChanges();
                db.Planets.RemoveRange(db.Planets.Where(p => p.GalaxyID == galaxyID));
                db.SaveChanges();
                var galaxy = db.Galaxies.FirstOrDefault(g => g.GalaxyID == galaxyID);
                if (galaxy != null) { db.Galaxies.Remove(galaxy); db.SaveChanges(); }
            }

            // /Planetwars caches the map it composed as a file, and that cache lives in the
            // repository beside the two tracked renders. Left behind, it is an untracked
            // artifact in a checked-out tree.
            var render = System.IO.Path.Combine(FindSiteRoot(), "img", "galaxies", "render_" + galaxyID + ".jpg");
            if (System.IO.File.Exists(render)) System.IO.File.Delete(render);
        }

        /// <summary>Points the galaxy at its winning faction, or at nobody again.</summary>
        private static void SetGalaxyWinner(int galaxyID, int? factionID, string endMessage)
        {
            using (var db = new ZkDataContext())
            {
                var galaxy = db.Galaxies.Single(g => g.GalaxyID == galaxyID);
                galaxy.WinnerFactionID = factionID;
                galaxy.EndMessage = endMessage;
                db.SaveChanges();
            }
        }

        private static int AddPlanet(ZkDataContext db, int galaxyID, string name, double x, double y, int resourceID)
        {
            var planet = new Planet
            {
                GalaxyID = galaxyID,
                Name = name,
                TeamSize = 2,
                X = x,
                Y = y,
                MapResourceID = resourceID,
            };
            db.Planets.Add(planet);
            db.SaveChanges();
            return planet.PlanetID;
        }

        /// <summary>
        /// A faction and a clan, for the pages that cannot exist without them.
        ///
        /// The fixture has neither - make-fixture.py nulls ClanID and FactionID on every account
        /// and copies no rows for either table - so Clans/Detail and Factions/Detail were not
        /// merely unrendered, they were unreachable: one answered "not found" and the other threw
        /// from Single(). A survey that asked for them would have been asserting that the fixture
        /// has data.
        ///
        /// Seeded and removed the way AsModerator seeds an account, and for the same reason: the
        /// alternative is putting rows in the committed fixture, which every other check would
        /// then have to account for. The ids are returned rather than assumed, because they are
        /// IDENTITY columns and the tables are empty - the first row is not necessarily 1.
        ///
        /// The account is joined to both, and put back afterwards, because a clan page with no
        /// members renders a different and much shorter view of itself.
        /// </summary>
        /// <summary>
        /// Seeds one poll with two options, the way WithFactionAndClan seeds a faction and a clan,
        /// and removes them again. The fixture has no Polls rows - no forum threads, news, missions
        /// or clans either - which is the whole reason 55 of the site's 119 views are never
        /// rendered by any run here. Seeding is how that gets fixed one view at a time without
        /// committing data to the fixture, where it would move every count the other checks assert.
        /// </summary>
        private static async Task<int> WithPoll(Func<int, Task<int>> body)
        {
            int pollID;
            using (var db = new ZkDataContext())
            {
                var poll = new Poll { QuestionText = "Harness Poll", IsHeadline = false, IsVisible = true };
                poll.PollOptions.Add(new PollOption { OptionText = "Harness Option One" });
                poll.PollOptions.Add(new PollOption { OptionText = "Harness Option Two" });
                db.Polls.Add(poll);
                db.SaveChanges();
                pollID = poll.PollID;
            }

            try
            {
                return await body(pollID);
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var poll = db.Polls.FirstOrDefault(x => x.PollID == pollID);
                    if (poll != null)
                    {
                        db.PollOptions.RemoveRange(db.PollOptions.Where(o => o.PollID == pollID));
                        db.Polls.Remove(poll);
                        db.SaveChanges();
                    }
                }
            }
        }

        /// <summary>
        /// The pages an anonymous request never reaches. /Charts, /My/Commanders, /Wiki and the
        /// rest answer 302 to a visitor who is not signed in, so every check here had only ever
        /// seen the logged-out half of the site - and the half that breaks is the other one. The
        /// layout is the proof: TopMenu calls a child action for authenticated visitors ONLY, so
        /// it was 200 anonymous and 500 signed in, on every page at once, and nothing anonymous
        /// could have found it.
        ///
        /// Each page is asserted to be a whole page with no unprocessed Razor left in it, which is
        /// what separates "the view ran" from "the layout ran and the view threw".
        /// </summary>
        private static async Task<int> CheckPagesOnlyASignedInVisitorSees()
        {
            Console.WriteLine();
            Console.WriteLine("pages only a signed-in visitor sees:");

            return await AsAccount(AdminLevel.None, async client => await WithForumThread(async (threadID, postID) =>
            {
                var failures = 0;
                foreach (var path in new[]
                         {
                             "/My/Commanders", "/My/UnlockList", "/Wiki", "/MapBans",
                             // Needs a thread to post into, which is what WithForumThread is for.
                             "/Forum/NewPost?threadID=" + threadID,
                             // And the thread itself. A thread is only reachable with a forum
                             // category attached: ForumPostListViewComponent reads
                             // thread.ForumCategory.ForumMode without a null check, exactly as
                             // ForumController.cs:177 does on MVC 5, so a category-less thread is
                             // a NullReferenceException on both stacks and not a port defect.
                             "/Forum/Thread/" + threadID,
                             "/PostHistory/Index/" + postID,
                         })
                {
                    var response = await client.GetAsync(Url + path);
                    var html = await response.Content.ReadAsStringAsync();
                    failures += Check(response.IsSuccessStatusCode && html.Contains("</html>"),
                        "  " + path + " is a whole page (" + (int)response.StatusCode + ", " + html.Length + " bytes)");
                    failures += Check(!html.Contains("@Html.") && !html.Contains("@Model"),
                        "  and no unprocessed Razor survived in it");
                }

                // /Clans/Create is NOT always a page, and it was in the loop above until a
                // database with a clan in it proved that. AsAccount signs in as the lowest
                // AccountID there is, so whether that account is in a clan is a property of the
                // rows, not of the port: with no clan it renders the form, with a clan it has no
                // right to it answers Content("You already have clan and you dont have rights to
                // it") - 51 bytes, no </html>, and a failure line that sends you looking for a
                // port defect. Both are correct, so this asserts that it is one of them.
                var clanCreate = await client.GetAsync(Url + "/Clans/Create");
                var clanHtml = await clanCreate.Content.ReadAsStringAsync();
                var refused = clanHtml.StartsWith("You already have clan");
                failures += Check(clanCreate.IsSuccessStatusCode
                                  && (clanHtml.Contains("</html>") || refused),
                    "  /Clans/Create answers for this account (" + (int)clanCreate.StatusCode + ", "
                    + (refused ? "already in a clan" : clanHtml.Length + " bytes of page") + ")");
                failures += Check(!clanHtml.Contains("@Html.") && !clanHtml.Contains("@Model"),
                    "  and no unprocessed Razor survived in it");

                // The seeded post reaches the page rather than only its chrome - the same
                // distinction WithFactionAndClan draws, and the one an "is it 200" check misses.
                var thread = await (await client.GetAsync(Url + "/Forum/Thread/" + threadID)).Content.ReadAsStringAsync();
                failures += Check(thread.Contains("Harness post body one"), "  and the thread shows its posts");

                return failures;
            }));
        }

        /// <summary>
        /// The pages behind [Auth(Role = AdminLevel.SuperAdmin)] and the moderator surface. Nothing
        /// here had ever rendered them: CheckSignIn deliberately sets AdminLevel.None, because its
        /// own assertion is that a 403 means "recognised, then refused", so the whole admin half of
        /// the site was reachable by no check at all. These are the least-visited pages on the site
        /// and the ones a port breaks most quietly.
        /// </summary>
        private static async Task<int> CheckPagesOnlyAnAdminSees()
        {
            Console.WriteLine();
            Console.WriteLine("pages only an admin sees:");

            int accountID;
            using (var db = new ZkDataContext()) accountID = db.Accounts.OrderBy(a => a.AccountID).First().AccountID;

            var failures = await AsAccount(AdminLevel.SuperAdmin, async client =>
            {
                var admin = 0;
                foreach (var path in new[]
                         {
                             "/Admin/EditDynamicConfig", "/Users", "/Users/Detail/" + accountID,
                             "/Charts", "/Lobby/BlockedVPNs", "/LobbyNews/Edit", "/Mods/Edit",
                         })
                {
                    var response = await client.GetAsync(Url + path);
                    var html = await response.Content.ReadAsStringAsync();
                    admin += Check(response.IsSuccessStatusCode && html.Contains("</html>"),
                        "  " + path + " is a whole page (" + (int)response.StatusCode + ", " + html.Length + " bytes)");
                    admin += Check(!html.Contains("@Html.") && !html.Contains("@Model"),
                        "  and no unprocessed Razor survived in it");
                }
                return admin;
            });

            // Which proves nothing unless these pages are actually shut to everyone else. Without
            // this, an [Auth] attribute that stopped being applied in the port would make the whole
            // sweep above pass more easily, not less - the failure would look like success.
            // AllowAutoRedirect = false matters more than it looks. A plain HttpClient follows the
            // 302 to the login page and reports ITS 200, so the first version of this check failed
            // against a site that was shutting the page correctly. Checked with curl before
            // believing it, which is the only reason it reads as a redirect here and not a finding.
            using (var anonymous = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
            {
                var shut = await anonymous.GetAsync(Url + "/Admin/EditDynamicConfig");
                failures += Check(shut.StatusCode == System.Net.HttpStatusCode.Redirect,
                    "  and /Admin/EditDynamicConfig redirects a visitor who is not signed in ("
                    + (int)shut.StatusCode + ")");
            }

            // /Tourney is the admin page this cannot reach. TourneyController.Index calls
            // Global.LobbyApi.GetTourneyBattles() with no null check, so with no lobby server
            // configured it is a NullReferenceException on BOTH stacks rather than a port defect -
            // the same shape as /Planetwars/MatchMaker, and it runs for real under tools/stack.sh.
            if (ZeroKWeb.Global.LobbyApi == null)
            {
                Console.WriteLine("   ....  /Tourney needs a lobby server and none is configured - "
                                  + "run tools/stack.sh, which starts one");
                return failures;
            }

            return failures + await AsAccount(AdminLevel.SuperAdmin, async client =>
            {
                using (var db = new ZkDataContext())
                {
                    var account = db.Accounts.Single(a => a.AccountID == accountID);
                    account.IsTourneyController = true;
                    db.SaveChanges();
                }

                try
                {
                    var response = await client.GetAsync(Url + "/Tourney");
                    var html = await response.Content.ReadAsStringAsync();
                    return Check(response.IsSuccessStatusCode && html.Contains("</html>"),
                        "  /Tourney rendered against a real lobby server ("
                        + (int)response.StatusCode + ", " + html.Length + " bytes)");
                }
                finally
                {
                    using (var db = new ZkDataContext())
                    {
                        var account = db.Accounts.Single(a => a.AccountID == accountID);
                        account.IsTourneyController = false;
                        db.SaveChanges();
                    }
                }
            });
        }

        /// <summary>
        /// The routes Global.asax declares before the default one, which this host did not have.
        ///
        /// Each of them is a URL whose third segment is NOT an id: Wiki/{node}, Static/{name},
        /// Replays/{name}, Missions/File/{name}. The default route binds a third segment to `id`
        /// and nothing else, so without these every one of them was a 404 - or a 500, where the
        /// action took a differently-named parameter and then used it.
        ///
        /// The replay and mission ones are asserted where their files are seeded, in
        /// CheckReplayDownload and CheckMissionDownloads. This covers the rest.
        ///
        /// Worth saying plainly, because it is how this was missed for so long: two checks here
        /// already reached these actions through ?name=, which works and proves nothing about the
        /// route. Writing the query-string form is the natural thing to do when the pretty URL
        /// 404s, and it quietly turns a broken route into a passing check.
        /// </summary>
        private static async Task<int> CheckGlobalAsaxRoutes()
        {
            Console.WriteLine();
            Console.WriteLine("the routes Global.asax declares:");

            var failures = 0;
            using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
            {
                var guide = await client.GetAsync(Url + "/Static/UnitGuide");
                var guideHtml = await guide.Content.ReadAsStringAsync();
                failures += Check(guide.IsSuccessStatusCode && guideHtml.Contains("</html>"),
                    "  Static/{name} serves the page (" + (int)guide.StatusCode + ", "
                    + guideHtml.Length + " bytes)");

                // A node that does not exist redirects you to create it, which is the Wiki
                // controller working rather than failing - the point here is that the request
                // reached the controller at all instead of looking for an action called
                // "Commander".
                foreach (var path in new[] { "/Wiki/Commander", "/p/zero-k/wiki/Commander" })
                {
                    var wiki = await client.GetAsync(Url + path);
                    failures += Check((int)wiki.StatusCode != 404,
                        "  " + path + " reaches the Wiki controller (" + (int)wiki.StatusCode + ")");
                }
            }

            // Contributions/Redeem/{code} is the one route whose presence a status code cannot
            // show, and the one that would otherwise go unchecked. BOTH route tables send
            // /Contributions/Redeem/ZZZ to the same action; the difference is what binds. The
            // default route calls the third segment `id`, so without the RedeemCode route `code`
            // stays null and the action answers "Code is empty" - a 200, from the right action,
            // with the route missing. With it, `code` is "ZZZ" and the answer names the lookup.
            // The body is the evidence here, not the status.
            //
            // The code is deliberately not in the database: Redeem assigns the contribution to
            // whoever asks, and the harness is not going to redeem a real one.
            failures += await AsAccount(AdminLevel.None, async client =>
            {
                var redeem = await client.GetAsync(Url + "/Contributions/Redeem/HARNESS-NO-SUCH-CODE");
                var body = (await redeem.Content.ReadAsStringAsync()).Trim();
                return Check(body == "No contribution with that code found",
                    "  Contributions/Redeem/{code} binds the code (\"" + body + "\")");
            });

            return failures;
        }

        /// <summary>
        /// The two GET-writes that were closed, asserted shut from the outside.
        ///
        /// tools/check-get-writes.py reads the source and is satisfied by an attribute; this asks
        /// the running site, which is the only thing that can say the routing agrees. Both were
        /// plain [Auth] GETs until the check learned to follow a call one level deeper and found
        /// the write it had been looking straight past:
        ///
        ///   /Factions/LeaveFaction    PerformLeaveFaction drops your roles, releases your
        ///                             planets and resets your quotas
        ///   /PostHistory/RevertTo     reverts a forum post through ForumController.SubmitPost
        ///
        /// Anonymous on purpose. A 405 says the ROUTE refuses the verb, which is the property
        /// being asserted; signing in first would let a 302 from [Auth] pass for the same thing
        /// and prove nothing about the method.
        /// </summary>
        private static async Task<int> CheckClosedGetWrites()
        {
            Console.WriteLine();
            Console.WriteLine("the GET-writes that were closed:");

            var failures = 0;
            using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
            {
                // ForceRatingsUpdate is [Auth(Moderator)] and still answers 405 rather than 302,
                // which is worth having asserted: an HttpPost constraint is applied when the
                // action is selected, before any filter runs, so the method is refused without
                // the request having to say who it is.
                foreach (var path in new[]
                         {
                             "/Factions/LeaveFaction", "/PostHistory/RevertTo?id=1",
                             "/Admin/ForceRatingsUpdate", "/Engines/MakeDefault?engine=x",
                         })
                {
                    var byLink = await client.GetAsync(Url + path);
                    failures += Check(byLink.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
                        "  " + path.Split('?')[0] + " is not reachable by GET ("
                        + (int)byLink.StatusCode + ")");
                }
            }

            return failures;
        }

        /// <summary>
        /// A PlanetWars event cannot carry markup into the page that renders it raw.
        ///
        /// Planetwars/Events.cshtml renders Event.Text with @Html.Raw, which is correct - the
        /// text IS markup, built by ZkHtmlFormat out of the entities the event names. What was
        /// not correct is what else reached it. Two routes, and this drives both:
        ///
        /// - a plain string argument. SubmitRenamePlanet validates a new name for whitespace and
        ///   nothing else, and the event is created BEFORE the rename, so the new name arrives
        ///   as a string and went into the page as markup;
        /// - ZkHtmlFormat.PrintPlanet. Renaming a second time renders the planet with the first
        ///   name, through a formatter that did not encode it.
        ///
        /// Moderator-only, so this is defence in depth rather than an open door - the point is
        /// that a moderator account, or whoever has taken one, cannot turn a rename into script
        /// that runs for every visitor to that page.
        /// </summary>
        private static async Task<int> CheckEventMarkup()
        {
            Console.WriteLine();
            Console.WriteLine("a planet rename cannot put markup into the events page:");

            const string payload = "<b>zk-harness-markup</b>";

            return await WithGalaxy(async (galaxyID, planetID, otherPlanetID) =>
                await AsAccount(AdminLevel.Moderator, async client =>
                {
                    var failures = 0;
                    string originalName, mapName;
                    using (var db = new ZkDataContext())
                    {
                        var planet = db.Planets.Single(x => x.PlanetID == planetID);
                        originalName = planet.Name;
                        mapName = db.Resources.First(x => x.TypeID == ResourceType.Map).InternalName;
                    }

                    async Task<int> Rename(string to)
                    {
                        var token = await (await client.GetAsync(Url + "/Harness/Token"))
                            .Content.ReadAsStringAsync();
                        var answer = await client.PostAsync(Url + "/Planetwars/SubmitRenamePlanet",
                            new FormUrlEncodedContent(new[]
                            {
                                new KeyValuePair<string, string>("planetID", planetID.ToString()),
                                new KeyValuePair<string, string>("newName", to),
                                new KeyValuePair<string, string>("teamSize", "4"),
                                new KeyValuePair<string, string>("map", mapName),
                                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                            }));
                        return (int)answer.StatusCode;
                    }

                    try
                    {
                        // First: the name arrives as a plain format argument.
                        var one = await Rename(payload);
                        failures += Check(one == 302 || one == 200,
                            "  a planet is renamed to markup (" + one + ")");

                        // Second: the planet now HAS that name, so PrintPlanet renders it.
                        var two = await Rename("Harness Plain Name");
                        failures += Check(two == 302 || two == 200,
                            "  and renamed again, which renders the first name through PrintPlanet ("
                            + two + ")");

                        using (var db = new ZkDataContext())
                        {
                            var texts = db.Events
                                .Where(x => x.Text.Contains("zk-harness-markup"))
                                .Select(x => x.Text).ToList();
                            failures += Check(texts.Count == 2,
                                "  both events are there to look at (" + texts.Count + ")");
                            failures += Check(texts.All(x => !x.Contains(payload)),
                                "  and neither carries the tags through");
                            failures += Check(texts.All(x => x.Contains("&lt;b&gt;")),
                                "  they carry the escaped form instead");
                        }
                    }
                    finally
                    {
                        using (var db = new ZkDataContext())
                        {
                            db.Events.RemoveRange(
                                db.Events.Where(x => x.Text.Contains("zk-harness-markup")));
                            db.SaveChanges();
                            var planet = db.Planets.FirstOrDefault(x => x.PlanetID == planetID);
                            if (planet != null) planet.Name = originalName;
                            db.SaveChanges();
                        }
                    }

                    return failures;
                }));
        }

        /// <summary>
        /// The RSS feed, with no news and with some.
        ///
        /// /News/Index was one of the seven recorded 500s on the bare GET surface, and the reason
        /// was written down beside it: Model.First() on an empty sequence, because the fixture has
        /// no news rows. That is not a fixture artefact. It is anonymous, it is the feed readers
        /// poll, and EVERY deployment starts with an empty News table - so the first thing a new
        /// Zero-K install serves to a feed reader was a 500, on both stacks.
        ///
        /// lastBuildDate is optional in RSS 2.0, so a feed with nothing in it carries none. Asked
        /// both ways here, because "it is 200 now" would be satisfied by a feed that had quietly
        /// stopped listing anything.
        /// </summary>
        private static async Task<int> CheckNewsFeed()
        {
            Console.WriteLine();
            Console.WriteLine("the RSS feed, which could not render a site with no news:");

            var failures = 0;
            using (var client = new HttpClient())
            {
                var empty = await client.GetAsync(Url + "/News/Index");
                var emptyXml = await empty.Content.ReadAsStringAsync();
                failures += Check(empty.IsSuccessStatusCode,
                    "  /News/Index renders with no news at all (" + (int)empty.StatusCode + ", "
                    + emptyXml.Length + " bytes)");
                failures += Check(emptyXml.Contains("<rss") && emptyXml.Contains("</channel>"),
                    "  and is a whole feed, opened and closed");
                failures += Check(!emptyXml.Contains("<lastBuildDate>"),
                    "  with no lastBuildDate, which RSS makes optional");
                failures += Check(!emptyXml.Contains("<item>"),
                    "  and no items, which is the honest answer");
            }

            return failures + await WithNews(async (newsID, title) =>
            {
                var inner = 0;
                using (var client = new HttpClient())
                {
                    var filled = await client.GetAsync(Url + "/News/Index");
                    var xml = await filled.Content.ReadAsStringAsync();
                    inner += Check(filled.IsSuccessStatusCode,
                        "  and with a news item (" + (int)filled.StatusCode + ", " + xml.Length + " bytes)");
                    inner += Check(xml.Contains("<lastBuildDate>"),
                        "  it carries lastBuildDate again");
                    inner += Check(xml.Contains(title),
                        "  and the item is the one that was seeded");
                }
                return inner;
            });
        }

        /// <summary>
        /// The three TransactionScope paths that had never run on .NET 9.
        ///
        /// System.Transactions promotes an ambient transaction to a DISTRIBUTED one the moment a
        /// second connection enlists, and .NET 9 cannot do that off Windows - it throws
        /// PlatformNotSupportedException. Six actions on this site open a TransactionScope.
        /// Reading them says all six use one connection, so none should promote; reading is not
        /// running, and three of the six had never been executed by anything here:
        /// News/PostNews, LobbyNews/PostNews and Planetwars/SubmitRenamePlanet.
        ///
        /// Each is asked over HTTP, as a moderator, with a token - and then the DATABASE is asked
        /// whether the change is there. That second half is the point: a scope whose Complete()
        /// never ran rolls back silently, and the action redirects either way.
        /// </summary>
        private static async Task<int> CheckTransactionScopes()
        {
            Console.WriteLine();
            Console.WriteLine("TransactionScope, on the three paths nothing had ever run:");

            var failures = 0;
            var title = "Harness News " + Guid.NewGuid().ToString("N").Substring(0, 8);

            // NewsController does ForumCategories.Single(x => x.ForumMode == News), so this needs
            // EXACTLY one - and seeding one unconditionally is how the first version of this
            // check failed. A database set up by the migrations already has one; the committed
            // fixture alone does not. Reuse it when it is there and seed only when it is not,
            // which is what WithGalaxy does with a round somebody else seeded.
            int? seededCategory = null;
            using (var db = new ZkDataContext())
            {
                var existing = db.ForumCategories.Count(x => x.ForumMode == ForumMode.News);
                if (existing > 1)
                    Console.WriteLine("   ....  " + existing + " News forum categories exist, and"
                                      + " NewsController.PostNews does Single() - it cannot run");
                else if (existing == 0)
                {
                    var category = new ForumCategory
                    {
                        Title = "Harness News Category",
                        ForumMode = ForumMode.News,
                    };
                    db.ForumCategories.Add(category);
                    db.SaveChanges();
                    seededCategory = category.ForumCategoryID;
                }
            }

            try
            {
                failures += await AsAccount(AdminLevel.Moderator, async client =>
                {
                    var inner = 0;
                    var token = await (await client.GetAsync(Url + "/Harness/Token")).Content.ReadAsStringAsync();

                    var posted = await client.PostAsync(Url + "/News/PostNews",
                        new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("Title", title),
                            new KeyValuePair<string, string>("Text", "seeded by the harness"),
                            new KeyValuePair<string, string>("Created", "2026-01-01"),
                            // Non-nullable DateTimes, both of them. Leaving HeadlineUntil out of
                            // the form binds it to DateTime.MinValue, which SQL Server's datetime
                            // cannot hold - "SqlDateTime overflow, must be between 1/1/1753 and
                            // 12/31/9999" from inside SaveChanges, which reads like a port defect
                            // and is an incomplete request.
                            new KeyValuePair<string, string>("HeadlineUntil", "2026-01-02"),
                            new KeyValuePair<string, string>("__RequestVerificationToken", token),
                        }));
                    inner += Check(posted.IsSuccessStatusCode || (int)posted.StatusCode == 302,
                        "  /News/PostNews ran its scope (" + (int)posted.StatusCode + ")");

                    using (var db = new ZkDataContext())
                    {
                        var news = db.News.FirstOrDefault(x => x.Title == title);
                        inner += Check(news != null, "  and the row committed, so Complete() ran");
                        inner += Check(news != null && news.ForumThreadID != 0,
                            "  with the forum thread the same scope created");
                    }
                    return inner;
                });

                var lobbyTitle = "Harness LobbyNews " + Guid.NewGuid().ToString("N").Substring(0, 8);
                failures += await AsAccount(AdminLevel.Moderator, async client =>
                {
                    var inner = 0;
                    var token = await (await client.GetAsync(Url + "/Harness/Token")).Content.ReadAsStringAsync();

                    var posted = await client.PostAsync(Url + "/LobbyNews/PostNews",
                        new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("Title", lobbyTitle),
                            new KeyValuePair<string, string>("Text", "seeded by the harness"),
                            new KeyValuePair<string, string>("__RequestVerificationToken", token),
                        }));
                    // Asserted with or without a lobby server now. It used to be a 500 without
                    // one: OnNewsChanged() runs after scope.Complete(), so the news was saved,
                    // the moderator got an error page, and posting again made a second copy of
                    // news that was already there. The notification is guarded, like the other
                    // eight calls on this site whose failure must not fail the request.
                    inner += Check(posted.IsSuccessStatusCode || (int)posted.StatusCode == 302,
                        "  /LobbyNews/PostNews ran its scope and redirected ("
                        + (int)posted.StatusCode + ")");

                    int newsID;
                    using (var db = new ZkDataContext())
                    {
                        var row = db.LobbyNews.FirstOrDefault(x => x.Title == lobbyTitle);
                        inner += Check(row != null,
                            "  and its row committed, which is what the scope had to do");
                        if (row == null) return inner;
                        newsID = row.LobbyNewsID;
                    }

                    // Deleted through the action rather than the context, because Delete carries
                    // the same notification after the same completed write - so the tidying up is
                    // the second half of the test instead of hiding it.
                    var deleted = await client.PostAsync(Url + "/LobbyNews/Delete",
                        new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("id", newsID.ToString()),
                            new KeyValuePair<string, string>("__RequestVerificationToken", token),
                        }));
                    inner += Check(deleted.IsSuccessStatusCode || (int)deleted.StatusCode == 302,
                        "  /LobbyNews/Delete does the same and redirects ("
                        + (int)deleted.StatusCode + ")");

                    using (var db = new ZkDataContext())
                    {
                        var gone = db.LobbyNews.FirstOrDefault(x => x.LobbyNewsID == newsID);
                        inner += Check(gone == null, "  and the row really is gone");
                        if (gone != null) { db.LobbyNews.Remove(gone); db.SaveChanges(); }
                    }
                    return inner;
                });

                failures += await WithGalaxy(async (galaxyID, planetID, otherPlanetID) =>
                    await AsAccount(AdminLevel.Moderator, async client =>
                    {
                        var inner = 0;
                        string originalName, mapName;
                        int originalTeamSize;
                        int? originalResourceID;
                        using (var db = new ZkDataContext())
                        {
                            var planet = db.Planets.Single(x => x.PlanetID == planetID);
                            originalName = planet.Name;
                            originalTeamSize = planet.TeamSize;
                            originalResourceID = planet.MapResourceID;
                            mapName = db.Resources.First(x => x.TypeID == ResourceType.Map).InternalName;
                        }

                        var renamed = "Harness Renamed " + Guid.NewGuid().ToString("N").Substring(0, 8);
                        var token = await (await client.GetAsync(Url + "/Harness/Token")).Content.ReadAsStringAsync();
                        var posted = await client.PostAsync(Url + "/Planetwars/SubmitRenamePlanet",
                            new FormUrlEncodedContent(new[]
                            {
                                new KeyValuePair<string, string>("planetID", planetID.ToString()),
                                new KeyValuePair<string, string>("newName", renamed),
                                new KeyValuePair<string, string>("teamSize", "4"),
                                new KeyValuePair<string, string>("map", mapName),
                                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                            }));
                        inner += Check(posted.IsSuccessStatusCode || (int)posted.StatusCode == 302,
                            "  /Planetwars/SubmitRenamePlanet ran its scope ("
                            + (int)posted.StatusCode + ")");

                        using (var db = new ZkDataContext())
                        {
                            var planet = db.Planets.Single(x => x.PlanetID == planetID);
                            inner += Check(planet.Name == renamed, "  and the rename committed");
                            // planet.Galaxy.IsDirty is set through a lazy-loaded navigation, which
                            // is the other thing this proves on .NET 9.
                            inner += Check(planet.Galaxy != null && planet.Galaxy.IsDirty,
                                "  and the galaxy it reached through a navigation is marked dirty");

                            planet.Name = originalName;
                            planet.TeamSize = originalTeamSize;
                            planet.MapResourceID = originalResourceID;
                            db.SaveChanges();
                        }
                        return inner;
                    }));
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var news = db.News.Where(x => x.Title == title).ToList();
                    var threadIDs = news.Where(x => x.ForumThreadID != 0)
                        .Select(x => x.ForumThreadID).ToList();
                    db.News.RemoveRange(news);
                    db.SaveChanges();

                    foreach (var threadID in threadIDs)
                    {
                        db.ForumPosts.RemoveRange(db.ForumPosts.Where(x => x.ForumThreadID == threadID));
                        db.ForumThreadLastReads.RemoveRange(
                            db.ForumThreadLastReads.Where(x => x.ForumThreadID == threadID));
                        db.SaveChanges();
                        var thread = db.ForumThreads.FirstOrDefault(x => x.ForumThreadID == threadID);
                        if (thread != null) { db.ForumThreads.Remove(thread); db.SaveChanges(); }
                    }

                    if (seededCategory != null)
                    {
                        var category = db.ForumCategories.FirstOrDefault(
                            x => x.ForumCategoryID == seededCategory.Value);
                        if (category != null) { db.ForumCategories.Remove(category); db.SaveChanges(); }
                    }
                }
            }

            return failures;
        }

        /// <summary>
        /// The campaign planet tooltip, which is where the last two unrendered views live.
        ///
        /// Home/PlanetTooltipCampaign.cshtml and Shared/PlanetIconCampaign.cshtml had never been
        /// executed by anything - not here, not in the stack, not by the site, because NOTHING
        /// emits a `campaignPlanet$` tooltip key. /Home/GetTooltip is anonymous and live all the
        /// same, so the three ways this threw were reachable by anyone who typed the URL:
        ///
        /// - CampaignPlanet.GetColor(Global.Account) read viewer.AccountID, and Global.Account is
        ///   null for anybody not signed in;
        /// - PlanetIconCampaign took its map from FirstOrDefault and then dereferenced it three
        ///   times, so a planet whose map is not a registered Resource threw;
        /// - and it called Mission.Resources.First(), which throws on a mission with none.
        ///
        /// Asked ANONYMOUSLY on purpose: signed in, the first of those three cannot fire, and it
        /// is the one a visitor would have hit.
        /// </summary>
        private static async Task<int> CheckCampaignTooltip()
        {
            Console.WriteLine();
            Console.WriteLine("the campaign planet tooltip, which nothing had ever rendered:");

            return await WithMission(async (missionID, missionName, mutator) =>
                await WithCampaignPlanet(missionID, async (campaignID, planetID, mapName) =>
                {
                    var failures = 0;
                    var url = Url + "/Home/GetTooltip?key="
                              + Uri.EscapeDataString("campaignPlanet$" + planetID);

                    using (var client = new HttpClient())
                    {
                        var complete = await client.GetAsync(url);
                        var completeHtml = await complete.Content.ReadAsStringAsync();
                        failures += Check(complete.IsSuccessStatusCode,
                            "  it renders for a visitor who is not signed in ("
                            + (int)complete.StatusCode + ", " + completeHtml.Length + " bytes)");
                        failures += Check(completeHtml.Contains("Harness Campaign Planet"),
                            "  and is the planet that was asked for");
                        failures += Check(completeHtml.Contains("/img/planets/"),
                            "  with the planet icon PlanetIconCampaign draws");

                        // The map the planet names stops existing. FirstOrDefault says that is a
                        // thing that happens; three dereferences said otherwise.
                        using (var db = new ZkDataContext())
                        {
                            db.CampaignPlanets.Single(x => x.CampaignID == campaignID
                                                            && x.PlanetID == planetID)
                                .DisplayedMap = "harness_no_such_map";
                            db.SaveChanges();
                        }

                        var missing = await client.GetAsync(url);
                        var missingHtml = await missing.Content.ReadAsStringAsync();
                        failures += Check(missing.IsSuccessStatusCode,
                            "  and still renders when its map is not a registered Resource ("
                            + (int)missing.StatusCode + ", " + missingHtml.Length + " bytes)");
                        failures += Check(missingHtml.Contains("Harness Campaign Planet"),
                            "  with the rest of the tooltip intact, icon and map box simply absent");
                        failures += Check(!missingHtml.Contains("/img/planets/"),
                            "  ... and the icon really is the part that went");
                    }

                    // Recorded rather than asserted, because fixing it is a decision and not a
                    // translation: the action looks the planet up with
                    // Single(x => x.PlanetID == id) and the primary key is (CampaignID, PlanetID).
                    // One campaign exists, so it works; a second campaign numbering its planets
                    // from 1 as this one does makes every campaign tooltip throw, and the key
                    // carries no campaign to choose by.
                    Console.WriteLine("   ....  HomeController looks this up by PlanetID alone,"
                                      + " and the key is (CampaignID, PlanetID) - a second"
                                      + " campaign reusing a planet id breaks every one of these");
                    return failures;
                }));
        }

        /// <summary>
        /// A campaign with one planet on it, pointing at a mission, and the fixture's own map
        /// attached to that mission so the icon has something to measure itself against.
        /// </summary>
        private static async Task<int> WithCampaignPlanet(
            int missionID, Func<int, int, string, Task<int>> body)
        {
            int campaignID;
            const int planetID = 1;
            string mapName;
            int resourceID;
            int? restoreMissionID;

            using (var db = new ZkDataContext())
            {
                var map = db.Resources.OrderBy(x => x.ResourceID).First();
                resourceID = map.ResourceID;
                mapName = map.InternalName;
                restoreMissionID = map.MissionID;
                // Gives Mission.Resources one element, so the icon's centring arithmetic runs
                // with a real size rather than only through its empty-collection branch.
                map.MissionID = missionID;

                var campaign = new Campaign
                {
                    Name = "Harness Campaign",
                    Description = "seeded by the harness",
                    MapWidth = 100,
                    MapHeight = 100,
                    MapImageName = "harness.jpg",
                };
                db.Campaigns.Add(campaign);
                db.SaveChanges();
                campaignID = campaign.CampaignID;

                db.CampaignPlanets.Add(new CampaignPlanet
                {
                    CampaignID = campaignID,
                    PlanetID = planetID,
                    Name = "Harness Campaign Planet",
                    MissionID = missionID,
                    X = 0.5,
                    Y = 0.5,
                    StartsUnlocked = true,
                    DisplayedMap = mapName,
                    Description = "a planet the harness made",
                });
                db.SaveChanges();
            }

            try
            {
                return await body(campaignID, planetID, mapName);
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var map = db.Resources.FirstOrDefault(x => x.ResourceID == resourceID);
                    if (map != null) map.MissionID = restoreMissionID;

                    var planet = db.CampaignPlanets.FirstOrDefault(
                        x => x.CampaignID == campaignID && x.PlanetID == planetID);
                    if (planet != null) db.CampaignPlanets.Remove(planet);
                    db.SaveChanges();

                    var campaign = db.Campaigns.FirstOrDefault(x => x.CampaignID == campaignID);
                    if (campaign != null) { db.Campaigns.Remove(campaign); db.SaveChanges(); }
                }
            }
        }

        /// <summary>
        /// The forum's own form, posted the way a browser posts it - and refused without the token.
        ///
        /// /Forum/SubmitPost was [HttpPost] with no [ValidateAntiForgeryToken] while both forms
        /// that post to it emitted @Html.AntiForgeryToken() on every page. The token was sent and
        /// thrown away, so an auto-submitting cross-site form could write a post, start a thread
        /// or edit one as the visitor - and both CSRF checks passed, one seeing a token in the
        /// view and the other seeing [HttpPost] and stopping there.
        ///
        /// Nothing in this harness had ever posted to it. 209 checks and the site's most-used
        /// write was reached only by rendering the page that holds the form, which is why adding
        /// [ValidateAntiForgeryToken] changed no result anywhere: there was nothing to change.
        ///
        /// The token is scraped from the REAL form rather than taken from /Harness/Token,
        /// because the half that was decorative is the form's own token. If the form stopped
        /// emitting one, a harness-issued token would hide it.
        /// </summary>
        private static async Task<int> CheckForumPostToken()
        {
            Console.WriteLine();
            Console.WriteLine("a forum post carries a token that is actually checked:");

            // (threadID, postID), which is what WithForumThread passes - not (categoryID,
            // threadID). Reading it the wrong way round aimed the post at a thread id that was
            // really a post id, and the first run passed anyway because the two numbers happened
            // to collide in that database. Reloading the fixture is what separated them.
            return await WithForumThread(async (threadID, postID) =>
                await AsAccount(AdminLevel.None, async client =>
                {
                    var failures = 0;
                    var text = "Harness token post " + Guid.NewGuid().ToString("N");

                    var form = await client.GetAsync(Url + "/Forum/NewPost?threadID=" + threadID);
                    var formHtml = await form.Content.ReadAsStringAsync();
                    // Asserted before the token is looked for, because "no token on the page" and
                    // "no page" read the same in a scrape and are not the same fault. Finding out
                    // which cost a run.
                    failures += Check(form.IsSuccessStatusCode,
                        "  the new-post form renders (" + (int)form.StatusCode + ", "
                        + formHtml.Length + " bytes)");
                    var token = System.Text.RegularExpressions.Regex.Match(formHtml,
                        "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
                    failures += Check(!string.IsNullOrEmpty(token),
                        "  and emits an anti-forgery token");

                    var without = await client.PostAsync(Url + "/Forum/SubmitPost",
                        new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("threadID", threadID.ToString()),
                            new KeyValuePair<string, string>("text", text),
                        }));
                    failures += Check(without.StatusCode == System.Net.HttpStatusCode.BadRequest,
                        "  a post with no token is refused (" + (int)without.StatusCode + ")");

                    var with = await client.PostAsync(Url + "/Forum/SubmitPost",
                        new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("threadID", threadID.ToString()),
                            new KeyValuePair<string, string>("text", text),
                            new KeyValuePair<string, string>("__RequestVerificationToken", token),
                        }));
                    failures += Check(with.IsSuccessStatusCode || (int)with.StatusCode == 302,
                        "  and one that carries the form's own token is accepted ("
                        + (int)with.StatusCode + ")");

                    // The refusal is only worth having if the acceptance really wrote. A
                    // [ValidateAntiForgeryToken] that rejected everything would satisfy the
                    // refusal, and a 302 says nothing about what happened behind it.
                    //
                    // It does NOT establish that the untokened post wrote nothing, and saying so
                    // would be wrong: with the attribute removed this still reads 1, because the
                    // two attempts carry identical text and the forum merges a repeat of the
                    // last post. The refusal above is what measures the refusal.
                    using (var db = new ZkDataContext())
                    {
                        var written = db.ForumPosts.Where(x => x.ForumThreadID == threadID
                                                               && x.Text == text).ToList();
                        failures += Check(written.Count == 1,
                            "  and the accepted post really is in the thread ("
                            + written.Count + " found)");
                        if (written.Count > 0)
                        {
                            db.ForumPosts.RemoveRange(written);
                            db.SaveChanges();
                        }
                    }

                    return failures;
                }));
        }

        /// <summary>
        /// A PayPal notification is acted on only once PayPal has confirmed it sent one.
        ///
        /// /Contributions/Ipn is anonymous and has to be - PayPal is the caller and carries no
        /// cookie or token - so the one thing standing between a stranger and the contributions
        /// table is the postback that asks PayPal whether the notification is real. That postback
        /// used to happen AFTER the write. Every field is sender-chosen, so a single unauthenticated
        /// request put a Contribution of any size under any name on the public /Contributions page,
        /// set HasKudos on whatever account the item code named, mailed a working redeem code from
        /// the team address to an address of the sender's choosing, and announced "WOOHOO! New
        /// contribution" in the public zk lobby channel. PayPal then said no, and the remedy was a
        /// comment on the row.
        ///
        /// Asked through HTTP rather than by calling ImportIpnPayment, because the method is only
        /// half of it: the handler took a GET until now, and ReadIpnRequest has to hand PayPal the
        /// same bytes it parsed the fields from.
        ///
        /// The POSITIVE CONTROL is the part that matters. A notification that writes nothing is
        /// also what an exception looks like - no contribution jar, a null account, SMTP refusing
        /// the thank-you mail - so "nothing was written" on its own would pass just as well with
        /// the fix absent and the seeding broken. The confirmed notification has to put a row in.
        /// </summary>
        private static async Task<int> CheckIpnVerification()
        {
            Console.WriteLine();
            Console.WriteLine("a PayPal notification is acted on only once PayPal confirms it:");

            var failures = 0;

            using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
            {
                var byLink = await client.GetAsync(Url + "/Contributions/Ipn");
                failures += Check(byLink.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
                    "  /Contributions/Ipn is not reachable by GET (" + (int)byLink.StatusCode + ")");
            }

            return failures + await WithContributionJar(async (jarID, accountID) =>
            {
                var inner = 0;
                var paypal = ZeroKWeb.Global.PayPalInterface;
                var realVerifier = paypal.Verifier;

                // The alerts the two paths raise, kept rather than printed, so the check can say
                // that the rejection was reported and not merely silent.
                var alerts = new List<string>();
                Action<string> collect = a => { lock (alerts) alerts.Add(a); };
                paypal.Error += collect;

                try
                {
                    foreach (var confirmed in new[] { false, true })
                    {
                        paypal.Verifier = _ => confirmed;

                        var txn = "HARNESS-" + Guid.NewGuid().ToString("N");
                        using (var client = new HttpClient())
                        {
                            var body = new FormUrlEncodedContent(new[]
                            {
                                new KeyValuePair<string, string>("payment_date", "12:00:00 Jan 01, 2026 PST"),
                                new KeyValuePair<string, string>("first_name", "Harness"),
                                new KeyValuePair<string, string>("last_name", "Sender"),
                                new KeyValuePair<string, string>("payment_status", "Completed"),
                                // EUR on purpose: any other currency goes through ConvertToEuros,
                                // which fetches an http:// rate service that has not existed for
                                // years, and the throw would be indistinguishable from the fix.
                                new KeyValuePair<string, string>("mc_currency", "EUR"),
                                new KeyValuePair<string, string>("mc_gross", "1000"),
                                new KeyValuePair<string, string>("mc_fee", "0"),
                                new KeyValuePair<string, string>("payer_email", "harness@example.invalid"),
                                new KeyValuePair<string, string>("txn_id", txn),
                                new KeyValuePair<string, string>("item_name", "Harness"),
                                // The account the sender nominates. Nothing proves it is theirs.
                                new KeyValuePair<string, string>("item_number",
                                    "ZK_ID_" + accountID + "_JAR_" + jarID),
                            });
                            var answer = await client.PostAsync(Url + "/Contributions/Ipn", body);
                            inner += Check(answer.IsSuccessStatusCode,
                                "  a POSTed notification reaches the handler ("
                                + (int)answer.StatusCode + ")");
                        }

                        using (var db = new ZkDataContext())
                        {
                            var row = db.Contributions.FirstOrDefault(x => x.PayPalTransactionID == txn);
                            var kudos = db.Accounts.First(x => x.AccountID == accountID).HasKudos;

                            if (confirmed)
                            {
                                inner += Check(row != null,
                                    "  a notification PayPal CONFIRMS is recorded");
                                inner += Check(kudos, "  ... and the named account is given kudos");
                            }
                            else
                            {
                                inner += Check(row == null,
                                    "  a notification PayPal DENIES records no contribution");
                                inner += Check(!kudos,
                                    "  ... and gives the named account nothing");
                                inner += Check(alerts.Any(x => x.Contains(txn)),
                                    "  ... and is reported with its details, not dropped");
                            }

                            if (row != null) { db.Contributions.Remove(row); db.SaveChanges(); }
                        }
                    }
                }
                finally
                {
                    paypal.Error -= collect;
                    paypal.Verifier = realVerifier;
                }

                return inner;
            });
        }

        /// <summary>
        /// A default contribution jar, which the fixture has none of - AddPayPalContribution reads
        /// `jar.IsDefault` with no null guard, so without one every notification throws on its way
        /// in and writes nothing, whatever the verification said.
        /// </summary>
        private static async Task<int> WithContributionJar(Func<int, int, Task<int>> body)
        {
            int jarID, accountID;
            bool restoreKudos;

            using (var db = new ZkDataContext())
            {
                var account = db.Accounts.OrderBy(a => a.AccountID).First();
                accountID = account.AccountID;
                restoreKudos = account.HasKudos;
                account.HasKudos = false;

                var jar = new ContributionJar
                {
                    Name = "Harness Jar",
                    IsDefault = true,
                    GuarantorAccountID = accountID,
                    TargetGrossEuros = 1,
                };
                db.ContributionJars.Add(jar);
                db.SaveChanges();
                jarID = jar.ContributionJarID;
            }

            try
            {
                return await body(jarID, accountID);
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    foreach (var left in db.Contributions.Where(x => x.ContributionJarID == jarID).ToList())
                        db.Contributions.Remove(left);
                    db.SaveChanges();

                    var jar = db.ContributionJars.FirstOrDefault(x => x.ContributionJarID == jarID);
                    if (jar != null) db.ContributionJars.Remove(jar);

                    var account = db.Accounts.FirstOrDefault(x => x.AccountID == accountID);
                    if (account != null) account.HasKudos = restoreKudos;
                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Every action a stranger can reach with a bare GET, asked for, and the ones that answer
        /// 500 pinned against a recorded list.
        ///
        /// HOSTING.md carried this as a SURVEY, done by hand on 2026-09-29: 207 actions, 86
        /// GET-able with no required argument, of which seven answered 500 for reasons written
        /// down one by one. That was useful once and then went stale the moment anything changed -
        /// and plenty has. /Replays/Download is a 404 now rather than a 500, seven routes exist
        /// that did not, and a page that had never been reached by an account that could play
        /// PlanetWars turned out to throw the first time one did.
        ///
        /// A number in a document decays; a list in a check does not. This finds the actions by
        /// REFLECTION over the linked controllers rather than from a list, so an action added
        /// tomorrow is surveyed tomorrow, and compares the 500s against tools/get-surface-500s.txt.
        /// Either direction fails: a new 500 is a regression, and one that stopped is a fix
        /// somebody should record rather than silently absorb.
        ///
        /// Anonymous, because that is the surface a stranger has. [Auth] answering 302 is the
        /// right answer and counts as one.
        /// </summary>
        private static async Task<int> CheckGetSurface()
        {
            Console.WriteLine();
            Console.WriteLine("the GET surface a stranger can reach:");

            var paths = new List<string>();
            foreach (var type in typeof(Program).Assembly.GetTypes())
            {
                if (!typeof(Controller).IsAssignableFrom(type) || type.IsAbstract) continue;
                if (!type.Name.EndsWith("Controller")) continue;

                // The harness's own controller is not the site's surface, and it is gated off in
                // a deployment anyway.
                var controller = type.Name.Substring(0, type.Name.Length - "Controller".Length);
                if (controller == "Harness") continue;

                foreach (var method in type.GetMethods(System.Reflection.BindingFlags.Public
                                                       | System.Reflection.BindingFlags.Instance
                                                       | System.Reflection.BindingFlags.DeclaredOnly))
                {
                    if (method.IsSpecialName || method.GetBaseDefinition() != method) continue;

                    var returns = method.ReturnType;
                    var isAction = typeof(IActionResult).IsAssignableFrom(returns)
                                   || typeof(ActionResult).IsAssignableFrom(returns)
                                   || (returns.IsGenericType
                                       && typeof(Task).IsAssignableFrom(returns)
                                       && (typeof(IActionResult).IsAssignableFrom(returns.GetGenericArguments()[0])
                                           || typeof(ActionResult).IsAssignableFrom(returns.GetGenericArguments()[0])));
                    if (!isAction) continue;

                    // Only what a bare GET can reach: every parameter has to be optional or
                    // bindable from nothing. A required int is not a GET surface, it is a 500
                    // about model binding and says nothing about the action.
                    if (method.GetParameters().Any(x => !x.IsOptional && x.ParameterType.IsValueType
                                                        && Nullable.GetUnderlyingType(x.ParameterType) == null))
                        continue;

                    paths.Add("/" + controller + "/" + method.Name);
                }
            }

            paths.Sort(StringComparer.Ordinal);

            var byStatus = new Dictionary<int, int>();
            var broke = new List<string>();
            using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
            {
                foreach (var path in paths)
                {
                    int status;
                    try
                    {
                        status = (int)(await client.GetAsync(Url + path)).StatusCode;
                    }
                    catch (Exception)
                    {
                        // A connection reset is not a status; recorded as one so the survey does
                        // not stop at the first endpoint that drops the connection.
                        status = 0;
                    }

                    byStatus[status] = byStatus.TryGetValue(status, out var seen) ? seen + 1 : 1;
                    if (status == 500 || status == 0) broke.Add(path);
                }
            }

            Console.WriteLine("   ....  " + paths.Count + " actions a bare GET can reach: "
                              + string.Join(", ", byStatus.OrderBy(x => x.Key)
                                                          .Select(x => x.Value + " x " + (x.Key == 0 ? "no reply" : x.Key.ToString()))));

            var newly = broke.Where(x => !SurfaceFiveHundreds.Contains(x)).ToList();
            var fixedUp = SurfaceFiveHundreds.Where(x => !broke.Contains(x)).ToList();

            // A NEW one is a regression wherever it appears, so this is asked in both
            // environments.
            var failures = Check(newly.Count == 0,
                "  no action answers 500 that did not before"
                + (newly.Count == 0 ? "" : " (" + string.Join(", ", newly) + ")"));

            // One that STOPPED is a fix worth recording rather than silently absorbing - but only
            // against the fixture the list describes. With a lobby server attached the round is
            // already seeded, so the two Planetwars pages answer 200 and the set differs by
            // design rather than by regression. Asking there would be asking the wrong question.
            if (ZeroKWeb.Global.LobbyApi != null)
            {
                Console.WriteLine("   ....  and the list is not checked the other way here - a seeded"
                                  + " PlanetWars round makes two of its entries answer 200, by design");
                return failures;
            }

            failures += Check(fixedUp.Count == 0,
                "  and none of the recorded ones stopped"
                + (fixedUp.Count == 0 ? "" : " (" + string.Join(", ", fixedUp)
                   + " - fixed? record it in SurfaceFiveHundreds)"));

            return failures;
        }

        /// <summary>
        /// The actions a bare anonymous GET reaches that answer 500, and why each one does.
        ///
        /// IN SOURCE rather than in a file beside the other baselines, and that is not a
        /// preference. This check also runs inside the site container, where there is no
        /// repository to read: the published image is /app and tools/ is not in it. The first
        /// version read tools/get-surface-500s.txt, worked here, and threw
        /// DirectoryNotFoundException the moment tools/stack.sh ran it.
        ///
        /// NONE OF THESE IS A PORT DEFECT. Each fails identically on MVC 5, and each is here
        /// because a bare GET is not how the action is called - a property of the survey rather
        /// than of the site.
        /// </summary>
        private static readonly HashSet<string> SurfaceFiveHundreds = new HashSet<string>(StringComparer.Ordinal)
        {
            // Want an argument a bare GET does not give them; Single()/First() on no match. Each
            // is reached properly by a check of its own elsewhere in this file.
            "/Forum/DeletePostPrompt",
            "/Forum/GetPostList",
            "/Home/GetTooltip",
            "/Missions/File",

            // Galaxies.Single(x => x.IsDefault) against a fixture with no galaxy. These two are
            // the reason the "stopped" half of this check is asked only without a lobby server:
            // tools/stack.sh seeds a PlanetWars round before it starts one, so over there both
            // answer 200 and the set legitimately differs.
            "/Planetwars/Index",
            "/Planetwars/Minimap",
        };

        /// <summary>
        /// The layout every ajax request is supposed to get, and the lobby pages nothing asked for.
        ///
        /// _ViewStart.cshtml chooses between three layouts, and one of its branches had never been
        /// taken:
        ///
        ///     if (ViewContext.IsChildActionCompat())        Layout = null;
        ///     else if (Request.IsAjaxRequest())             Layout = "_AjaxLayout.cshtml";
        ///     else                                          Layout = "_SiteLayout.cshtml";
        ///
        /// _AjaxLayout is two lines - a head section and RenderBody - so a request that should get
        /// it and gets _SiteLayout instead comes back as a whole web page wrapped around what was
        /// meant to be a fragment. Mvc5RequestCompat.IsAjaxRequest reads the X-Requested-With
        /// header and is the only thing standing between those two outcomes, and NOTHING had ever
        /// called it in the place where it decides something.
        ///
        /// Asserted in BOTH directions on the same URL, because either alone is weak: a port that
        /// always answered the site layout would pass "the plain request is a whole page", and one
        /// that never did would pass "the ajax request is not".
        ///
        /// The lobby pages are the ordinary kind of gap - nothing requested them. Two of the three
        /// need only a signed-in visitor. LobbyChatMessages needs a lobby server, because its
        /// action asks Global.LobbyApi.CanJoinChannel before choosing a view, so it is skipped
        /// here with the wording tools/stack.sh looks for rather than failed.
        /// </summary>
        private static async Task<int> CheckAjaxLayoutAndLobbyViews()
        {
            Console.WriteLine();
            Console.WriteLine("the ajax layout, and the lobby's own pages:");

            var failures = 0;
            using (var client = new HttpClient())
            {
                const string path = "/Home/NotLoggedIn";

                var plain = await (await client.GetAsync(Url + path)).Content.ReadAsStringAsync();
                failures += Check(plain.Contains("</html>"),
                    "  " + path + " is a whole page to an ordinary request (" + plain.Length + " bytes)");

                var request = new HttpRequestMessage(HttpMethod.Get, Url + path);
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                var ajax = await (await client.SendAsync(request)).Content.ReadAsStringAsync();

                failures += Check(!ajax.Contains("</html>") && ajax.Length < plain.Length,
                    "  and a FRAGMENT to an ajax one, which is _ViewStart taking the _AjaxLayout "
                    + "branch (" + ajax.Length + " bytes)");
            }

            // /Forum/Preview, which is the BBCode parser with no page around it. The view is two
            // lines - @model string and @Html.BBCode(Model) - so what comes back IS the parser's
            // output, and the action carries [ValidateInput(false)], meaning it takes whatever is
            // sent.
            //
            // Worth more than one more rendered view. Html.BBCode is one of the thirty helpers
            // that exist TWICE - MVC 5 compiles HtmlHelperExtensions.cs and ASP.NET Core compiles
            // HtmlHelperExtensions.Ported.cs - and that pair has already drifted once in exactly
            // this way: two encoding fixes reached the MVC 5 copies and not the ported ones, so
            // the .NET 9 site carried two vulnerabilities the site it replaces had fixed. This is
            // the parser's own endpoint, and nothing had ever asked it anything.
            using (var client = new HttpClient())
            {
                var markup = await (await client.GetAsync(
                    Url + "/Forum/Preview?text=" + Uri.EscapeDataString("[b]bold[/b]")))
                    .Content.ReadAsStringAsync();
                // <strong>, not <b> - which is what the parser actually emits, and what the first
                // version of this check guessed wrong.
                failures += Check(markup.Contains("<strong>bold</strong>"),
                    "  /Forum/Preview parses BBCode rather than echoing it");

                // The half with teeth. A tag the parser does NOT know has to come back as text,
                // not as a tag - the endpoint turns off request validation, so nothing upstream
                // is going to catch it either.
                const string attack = "<script>alert(1)</script>";
                var hostile = await (await client.GetAsync(
                    Url + "/Forum/Preview?text=" + Uri.EscapeDataString(attack)))
                    .Content.ReadAsStringAsync();

                // BOTH directions, and on the exact string rather than on "<script>". The page
                // around the fragment is a real page with the site's own script tags in it, so
                // asking whether it contains "<script>" anywhere answers yes for every response
                // and tests nothing - which is what the first version of this did.
                failures += Check(hostile.Contains("&lt;script&gt;alert(1)&lt;/script&gt;"),
                    "  and encodes a script tag rather than passing it through");
                failures += Check(!hostile.Contains(attack),
                    "  with the raw tag nowhere in the response");
            }

            // The planet image picker, which is a moderator page over a map the fixture already
            // has. It lists the files in img/planets off disk, so it also says that MapPath
            // resolves to somewhere real from a linked controller - which is the half of this
            // that is not just one more rendered view.
            failures += await AsModerator(async moderator =>
            {
                int resourceID;
                using (var db = new ZkDataContext())
                    resourceID = db.Resources.OrderBy(x => x.ResourceID).First().ResourceID;

                var page = await moderator.GetAsync(Url + "/Maps/PlanetImageSelect?resourceID=" + resourceID);
                var html = await page.Content.ReadAsStringAsync();
                return Check(page.IsSuccessStatusCode && html.Contains("Remove planet"),
                    "  /Maps/PlanetImageSelect renders for a moderator (" + (int)page.StatusCode
                    + ", " + html.Length + " bytes)");
            });

            // Clans/JoinClan, which renders on the FAILURE path and only there:
            //
            //     if (!string.IsNullOrEmpty(clan.Password) && clan.Password != password)
            //         return View(clan.ClanID);
            //
            // A right password joins the clan and redirects, so the only way to see this view is
            // to get the password wrong - which is also the only way to ask for it without
            // writing to the database.
            //
            // Seeded OUTSIDE WithFactionAndClan on purpose: Clan.CanJoin refuses an account that
            // is already in a clan (Clan.cs:59), and that helper puts the harness account in one.
            failures += await WithPasswordedClan(async clanID =>
                await AsAccount(AdminLevel.None, async joiner =>
                {
                    var token = await (await joiner.GetAsync(Url + "/Harness/Token")).Content.ReadAsStringAsync();
                    var attempt = await joiner.PostAsync(Url + "/Clans/JoinClan/" + clanID,
                        new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("__RequestVerificationToken", token),
                            new KeyValuePair<string, string>("password", "not-the-password"),
                        }));
                    var html = await attempt.Content.ReadAsStringAsync();

                    var rendered = Check(attempt.IsSuccessStatusCode
                                         && html.Contains("Password to join the clan"),
                        "  a wrong clan password renders Clans/JoinClan.cshtml ("
                        + (int)attempt.StatusCode + ")");

                    // And it did NOT join, which is what makes asking safe to do twice.
                    using (var db = new ZkDataContext())
                    {
                        var joined = db.Accounts.OrderBy(a => a.AccountID).First().ClanID;
                        rendered += Check(joined == null,
                            "  and the account is still in no clan, so the check did not join one");
                    }
                    return rendered;
                }));

            // Two of the lobby's pages, which only ever needed asking for.
            failures += await AsAccount(AdminLevel.None, async client =>
            {
                var chat = await client.GetAsync(Url + "/Lobby/Chat");
                var chatHtml = await chat.Content.ReadAsStringAsync();
                return Check(chat.IsSuccessStatusCode && chatHtml.Contains("</html>"),
                    "  /Lobby/Chat renders Lobby/LobbyChat.cshtml (" + (int)chat.StatusCode + ", "
                    + chatHtml.Length + " bytes)");
            });

            failures += await AsModerator(async moderator =>
            {
                var history = await moderator.GetAsync(Url + "/Lobby/ChatHistory");
                var historyHtml = await history.Content.ReadAsStringAsync();
                return Check(history.IsSuccessStatusCode && historyHtml.Contains("</html>"),
                    "  /Lobby/ChatHistory renders Lobby/LobbyChatHistory.cshtml ("
                    + (int)history.StatusCode + ", " + historyHtml.Length + " bytes)");
            });

            if (ZeroKWeb.Global.LobbyApi == null)
            {
                Console.WriteLine("   ....  /Lobby/ChatMessages needs a lobby server and none is configured"
                                  + " - run tools/stack.sh, which starts one");
                return failures;
            }

            failures += await AsAccount(AdminLevel.None, async client =>
            {
                // A POST with a token, because the action takes one now. It used to be asked with
                // a GET, which is the thing that was wrong with it: model.Message posts into the
                // channel as the visitor, so a link could say anything in anyone's name. The
                // panel itself always submitted through Ajax.BeginForm - a POST with no token -
                // so neither end was closed and neither CSRF check could see it: one reads forms
                // and skips Ajax.BeginForm, the other matches database calls and this is a
                // lobby-server effect. tools/check-lobby-writes.py is that gap.
                var inner = 0;
                var token = await (await client.GetAsync(Url + "/Harness/Token")).Content.ReadAsStringAsync();

                var refused = await client.GetAsync(Url + "/Lobby/ChatMessages?Channel=zk");
                inner += Check(refused.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
                    "  /Lobby/ChatMessages is not reachable by GET (" + (int)refused.StatusCode + ")");

                var messages = await client.PostAsync(Url + "/Lobby/ChatMessages",
                    new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("Channel", "zk"),
                        new KeyValuePair<string, string>("__RequestVerificationToken", token),
                    }));
                var messagesHtml = await messages.Content.ReadAsStringAsync();
                inner += Check(messages.IsSuccessStatusCode,
                    "  and renders against a real lobby server when posted with a token ("
                    + (int)messages.StatusCode + ", " + messagesHtml.Length + " bytes)");

                // Without the token the action must not run at all. Asserting only the success
                // above would pass just as well with [ValidateAntiForgeryToken] absent.
                var untokened = await client.PostAsync(Url + "/Lobby/ChatMessages",
                    new FormUrlEncodedContent(new[]
                        { new KeyValuePair<string, string>("Channel", "zk") }));
                inner += Check(untokened.StatusCode == System.Net.HttpStatusCode.BadRequest,
                    "  while a POST with no token is refused (" + (int)untokened.StatusCode + ")");

                return inner;
            });

            return failures;
        }

        /// <summary>
        /// Views no request had ever rendered, and the one endpoint that renders eight of them.
        ///
        /// tools/rendered-views.sh counts which of the site's 119 views Razor actually executes
        /// while the harness runs. It stood at 88, and the 31 it did not name are not obscure -
        /// they are every tooltip the site pops up, the forum search page, and the thank-you page
        /// a donor lands on. A view nobody renders is a view whose Html helpers nobody runs, and
        /// those helpers are where this port has been at its most dangerous: two XSS fixes once
        /// failed to cross to the ported copies and nothing noticed.
        ///
        /// /Home/GetTooltip is the lever. One action, one `key` of the form `kind$id`, and a
        /// switch that returns a different PartialView for each kind - so eight of the thirty-one
        /// are reachable from a single endpoint, if the rows exist to point it at. Most did not:
        /// the fixture has no Unlocks, no Planets, no StructureTypes and no FactionTreaties, which
        /// is WHY those views never rendered rather than an accident of what the checks asked for.
        ///
        /// Every row seeded here is removed again. The galaxy is the exception and is borrowed
        /// rather than seeded, for the reason WithGalaxy gives.
        /// </summary>
        private static async Task<int> CheckTooltipsAndUnvisitedPages()
        {
            Console.WriteLine();
            Console.WriteLine("views no request had rendered:");

            var failures = 0;
            using (var client = new HttpClient())
            {
                // Two whole pages first, because neither needs a row of any kind - which makes
                // "nothing asked for them" the only reason they had never run.
                foreach (var path in new[] { "/Contributions/ThankYou", "/Forum/Search" })
                {
                    var response = await client.GetAsync(Url + path);
                    var html = await response.Content.ReadAsStringAsync();
                    failures += Check(response.IsSuccessStatusCode && html.Contains("</html>"),
                        "  " + path + " is a whole page (" + (int)response.StatusCode + ", "
                        + html.Length + " bytes)");
                    failures += Check(!html.Contains("@Html.") && !html.Contains("@Model"),
                        "  and no unprocessed Razor survived in it");
                }
            }

            using (var client = new HttpClient())
            {
                // SubmitSearch, which renders SearchResults - and which is the only thing in the
                // site that runs ForumPostIndexer.FilterPosts over a real request. The indexer is
                // built eagerly by GlobalCompat and had never been asked a question.
                var search = await client.GetAsync(Url + "/Forum/SubmitSearch?keywords=harness");
                var searchHtml = await search.Content.ReadAsStringAsync();
                failures += Check(search.IsSuccessStatusCode && searchHtml.Contains("</html>"),
                    "  /Forum/SubmitSearch is a whole page (" + (int)search.StatusCode + ", "
                    + searchHtml.Length + " bytes)");

                // A user's poll votes. Account 1 is the one AsAccount signs in as, so it exists.
                var votes = await client.GetAsync(Url + "/Poll/UserVotes/1");
                var votesHtml = await votes.Content.ReadAsStringAsync();
                failures += Check(votes.IsSuccessStatusCode && votesHtml.Contains("</html>"),
                    "  /Poll/UserVotes is a whole page (" + (int)votes.StatusCode + ", "
                    + votesHtml.Length + " bytes)");
            }

            // A tooltip is a PartialView: no layout, no </html>, and an empty body is exactly what
            // a silently broken one looks like. So each is asserted on something only its OWN view
            // could have written.
            failures += await WithForumThread(async (threadID, postID) =>
            {
                var tooltip = await Tooltip("forumVotes$" + postID, "Forum/ForumVotesForPost", "<");

                using (var client = new HttpClient())
                {
                    var prompt = await client.GetAsync(Url + "/Forum/DeletePostPrompt?postID=" + postID);
                    var promptHtml = await prompt.Content.ReadAsStringAsync();
                    tooltip += Check(prompt.IsSuccessStatusCode && promptHtml.Contains("</html>"),
                        "  /Forum/DeletePostPrompt is a whole page (" + (int)prompt.StatusCode + ", "
                        + promptHtml.Length + " bytes)");
                }

                // The history of an edit, which needs an edit. PostHistory/Index was already
                // checked and renders the LIST; this is the entry behind a row of it.
                int editID;
                using (var db = new ZkDataContext())
                {
                    var edit = new ForumPostEdit
                    {
                        ForumPostID = postID,
                        EditorAccountID = db.Accounts.OrderBy(a => a.AccountID).First().AccountID,
                        OriginalText = "Harness original text",
                        NewText = "Harness edited text",
                        EditTime = DateTime.UtcNow,
                    };
                    db.ForumPostEdits.Add(edit);
                    db.SaveChanges();
                    editID = edit.ForumPostEditID;
                }

                try
                {
                    using (var client = new HttpClient())
                    {
                        var entry = await client.GetAsync(Url + "/PostHistory/ViewEntry/" + editID);
                        var entryHtml = await entry.Content.ReadAsStringAsync();
                        tooltip += Check(entry.IsSuccessStatusCode
                                         && entryHtml.Contains("Harness original text"),
                            "  /PostHistory/ViewEntry shows the text that was edited ("
                            + (int)entry.StatusCode + ")");

                        // The DIFF, which is the same page the harness already requested and a
                        // different half of it. PostHistoryController.Index only builds
                        // ViewBag.DiffModel when the post HAS an edit, and PostHistoryIndex only
                        // renders DisplayTemplates/Diff when that is not null - so every earlier
                        // request for this page took the empty branch, and the two diff views had
                        // never run. The edit seeded just above is what makes the other branch
                        // reachable; it exists for ViewEntry and this costs one more request.
                        //
                        // DiffPiece renders inside Diff, so each is asserted on markup only IT
                        // writes: the Before/After header row is Diff's, and the coloured span is
                        // DiffPiece's ChangeType.Inserted branch.
                        //
                        // NOT on the edited text appearing whole, which is what the first version
                        // of this check looked for and why it failed against working code. DiffPlex
                        // splits a changed line into WORD-level sub-pieces, and DiffPiece renders
                        // each in its own span - so "Harness edited text" is three spans and that
                        // string is never in the page.
                        var history = await client.GetAsync(Url + "/PostHistory/Index/" + postID);
                        var historyHtml = await history.Content.ReadAsStringAsync();
                        tooltip += Check(history.IsSuccessStatusCode
                                         && historyHtml.Contains(">Before</th>")
                                         && historyHtml.Contains(">After</th>"),
                            "  /PostHistory/Index renders the diff table of that edit ("
                            + (int)history.StatusCode + ", " + historyHtml.Length + " bytes)");
                        tooltip += Check(historyHtml.Contains("color: lightgreen")
                                         && historyHtml.Contains("color: hotpink"),
                            "  with the inserted and deleted words marked, which is DiffPiece running");
                    }
                }
                finally
                {
                    using (var db = new ZkDataContext())
                    {
                        var edit = db.ForumPostEdits.FirstOrDefault(x => x.ForumPostEditID == editID);
                        if (edit != null) { db.ForumPostEdits.Remove(edit); db.SaveChanges(); }
                    }
                }

                return tooltip;
            });

            failures += await WithPoll(async pollID =>
            {
                int optionID;
                using (var db = new ZkDataContext())
                    optionID = db.PollOptions.Where(x => x.PollID == pollID)
                                 .OrderBy(x => x.OptionID).First().OptionID;
                return await Tooltip("polloption$" + optionID, "Poll/PollVoteList", "<span");
            });

            failures += await WithUnlock(async (unlockID, unlockName) =>
                await Tooltip("unlock$" + unlockID, "Home/UnlockTooltip", unlockName));

            failures += await WithStructureType(async (structureTypeID, structureName) =>
                await Tooltip("structuretype$" + structureTypeID,
                              "Shared/DisplayTemplates/StructureType", structureName));

            failures += await WithGalaxy(async (galaxyID, planetID, otherPlanetID) =>
            {
                var planet = await Tooltip("planet$" + planetID, "Home/PlanetTooltip", "Harness Planet");
                // The influence list over a planet with no factions on it. An empty table is the
                // view running, which is the question here - not whether it has anything to show.
                var influence = await Tooltip("planetInfluence$" + planetID,
                                              "Shared/InfluenceListShort", "<table");
                return planet + influence;
            });

            return failures;
        }

        /// <summary>One tooltip, asserted on something only its own view writes.</summary>
        private static async Task<int> Tooltip(string key, string view, string marks)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(Url + "/Home/GetTooltip?key=" + Uri.EscapeDataString(key));
                var html = await response.Content.ReadAsStringAsync();

                var failures = Check(response.IsSuccessStatusCode && html.Contains(marks),
                    "  " + view + " renders as a tooltip (" + (int)response.StatusCode + ", "
                    + html.Length + " bytes)");
                failures += Check(!html.Contains("@Html.") && !html.Contains("@Model"),
                    "  and no unprocessed Razor survived in it");
                return failures;
            }
        }

        /// <summary>
        /// One Unlock, which the fixture has none of. Code and Name are the NOT NULL columns;
        /// everything else is left at its default on purpose, because a tooltip that only renders
        /// for a fully populated row is a tooltip that breaks on a real one.
        /// </summary>
        private static async Task<int> WithUnlock(Func<int, string, Task<int>> body)
        {
            const string name = "Harness Unlock";
            int unlockID;
            using (var db = new ZkDataContext())
            {
                var unlock = new Unlock { Code = "harness_unlock", Name = name, NeededLevel = 1 };
                db.Unlocks.Add(unlock);
                db.SaveChanges();
                unlockID = unlock.UnlockID;
            }

            try
            {
                return await body(unlockID, name);
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var unlock = db.Unlocks.FirstOrDefault(x => x.UnlockID == unlockID);
                    if (unlock != null) { db.Unlocks.Remove(unlock); db.SaveChanges(); }
                }
            }
        }

        /// <summary>
        /// One PlanetWars structure type, which the fixture also has none of. Name is the only
        /// NOT NULL column besides the key; every nullable one is left null on purpose, because a
        /// display template that only survives a fully populated row is one that breaks on a real
        /// one.
        /// </summary>
        private static async Task<int> WithStructureType(Func<int, string, Task<int>> body)
        {
            const string name = "Harness Structure";
            int structureTypeID;
            using (var db = new ZkDataContext())
            {
                var structure = new StructureType { Name = name };
                db.StructureTypes.Add(structure);
                db.SaveChanges();
                structureTypeID = structure.StructureTypeID;
            }

            try
            {
                return await body(structureTypeID, name);
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var structure = db.StructureTypes.FirstOrDefault(x => x.StructureTypeID == structureTypeID);
                    if (structure != null) { db.StructureTypes.Remove(structure); db.SaveChanges(); }
                }
            }
        }

        /// <summary>
        /// The site's Web API, which the port did not have at all.
        ///
        /// Application_Start configures a SECOND pipeline - GlobalConfiguration.Configure(
        /// WebApiConfig.Register) - with its own routes and its own controllers. Auditing
        /// RegisterRoutes found seven missing MVC routes and said nothing about this one, because
        /// it is a different table in a different file.
        ///
        /// POST /api/whr/battles is the whole of that surface: WhrController returns each battle's
        /// players with their WHR internal ratings. **Nothing in this repository calls it**, which
        /// is why no check here would ever have missed it - the consumer is outside the repo, and
        /// would have found out at cutover.
        ///
        /// The ratings come back null with no lobby server, and that is correct rather than a
        /// gap: they are read from the running rating system through Global.LobbyApi. What is
        /// asserted then is the shape and the data that comes from the DATABASE - the battle id,
        /// and a player roster that matches the fixture's own rows - because an endpoint that
        /// answered `[]` with a 200 would pass anything weaker, which is the mistake the
        /// autocomplete endpoints made before they were checked properly.
        ///
        /// **With a lobby server attached it answers 500 against this fixture, and that is
        /// recorded rather than asserted.** Measured in tools/stack.sh, where the site's exception
        /// is
        ///
        ///     ZkLobbyServer.Api.LobbyApiException: lobby API GetInternalRating failed (500)
        ///
        /// so the failure is on the lobby server's side of the call. It is not a port defect: the
        /// site code is identical on both stacks and MVC 5 calls the same method in-process, so
        /// MVC 5 would surface the same throw. What it is NOT is diagnosed here - the lobby
        /// server's own exception was not captured, and the lead is only read rather than
        /// measured: WholeHistoryRating.GetInternalRating indexes players[accountID] with none of
        /// the ContainsKey guard that every neighbouring method in that file has, which would
        /// throw for an account the rating pass produced no Player for.
        ///
        /// So the assertion that holds in BOTH environments is the one this change is actually
        /// about: the request reaches the controller instead of answering 404, which is what it
        /// did before /api existed at all.
        /// </summary>
        private static async Task<int> CheckWebApi()
        {
            Console.WriteLine();
            Console.WriteLine("the Web API route table, which is a second route table:");

            int battleID, players;
            using (var db = new ZkDataContext())
            {
                // A battle the fixture really has, with players on it - picked rather than
                // assumed, so this says nothing about a battle that does not exist.
                var battle = db.SpringBattles
                               .Where(x => x.SpringBattlePlayers.Any(p => !p.IsSpectator))
                               .OrderBy(x => x.SpringBattleID).FirstOrDefault();
                if (battle == null)
                {
                    Console.WriteLine("   ....  the fixture has no battle with players, so this cannot run");
                    return 0;
                }
                battleID = battle.SpringBattleID;
                players = battle.SpringBattlePlayers.Count(p => !p.IsSpectator);
            }

            var failures = 0;
            using (var client = new HttpClient())
            {
                var body = new StringContent("{\"battleIds\":[" + battleID + "]}",
                                             System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(Url + "/api/whr/battles", body);
                var json = await response.Content.ReadAsStringAsync();

                // The route, which is the whole claim of this change and holds either way. 404 is
                // what this answered before the controller was linked.
                failures += Check((int)response.StatusCode != 404,
                    "  POST /api/whr/battles reaches the controller (" + (int)response.StatusCode + ")");

                if (ZeroKWeb.Global.LobbyApi != null)
                {
                    // Worded with the phrase tools/stack.sh greps for, so it is PRINTED there
                    // rather than sitting in a temp file nobody reads. A note nobody sees is a
                    // comment with extra steps.
                    Console.WriteLine("   note  POST /api/whr/battles answers " + (int)response.StatusCode
                                      + " against a real lobby server - the site's call to the lobby"
                                      + " API's GetInternalRating fails on the server's side, and"
                                      + " MVC 5 makes the same call in-process");
                    return failures;
                }

                failures += Check(response.IsSuccessStatusCode,
                    "  and is served (" + (int)response.StatusCode + ")");
                failures += Check(response.Content.Headers.ContentType?.MediaType == "application/json",
                    "  as JSON (" + (response.Content.Headers.ContentType?.MediaType ?? "none") + ")");

                // The battle asked for, and the right number of players on it. A 200 carrying
                // "[]" passes neither.
                failures += Check(json.Contains("\"id\":" + battleID),
                    "  and it is the battle that was asked for (" + Trim(json) + ")");
                failures += Check(System.Text.RegularExpressions.Regex.Matches(json, "accountId").Count == players,
                    "  with the fixture's own players on it (" + players + " expected)");
            }

            return failures;
        }

        private static string Trim(string text) =>
            text.Length <= 90 ? text : text.Substring(0, 90) + "...";

        /// <summary>
        /// The menu the web lobby asks not to be given.
        ///
        /// _SiteLayout.cshtml:114 draws the site menu unless Global.IsWebLobbyAccess, and on this
        /// host that property was a hardcoded `false` with a comment saying session had not been
        /// decided about. So the port drew the menu for every visitor, including the one who had
        /// asked not to see it.
        ///
        /// Three requests, because the interesting one is the FIRST. The web lobby opens the site
        /// with ?weblobby= and no cookie yet, so a port that only consulted Request.Cookies would
        /// get every page right except the one page the web lobby actually opens.
        ///
        /// EVERY request here carries zk_lobby, and the first version of this check did not, which
        /// is why it reported three failures against working code. _SiteLayout has two branches
        /// and only the second one asks about the web lobby at all: the first draws the menu
        /// unconditionally, and you reach the second only with ViewBag.Minimal set or the
        /// zk_lobby cookie present - which is the cookie ZeroKLobby sets on the embedded browser
        /// (BrowserInterop.cs:21). So a visitor who is not coming from a lobby sees the menu no
        /// matter what they put in the query string, and that is correct.
        /// </summary>
        private static async Task<int> CheckWebLobbyFlag()
        {
            Console.WriteLine();
            Console.WriteLine("the web lobby's menu-less view:");

            var failures = 0;
            var cookies = new System.Net.CookieContainer();
            cookies.Add(new Uri(Url), new System.Net.Cookie(GlobalConst.LobbyAccessCookieName, "1") { Path = "/" });

            using (var handler = new HttpClientHandler { UseCookies = true, CookieContainer = cookies })
            using (var client = new HttpClient(handler))
            {
                var plain = await (await client.GetAsync(Url + "/Home/NotLoggedIn"))
                                  .Content.ReadAsStringAsync();
                failures += Check(plain.Contains("id=\"menu\""),
                    "  a visitor from the desktop lobby still gets the site menu");

                var first = await (await client.GetAsync(Url + "/Home/NotLoggedIn?weblobby=1"))
                                  .Content.ReadAsStringAsync();
                failures += Check(!first.Contains("id=\"menu\""),
                    "  and the request that CARRIES ?weblobby= already does not - "
                    + "the first page is the one the web lobby opens");

                var carried = cookies.GetCookies(new Uri(Url));
                var stored = false;
                foreach (System.Net.Cookie cookie in carried)
                    if (cookie.Name == ZeroKWeb.Global.WebLobbyCookie) stored = true;
                failures += Check(stored, "  the flag is stored, so it outlives the query string");

                var later = await (await client.GetAsync(Url + "/Home/NotLoggedIn"))
                                  .Content.ReadAsStringAsync();
                failures += Check(!later.Contains("id=\"menu\""),
                    "  and a later page with no query string still has no menu");
            }

            // The other half of the same condition, which nothing had asked for either: the
            // layout also honours ?no_menu=1, through Request.Params - a MVC 5 API this port
            // shims, and one that had never been exercised from a view.
            var lobbyOnly = new System.Net.CookieContainer();
            lobbyOnly.Add(new Uri(Url), new System.Net.Cookie(GlobalConst.LobbyAccessCookieName, "1") { Path = "/" });
            using (var handler = new HttpClientHandler { UseCookies = true, CookieContainer = lobbyOnly })
            using (var client = new HttpClient(handler))
            {
                var hidden = await (await client.GetAsync(Url + "/Home/NotLoggedIn?no_menu=1"))
                                   .Content.ReadAsStringAsync();
                failures += Check(!hidden.Contains("id=\"menu\""),
                    "  and ?no_menu=1 hides it too, which reads Request.Params from a view");
            }

            return failures;
        }

        /// <summary>
        /// The rate limiter answers, which is the half the unit tests cannot say anything about.
        ///
        /// Tests.Portable drives DosProtector's arithmetic by hand with a fake clock and proves
        /// what it counts. None of that says the middleware is REGISTERED, that it is registered
        /// where static files have already been served, or that a refusal comes back as 429 rather
        /// than as a 500 from somewhere deeper. This asks over HTTP.
        ///
        /// Twenty requests at once against an endpoint that sleeps, because the parallel limit is
        /// about requests IN FLIGHT: twenty requests that each finish in a millisecond are never
        /// twenty at once, and would leave this passing against no limiter at all.
        ///
        /// It then waits out the window on purpose. Everything after it runs from the same address,
        /// and leaving ten seconds of request time inside a five-second window would hand the next
        /// check a 429 that has nothing to do with it.
        /// </summary>
        private static async Task<int> CheckRateLimit()
        {
            Console.WriteLine();
            Console.WriteLine("the rate limiter Global.asax installs:");

            var failures = 0;
            using (var client = new HttpClient())
            {
                // One request first, so the connection pool and the route are warm. The burst then
                // measures what it is meant to - how many requests are IN FLIGHT at once - rather
                // than how long this process takes to get twenty of them out of the door.
                await client.GetAsync(Url + "/Harness/Slow?ms=1");

                // Thirty, each sleeping three seconds, and both numbers are margin rather than
                // taste. The limiter refuses above FIFTEEN in flight, so the burst has to get
                // sixteen overlapping; at twenty requests of 1.5s this measured 4 refusals on a
                // quiet machine, 1 on a busy one and 0 - a failure - on a loaded one, because
                // dispatch time ate the overlap. Thirty of three seconds cannot.
                var burst = new List<Task<HttpResponseMessage>>();
                for (var i = 0; i < 30; i++) burst.Add(client.GetAsync(Url + "/Harness/Slow?ms=3000"));

                // WHILE the limiter is saturated, a static file still has to come back. This is
                // the only assertion that pins WHERE the middleware is registered rather than
                // that it exists: Global.asax exempts static content by testing for a null
                // handler, and the equivalent here is being registered after UseStaticFiles. Put
                // it one line higher, in front of the img/ and Resources/ mappings, and this goes
                // red - which is what it is for, because fifteen parallel requests is one
                // ordinary page's worth of images and the site would start refusing its own
                // stylesheets.
                await Task.Delay(400);
                using (var fresh = new HttpClient())
                {
                    var asset = await fresh.GetAsync(Url + "/img/abuse.png");
                    failures += Check(asset.IsSuccessStatusCode,
                        "  a static file is served while the limiter is full, as IIS does ("
                        + (int)asset.StatusCode + ")");
                }

                var answers = await Task.WhenAll(burst);

                var refused = answers.Count(x => (int)x.StatusCode == 429);
                failures += Check(refused > 0,
                    "  thirty requests at once from one address get some 429s (" + refused + " of 30)");

                // Not all of them: the limit is fifteen in flight, so the first fifteen are served.
                failures += Check(answers.Count(x => x.IsSuccessStatusCode) > 0,
                    "  and the ones inside the limit are still served ("
                    + answers.Count(x => x.IsSuccessStatusCode) + " of 30)");

                var body = "";
                foreach (var answer in answers)
                    if ((int)answer.StatusCode == 429) body = await answer.Content.ReadAsStringAsync();
                failures += Check(body.Contains("Too many requests"),
                    "  saying why, in the sentence Global.asax uses (\"" + body + "\")");

                foreach (var answer in answers) answer.Dispose();

                // The window has to slide, or a burst would be a ban. Waiting it out is also what
                // keeps the checks after this one from inheriting a limiter that is still full.
                await Task.Delay(ZeroKWeb.DosProtector.Window + TimeSpan.FromSeconds(2));

                var after = await client.GetAsync(Url + "/Harness/Slow?ms=1");
                failures += Check(after.IsSuccessStatusCode,
                    "  and the same address is served again once the window passes ("
                    + (int)after.StatusCode + ")");
            }

            return failures;
        }

        /// <summary>
        /// The hourly tick Global.asax runs, which this host did not, so an expired PlanetWars
        /// role election stayed open forever and the vote never took effect.
        ///
        /// Both halves are checked, because only one of them is obvious. That the tick CLOSES an
        /// expired poll is the fix; that it does not close one before the hour is up is the part
        /// a careless port gets wrong in the other direction, and it is the difference between
        /// reproducing Global.asax and running AutoClosePolls on every single request.
        ///
        /// The poll is seeded with no votes on purpose. AutoClosePolls applies the result only
        /// when yes > no, and 0 > 0 is false, so it goes straight to deleting the poll - which
        /// keeps the check off RoleType hierarchies, faction rights and Global.LobbyApi.GhostPm,
        /// none of which are the subject here and the last of which is null on a host with no
        /// lobby server.
        /// </summary>
        private static async Task<int> CheckPollAutoClose()
        {
            Console.WriteLine();
            Console.WriteLine("the hourly poll tick:");

            var failures = 0;
            int roleTypeID, expiredID, gatedID = 0;

            using (var db = new ZkDataContext())
            {
                var role = db.RoleTypes.FirstOrDefault(x => x.Name == "Harness Role")
                           ?? db.RoleTypes.Add(new RoleType
                           {
                               Name = "Harness Role",
                               Description = "Seeded by the host harness",
                               PollDurationDays = 1,
                           }).Entity;
                db.SaveChanges();
                roleTypeID = role.RoleTypeID;

                // Expired an hour ago, headline, and attached to a role - the three things
                // AutoClosePolls selects on.
                var expired = new Poll
                {
                    QuestionText = "Harness Expired Role Poll",
                    IsHeadline = true,
                    IsVisible = true,
                    RoleTypeID = roleTypeID,
                    ExpireBy = DateTime.UtcNow.AddHours(-1),
                };
                db.Polls.Add(expired);
                db.SaveChanges();
                expiredID = expired.PollID;
            }

            try
            {
                using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
                {
                    // The gate first, while the clock still says the hour has not passed - which
                    // it does, because the process started minutes ago.
                    PollAutoClose.LastRun = DateTime.UtcNow;
                    await client.GetAsync(Url + "/Home/NotLoggedIn");
                    failures += Check(PollExists(expiredID),
                        "  an expired poll survives a request inside the hour");

                    // Then move the clock back past the interval, exactly as a running site does
                    // by waiting, and ask for one page.
                    PollAutoClose.LastRun = DateTime.UtcNow - PollAutoClose.Interval - TimeSpan.FromMinutes(1);
                    await client.GetAsync(Url + "/Home/NotLoggedIn");

                    failures += Check(!PollExists(expiredID),
                        "  and the tick closes it once the hour has passed");

                    // And the hour is claimed again. A SECOND expired poll, created now, has to
                    // survive the next request - otherwise the tick is running on every request
                    // and the interval is doing nothing.
                    //
                    // The first version of this check seeded both polls up front and expected the
                    // second to survive. It does not, and should not: AutoClosePolls closes every
                    // expired poll it finds in one pass, so one tick takes both. The check was
                    // wrong, not the tick.
                    gatedID = SeedExpiredRolePoll(roleTypeID, "Harness Poll After The Tick");
                    await client.GetAsync(Url + "/Home/NotLoggedIn");
                    failures += Check(PollExists(gatedID),
                        "  and the hour is claimed, so the next one waits for the next tick");
                }
                return failures;
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    foreach (var id in new[] { expiredID, gatedID })
                    {
                        var poll = db.Polls.FirstOrDefault(x => x.PollID == id);
                        if (poll != null) db.Polls.Remove(poll);
                    }
                    var role = db.RoleTypes.FirstOrDefault(x => x.RoleTypeID == roleTypeID);
                    if (role != null) db.RoleTypes.Remove(role);
                    db.SaveChanges();
                }
            }
        }

        private static int SeedExpiredRolePoll(int roleTypeID, string question)
        {
            using (var db = new ZkDataContext())
            {
                var poll = new Poll
                {
                    QuestionText = question,
                    IsHeadline = true,
                    IsVisible = true,
                    RoleTypeID = roleTypeID,
                    ExpireBy = DateTime.UtcNow.AddHours(-1),
                };
                db.Polls.Add(poll);
                db.SaveChanges();
                return poll.PollID;
            }
        }

        private static bool PollExists(int pollID)
        {
            using (var db = new ZkDataContext()) return db.Polls.Any(x => x.PollID == pollID);
        }

        /// <summary>
        /// A body larger than Kestrel's default is accepted, because Web.config says IIS accepts
        /// one and the port has to match.
        ///
        ///     <httpRuntime maxRequestLength="5000000" />              ASP.NET, in KILOBYTES
        ///     <requestLimits maxAllowedContentLength="500000000" />   IIS, in BYTES
        ///
        /// The smaller binds, so the live site takes roughly 477MB. Kestrel defaults to 30,000,000
        /// bytes, so the port answered 413 to anything larger - and map archives, which arrive
        /// through UploadResource, are routinely larger than that.
        ///
        /// It posts to Harness/BodyLength, which reads the body and answers with the count and
        /// nothing else. Posting to a real endpoint instead put 31MB of zeroes through an exception
        /// message and into the harness log - a 31MB log file on every run, measured the hard way.
        /// Counting the bytes back is also a stronger assertion than "not a 413": it says the whole
        /// body arrived, not merely that the request was not rejected outright.
        /// </summary>
        private static async Task<int> CheckUploadSizeLimit()
        {
            Console.WriteLine();
            Console.WriteLine("upload size:");

            // Just over Kestrel's 30,000,000-byte default: enough to prove the limit moved,
            // small enough not to make every run carry half a gigabyte.
            var body = new byte[31_000_000];

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(2);
                try
                {
                    var response = await client.PostAsync(Url + "/Harness/BodyLength", new ByteArrayContent(body));
                    var answer = (await response.Content.ReadAsStringAsync()).Trim();

                    return Check(response.IsSuccessStatusCode && answer == "read " + body.Length,
                        "  a " + (body.Length / 1_000_000) + "MB body arrives whole ("
                        + (int)response.StatusCode + ", " + answer + ") - Kestrel's default would make this 413");
                }
                catch (Exception ex)
                {
                    // Refusing an oversized body does not always arrive as a 413. Kestrel can reset
                    // the connection while the client is still writing, which surfaces here as an
                    // HttpRequestException - and an uncaught one took the whole harness down with
                    // SIGABRT instead of failing this one line. Found by running the control for
                    // this check, which is the point of running controls.
                    return Check(false, "  a " + (body.Length / 1_000_000) + "MB body was refused: "
                                        + ex.GetType().Name + " - the request limit is not configured");
                }
            }
        }

        /// <summary>
        /// Map and game archives download from the site itself, and until 2026-10-02 they did not.
        ///
        /// ResourceLinkProvider hands a client {BaseSiteUrl}/content/{maps|games}/{file} as a
        /// download link whenever that file is on the site's disk - so the site advertises these
        /// URLs. IIS serves them, with the media type Web.config spells out in a mimeMap. The port
        /// served neither: `content` was not among the mapped directories, and ASP.NET Core's
        /// content-type provider knows neither .sd7 nor .sdz - and an unknown extension is not
        /// served untyped, it is a 404.
        ///
        /// So a player following the site's own download link got a 404 from the ported stack
        /// while the link kept being advertised. Both halves are fixed; this is what holds them.
        /// </summary>
        private static async Task<int> CheckContentArchivesAreServed()
        {
            Console.WriteLine();
            Console.WriteLine("map and game archives:");

            var root = FindSiteRoot();
            var maps = System.IO.Path.Combine(root, "content", "maps");
            var archive = System.IO.Path.Combine(maps, "harness-probe.sd7");
            var unmapped = System.IO.Path.Combine(maps, "harness-probe.cs");

            System.IO.Directory.CreateDirectory(maps);
            System.IO.File.WriteAllText(archive, "not a real archive");
            System.IO.File.WriteAllText(unmapped, "// not an archive either");

            try
            {
                var failures = 0;
                using (var client = new HttpClient())
                {
                    var got = await client.GetAsync(Url + "/content/maps/harness-probe.sd7");
                    failures += Check(got.IsSuccessStatusCode,
                        "  an .sd7 under content/maps is served (" + (int)got.StatusCode + ")");
                    failures += Check(got.Content.Headers.ContentType?.MediaType == "application/octet-stream",
                        "  as application/octet-stream, the type Web.config gives IIS ("
                        + (got.Content.Headers.ContentType?.MediaType ?? "none") + ")");

                    // The other half of the fix: two extensions were mapped, not "serve anything".
                    // ServeUnknownFileTypes = true would have made the first check pass too, while
                    // also publishing every file that lands in these directories.
                    var denied = await client.GetAsync(Url + "/content/maps/harness-probe.cs");
                    failures += Check(!denied.IsSuccessStatusCode,
                        "  and an extension with no mapping is still refused ("
                        + (int)denied.StatusCode + ")");
                }
                return failures;
            }
            finally
            {
                try { System.IO.File.Delete(archive); } catch { }
                try { System.IO.File.Delete(unmapped); } catch { }
            }
        }

        /// <summary>
        /// The last two endpoints that name a content type and had never been called: the game
        /// mode's JSON download, and the infolog page.
        ///
        /// The second one does not work, and cannot be made to from here. BattlesController.Logs
        /// calls ReplayStorage.GetFileContent, which dereferences the Azure container client
        /// without checking it - and that client is NULL whenever replay storage is not configured,
        /// which the constructor already warns about before returning early. So on any deployment
        /// without blob storage the infolog page is a 500 for the moderators who are the only ones
        /// who can see it.
        ///
        /// Its sibling handles the same case: ReplaysController.Download wraps the blob call in a
        /// try/catch and falls back to local disk. Logs has neither the catch nor the fallback.
        /// Same code on both stacks, so it is not a port defect, and the note records it rather
        /// than asserting it.
        /// </summary>
        private static async Task<int> CheckGameModeAndInfolog()
        {
            Console.WriteLine();
            Console.WriteLine("game mode download, and the infolog page:");

            var failures = await WithGameMode(async (gameModeID, shortName, json) =>
            {
                var seeded = 0;
                using (var client = new HttpClient())
                {
                    var detail = await client.GetAsync(Url + "/Mods/Detail/" + gameModeID);
                    var detailHtml = await detail.Content.ReadAsStringAsync();
                    seeded += Check(detail.IsSuccessStatusCode && detailHtml.Contains("</html>"),
                        "  /Mods/Detail is a whole page (" + (int)detail.StatusCode + ", "
                        + detailHtml.Length + " bytes)");

                    var download = await client.GetAsync(Url + "/Mods/Download/" + gameModeID);
                    var body = await download.Content.ReadAsStringAsync();
                    seeded += Check(download.IsSuccessStatusCode
                                    && download.Content.Headers.ContentType?.MediaType == "application/json",
                        "  /Mods/Download is application/json ("
                        + (download.Content.Headers.ContentType?.MediaType ?? "none") + ")");
                    seeded += Check(body == json, "  and the body is that game mode's JSON");

                    // The filename is built from ShortName. A download whose name is lost is still
                    // a 200 of the right bytes, so nothing else here would notice.
                    var disposition = download.Content.Headers.ContentDisposition;
                    seeded += Check(disposition?.FileName?.Contains(shortName) == true,
                        "  offered as " + shortName + ".json (" + (disposition?.FileName ?? "no filename") + ")");
                }
                return seeded;
            });

            // This was a NOTE until the defect behind it was fixed, and it is an assertion now.
            //
            // With no blob storage configured, ReplayStorage.GetFileContent used to dereference a
            // null container client and the page was a 500 - for the moderators who are the only
            // people who can reach it, on any deployment that has not set ReplaysConnectionString.
            // The constructor already traced a warning about exactly that and nothing connected
            // the two.
            //
            // "No infolog stored" is the right answer here rather than a 404: the battle exists,
            // the page exists, and what is missing is the file. The assertion is on the BODY,
            // because a 200 carrying an exception page would pass a status check.
            failures += await AsModerator(async moderator =>
            {
                int battleID;
                using (var db = new ZkDataContext()) battleID = db.SpringBattles.OrderBy(x => x.SpringBattleID).First().SpringBattleID;

                var logs = await moderator.GetAsync(Url + "/Battles/Logs/" + battleID);
                var body = await logs.Content.ReadAsStringAsync();
                return Check(logs.IsSuccessStatusCode && body.Contains("No infolog stored"),
                    "  /Battles/Logs says so plainly with no blob storage, rather than 500ing ("
                    + (int)logs.StatusCode + ", \"" + body.Trim() + "\")");
            });

            return failures;
        }

        /// <summary>
        /// Seeds one game mode and removes it. ForumThreadID is nullable here and Detail reaches it
        /// with ?., so unlike the mission and news helpers this one needs no thread behind it.
        /// </summary>
        private static async Task<int> WithGameMode(Func<int, string, string, Task<int>> body)
        {
            const string shortName = "harnessmode";
            const string json = "{\"harness\":true}";
            int gameModeID;

            using (var db = new ZkDataContext())
            {
                var maintainer = db.Accounts.OrderBy(a => a.AccountID).First();
                var mode = new GameMode
                {
                    ShortName = shortName,
                    DisplayName = "Harness Game Mode",
                    GameModeJson = json,
                    IsFeatured = false,
                    Created = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    MaintainerAccountID = maintainer.AccountID,
                };
                db.GameModes.Add(mode);
                db.SaveChanges();
                gameModeID = mode.GameModeID;
            }

            try
            {
                return await body(gameModeID, shortName, json);
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var mode = db.GameModes.FirstOrDefault(x => x.GameModeID == gameModeID);
                    if (mode != null) { db.GameModes.Remove(mode); db.SaveChanges(); }
                }
            }
        }

        /// <summary>
        /// Downloading a replay off local disk, which nothing here had done.
        ///
        /// ReplaysController.Download tries blob storage first and falls back to
        /// ReplayStorage.GetLocalFileContent, under GlobalConst.SpringieDataDir. That path used to
        /// be a hardcoded c:\projekty\springie_spring which off Windows was not a path at all but
        /// an ordinary filename - which is where the 72MB of engine once in this working tree came
        /// from. It resolves under the user profile on Unix now, and this is the first thing to
        /// read a file back through it.
        ///
        /// **A replay that is not there answers 500**, and that is recorded rather than asserted.
        /// GetLocalFileContent returns null and File(null, ...) throws ArgumentNullException, on
        /// both stacks - MVC 5's FileContentResult rejects a null array in the same way. It is the
        /// third instance of one shape in this harness: a lookup returns null for "not found" and
        /// the thing handed that null does not expect one. /ContentService has the same, and
        /// PollController had it until it was fixed. Fixing this one is a production behaviour
        /// change and belongs in its own review.
        /// </summary>
        private static async Task<int> CheckReplayDownload()
        {
            Console.WriteLine();
            Console.WriteLine("replay download:");

            var name = "harness-probe.sdfz";
            var contents = new byte[] { 11, 22, 33, 44, 55 };
            var folder = Path.Combine(GlobalConst.SpringieDataDir, "demos-server");
            var path = Path.Combine(folder, name);

            Directory.CreateDirectory(folder);
            File.WriteAllBytes(path, contents);

            try
            {
                var failures = 0;
                using (var client = new HttpClient())
                {
                    // /Replays/{name}, which is the only shape that works. See the note below
                    // about /Replays/Download?name=, which this check used to ask for.
                    var got = await client.GetAsync(Url + "/Replays/" + name);
                    var bytes = await got.Content.ReadAsByteArrayAsync();

                    failures += Check(got.IsSuccessStatusCode,
                        "  a replay on local disk is served (" + (int)got.StatusCode + ")");
                    failures += Check(got.Content.Headers.ContentType?.MediaType == "application/octet-stream",
                        "  as an octet-stream ("
                        + (got.Content.Headers.ContentType?.MediaType ?? "none") + ")");

                    // The bytes, not the length: a wrong file of the right size would pass a count.
                    failures += Check(bytes.Length == contents.Length && bytes[0] == contents[0]
                                      && bytes[4] == contents[4],
                        "  and it is that file's bytes (" + bytes.Length + " of " + contents.Length + ")");

                    // Which also says SpringieDataDir resolved to somewhere real: the file was
                    // written through GlobalConst and read back through ReplayStorage, and the two
                    // agreeing is the whole point on a platform where that used to be a filename.
                    failures += Check(Directory.Exists(folder),
                        "  through SpringieDataDir, which is a directory here (" + folder + ")");

                    // The route EATS the action segment, and that is the point of this note.
                    // Replays/{name} sits above the default route and its first segment after
                    // "Replays" is the file name, so /Replays/Download?name=x binds name from the
                    // ROUTE - "Download" - and the query string never gets a say. Route values
                    // beat the query string on both stacks, and MVC 5 declares the same template
                    // with name = UrlParameter.Optional, so this URL has never worked in
                    // production. It worked HERE, and this check asked for it, only because the
                    // route was missing and the default one bound ?name= instead. A check that
                    // passed BECAUSE of the defect next to it.
                    var shadowed = await client.GetAsync(Url + "/Replays/Download?name=" + name);
                    Console.WriteLine("   note  /Replays/Download?name= answers " + (int)shadowed.StatusCode
                                      + " - Replays/{name} binds name=\"Download\" and the query is"
                                      + " ignored, on both stacks");

                    // Same cause, and it costs nothing: /Replays and /Replays/Index reach Download
                    // too, so Index() is unreachable. It returns Content("") and nothing in the
                    // views or the controllers links to it, which is why no one has noticed.
                    var index = await client.GetAsync(Url + "/Replays/Index");
                    Console.WriteLine("   note  /Replays/Index answers " + (int)index.StatusCode
                                      + " for the same reason - ReplaysController.Index is dead on"
                                      + " both stacks, and returns an empty page anyway");

                    // Was a note - "File(null, ...) throws on both stacks" - and is an assertion
                    // now that the defect behind it has been reviewed and fixed. 404 rather than
                    // the Content("No such ...") this codebase usually writes, because a browser
                    // handed 200 and a sentence saves the sentence as a .sdfz.
                    var missing = await client.GetAsync(Url + "/Replays/not-a-replay.sdfz");
                    failures += Check((int)missing.StatusCode == 404,
                        "  a replay that is not there is a 404, not a 500 ("
                        + (int)missing.StatusCode + ")");
                }
                return failures;
            }
            finally
            {
                try { File.Delete(path); } catch { }
            }
        }

        /// <summary>
        /// The two file downloads a mission serves, and the resumable one in particular.
        ///
        /// CheckResumableDownload already exercises ResumingFileContentResult - but through
        /// /Harness/Resumable, an endpoint written for it, over ten bytes this process made up.
        /// That checks the SHIM. It does not check that a production action wires it up, or that it
        /// survives a byte array that came out of a varbinary column. This does, over
        /// MissionsController.File, which is what a player's client actually downloads.
        ///
        /// Note the URL, and the correction in it: this said that /Missions/File/1 ending in a
        /// 500 was "routing, not a port defect", because the default route calls a third segment
        /// `id` and the action takes `name`. That was wrong. Global.asax declares
        /// Missions/File/{name} explicitly, and the 500 was this host never registering it. Both
        /// shapes are asked for below and both now answer 200.
        /// </summary>
        private static async Task<int> CheckMissionDownloads()
        {
            Console.WriteLine();
            Console.WriteLine("mission downloads:");

            return await WithMission(async (missionID, name, mutator) =>
            {
                var failures = 0;
                using (var client = new HttpClient())
                {
                    var detail = await client.GetAsync(Url + "/Missions/Detail/" + missionID);
                    var detailHtml = await detail.Content.ReadAsStringAsync();
                    failures += Check(detail.IsSuccessStatusCode && detailHtml.Contains("</html>"),
                        "  /Missions/Detail is a whole page (" + (int)detail.StatusCode + ", "
                        + detailHtml.Length + " bytes)");
                    failures += Check(detailHtml.Contains(name), "  and the mission reached it");

                    var script = await client.GetAsync(Url + "/Missions/Script/" + missionID);
                    failures += Check(script.IsSuccessStatusCode
                                      && script.Content.Headers.ContentType?.MediaType == "application/octet-stream",
                        "  /Missions/Script is an octet-stream ("
                        + (script.Content.Headers.ContentType?.MediaType ?? "none") + ")");

                    var whole = await client.GetAsync(Url + "/Missions/File?name=" + missionID);
                    var wholeBytes = await whole.Content.ReadAsByteArrayAsync();
                    failures += Check(whole.IsSuccessStatusCode && wholeBytes.Length == mutator.Length,
                        "  /Missions/File returns the mutator whole (" + (int)whole.StatusCode + ", "
                        + wholeBytes.Length + " of " + mutator.Length + " bytes)");
                    failures += Check(whole.Headers.AcceptRanges.Contains("bytes"),
                        "  advertising Accept-Ranges: bytes");

                    // The part the harness endpoint cannot tell you: a real action, a real column.
                    // The ROUTE form. Global.asax maps Missions/File/{name}, and this check used
                    // ?name= until that route was ported - at which point the comment here said the
                    // 500 from /Missions/File/5011 was "routing, not the port". It was the port:
                    // MVC 5 has the route and this host did not.
                    var byRoute = await client.GetAsync(Url + "/Missions/File/" + missionID);
                    var routeBytes = await byRoute.Content.ReadAsByteArrayAsync();
                    failures += Check(byRoute.IsSuccessStatusCode && routeBytes.Length == mutator.Length,
                        "  and /Missions/File/{name} reaches it too (" + (int)byRoute.StatusCode + ")");

                    var request = new HttpRequestMessage(HttpMethod.Get, Url + "/Missions/File?name=" + missionID);
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(1, 3);
                    var partial = await client.SendAsync(request);
                    var partialBytes = await partial.Content.ReadAsByteArrayAsync();

                    failures += Check((int)partial.StatusCode == 206,
                        "  a Range request is 206 Partial Content (" + (int)partial.StatusCode + ")");
                    failures += Check(partial.Content.Headers.ContentRange?.ToString()
                                      == "bytes 1-3/" + mutator.Length,
                        "  Content-Range names the slice and the total ("
                        + partial.Content.Headers.ContentRange + ")");
                    failures += Check(partialBytes.Length == 3 && partialBytes[0] == mutator[1]
                                      && partialBytes[2] == mutator[3],
                        "  and the bytes are the slice asked for, not the first three");
                }
                return failures;
            });
        }

        /// <summary>
        /// Seeds one mission, and the thread and category its NOT NULL ForumThreadID needs. The
        /// mutator is six bytes whose values differ, so a check that returned the wrong slice cannot
        /// pass by accident.
        /// </summary>
        private static async Task<int> WithMission(Func<int, string, byte[], Task<int>> body)
        {
            const string name = "Harness Mission";
            var mutator = new byte[] { 10, 20, 30, 40, 50, 60 };
            int missionID, threadID, categoryID;

            using (var db = new ZkDataContext())
            {
                var author = db.Accounts.OrderBy(a => a.AccountID).First();

                var category = new ForumCategory { Title = "Harness Mission Category", ForumMode = ForumMode.Missions };
                db.ForumCategories.Add(category);
                db.SaveChanges();
                categoryID = category.ForumCategoryID;

                var thread = new ForumThread
                {
                    Title = name,
                    Created = DateTime.UtcNow,
                    LastPost = DateTime.UtcNow,
                    CreatedAccountID = author.AccountID,
                    LastPostAccountID = author.AccountID,
                    ForumCategoryID = categoryID,
                };
                db.ForumThreads.Add(thread);
                db.SaveChanges();
                threadID = thread.ForumThreadID;

                var mission = new Mission
                {
                    Name = name,
                    Image = new byte[] { 1, 2, 3, 4 },
                    Mutator = mutator,
                    Script = "seeded by the harness",
                    CreatedTime = DateTime.UtcNow,
                    ModifiedTime = DateTime.UtcNow,
                    Revision = 1,
                    AccountID = author.AccountID,
                    MinHumans = 1,
                    MaxHumans = 2,
                    IsScriptMission = true,
                    MissionRunCount = 0,
                    IsDeleted = false,
                    IsCoop = false,
                    ForumThreadID = threadID,
                    RequiredForMultiplayer = false,
                };
                db.Missions.Add(mission);
                db.SaveChanges();
                missionID = mission.MissionID;
            }

            try
            {
                return await body(missionID, name, mutator);
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var mission = db.Missions.FirstOrDefault(x => x.MissionID == missionID);
                    if (mission != null) db.Missions.Remove(mission);
                    db.ForumThreadLastReads.RemoveRange(db.ForumThreadLastReads.Where(x => x.ForumThreadID == threadID));
                    var thread = db.ForumThreads.FirstOrDefault(x => x.ForumThreadID == threadID);
                    if (thread != null) db.ForumThreads.Remove(thread);
                    var category = db.ForumCategories.FirstOrDefault(x => x.ForumCategoryID == categoryID);
                    if (category != null) db.ForumCategories.Remove(category);
                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// /ContentService, the other .svc replacement - and the one nothing here had ever called.
        /// /MissionService has had six assertions since it was ported; this had none, although it is
        /// the endpoint the GAME CLIENT uses to resolve a map or a game before downloading it.
        ///
        /// Two defects turned up on the first call, both in linked production code and therefore on
        /// BOTH stacks - neither is a port regression, and neither is fixed here. They are recorded
        /// as notes rather than asserted, because pinning a defect as expected behaviour is how it
        /// stops being one:
        ///
        /// 1. GetResourceDataRequest for a name that does not exist answers **HTTP 500 with an empty
        ///    body**. The handler returns null and CommandJsonSerializer.SerializeToLine(null)
        ///    dereferences it. A client asking about a map this server has never heard of is the
        ///    ordinary case, not an exceptional one.
        /// 2. FindResourceDataRequest's exact-name fast path is dead: it builds a
        ///    FindResourceDataResponse and never returns it - `new FindResourceDataResponse() {...};`
        ///    with no `return` - so execution falls through to the search below it, which
        ///    additionally requires a resource to have a content file with LinkCount > 0. The two
        ///    paths do not answer the same question, which is why the dead one was written.
        /// </summary>
        private static async Task<int> CheckContentService()
        {
            Console.WriteLine();
            Console.WriteLine("/ContentService (the client's resource lookup):");

            var failures = 0;
            string mapName;
            int resourceID;
            using (var db = new ZkDataContext())
            {
                var map = db.Resources.OrderBy(x => x.ResourceID).First();
                mapName = map.InternalName;
                resourceID = map.ResourceID;
            }

            using (var client = new HttpClient())
            {
                async Task<HttpResponseMessage> Post(string line) =>
                    await client.PostAsync(Url + "/ContentService",
                        new StringContent(line, System.Text.Encoding.UTF8, "text/plain"));

                // An empty body is the documented hint, and it is how you find out the protocol.
                var hint = await Post("");
                var hintBody = await hint.Content.ReadAsStringAsync();
                failures += Check(hint.IsSuccessStatusCode && hintBody.Contains("ClassName JsonSerializedClassContent"),
                    "  an empty request answers with the format hint (" + (int)hint.StatusCode + ")");

                // The real thing: resolve a map by the name a client holds.
                var hit = await Post("GetResourceDataRequest {\"InternalName\":\"" + mapName + "\"}");
                var hitBody = await hit.Content.ReadAsStringAsync();
                failures += Check(hit.IsSuccessStatusCode, "  GetResourceDataRequest answered ("
                                                           + (int)hit.StatusCode + ", " + hitBody.Length + " bytes)");
                failures += Check(hitBody.StartsWith("ResourceData "),
                    "  dispatched by request class, and answered with the matching response");
                failures += Check(hitBody.Contains("\"InternalName\":\"" + mapName + "\"")
                                  && hitBody.Contains("\"ResourceID\":" + resourceID),
                    "  carrying that resource and not an empty envelope");
                failures += Check(hit.Content.Headers.ContentType?.MediaType == "application/json",
                    "  as application/json (" + (hit.Content.Headers.ContentType?.MediaType ?? "none") + ")");

                // The search shape. Against this fixture it finds nothing and that is CORRECT - see
                // the note below - so the assertion is on the envelope, not on the rows.
                var find = await Post("FindResourceDataRequest {\"Words\":[\"test\"],\"Type\":0}");
                var findBody = await find.Content.ReadAsStringAsync();
                failures += Check(find.IsSuccessStatusCode && findBody.StartsWith("FindResourceDataResponse "),
                    "  FindResourceDataRequest answered with its own response type");

                using (var db = new ZkDataContext())
                {
                    var downloadable = db.ResourceContentFiles.Count(x => x.LinkCount > 0);
                    Console.WriteLine("   note  it returned no rows, and should: the search requires a content file "
                                      + "with LinkCount > 0 and this fixture has " + downloadable + " of them");
                }

                // The SAME null, in the handler next door, and this one is fixed - because the
                // client has a branch for an empty answer. TorrentDownloader.cs:66 reads
                //
                //     if (e == null || e.links == null || e.torrent == null || e.links.Count == 0)
                //
                // and treats that as "not registered or has no links". A 500 instead throws inside
                // Query and lands in the generic catch above it. Both end at down.Finish(false),
                // so this changes which reason a client is given and nothing else.
                var unknown = await Post("DownloadFileRequest {\"InternalName\":\"no_such_map_at_all\"}");
                var unknownBody = await unknown.Content.ReadAsStringAsync();
                failures += Check(unknown.IsSuccessStatusCode && unknownBody.Contains("DownloadFileResponse"),
                    "  an unknown name is an empty DownloadFileResponse, not a 500 ("
                    + (int)unknown.StatusCode + ", " + unknownBody.Length + " bytes)");

                // Recorded, not asserted, and deliberately NOT fixed the same way - see the note.
                var miss = await Post("GetResourceDataRequest {\"InternalName\":\"no_such_map_at_all\"}");
                Console.WriteLine("   note  GetResourceDataRequest for a name that does not exist still answers "
                                  + (int)miss.StatusCode + " with "
                                  + (await miss.Content.ReadAsStringAsync()).Length
                                  + " bytes, on both stacks - and an empty response would be WORSE than the 500 "
                                  + "here: PlasmaResourceChecker.cs:307 treats a NULL result as 'ask later' and "
                                  + "anything non-null as a resource it now knows, so it would copy "
                                  + "result.InternalName and result.ResourceType straight off an empty object. "
                                  + "Fixing it needs a wire representation of null that Query turns back into "
                                  + "null, which is a protocol decision rather than a one-line one");
            }

            return failures;
        }

        /// <summary>
        /// The RSS feed, which is the one place here where the CONTENT TYPE is the thing being
        /// checked rather than the body.
        ///
        /// NewsController.Index sets Response.ContentType = "application/rss+xml" and then returns a
        /// View. On MVC 5 that sticks. On ASP.NET Core a ViewResult resolves its own content type
        /// when it executes, and only honours what the action already set because the response's
        /// value takes precedence over the default - which is a framework detail, not something the
        /// code here states. Nothing else in this harness would notice it becoming text/html: the
        /// status is 200 either way, the body is the same bytes, and the only thing that breaks is
        /// every feed reader subscribed to it.
        ///
        /// It also renders News/Index.cshtml and News/NewsDetail.cshtml, which no run reached before
        /// - the fixture carries no News rows, so the feed's @Model.First() threw and the page 500d.
        /// </summary>
        private static async Task<int> CheckTheNewsFeed()
        {
            Console.WriteLine();
            Console.WriteLine("the news feed:");

            return await WithNews(async (newsID, title) =>
            {
                var failures = 0;
                using (var client = new HttpClient())
                {
                    var feed = await client.GetAsync(Url + "/News");
                    var body = await feed.Content.ReadAsStringAsync();

                    failures += Check(feed.IsSuccessStatusCode,
                        "  /News was served (" + (int)feed.StatusCode + ", " + body.Length + " bytes)");

                    // The assertion this check exists for.
                    var type = feed.Content.Headers.ContentType?.MediaType;
                    failures += Check(type == "application/rss+xml",
                        "  and as application/rss+xml, not a page (" + (type ?? "none") + ")");

                    // ...and that it is really a feed, because a 200 of the wrong thing under the
                    // right header would pass the line above.
                    failures += Check(body.TrimStart().StartsWith("<?xml") && body.Contains("<rss"),
                        "  and the body is an RSS document");
                    failures += Check(body.Contains(title), "  with the seeded item in it");

                    var detail = await client.GetAsync(Url + "/News/Detail/" + newsID);
                    var detailHtml = await detail.Content.ReadAsStringAsync();
                    failures += Check(detail.IsSuccessStatusCode && detailHtml.Contains("</html>"),
                        "  /News/Detail is a whole page (" + (int)detail.StatusCode + ", "
                        + detailHtml.Length + " bytes)");
                    failures += Check(detailHtml.Contains(title), "  and the item reached it");
                }
                return failures;
            });
        }

        /// <summary>
        /// Seeds one news item and removes it again. News.ForumThreadID is NOT NULL, so this needs a
        /// thread, which needs a category - all three are seeded and all three are removed.
        /// </summary>
        private static async Task<int> WithNews(Func<int, string, Task<int>> body)
        {
            const string title = "Harness News Item";
            int newsID, threadID, categoryID;

            using (var db = new ZkDataContext())
            {
                var author = db.Accounts.OrderBy(a => a.AccountID).First();

                var category = new ForumCategory { Title = "Harness News Category", ForumMode = ForumMode.News };
                db.ForumCategories.Add(category);
                db.SaveChanges();
                categoryID = category.ForumCategoryID;

                var thread = new ForumThread
                {
                    Title = title,
                    Created = DateTime.UtcNow,
                    LastPost = DateTime.UtcNow,
                    CreatedAccountID = author.AccountID,
                    LastPostAccountID = author.AccountID,
                    ForumCategoryID = categoryID,
                };
                db.ForumThreads.Add(thread);
                db.SaveChanges();
                threadID = thread.ForumThreadID;

                var news = new News
                {
                    // Yesterday: Index filters on Created < UtcNow, so an item stamped "now" can
                    // lose a race with the clock and leave the feed empty.
                    Created = DateTime.UtcNow.AddDays(-1),
                    Title = title,
                    Text = "Seeded by the harness to render the feed.",
                    AuthorAccountID = author.AccountID,
                    HeadlineUntil = DateTime.UtcNow.AddDays(1),
                    ForumThreadID = threadID,
                };
                db.News.Add(news);
                db.SaveChanges();
                newsID = news.NewsID;
            }

            try
            {
                return await body(newsID, title);
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var news = db.News.FirstOrDefault(x => x.NewsID == newsID);
                    if (news != null) db.News.Remove(news);
                    db.ForumThreadLastReads.RemoveRange(db.ForumThreadLastReads.Where(x => x.ForumThreadID == threadID));
                    var thread = db.ForumThreads.FirstOrDefault(x => x.ForumThreadID == threadID);
                    if (thread != null) db.ForumThreads.Remove(thread);
                    var category = db.ForumCategories.FirstOrDefault(x => x.ForumCategoryID == categoryID);
                    if (category != null) db.ForumCategories.Remove(category);
                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Seeds a forum category, a thread in it and two posts, and removes them again. The
        /// fixture has no ForumThreads rows, which is why Forum/Thread.cshtml, Forum/PostList.cshtml,
        /// Shared/DisplayTemplates/ForumPost.cshtml and PostHistory/PostHistoryIndex.cshtml were
        /// among the views no run here renders.
        /// </summary>
        private static async Task<int> WithForumThread(Func<int, int, Task<int>> body)
        {
            int categoryID, threadID, postID;

            using (var db = new ZkDataContext())
            {
                var category = new ForumCategory { Title = "Harness Category", ForumMode = ForumMode.General };
                db.ForumCategories.Add(category);
                db.SaveChanges();
                categoryID = category.ForumCategoryID;

                var author = db.Accounts.OrderBy(a => a.AccountID).First();
                var thread = new ForumThread
                {
                    Title = "Harness Thread",
                    Created = DateTime.UtcNow,
                    LastPost = DateTime.UtcNow,
                    CreatedAccountID = author.AccountID,
                    LastPostAccountID = author.AccountID,
                    ForumCategoryID = categoryID,
                    PostCount = 2,
                };
                db.ForumThreads.Add(thread);
                db.SaveChanges();
                threadID = thread.ForumThreadID;

                var first = new ForumPost { AuthorAccountID = author.AccountID, ForumThreadID = threadID, Text = "Harness post body one" };
                var second = new ForumPost { AuthorAccountID = author.AccountID, ForumThreadID = threadID, Text = "Harness post body two" };
                db.ForumPosts.Add(first);
                db.ForumPosts.Add(second);
                db.SaveChanges();
                postID = first.ForumPostID;
            }

            try
            {
                return await body(threadID, postID);
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    db.ForumThreadLastReads.RemoveRange(db.ForumThreadLastReads.Where(x => x.ForumThreadID == threadID));
                    db.ForumPosts.RemoveRange(db.ForumPosts.Where(x => x.ForumThreadID == threadID));
                    var thread = db.ForumThreads.FirstOrDefault(x => x.ForumThreadID == threadID);
                    if (thread != null) db.ForumThreads.Remove(thread);
                    var category = db.ForumCategories.FirstOrDefault(x => x.ForumCategoryID == categoryID);
                    if (category != null) db.ForumCategories.Remove(category);
                    db.SaveChanges();
                }
            }
        }

        private static async Task<int> WithFactionAndClan(Func<int, int, Task<int>> body)
        {
            int factionID, clanID, accountID;
            int? originalFaction, originalClan;

            using (var db = new ZkDataContext())
            {
                var faction = NewHarnessFaction();
                db.Factions.Add(faction);

                var clan = new Clan { ClanName = "Harness Clan", Shortcut = "HC", IsDeleted = false };
                db.Clans.Add(clan);
                db.SaveChanges();

                factionID = faction.FactionID;
                clanID = clan.ClanID;

                var account = db.Accounts.OrderBy(a => a.AccountID).First();
                accountID = account.AccountID;
                originalFaction = account.FactionID;
                originalClan = account.ClanID;
                account.FactionID = factionID;
                account.ClanID = clanID;
                db.SaveChanges();
            }

            try
            {
                return await body(factionID, clanID);
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var account = db.Accounts.Single(a => a.AccountID == accountID);
                    account.FactionID = originalFaction;
                    account.ClanID = originalClan;
                    db.SaveChanges();

                    var clan = db.Clans.FirstOrDefault(c => c.ClanID == clanID);
                    if (clan != null) db.Clans.Remove(clan);
                    var faction = db.Factions.FirstOrDefault(f => f.FactionID == factionID);
                    if (faction != null) db.Factions.Remove(faction);
                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// A clan with a password on it, and nobody in it.
        ///
        /// Outside any faction/clan scope deliberately - Clan.CanJoin returns false for an
        /// account that already belongs to one, so the harness account has to be clanless when
        /// this runs.
        ///
        /// The clan takes the account's OWN faction rather than having none, and that is not
        /// tidiness either. Clan.cs:60 refuses a join when the account has a faction and the clan
        /// is a different one, and whether this account has a faction depends on what else has
        /// run: nothing in the plain harness gives it one, and tools/stack.sh seeds a PlanetWars
        /// round that does. A clan with no faction therefore passed here and failed there, which
        /// is exactly what the two-container job is for. Reading the account first makes the
        /// check say the same thing in both.
        /// </summary>
        private static async Task<int> WithPasswordedClan(Func<int, Task<int>> body)
        {
            int clanID;

            // Generated rather than written down, and tools/check-secrets.py is why: a literal
            // assigned to anything called Password is exactly what that check hunts for, and it
            // caught this one. It was right to - a committed password is a committed password
            // whether or not it guards a fixture - and the check has no way to tell the two
            // apart. Nothing here needs a KNOWN password, only one the request below does not
            // send, so there is no reason for a literal to exist at all.
            // Sixteen characters, because Clan.Password is [StringLength(20)] and a whole GUID is
            // thirty-two. The port's reproduced EF6 save-time validation said so rather than the
            // database truncating it quietly, which is the behaviour that validation exists for.
            var password = Guid.NewGuid().ToString("N").Substring(0, 16);

            int? restoreClan;
            int accountID;

            using (var db = new ZkDataContext())
            {
                var account = db.Accounts.OrderBy(a => a.AccountID).First();
                accountID = account.AccountID;
                restoreClan = account.ClanID;
                account.ClanID = null;

                var clan = new Clan
                {
                    ClanName = "Harness Locked Clan",
                    Shortcut = "HLC",
                    Password = password,
                    FactionID = account.FactionID,
                };
                db.Clans.Add(clan);
                db.SaveChanges();
                clanID = clan.ClanID;
            }

            try
            {
                return await body(clanID);
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    // Anyone who did join is put back first, or the clan will not delete.
                    foreach (var joined in db.Accounts.Where(x => x.ClanID == clanID).ToList())
                        joined.ClanID = null;

                    var account = db.Accounts.FirstOrDefault(x => x.AccountID == accountID);
                    if (account != null && account.ClanID == null) account.ClanID = restoreClan;
                    db.SaveChanges();

                    var clan = db.Clans.FirstOrDefault(x => x.ClanID == clanID);
                    if (clan != null) { db.Clans.Remove(clan); db.SaveChanges(); }
                }
            }
        }

        /// <summary>
        /// A structure that costs upkeep energy, on a planet this faction owns.
        ///
        /// The planet's owner is set and then put BACK. WithGalaxy leaves its planets neutral on
        /// purpose - that is what makes them attackable without seeding a whole game state, and it
        /// says so - so borrowing one has to be temporary or the next check inherits a conquered
        /// galaxy.
        ///
        /// UpkeepEnergy above zero is not decoration: Planet.cshtml:237 tests exactly that before
        /// it gets as far as asking who may change the priority.
        /// </summary>
        private static async Task<int> WithOwnedStructure(int planetID, int factionID, Func<string, Task<int>> body)
        {
            const string structureName = "Harness Energy Structure";
            int structureTypeID;
            int? originalOwner;

            using (var db = new ZkDataContext())
            {
                var type = new StructureType
                {
                    Name = structureName,
                    UpkeepEnergy = 1,
                };
                db.StructureTypes.Add(type);
                db.SaveChanges();
                structureTypeID = type.StructureTypeID;

                var planet = db.Planets.Single(x => x.PlanetID == planetID);
                originalOwner = planet.OwnerFactionID;
                planet.OwnerFactionID = factionID;

                db.PlanetStructures.Add(new PlanetStructure
                {
                    PlanetID = planetID,
                    StructureTypeID = structureTypeID,
                    IsActive = true,
                });
                db.SaveChanges();
            }

            try
            {
                return await body(structureName);
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var structures = db.PlanetStructures
                                       .Where(x => x.StructureTypeID == structureTypeID).ToList();
                    if (structures.Count > 0) db.PlanetStructures.RemoveRange(structures);

                    var planet = db.Planets.FirstOrDefault(x => x.PlanetID == planetID);
                    if (planet != null) planet.OwnerFactionID = originalOwner;
                    db.SaveChanges();

                    var type = db.StructureTypes.FirstOrDefault(x => x.StructureTypeID == structureTypeID);
                    if (type != null) { db.StructureTypes.Remove(type); db.SaveChanges(); }
                }
            }
        }

        /// <summary>
        /// A FACTION role on an account, carrying whichever right the caller needs.
        ///
        /// Not clan-only, and that is the whole point: Account.HasFactionRight is "any AccountRole
        /// whose RoleType is NOT clan-only and passes the test", so the clan-only role
        /// WithRoleAndPunishment seeds cannot serve here however similar it looks.
        ///
        /// The right is a parameter because two checks want different ones - RightDiplomacy to
        /// propose a treaty, RightSetEnergyPriority to change a structure's priority - and a
        /// second near-identical helper would be two things to keep in step.
        /// </summary>
        private static async Task<int> WithFactionRole(int accountID, Action<RoleType> grant, Func<Task<int>> body)
        {
            int roleTypeID;
            using (var db = new ZkDataContext())
            {
                var role = new RoleType
                {
                    Name = "Harness Faction Role",
                    Description = "Seeded by the host harness",
                    IsClanOnly = false,
                    PollDurationDays = 1,
                };
                grant(role);
                db.RoleTypes.Add(role);
                db.SaveChanges();
                roleTypeID = role.RoleTypeID;

                db.AccountRoles.Add(new AccountRole
                {
                    AccountID = accountID,
                    RoleTypeID = roleTypeID,
                    Inauguration = DateTime.UtcNow,
                });
                db.SaveChanges();
            }

            try
            {
                return await body();
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var roles = db.AccountRoles.Where(x => x.RoleTypeID == roleTypeID).ToList();
                    if (roles.Count > 0) { db.AccountRoles.RemoveRange(roles); db.SaveChanges(); }
                    var role = db.RoleTypes.FirstOrDefault(x => x.RoleTypeID == roleTypeID);
                    if (role != null) { db.RoleTypes.Remove(role); db.SaveChanges(); }
                }
            }
        }

        /// <summary>
        /// A second faction, a treaty between the two, and one effect under it.
        ///
        /// PROPOSED rather than Accepted, because Factions/Detail.cshtml:77 calls DisplayFor only
        /// for that state - the other states are summarised in a line of their own and never reach
        /// the template.
        ///
        /// The treaty is viewed ANONYMOUSLY, which is safe and deliberate: the template ends with
        /// tr.CanCancel(Global.Account), and CanCancel returns false for a null account rather
        /// than dereferencing it. Nothing here needs the diplomacy right that PROPOSING one does.
        /// </summary>
        private static async Task<int> WithProposedTreaty(int factionID, int accountID,
                                                          Func<int, string, Task<int>> body)
        {
            const string effectName = "Harness Treaty Effect";
            int otherFactionID, effectTypeID, treatyID;

            using (var db = new ZkDataContext())
            {
                var other = NewHarnessFaction();
                other.Name = "Harness Other Faction";
                other.Shortcut = "HARN2";
                db.Factions.Add(other);

                var effectType = new TreatyEffectType
                {
                    Name = effectName,
                    Description = "Seeded by the host harness",
                };
                db.TreatyEffectTypes.Add(effectType);
                db.SaveChanges();
                otherFactionID = other.FactionID;
                effectTypeID = effectType.EffectTypeID;

                var treaty = new FactionTreaty
                {
                    ProposingFactionID = factionID,
                    AcceptingFactionID = otherFactionID,
                    ProposingAccountID = accountID,
                    TreatyState = TreatyState.Proposed,
                };
                db.FactionTreaties.Add(treaty);
                db.SaveChanges();
                treatyID = treaty.FactionTreatyID;

                db.TreatyEffects.Add(new TreatyEffect
                {
                    FactionTreatyID = treatyID,
                    EffectTypeID = effectTypeID,
                    GivingFactionID = factionID,
                    ReceivingFactionID = otherFactionID,
                });
                db.SaveChanges();
            }

            try
            {
                return await body(otherFactionID, effectName);
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var effects = db.TreatyEffects.Where(x => x.FactionTreatyID == treatyID).ToList();
                    if (effects.Count > 0) db.TreatyEffects.RemoveRange(effects);
                    db.SaveChanges();

                    var treaty = db.FactionTreaties.FirstOrDefault(x => x.FactionTreatyID == treatyID);
                    if (treaty != null) db.FactionTreaties.Remove(treaty);
                    var effectType = db.TreatyEffectTypes.FirstOrDefault(x => x.EffectTypeID == effectTypeID);
                    if (effectType != null) db.TreatyEffectTypes.Remove(effectType);
                    db.SaveChanges();

                    var other = db.Factions.FirstOrDefault(x => x.FactionID == otherFactionID);
                    if (other != null) { db.Factions.Remove(other); db.SaveChanges(); }
                }
            }
        }

        /// <summary>
        /// One clan role and one EXPIRED punishment on an account, which are what
        /// DisplayTemplates/AccountRole and DisplayTemplates/Punishment need in order to render
        /// at all. The fixture has neither.
        ///
        /// Expired, and with no ban flag set, and that is not tidiness. AsAccount and AsModerator
        /// sign in as the lowest AccountID there is - the same account this seeds against - so a
        /// live punishment carrying BanSite would put the harness behind its own ban middleware
        /// and every check after this one would answer 403. AdminUserDetail renders the EXPIRED
        /// list anyway, which is the branch wanted here.
        ///
        /// IsClanOnly on the role for a related reason: UserRoleList skips a role that is not
        /// clan-only while PlanetWarsMode is AllOffline, and a clan-only one renders whatever the
        /// mode is.
        /// </summary>
        private static async Task<int> WithRoleAndPunishment(int accountID, int clanID, Func<string, Task<int>> body)
        {
            const string roleName = "Harness Clan Role";
            int roleTypeID, punishmentID;

            using (var db = new ZkDataContext())
            {
                var role = new RoleType
                {
                    Name = roleName,
                    Description = "Seeded by the host harness",
                    IsClanOnly = true,
                    PollDurationDays = 1,
                };
                db.RoleTypes.Add(role);
                db.SaveChanges();
                roleTypeID = role.RoleTypeID;

                db.AccountRoles.Add(new AccountRole
                {
                    AccountID = accountID,
                    RoleTypeID = roleTypeID,
                    ClanID = clanID,
                    Inauguration = DateTime.UtcNow,
                });

                var punishment = new Punishment
                {
                    AccountID = accountID,
                    Reason = "Harness expired punishment",
                    Time = DateTime.UtcNow.AddDays(-2),
                    BanExpires = DateTime.UtcNow.AddDays(-1),
                };
                db.Punishments.Add(punishment);
                db.SaveChanges();
                punishmentID = punishment.PunishmentID;
            }

            try
            {
                return await body(roleName);
            }
            finally
            {
                using (var db = new ZkDataContext())
                {
                    var roles = db.AccountRoles.Where(x => x.RoleTypeID == roleTypeID).ToList();
                    if (roles.Count > 0) db.AccountRoles.RemoveRange(roles);
                    var punishment = db.Punishments.FirstOrDefault(x => x.PunishmentID == punishmentID);
                    if (punishment != null) db.Punishments.Remove(punishment);
                    db.SaveChanges();

                    var role = db.RoleTypes.FirstOrDefault(x => x.RoleTypeID == roleTypeID);
                    if (role != null) { db.RoleTypes.Remove(role); db.SaveChanges(); }
                }
            }
        }

        /// <summary>
        /// The two views that diverged for their child actions - Users/Detail and
        /// Battles/Detail - requested as pages.
        ///
        /// Divergence is where this port has had silent breakage before: the mechanism was once
        /// producing mixed path separators, so the Razor SDK found no _ViewImports and the
        /// PortedViews copies were quietly not being used. The report notices an orphaned or
        /// broken copy, but only a request notices that the component inside it actually runs.
        ///
        /// Both were in `other` until now for unrelated compile errors, which is what had been
        /// hiding the fact that they were child-action views at all.
        /// </summary>
        /// <summary>
        ///     The controllers no request had ever reached.
        ///
        ///     Eighteen of the site's thirty-four were exercised here; these are from the other
        ///     sixteen. They compiled, their views compiled, and nothing more - which is exactly the
        ///     gap that hid every defect the lobby port found by running things. It hid three more
        ///     here, all in Autocomplete, and all of them would have passed a check that only
        ///     asserted a status code:
        ///
        ///     - Json(data, JsonRequestBehavior.AllowGet) bound to ASP.NET Core's OWN
        ///       Json(object, object), because an instance method beats an extension method. The
        ///       compat overload was never called and the enum arrived as serializer settings: 500.
        ///     - AutocompleteItem had public FIELDS. System.Text.Json does not serialise those, so
        ///       the endpoints answered [{},{},{}] - valid JSON, correct status, no data.
        ///     - PrintMap(null, name) threw, because the ported Url(helper) dereferenced the helper
        ///       that MVC 5's version ignores.
        ///
        ///     So these assert what came back, not that something did.
        /// </summary>
        /// <summary>
        ///     Records which views render, for ZK_RENDERED_VIEWS.
        ///
        ///     A plain IObserver rather than SubscribeWithAdapter: Microsoft.Extensions
        ///     .DiagnosticAdapter, which that needs, does not exist on .NET 9. The payload of
        ///     BeforeViewPage is an anonymous type, so its `Page` comes out by reflection. The
        ///     property is capitalised; `page`, which the older adapter API used, silently finds
        ///     nothing and the log comes out empty.
        /// </summary>
        private sealed class RenderedViewRecorder : IObserver<KeyValuePair<string, object>>
        {
            private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> seen;
            private readonly string path;

            public RenderedViewRecorder(System.Collections.Concurrent.ConcurrentDictionary<string, byte> seen, string path)
            {
                this.seen = seen;
                this.path = path;
            }

            public void OnNext(KeyValuePair<string, object> item)
            {
                if (item.Key != "Microsoft.AspNetCore.Mvc.Razor.BeforeViewPage" || item.Value == null) return;

                var page = item.Value.GetType().GetProperty("Page")?.GetValue(item.Value);
                var viewPath = page?.GetType().GetProperty("Path")?.GetValue(page) as string;
                if (viewPath != null && seen.TryAdd(viewPath, 0))
                    lock (seen) System.IO.File.AppendAllText(path, viewPath + Environment.NewLine);
            }

            public void OnCompleted() { }
            public void OnError(Exception error) { }
        }

        private static async Task<int> CheckPagesNobodyHadRequested(HttpClient client)
        {
            Console.WriteLine();
            Console.WriteLine("pages no request had ever reached:");
            var failures = 0;

            // /Home, /Forum and /Clans are not from the untouched sixteen - their controllers were
            // already reached by other checks. Their INDEX views were not rendered by anything,
            // which the ZK_RENDERED_VIEWS measurement made visible: a controller being exercised
            // says nothing about which of its views ever execute.
            foreach (var path in new[]
                     {
                         "/Factions", "/Missions", "/Mods", "/Contributions", "/LobbyNews",
                         "/Download", "/Images", "/Home", "/Forum", "/Clans",
                         // /Static needs its name: Index(string name = "LobbyStart") answers
                         // Content("") for everything except UnitGuide, so the bare path is a
                         // 200 of nothing BY DESIGN. Asking for the page without it measured the
                         // default and would have been read as the view rendering.
                         "/Static?name=UnitGuide",
                     })
            {
                var response = await client.GetAsync(Url + path);
                var html = await response.Content.ReadAsStringAsync();
                failures += Check(response.IsSuccessStatusCode && html.Contains("</html>"),
                    path + " is a whole page (" + (int)response.StatusCode + ", " + html.Length + " bytes)");
            }

            // Autocomplete answers JSON, and answering it EMPTY is the failure that looked like
            // success - so every one of these checks the payload rather than the status.
            foreach (var probe in new[]
                     {
                         new { Path = "/Autocomplete?term=t", What = "everything" },
                         new { Path = "/Autocomplete/Users?term=p", What = "users" },
                         new { Path = "/Autocomplete/Maps?term=t", What = "maps" },
                     })
            {
                var response = await client.GetAsync(Url + probe.Path);
                var json = await response.Content.ReadAsStringAsync();
                failures += Check(response.IsSuccessStatusCode, probe.Path + " answered (" + (int)response.StatusCode + ")");
                failures += Check(json.Contains("\"id\"") && json.Contains("\"label\""),
                    "and the " + probe.What + " it returned have fields, not [{},{}] - "
                    + "System.Text.Json ignores public fields where MVC 5's serialiser wrote them");
            }

            // The site's other JSON endpoint, and the only one outside Autocomplete. It serialises
            // an ANONYMOUS type, whose members are properties, so it never had the empty-object
            // problem - but it did go through the same Json(data, JsonRequestBehavior) overload, so
            // it answered 500 like the rest until that was fixed.
            //
            // isDownloadable=0 because the default is 1 and the fixture's maps have no download
            // rows: the default answers [] correctly, which would make this check pass while
            // proving nothing. /Maps itself still lists maps, through three UNFILTERED sections of
            // its view - checked, so the difference is understood rather than suspected.
            var maps = await client.GetAsync(Url + "/Maps/JsonSearch?search=test&isDownloadable=0");
            var mapsJson = await maps.Content.ReadAsStringAsync();
            failures += Check(maps.IsSuccessStatusCode, "/Maps/JsonSearch answered (" + (int)maps.StatusCode + ")");
            failures += Check(mapsJson.Contains("\"internalName\""), "and it returned maps with their fields");

            // Clans/Detail and Factions/Detail need rows to exist at all: the fixture has no
            // clans and no factions, so one answered "not found" and the other threw from
            // Single(). Seeded here rather than committed to the fixture - see WithFactionAndClan.
            failures += await WithFactionAndClan(async (factionID, clanID) =>
            {
                var seeded = 0;
                foreach (var path in new[] { "/Factions/Detail/" + factionID, "/Clans/Detail/" + clanID })
                {
                    var response = await client.GetAsync(Url + path);
                    var html = await response.Content.ReadAsStringAsync();
                    seeded += Check(response.IsSuccessStatusCode && html.Contains("</html>"),
                        path + " is a whole page (" + (int)response.StatusCode + ", " + html.Length + " bytes)");
                }

                // The seeded rows reach the page, not just its chrome. Asserting only "</html>"
                // would pass on a page that rendered its layout and lost its model, which is the
                // shape of failure this port has hit twice - an empty grid and an empty post.
                var clan = await (await client.GetAsync(Url + "/Clans/Detail/" + clanID)).Content.ReadAsStringAsync();
                seeded += Check(clan.Contains("Harness Clan"), "and the clan's name is in it");
                var faction = await (await client.GetAsync(Url + "/Factions/Detail/" + factionID)).Content.ReadAsStringAsync();
                seeded += Check(faction.Contains("Harness Faction"), "and the faction's name is in it");

                // The hover tooltips, which the site fetches by AJAX and no check had ever asked
                // for. They are partial views returning a fragment, which is the shape that broke
                // in Autocomplete - so each asserts the content it should carry, not the status.
                // An empty fragment is a 200, and 200 is what a tooltip that lost its model gives.
                int accountID;
                using (var db = new ZkDataContext()) accountID = db.Accounts.OrderBy(a => a.AccountID).First().AccountID;

                foreach (var probe in new[]
                         {
                             new { Key = "user$" + accountID, Expect = "player", What = "Home/UserTooltip" },
                             new { Key = "clan$" + clanID, Expect = "Harness Clan", What = "Clans/Tooltip" },
                             new { Key = "faction$" + factionID, Expect = "Harness Faction", What = "Factions/FactionTooltip" },
                         })
                {
                    var response = await client.GetAsync(Url + "/Home/GetTooltip?key=" + Uri.EscapeDataString(probe.Key));
                    var fragment = await response.Content.ReadAsStringAsync();
                    seeded += Check(response.IsSuccessStatusCode, "tooltip " + probe.Key + " answered ("
                                                                  + (int)response.StatusCode + ", " + fragment.Length + " bytes)");
                    seeded += Check(fragment.Contains(probe.Expect),
                        "and " + probe.What + ".cshtml put something in it");
                }

                // UserDetail renders Users/UserRoleList.cshtml only for an account that HAS one -
                // @if (Model.Faction != null || Model.Clan != null) - and this is the one place
                // where it does, because WithFactionAndClan put both on it. The admin sweep asks
                // for the same page with neither, and that request walks straight past the partial.
                // Asserted through the clan link rather than any text of its own, because
                // UserRoleList renders NOTHING visible for an account with no roles seen by a
                // visitor who shares neither its clan nor its faction - which is this one. The link
                // is emitted by the same @if that gates the partial, so it is the evidence that the
                // gate opened; the first version of this looked for "Harness Clan" in the page and
                // failed, because the clan is drawn as an icon and never written out.
                var detail = await (await client.GetAsync(Url + "/Users/Detail/" + accountID)).Content.ReadAsStringAsync();
                seeded += Check(detail.Contains("/Clans/Detail/" + clanID),
                    "/Users/Detail took the branch that renders Users/UserRoleList.cshtml");

                // ...and the two display templates HANGING OFF that page, which the branch above
                // reaches and nothing had ever given it rows for. UserRoleList loops over the
                // account's roles and calls DisplayFor on each, and AdminUserDetail does the same
                // for its expired punishments - so both templates needed a row to exist, not a
                // request to be made.
                // The Enum EDITOR template, which is the last view reachable by seeding alone and
                // took four separate things to reach. Planetwars/Planet.cshtml:244 draws it as
                //
                //     @Html.EditorFor(x => priority, "Enum")
                //
                // inside two nested conditions: the structure's type must cost upkeep energy
                // (UpkeepEnergy > 0), and Global.Account.CanSetPriority(structure) must hold -
                // which needs the account to be in a faction, the PLANET to be owned by that same
                // faction, and a faction role carrying RightSetEnergyPriority (Account.cs:372).
                //
                // So: a structure type that costs energy, a structure of it on the planet, the
                // planet made this faction's, and the right. Miss any one and the page still
                // renders perfectly well down the else branch, which writes the priority as plain
                // text - which is why nothing had ever noticed the editor was unreached.
                //
                // ONLY with a lobby server, and finding out why is the result here. Planet.cshtml
                // line 44 reads
                //
                //     Global.IsAccountAuthorized && db.CurrentAccount().CanPlayerPlanetWars()
                //         && Global.LobbyApi.PlanetWarsPhase == PwPhase.AttackCollect && ...
                //
                // and dereferences Global.LobbyApi with no guard. C# short-circuits, so the third
                // term is reached only by a signed-in account that CAN play PlanetWars - which
                // nothing had ever been, because being one takes a faction and an owned planet.
                // Seeding that state for the first time turned the page into a 500.
                //
                // Both copies of the view are identical there - tools/diverged-views.txt records
                // one difference in this file and it is the child-action line - so this is not a
                // port defect in the view. What differs is that MVC 5 can never HAVE a null
                // LobbyApi: StartApplication always sets one, in-process or remote. The port's
                // GlobalCompat returns null when LobbyApiUrl is unset, which is a state the old
                // stack does not have. Same shape as /Battles/Logs: an optional dependency that
                // was never optional before.
                //
                // So it is recorded, not asserted, and the editor is rendered where a lobby
                // server exists - which is also the only place a real player would be.
                if (ZeroKWeb.Global.LobbyApi == null)
                {
                    Console.WriteLine("   note  /Planetwars/Planet answers 500 for a signed-in player who can"
                                      + " play PlanetWars and no lobby server is configured - the view"
                                      + " dereferences Global.LobbyApi unguarded, on both stacks, and only"
                                      + " this port can have a null one");
                }
                else
                {
                    seeded += await WithGalaxy(async (galaxyID, planetID, otherPlanetID) =>
                        await WithOwnedStructure(planetID, factionID, async structureName =>
                        await WithFactionRole(accountID, role => role.RightSetEnergyPriority = true, async () =>
                        await AsAccount(AdminLevel.None, async owner =>
                        {
                            var planet = await (await owner.GetAsync(Url + "/Planetwars/Planet/" + planetID))
                                               .Content.ReadAsStringAsync();

                            // On the FORM the editor sits in rather than on the word "priority",
                            // which the else branch writes too. This is the branch, not the topic.
                            return Check(planet.Contains("SetEnergyPriority"),
                                "  an owned structure's energy priority is an editor against a real"
                                + " lobby server, which renders Shared/EditorTemplates/Enum.cshtml");
                        }))));
                }

                // The treaty pair, which needs a SECOND faction and so could not come from the
                // one this scope seeds. /Factions/Detail renders DisplayTemplates/FactionTreaty
                // for a treaty in the Proposed state, and that template renders
                // DisplayTemplates/TreatyEffect for each of its effects - so the two stand or
                // fall together, like Diff and DiffPiece.
                seeded += await WithProposedTreaty(factionID, accountID, async (otherFactionID, effectName) =>
                {
                    var page = await (await client.GetAsync(Url + "/Factions/Detail/" + factionID))
                                     .Content.ReadAsStringAsync();
                    var treaty = Check(page.Contains("DRAFT PROPOSAL"),
                        "  a proposed treaty renders DisplayTemplates/FactionTreaty");
                    treaty += Check(page.Contains(effectName),
                        "  and its effect renders DisplayTemplates/TreatyEffect");

                    // The third view of the set, and the only one of the three with a RIGHT in
                    // front of it. FactionsController.NewTreaty refuses anyone whose account does
                    // not pass HasFactionRight(x => x.RightDiplomacy), which is
                    // "any AccountRole whose RoleType is not clan-only and satisfies the test"
                    // (Account.cs:502) - so viewing a treaty needs nothing and PROPOSING one needs
                    // a seeded faction role. That is why this view outlived the other two.
                    // Inside WithGalaxy as well, and that is the OTHER reason this view never
                    // rendered. FactionTreatyDefinition.cshtml:50 builds its planet dropdown from
                    //
                    //     new ZkDataContext().Galaxies.First(x => x.IsDefault).Planets
                    //
                    // and First throws "Sequence contains no elements" with no default galaxy -
                    // on both stacks. So the page needs a PlanetWars round to exist before it can
                    // be drawn at all, which is not obvious from the action and cost a 500 to
                    // find. WithGalaxy borrows a seeded round when one is there and seeds its own
                    // otherwise, so nesting it here is safe.
                    return treaty + await WithGalaxy(async (galaxyID, planetID, otherPlanetID) =>
                        await WithFactionRole(accountID, role => role.RightDiplomacy = true, async () =>
                        await AsAccount(AdminLevel.None, async diplomat =>
                        {
                            var form = await diplomat.GetAsync(
                                Url + "/Factions/NewTreaty?acceptingFactionID=" + otherFactionID);
                            var formHtml = await form.Content.ReadAsStringAsync();

                            // Asserted on the form's own fields rather than on a status, because
                            // NewTreaty answers 200 with Content("Not a diplomat!") when the right
                            // is missing - which is the exact failure this seeding exists to avoid
                            // and would otherwise pass a status check.
                            return Check(form.IsSuccessStatusCode
                                         && formHtml.Contains("Proposing diplomat")
                                         && formHtml.Contains("acceptingFactionID"),
                                "  and with the diplomacy right, Factions/FactionTreatyDefinition renders ("
                                + (int)form.StatusCode + ", " + formHtml.Length + " bytes)");
                        })));
                });

                seeded += await WithRoleAndPunishment(accountID, clanID, async roleName =>
                {
                    var withRole = await (await client.GetAsync(Url + "/Users/Detail/" + accountID))
                                         .Content.ReadAsStringAsync();
                    var roles = Check(withRole.Contains(roleName),
                        "  and with a role on it, UserRoleList renders DisplayTemplates/AccountRole");

                    return roles + await AsModerator(async moderator =>
                    {
                        var admin = await (await moderator.GetAsync(Url + "/Users/AdminUserDetail/" + accountID))
                                          .Content.ReadAsStringAsync();
                        return Check(admin.Contains("Harness expired punishment"),
                            "  and an expired ban renders DisplayTemplates/Punishment");
                    });
                });

                return seeded;
            });

            // Poll/PollView.cshtml, which no run had ever rendered. Two requests, because the
            // interesting one is the miss: PollController.Index returned a bare null when the poll
            // was not there, MVC 5 turned that into EmptyResult and answered 200 with nothing, and
            // ASP.NET Core throws "Cannot return null from an action method" - so the port answered
            // 500 where the site answered 200. Nothing requested the action, so nothing said so.
            failures += await WithPoll(async pollID =>
            {
                var seeded = 0;

                var hit = await client.GetAsync(Url + "/Poll?pollID=" + pollID);
                var html = await hit.Content.ReadAsStringAsync();
                seeded += Check(hit.IsSuccessStatusCode, "/Poll renders a poll that exists ("
                                                         + (int)hit.StatusCode + ", " + html.Length + " bytes)");
                seeded += Check(html.Contains("Harness Poll"), "and the question text is in it");

                var miss = await client.GetAsync(Url + "/Poll?pollID=2147483647");
                seeded += Check((int)miss.StatusCode == 200,
                    "and a poll that is not there is an empty 200, not a 500 ("
                    + (int)miss.StatusCode + ") - MVC 5 answers 200 here");

                return seeded;
            });

            // PlanetWars. Four actions open with Single() over a table the fixture leaves empty,
            // so none of them had ever reached a view - eleven never-rendered views behind two
            // missing rows.
            failures += await WithGalaxy(async (galaxyID, planetID, otherPlanetID) =>
            {
                var pw = 0;
                foreach (var path in new[] { "/Planetwars", "/Planetwars/Minimap", "/Planetwars/Ladder", "/Planetwars/Planet/" + planetID })
                {
                    var response = await client.GetAsync(Url + path);
                    var html = await response.Content.ReadAsStringAsync();
                    pw += Check(response.IsSuccessStatusCode && html.Contains("</html>"),
                        path + " is a whole page (" + (int)response.StatusCode + ", " + html.Length + " bytes)");
                }

                // The planet page carries the seeded planet, not just the layout around it.
                var planet = await (await client.GetAsync(Url + "/Planetwars/Planet/" + planetID)).Content.ReadAsStringAsync();
                pw += Check(planet.Contains("Harness Planet"), "and the planet's name is in it");

                // The galaxy map drew its link. Both planets being named proves the planet loop
                // ran; the gradient id proves the link loop did, and that is the part of the view
                // that replaced Raphael. Asserting the page alone would pass with Links empty.
                var galaxy = await (await client.GetAsync(Url + "/Planetwars")).Content.ReadAsStringAsync();
                pw += Check(galaxy.Contains("Harness Planet") && galaxy.Contains("Harness Neighbour"),
                    "the galaxy map names both planets");
                pw += Check(galaxy.Contains("id=\"lg" + planetID + "_" + otherPlanetID + "\""),
                    "and drew the link between them");

                // The page PlanetWars shows between seasons. It used to be its own view - the
                // Index action returned View("GalaxyOffline") - until 2017 moved it into the
                // switch at the top of Galaxy.cshtml and drove it from the galaxy row instead.
                // MiscVar.PlanetWarsMode defaults to AllOffline when unset, so every request the
                // harness has ever made was already in that mode; what had never run is the half
                // of it that needs a winner, which is the half that replaced the deleted view.
                pw += await WithFactionAndClan(async (factionID, clanID) =>
                {
                    // One word between the stars on purpose: the wiki parser's bold stops at a
                    // space, and a phrase would come back with the stars still in it.
                    SetGalaxyWinner(galaxyID, factionID, "The *Harness* won the season.");

                    // Set, not assumed. MiscVar.PlanetWarsMode reads AllOffline only while
                    // nothing has set it, and tools/stack.sh seeds a RUNNING round - so a check
                    // that took the ambient mode passed here and failed there, asserting the
                    // offline page against a galaxy the site was rendering as live.
                    var mode = MiscVar.PlanetWarsMode;
                    MiscVar.PlanetWarsMode = PlanetWarsModes.AllOffline;
                    try
                    {
                        var ended = await (await client.GetAsync(Url + "/Planetwars")).Content.ReadAsStringAsync();
                        return Check(ended.Contains("PlanetWars ended, Harness Faction won!"),
                                   "the finished season names its winner")
                             + Check(ended.Contains("The <strong>Harness</strong> won the season."),
                                   "and its end message went through the wiki parser");
                    }
                    finally
                    {
                        MiscVar.PlanetWarsMode = mode;
                        // Before the faction is removed under us: the galaxy points at it.
                        SetGalaxyWinner(galaxyID, null, null);
                    }
                });

                return pw;
            });

            // Pages that need somebody signed in. They redirect anonymously, so the survey above
            // could only see a 302 - and a 302 says nothing about whether the view behind it
            // renders. ZK_RENDERED_VIEWS made that gap countable; these close part of it.
            failures += await AsModerator(async moderator =>
            {
                var signedIn = 0;
                foreach (var path in new[]
                         {
                             "/Users", "/Charts",
                             // The next layer of pages no request had reached. Every one of these
                             // renders a view the ZK_RENDERED_VIEWS measurement lists as never
                             // executed, and each needs a signed-in moderator to get past [Auth].
                             "/Users/ReportLog", "/Users/MassBan", "/Users/ReportToAdmin/1",
                             "/Admin/TraceLogs", "/Charts/Ratings", "/Users/AdminUserDetail/1",
                         })
                {
                    var response = await moderator.GetAsync(Url + path);
                    var html = await response.Content.ReadAsStringAsync();
                    signedIn += Check(response.IsSuccessStatusCode && html.Contains("</html>"),
                        path + " renders for a signed-in user (" + (int)response.StatusCode + ", " + html.Length + " bytes)");
                }

                // /Wiki asked for no node has TWO correct answers, and which one you get is a
                // property of the rows. WikiController.Index looks up WikiKey == node with node
                // null, which on EF Core is WikiKey IS NULL - and that is every ordinary forum
                // thread, not none of them. On the committed fixture there are no threads yet, so
                // it finds nothing and redirects you to create the page; on a database that has
                // been browsed it renders whichever thread sorts first as a wiki page, which is
                // odd but is the controller's own doing and not the port's. This check asserted
                // the 302 and so asserted an empty database: five leftover threads turned it red
                // while nothing was wrong. What is actually worth holding is that neither answer
                // is a crash.
                var wiki = await moderator.GetAsync(Url + "/Wiki");
                var wikiHtml = await wiki.Content.ReadAsStringAsync();
                var offersToCreate = (int)wiki.StatusCode == 302
                                     && (wiki.Headers.Location?.ToString() ?? "").Contains("/Forum/NewPost");
                signedIn += Check(offersToCreate || (wiki.IsSuccessStatusCode && wikiHtml.Contains("</html>")),
                    "/Wiki with no node either offers to create it or renders a thread ("
                    + (int)wiki.StatusCode + (offersToCreate ? " -> " + wiki.Headers.Location : "") + ")");

                return signedIn;
            });

            // The culture every request runs under. Web.config pins en-US, and the port pinned
            // nothing - so this asserts the EFFECT, not the setting: a culture that failed to
            // apply would still be readable somewhere while formatting the wrong way.
            var culture = await (await client.GetAsync(Url + "/Harness/Culture")).Content.ReadAsStringAsync();
            failures += Check(culture.StartsWith("en-US|en|"),
                "requests run under en-US/en, as <globalization> asks (" + culture.Split('|')[0] + "/" + culture.Split('|')[1] + ")");
            failures += Check(culture.EndsWith("|1234.56|9/29/2026"),
                "and a number and a date come out the American way (" + string.Join(" ", culture.Split('|').Skip(2)) + ")");

            // An unhandled exception must reach Trace, because that is where the site's own log
            // comes from - Global.StartApplication puts a ZkServerTraceListener there and
            // Admin/TraceLogs reads it. ASP.NET Core would otherwise log to ILogger and the site
            // would never see its own errors.
            var captured = new System.Text.StringBuilder();
            var capture = new System.Diagnostics.TextWriterTraceListener(new System.IO.StringWriter(captured));
            System.Diagnostics.Trace.Listeners.Add(capture);
            try
            {
                var threw = await client.GetAsync(Url + "/Harness/Throw");
                System.Diagnostics.Trace.Flush();
                failures += Check((int)threw.StatusCode == 500, "a deliberate failure answers 500 (" + (int)threw.StatusCode + ")");
                failures += Check(captured.ToString().Contains("harness-deliberate-failure"),
                    "and it reached Trace, which is where Admin/TraceLogs reads the site's errors from");

                // ...and did NOT reach the client, which is the other half and the one with teeth.
                // On MVC 5 that is customErrors mode="RemoteOnly" in Web.config. This host has no
                // equivalent setting: it logs, rethrows, and lets Kestrel answer - so the only
                // thing keeping a stack trace off the wire is that nobody has added
                // UseDeveloperExceptionPage. That is one line away from being untrue, and unlike
                // the MVC 5 setting it would be untrue in PRODUCTION rather than only locally,
                // because there is no RemoteOnly to fall back on.
                var body = await threw.Content.ReadAsStringAsync();
                failures += Check(!body.Contains("harness-deliberate-failure")
                                  && !body.Contains("at ZeroKWeb.") && !body.Contains("Exception"),
                    "and NOT to the client - the response carries no exception text ("
                    + body.Length + " bytes)");

                // ...and all the way to the page a moderator actually looks at. The assertion
                // above proves the Trace CALL happens; it would pass just as well with no listener
                // installed, which is what this host did for the whole of its life - the call went
                // nowhere, LogEntries stayed empty, and Admin/TraceLogs rendered a working page
                // with nothing in it. The capture listener above is the harness's own, so it
                // cannot tell the difference. Only the database can.
                //
                // Polled rather than read once: ZkServerTraceListener writes asynchronously and
                // does not await, so the row lands shortly after the response does.
                var stored = false;
                for (var attempt = 0; attempt < 50 && !stored; attempt++)
                {
                    using (var db = new ZkDataContext())
                        stored = db.LogEntries.Any(x => x.Message.Contains("harness-deliberate-failure"));
                    if (!stored) await Task.Delay(100);
                }
                failures += Check(stored, "and into LogEntries, which is the site's own error log");

                // UNFILTERED, and that is not a detail. The first version asked for
                // /Admin/TraceLogs?Text=harness-deliberate-failure, and the page echoes the search
                // text back into its own filter box - so Contains() matched the form field and the
                // check passed with the listener removed and the table empty. A false pass, found
                // by the control that removes the listener, in the check written to prove the
                // listener works. Asking for the page with no filter leaves the rows as the only
                // place that string can come from.
                failures += await AsModerator(async moderator =>
                {
                    var page = await (await moderator.GetAsync(Url + "/Admin/TraceLogs"))
                                     .Content.ReadAsStringAsync();
                    return Check(page.Contains("harness-deliberate-failure"),
                        "and Admin/TraceLogs shows it, which is the whole path end to end");
                });
            }
            finally
            {
                System.Diagnostics.Trace.Listeners.Remove(capture);

                // The harness leaves the fixture as it found it. LogEntries is not in the set the
                // stack job counts, but a check that writes rows and leaves them is how a database
                // stops being a fixture.
                using (var db = new ZkDataContext())
                {
                    var mine = db.LogEntries.Where(x => x.Message.Contains("harness-deliberate-failure")).ToList();
                    if (mine.Count > 0) { db.LogEntries.RemoveRange(mine); db.SaveChanges(); }
                }
            }

            return failures;
        }

        private static async Task<int> CheckDivergedViews(HttpClient client)
        {
            Console.WriteLine();
            Console.WriteLine("the two newly diverged views:");
            var failures = 0;

            var user = await client.GetAsync(Url + "/Users/Detail/1");
            var userHtml = await user.Content.ReadAsStringAsync();
            failures += Check(user.IsSuccessStatusCode, "/Users/Detail/1 was served (" + (int)user.StatusCode + ")");
            failures += Check(userHtml.Contains("player01"), "the account reached the page");
            failures += Check(!userHtml.Contains("@"), "no unprocessed Razor markers survived");

            var battle = await client.GetAsync(Url + "/Battles/Detail/1");
            var battleHtml = await battle.Content.ReadAsStringAsync();
            failures += Check(battle.IsSuccessStatusCode, "/Battles/Detail/1 was served (" + (int)battle.StatusCode + ")");
            failures += Check(battleHtml.Contains("Battle 1 detail"), "the battle reached the page");
            // NOT asserted here: that the PlanetwarsEvents component produced output. Its call
            // sits inside `@if (Model.Events.Any())`, and battle 1 in the fixture has no events,
            // so the block never runs however the view was compiled - an assertion on it would
            // have passed for the wrong reason, or, as it first did, failed for one.
            // The component itself is exercised in ZeroKWeb.Render (CheckEventsComponent), and
            // that the PortedViews copy is the one compiled is what view-port-report --check
            // establishes, by failing on an orphaned or un-removed original.
            failures += Check(!battleHtml.Contains("@"), "no unprocessed Razor markers survived");

            return failures;
        }

        /// <summary>
        /// MultiSelectFor and EnumDropDownListFor on a real page, through the real IHtmlHelper.
        ///
        /// The markup in SelectHelpersCompat is copied verbatim from HtmlHelperExtensions.cs and
        /// can be diffed against it. What cannot be diffed is the substitution underneath -
        /// NameFor in place of ExpressionHelper.GetExpressionText, and a compiled expression in
        /// place of ModelMetadata.FromLambdaExpression(...).Model. The capture says those agree
        /// on the name and the value; this says they agree when MVC, not the capture harness,
        /// builds the helper.
        /// </summary>
        private static async Task<int> CheckSelectHelpers(HttpClient client)
        {
            Console.WriteLine();
            Console.WriteLine("the site's own select helpers:");
            var failures = 0;

            var battles = await client.GetAsync(Url + "/Battles");
            var html = await battles.Content.ReadAsStringAsync();
            failures += Check(battles.IsSuccessStatusCode, "/Battles was served (" + (int)battles.StatusCode + ")");
            // The id is the expression text, which is the half of MVC 5's API this replaces.
            failures += Check(html.Contains("data-autocomplete-action='add' id='UserId' name=''"),
                "MultiSelectFor named the field from the lambda, as ExpressionHelper did");
            failures += Check(html.Contains("<div id='UserIdplayers'></div>"),
                "and the div the site's javascript looks for is there");
            // Seven EnumDropDownListFor calls on this page, through the real helper.
            failures += Check(html.Contains("<select id=\"Bots\" name=\"Bots\">"),
                "EnumDropDownListFor rendered on a real page");
            failures += Check(!html.Contains("@"), "no unprocessed Razor markers survived");

            return failures;
        }

        /// <summary>
        /// Byte ranges, which is the entire port of MVC.ResumingActionResults - a .NET Framework
        /// package from 2014 that MissionsController uses once, to let a mission mutator
        /// download resume.
        ///
        /// ASP.NET Core implements ranges itself, but only when EnableRangeProcessing is set,
        /// and the default is false. A ResumingFileContentResult that simply derived from
        /// FileContentResult would compile, serve the file, and quietly stop resuming - a
        /// property nothing about a downloaded file looks wrong without. So it is asserted:
        /// a Range request has to come back 206 with the right bytes, and the no-range request
        /// has to still be a whole 200.
        /// </summary>
        private static async Task<int> CheckResumableDownload(HttpClient client)
        {
            Console.WriteLine();
            Console.WriteLine("resumable downloads:");
            var failures = 0;

            var whole = await client.GetAsync(Url + "/Harness/Resumable");
            var wholeBytes = await whole.Content.ReadAsByteArrayAsync();
            failures += Check((int)whole.StatusCode == 200 && wholeBytes.Length == 10,
                "a plain request is still a whole 200 (" + (int)whole.StatusCode + ", " + wholeBytes.Length + " bytes)");
            failures += Check(whole.Headers.AcceptRanges.Contains("bytes"),
                "it advertises Accept-Ranges: bytes");

            var request = new HttpRequestMessage(HttpMethod.Get, Url + "/Harness/Resumable");
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(2, 4);
            var partial = await client.SendAsync(request);
            var partialBytes = await partial.Content.ReadAsByteArrayAsync();

            failures += Check((int)partial.StatusCode == 206,
                "a Range request is answered 206 Partial Content (" + (int)partial.StatusCode + ")");
            failures += Check(partialBytes.Length == 3 && partialBytes[0] == 2 && partialBytes[2] == 4,
                "the bytes are the ones asked for, not the whole file ("
                + string.Join(",", partialBytes) + ")");
            failures += Check(partial.Content.Headers.ContentRange?.ToString() == "bytes 2-4/10",
                "Content-Range names the slice and the total ("
                + partial.Content.Headers.ContentRange + ")");

            return failures;
        }

        /// <summary>
        /// The Ladders pages, which are the first to exercise three things that until now only
        /// compiled.
        ///
        /// **Global.AwardCalculator**, which the port builds on FIRST USE rather than at
        /// application start - MVC 5 has an Application_Start to put it in and this does not.
        /// Nothing had ever touched it, so "it is there" was a claim about a property, not about
        /// a query. /Ladders runs the real monthly-awards query against the fixture.
        ///
        /// **EnumDropDownListFor**, whose markup is compared byte for byte against MVC 5 in
        /// ZeroKWeb.Render. That comparison drives Render() directly; this is the only thing
        /// that drives the public helper, and therefore the only thing that exercises NameFor,
        /// IdFor and the model-value lookup behind the selection.
        ///
        /// **A SelectedValue that is not the first option.** RatingCategory starts at 1 and
        /// LaddersFull defaults to Casual = 1, so an implementation that always marked the first
        /// option selected would agree here by accident; the assertion names the value instead.
        /// </summary>
        private static async Task<int> CheckLadders(HttpClient client)
        {
            Console.WriteLine();
            Console.WriteLine("ladders:");
            var failures = 0;

            var index = await client.GetAsync(Url + "/Ladders");
            var indexHtml = await index.Content.ReadAsStringAsync();
            failures += Check(index.IsSuccessStatusCode,
                "/Ladders was served (" + (int)index.StatusCode + ") - AwardCalculator ran its query");

            // NOT just a 200. The ladder is built from GetTopPlayers, and a process that reads
            // ratings without computing them used to get an empty list from it - a heading over no
            // rows, which is a 200 and a clean check and a broken page. This process deliberately
            // calls CreateRatingSystems rather than Init, so this assertion is the one that says
            // the database fallback works.
            var ladderRows = System.Text.RegularExpressions.Regex.Matches(indexHtml, "/Users/Detail/").Count;
            failures += Check(ladderRows >= 10,
                "the ladder has players in it (" + ladderRows + " links) - not an empty list behind a 200");
            failures += Check(!indexHtml.Contains("@"), "no unprocessed Razor markers survived");

            var full = await client.GetAsync(Url + "/Ladders/Full");
            var fullHtml = await full.Content.ReadAsStringAsync();
            failures += Check(full.IsSuccessStatusCode, "/Ladders/Full was served (" + (int)full.StatusCode + ")");
            failures += Check(fullHtml.Contains("<select class=\"width-100\" id=\"RatingCategory\" name=\"RatingCategory\">"),
                "EnumDropDownListFor emitted the select, with htmlAttributes, through the real helper");
            failures += Check(fullHtml.Contains("<option selected=\"selected\" value=\"1\">Casual</option>"),
                "the model's value is the selected option, by value and not by position");

            // MapSupportLevel? - the nullable shape, whose extra empty option only the capture
            // could have told us about.
            var maps = await client.GetAsync(Url + "/Ladders/Maps");
            var mapsHtml = await maps.Content.ReadAsStringAsync();
            failures += Check(maps.IsSuccessStatusCode, "/Ladders/Maps was served (" + (int)maps.StatusCode + ")");
            failures += Check(mapsHtml.Contains("<option selected=\"selected\" value=\"\"></option>"),
                "a nullable enum got its empty option, selected, as MVC 5 emits it");

            // MapRatings.GetMapRanking is computed by the WHR pass and has no database fallback, so
            // this page is expected to be EMPTY in a process that only reads ratings. Asserted, not
            // assumed: if it ever starts having rows the recorded remaining work is wrong.
            var mapRows = System.Text.RegularExpressions.Regex.Matches(mapsHtml, "/Maps/Detail/").Count;
            Console.WriteLine("   note  /Ladders/Maps has " + mapRows + " map rows"
                              + " - MapRatings has no database fallback, see EFCORE-MIGRATION.md");

            return failures;
        }

        /// <summary>
        /// A PAGE, not a view: a ViewResult runs _ViewStart, which picks up
        /// Shared/_SiteLayout.cshtml, which pulls in TopMenu, LoginBar, the bundles and every
        /// helper behind them. Home/NotLoggedIn.cshtml is the site's own view, unmodified.
        /// </summary>
        private static async Task<int> CheckItServesAPage(HttpClient client)
        {
            var response = await client.GetAsync(Url + "/Home/NotLoggedIn");
            var html = await response.Content.ReadAsStringAsync();

            Console.WriteLine();
            Console.WriteLine("GET /Home/NotLoggedIn -> " + (int)response.StatusCode
                              + ", " + html.Length + " bytes");

            var failures = 0;
            failures += Check(response.IsSuccessStatusCode, "the request succeeded");
            failures += Check(html.Contains("You are not logged in"), "the view's own content is there");
            // Page.Title is the MVC 5 ambient the shim maps onto ViewBag; the layout reads it
            // back into <title>, so this is the shim working across a view/layout boundary.
            failures += Check(html.Contains("Not logged in"), "Page.Title reached the layout's <title>");
            failures += Check(html.Contains("<html") && html.Contains("</html>"), "it is a whole document");
            // The bundling shim, standing in for System.Web.Optimization.
            failures += Check(html.Contains("/Scripts/site_main.js"), "the script bundle rendered its files");
            failures += Check(html.Contains("/Styles/style.css"), "the style bundle rendered its files");
            // TopMenu and LoginBar are partials the layout pulls in; both were blocked until now.
            failures += Check(html.Contains("PlanetWars"), "TopMenu rendered inside the layout");
            failures += Check(!html.Contains("@"), "no unprocessed Razor markers survived");
            return failures;
        }

        private static int Check(bool ok, string what)
        {
            Console.WriteLine((ok ? "   ok    " : "   FAIL  ") + what);
            return ok ? 0 : 1;
        }
    }
}
