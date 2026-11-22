using System;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace LobbyClientProbe
{
    /// <summary>
    ///     Registers an account and logs in, against a lobby server that is already running.
    ///
    ///     usage: LobbyClientProbe &lt;host&gt; &lt;port&gt; &lt;name&gt; &lt;password&gt;
    ///
    ///     The point is that opening a port is not the same as serving anybody. This completes the
    ///     handshake: the TCP connect, the line-framed JSON both ways, a Register that writes an
    ///     Account through EF Core, and a Login the server accepts. Exits 0 only if the server said
    ///     Ok to both.
    /// </summary>
    public static class Program
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

        // Deliberately NOT one of the channels ChannelManager hands out by default ("zk", zkadmin,
        // top20, core). Those are joined by the server at login, so asking for one of them proves
        // nothing about the explicit JoinChannel below - the response would already have arrived.
        // An unknown name is allowed by CanJoin, which still has to read the account to decide.
        private const string Channel = "probechannel";

        public static async Task<int> Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.Error.WriteLine(
                    "usage: LobbyClientProbe <host> <port> <name> <password> [map expected-text]");
                return 2;
            }

            var host = args[0];
            var port = int.Parse(args[1]);
            var name = args[2];
            var password = args[3];
            // Optional, and only the two-container stack passes it. Without it this probe behaves
            // exactly as it did.
            var siteUrl = Environment.GetEnvironmentVariable("ZK_SITE_URL");
            if (!string.IsNullOrWhiteSpace(siteUrl)) siteUrl = siteUrl.TrimEnd('/'); else siteUrl = null;

            var client = new TasClient("LobbyClientProbe 1.0");

            var connected = new TaskCompletionSource<bool>();
            var joinedChannel = new TaskCompletionSource<string>();
            var heard = new TaskCompletionSource<bool>();
            var battleOpened = new TaskCompletionSource<bool>();
            var registered = new TaskCompletionSource<string>();
            var loggedIn = new TaskCompletionSource<string>();

            client.Connected += (s, e) => connected.TrySetResult(true);
            client.ConnectionLost += (s, e) =>
            {
                connected.TrySetException(new Exception("connection lost: " + e.ServerParams?[0]));
                registered.TrySetException(new Exception("connection lost before the server answered Register"));
                loggedIn.TrySetException(new Exception("connection lost before the server answered Login"));
                joinedChannel.TrySetException(new Exception("connection lost before the server answered JoinChannel"));
                heard.TrySetException(new Exception("connection lost before the server relayed the message"));
                battleOpened.TrySetException(new Exception("connection lost before the server opened the battle"));
            };
            client.RegistrationAccepted += (s, e) => registered.TrySetResult(null);
            client.RegistrationDenied += (s, e) => registered.TrySetResult(e.ResultCode.ToString());
            client.LoginAccepted += (s, e) => loggedIn.TrySetResult(null);
            client.LoginDenied += (s, e) => loggedIn.TrySetResult(e.ResultCode.ToString());
            // Both handlers check WHICH channel. The server auto-joins an account's default
            // channels at login, so an unfiltered handler is satisfied by one of those before the
            // explicit JoinChannel below is even sent - which made a deliberately refused join to
            // the moderator channel report success.
            client.BattleOpened += (s, e) => { if (e.FounderName == name) battleOpened.TrySetResult(true); };
            client.ChannelJoined += (s, e) => { if (e.Name == Channel) joinedChannel.TrySetResult(null); };
            client.ChannelJoinFailed += (s, e) => { if (e.ChannelName == Channel) joinedChannel.TrySetResult(e.Reason ?? "refused without a reason"); };

            Console.WriteLine($"connecting to {host}:{port}");
            client.Connect(host, port);
            if (!await Within(connected.Task, "connect")) return 1;
            Console.WriteLine("   ok    connected, and the server greeted us");

            await client.Register(name, password);
            if (!await Within(registered.Task, "register")) return 1;
            // "It already exists" is a pass: the account is there, which is all login needs. Both
            // spellings happen when this runs twice against a database that was not reloaded in
            // between - the server says AlreadyRegisteredWithThisPassword when the password also
            // matches, and NameAlreadyTaken when it does not.
            var reg = registered.Task.Result;
            if (reg != null && reg != "NameAlreadyTaken" && reg != "AlreadyRegisteredWithThisPassword")
            {
                Console.WriteLine($"   FAIL  the server refused to register '{name}': {reg}");
                return 1;
            }
            Console.WriteLine(reg == null
                ? $"   ok    registered '{name}' - an Account was written through EF Core"
                : $"   ok    '{name}' was already registered ({reg}), which login is happy with");

            await client.Login(name, password);
            if (!await Within(loggedIn.Task, "login")) return 1;
            var login = loggedIn.Task.Result;
            if (login != null)
            {
                Console.WriteLine($"   FAIL  the server refused the login: {login}");
                return 1;
            }
            Console.WriteLine("   ok    logged in - the server accepted a real client");

            // Single sign-on, which only this process can check: the token the server just issued
            // lives in memory and ConnectedUser.Process(Disconnect) removes every token for the
            // account, so it dies the moment this client goes away. Nothing outside a connected
            // session can hold one.
            if (siteUrl != null)
            {
                // A SECOND token, from a second account on a second connection, because a token is
                // single use and the cookie form has to be asked the same question as the URL
                // form. One client cannot supply two: TasClient.SessionToken is set once, at
                // login, and logging the same account in again would take the first session's
                // tokens with it (ConnectedUser removes them all on disconnect).
                var second = await ConnectAndLogin(host, port, name + "Cookie", password);
                if (second == null) return 1;

                try
                {
                    var sso = await CheckSingleSignOn(siteUrl, client.SessionToken, name,
                                                      second.SessionToken, name + "Cookie");
                    if (sso != 0) return sso;
                }
                finally
                {
                    try { second.RequestDisconnect(); } catch { }
                }
            }

            // Past login. ChannelManager.CanJoin does db.Accounts.FindAsync, so joining is another
            // EF Core round trip on a path a build cannot reach; saying something comes back
            // through the server, which is the first thing here that proves two-way traffic rather
            // than request and reply.
            await client.JoinChannel(Channel);
            if (!await Within(joinedChannel.Task, "join channel")) return 1;
            var join = joinedChannel.Task.Result;
            if (join != null)
            {
                Console.WriteLine($"   FAIL  the server refused to join #{Channel}: {join}");
                return 1;
            }
            Console.WriteLine($"   ok    joined #{Channel} - not a default channel, so this is the explicit join, and CanJoin read the account through EF Core");

            var message = "probe " + Guid.NewGuid().ToString("N").Substring(0, 8);
            client.Said += (s, e) => { if (e.Text == message) heard.TrySetResult(true); };
            await client.Say(SayPlace.Channel, Channel, message, false);
            if (!await Within(heard.Task, "say")) return 1;
            Console.WriteLine($"   ok    the server relayed what the client said back to it");

            // The last protocol step before a game: the server constructs a ServerBattle and
            // announces it to everyone, then joins us to it. Nothing starts Spring here.
            await client.OpenBattle(new BattleHeader
            {
                Title = "probe battle",
                Map = args.Length >= 6 ? args[4] : "test_map_1",
                Game = "test_mod_1",
                Engine = "105.1.1-2511-g2c4d0a1",
                MaxPlayers = 2,
                Mode = AutohostMode.None,
                Password = null,
            });
            if (!await Within(battleOpened.Task, "open battle")) return 1;
            Console.WriteLine($"   ok    the server opened a battle and announced it");

            // Opening the battle is what makes the server look up the map's metadata, and
            // !listmapoptions is the only place that lookup is visible from outside: it answers
            // "this map has no map options" when HostedMapInfo is null, which is what a player
            // sees when the server cannot reach the metadata at all.
            //
            // Only when asked for. Without the argument this probe behaves exactly as before, so
            // tools/lobby-core-start.sh - which runs against a server that does not even compile
            // the battle commands - is untouched.
            if (args.Length >= 6)
            {
                var expected = args[5];
                var answered = new TaskCompletionSource<string>();
                // BattlePrivate, not Battle. ServerBattle.Respond passes the asking user to
                // SayBattle, and that addresses the reply to them - a battle command answers
                // the person who ran it, not the room. Filtering on Battle alone waits forever.
                client.Said += (s, e) =>
                {
                    if ((e.Place == SayPlace.Battle || e.Place == SayPlace.BattlePrivate)
                        && e.Text != null && e.Text.Contains(expected))
                        answered.TrySetResult(e.Text);
                };

                // Asked repeatedly, because a battle say is dropped outright unless the sender is
                // already in the battle - and the founder is joined by the server AFTER it has
                // broadcast BattleAdded, which is the event this probe waited on. One ask races
                // that join and loses silently; the command is a read, so asking again is free.
                string reply = null;
                for (var attempt = 0; attempt < 15 && reply == null; attempt++)
                {
                    await client.Say(SayPlace.Battle, "", "!listmapoptions", false);
                    if (await Task.WhenAny(answered.Task, Task.Delay(TimeSpan.FromSeconds(2))) == answered.Task)
                        reply = answered.Task.Result;
                }

                if (reply == null)
                {
                    Console.WriteLine($"   FAIL  !listmapoptions for {args[4]} never mentioned '{expected}'");
                    return 1;
                }

                Console.WriteLine($"   ok    !listmapoptions for {args[4]} answered '{reply.Trim()}'");
            }

            // A login that must FAIL, and fail cleanly - on its own connection, because a refused
            // login leaves the server-side user unauthenticated and everything after it on that
            // connection then silently does nothing. Found by doing it inline first: the battle
            // stopped opening.
            //
            // LoginChecker looks the name up with an exact-match query and a case-insensitive
            // fallback. The fallback used string.Equals with a StringComparison, which EF Core
            // cannot translate, so on .NET 9 it threw for every name that does not exist - the
            // ordinary "wrong username" case - and the server answered nothing at all.
            return await AnUnknownNameIsRefused(host, port);
        }

        private static async Task<int> AnUnknownNameIsRefused(string host, int port)
        {
            var client = new TasClient("LobbyClientProbe 1.0");
            var connected = new TaskCompletionSource<bool>();
            var answered = new TaskCompletionSource<string>();

            client.Connected += (s, e) => connected.TrySetResult(true);
            client.ConnectionLost += (s, e) =>
            {
                connected.TrySetException(new Exception("connection lost"));
                answered.TrySetException(new Exception("the server dropped the connection instead of refusing the login"));
            };
            client.LoginAccepted += (s, e) => answered.TrySetResult(null);
            client.LoginDenied += (s, e) => answered.TrySetResult(e.ResultCode.ToString());

            client.Connect(host, port);
            if (!await Within(connected.Task, "connect for the unknown-name login")) return 1;

            await client.Login("nosuchaccount" + Guid.NewGuid().ToString("N").Substring(0, 8), "whatever");
            if (!await Within(answered.Task, "login with an unknown name")) return 1;

            var denial = answered.Task.Result;
            if (denial == null)
            {
                Console.WriteLine("   FAIL  the server ACCEPTED a login for an account that does not exist");
                return 1;
            }
            Console.WriteLine($"   ok    an unknown name is refused, not crashed on ({denial})");
            return 0;
        }

        /// <summary>
        ///     Waits, and turns "it never answered" into a message that says which step hung rather
        ///     than a stack trace or a silent hang in CI.
        /// </summary>
        /// <summary>
        /// A player arriving from the game client, which is the one sign-in path neither half of
        /// the stack can check alone.
        ///
        /// ZeroKLobby opens the website with ?asmallcake=&lt;token&gt; on the URL (BrowserInterop.cs),
        /// the site redeems it against the lobby server, and from then on the player is signed in
        /// by cookie. Three things have to happen and only the first is obvious:
        ///
        ///   1. the token is redeemed and the page is served as that account;
        ///   2. a session cookie is ISSUED, because the token is single use - SessionTokenStore
        ///      .Redeem removes it whether or not it was valid - so the redeeming request is the
        ///      only chance to turn it into a session. Without this the player is signed in for
        ///      exactly one request and anonymous on their next click;
        ///   3. the spent token is taken back out of the URL, which Global.asax does with a
        ///      redirect, so it does not sit in history and in the Referer of every outbound link.
        ///
        /// The control is at the end: a token that was never issued must sign nobody in. Without
        /// it, a site that signed in any caller at all would pass every line above.
        ///
        /// Every line is reported and none of them returns early, which matters for the controls
        /// rather than for a passing run. The first version returned on the first failure, and
        /// the control that reverted the fix therefore only ever showed the redirect - the
        /// missing session cookie, which is the worse of the two, was never reached and so was
        /// never actually proven to be caught.
        /// </summary>
        private static async Task<int> CheckSingleSignOn(string siteUrl, string token, string name,
                                                         string cookieToken, string cookieName)
        {
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("   FAIL  the server accepted the login but issued no session token");
                return 1;
            }

            var failures = 0;
            var cookies = new System.Net.CookieContainer();
            using (var handler = new System.Net.Http.HttpClientHandler
                   { UseCookies = true, CookieContainer = cookies, AllowAutoRedirect = false })
            using (var web = new System.Net.Http.HttpClient(handler))
            {
                var landing = siteUrl + "/Home/NotLoggedIn?" + GlobalConst.SessionTokenVariable
                              + "=" + Uri.EscapeDataString(token) + "&keep=me";
                var first = await web.GetAsync(landing);

                var location = first.Headers.Location?.ToString() ?? "";
                var redirected = (int)first.StatusCode == 302
                                 && !location.Contains(GlobalConst.SessionTokenVariable);
                // The rest of the query string has to survive, or the redirect loses whatever the
                // player was actually asking for.
                failures += Report(redirected && location.Contains("keep=me"),
                    $"the site redirects the spent token out of the URL, keeping the rest "
                    + $"({(int)first.StatusCode} -> {(location == "" ? "no Location" : location)})");

                var signedInCookie = false;
                foreach (System.Net.Cookie cookie in cookies.GetCookies(new Uri(siteUrl)))
                    if (cookie.Name == "ZkAuth") signedInCookie = true;
                failures += Report(signedInCookie,
                    "and issues a session cookie, so the single-use token becomes a session"
                    + (signedInCookie ? "" : " - without it the player is signed in for one"
                                             + " request and anonymous on their next click"));

                // The cookie alone, on a different URL, with no token anywhere.
                var whoami = await (await web.GetAsync(siteUrl + "/Harness/Whoami"))
                                   .Content.ReadAsStringAsync();
                failures += Report(whoami.Contains("signed in as " + name),
                    $"and the NEXT request is still {name}, by cookie alone ({whoami.Trim()})");
            }

            // The COOKIE form, which is the other half of how a player arrives. ZeroKLobby sets
            // the token as a cookie with InternetSetCookiePub (BrowserInterop.cs:37) as well as
            // putting it on the URL, and MVC 5 reads it because Request[key] looks in the query
            // string, then the form, then the COOKIES. This read only the first two.
            var byCookie = new System.Net.CookieContainer();
            byCookie.Add(new Uri(siteUrl), new System.Net.Cookie(GlobalConst.SessionTokenVariable,
                                                                 cookieToken) { Path = "/" });
            using (var handler = new System.Net.Http.HttpClientHandler
                   { UseCookies = true, CookieContainer = byCookie, AllowAutoRedirect = false })
            using (var web = new System.Net.Http.HttpClient(handler))
            {
                await web.GetAsync(siteUrl + "/Home/NotLoggedIn");
                var whoami = await (await web.GetAsync(siteUrl + "/Harness/Whoami"))
                                   .Content.ReadAsStringAsync();
                failures += Report(whoami.Contains("signed in as " + cookieName),
                    $"a token carried as a COOKIE signs {cookieName} in too ({whoami.Trim()})");
            }

            // THE CONTROL. A token the server never issued must sign nobody in; otherwise every
            // line above would pass on a site that signed in anyone who asked.
            var madeUp = new System.Net.CookieContainer();
            using (var handler = new System.Net.Http.HttpClientHandler
                   { UseCookies = true, CookieContainer = madeUp, AllowAutoRedirect = false })
            using (var web = new System.Net.Http.HttpClient(handler))
            {
                await web.GetAsync(siteUrl + "/Home/NotLoggedIn?" + GlobalConst.SessionTokenVariable
                                   + "=" + Guid.NewGuid().ToString("N"));
                var signedIn = false;
                foreach (System.Net.Cookie cookie in madeUp.GetCookies(new Uri(siteUrl)))
                    if (cookie.Name == "ZkAuth") signedIn = true;
                failures += Report(!signedIn, "and a token the server never issued signs nobody in");
            }

            return failures == 0 ? 0 : 1;
        }

        private static int Report(bool ok, string what)
        {
            Console.WriteLine((ok ? "   ok    " : "   FAIL  ") + what);
            return ok ? 0 : 1;
        }

        /// <summary>
        /// A second connected client, registered and logged in, held open by the caller. Null if
        /// any step failed, having said which.
        /// </summary>
        private static async Task<TasClient> ConnectAndLogin(string host, int port, string name, string password)
        {
            var client = new TasClient("LobbyClientProbe 1.0");
            var connected = new TaskCompletionSource<bool>();
            var registered = new TaskCompletionSource<string>();
            var loggedIn = new TaskCompletionSource<string>();

            client.Connected += (s, e) => connected.TrySetResult(true);
            client.ConnectionLost += (s, e) =>
            {
                connected.TrySetException(new Exception("connection lost: " + e.ServerParams?[0]));
                registered.TrySetException(new Exception("connection lost before Register was answered"));
                loggedIn.TrySetException(new Exception("connection lost before Login was answered"));
            };
            client.RegistrationAccepted += (s, e) => registered.TrySetResult(null);
            client.RegistrationDenied += (s, e) => registered.TrySetResult(e.ResultCode.ToString());
            client.LoginAccepted += (s, e) => loggedIn.TrySetResult(null);
            client.LoginDenied += (s, e) => loggedIn.TrySetResult(e.ResultCode.ToString());

            client.Connect(host, port);
            if (!await Within(connected.Task, $"connect as {name}")) return null;

            await client.Register(name, password);
            if (!await Within(registered.Task, $"register {name}")) return null;
            var reg = registered.Task.Result;
            if (reg != null && reg != "NameAlreadyTaken" && reg != "AlreadyRegisteredWithThisPassword")
            {
                Console.WriteLine($"   FAIL  the server refused to register '{name}': {reg}");
                return null;
            }

            await client.Login(name, password);
            if (!await Within(loggedIn.Task, $"login as {name}")) return null;
            if (loggedIn.Task.Result != null)
            {
                Console.WriteLine($"   FAIL  the server refused the login for '{name}': {loggedIn.Task.Result}");
                return null;
            }

            return client;
        }

        private static async Task<bool> Within(Task task, string step)
        {
            var finished = await Task.WhenAny(task, Task.Delay(Timeout));
            if (finished != task)
            {
                Console.WriteLine($"   FAIL  the server never answered {step} within {Timeout.TotalSeconds:0}s");
                return false;
            }

            try
            {
                await task;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   FAIL  {step}: {ex.Message}");
                return false;
            }
        }
    }
}
