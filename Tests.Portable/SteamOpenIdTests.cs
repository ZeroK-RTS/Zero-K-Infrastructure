using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeroKWeb;

namespace Tests.Portable
{
    /// <summary>
    /// Steam's OpenID 2.0 exchange, which the .NET 9 port implements itself because
    /// DotNetOpenAuth - what MVC 5 uses - has no .NET Core version.
    ///
    /// **Why these tests are mostly about failing.** An OpenID callback is a query string a
    /// browser was told to send; every byte of it is attacker-supplied until Steam confirms the
    /// signature. An implementation that simply reads openid.claimed_id works perfectly for every
    /// honest user and lets anyone sign in as anyone, and no amount of trying it by hand would
    /// show that. So most of what follows hands the protocol a well-formed assertion and checks
    /// it is REFUSED.
    ///
    /// **What these tests cannot establish.** Nothing here talks to Steam. The provider is a stub,
    /// so this proves the logic and says nothing about whether Steam's live endpoint behaves as
    /// documented. That gap is real and is stated in SteamOpenIdProtocol's own summary.
    /// </summary>
    [TestClass]
    public class SteamOpenIdTests
    {
        private const string Origin = "https://zero-k.info";
        private const string ReturnTo = Origin + "/Home/Logon?referer=%2FForum";
        private const string ClaimedId = "https://steamcommunity.com/openid/id/76561198000000001";

        /// <summary>A well-formed assertion, as Steam would send it. Tests spoil one field each.</summary>
        private static Dictionary<string, string> Assertion()
            => new Dictionary<string, string>
            {
                ["openid.ns"] = SteamOpenIdProtocol.Namespace,
                ["openid.mode"] = "id_res",
                ["openid.op_endpoint"] = SteamOpenIdProtocol.Endpoint,
                ["openid.claimed_id"] = ClaimedId,
                ["openid.identity"] = ClaimedId,
                ["openid.return_to"] = ReturnTo,
                ["openid.response_nonce"] = "2026-09-24T00:00:00Zabcdef",
                ["openid.assoc_handle"] = "handle",
                ["openid.signed"] = "signed,op_endpoint,claimed_id,identity,return_to,response_nonce,assoc_handle",
                ["openid.sig"] = "c2lnbmF0dXJl",
            };

        [TestMethod]
        public void The_redirect_asks_Steam_for_identifier_select()
        {
            var url = SteamOpenIdProtocol.BuildRedirectUrl(ReturnTo, Origin + "/");

            StringAssert.StartsWith(url, SteamOpenIdProtocol.Endpoint + "?");
            StringAssert.Contains(url, "openid.mode=checkid_setup");
            StringAssert.Contains(url, Uri.EscapeDataString(SteamOpenIdProtocol.Namespace));
            // Steam tells us who it is; we do not claim to know beforehand.
            StringAssert.Contains(url, Uri.EscapeDataString("http://specs.openid.net/auth/2.0/identifier_select"));
            StringAssert.Contains(url, "openid.return_to=" + Uri.EscapeDataString(ReturnTo));
            StringAssert.Contains(url, "openid.realm=" + Uri.EscapeDataString(Origin + "/"));
        }

        [TestMethod]
        public async Task An_ordinary_post_is_not_an_openid_callback()
        {
            var stub = new StubProvider("is_valid:true");
            var password = new Dictionary<string, string> { ["login"] = "player01", ["password"] = "x" };

            Assert.IsNull(await SteamOpenIdProtocol.CompleteAsync(password, Origin, stub),
                "null is how the controller tells a password post from a return from Steam");
            Assert.AreEqual(0, stub.Calls, "and it must not have gone asking Steam about it");
        }

        [TestMethod]
        public async Task A_verified_assertion_signs_the_right_account_in()
        {
            var stub = new StubProvider("ns:" + SteamOpenIdProtocol.Namespace + "\nis_valid:true\n");

            var result = await SteamOpenIdProtocol.CompleteAsync(Assertion(), Origin, stub);

            Assert.AreEqual(SteamOpenIdStatus.Authenticated, result.Status);
            Assert.AreEqual(76561198000000001UL, result.SteamID);
            Assert.AreEqual("/Forum", result.Referer, "the referer rides in the SIGNED return_to");
        }

        [TestMethod]
        public async Task The_assertion_is_actually_checked_with_Steam()
        {
            var stub = new StubProvider("is_valid:true");

            await SteamOpenIdProtocol.CompleteAsync(Assertion(), Origin, stub);

            Assert.AreEqual(1, stub.Calls, "exactly one check_authentication");
            Assert.AreEqual(SteamOpenIdProtocol.Endpoint, stub.LastUrl);
            Assert.AreEqual("check_authentication", stub.LastForm["openid.mode"]);
            // The signature and the fields it covers have to go back untouched, or Steam is being
            // asked about a different assertion than the one that arrived.
            Assert.AreEqual("c2lnbmF0dXJl", stub.LastForm["openid.sig"]);
            Assert.AreEqual(Assertion()["openid.signed"], stub.LastForm["openid.signed"]);
            Assert.AreEqual(ClaimedId, stub.LastForm["openid.claimed_id"]);
        }

        [TestMethod]
        public async Task A_forged_assertion_is_refused_when_Steam_says_so()
        {
            var stub = new StubProvider("ns:" + SteamOpenIdProtocol.Namespace + "\nis_valid:false\n");

            var result = await SteamOpenIdProtocol.CompleteAsync(Assertion(), Origin, stub);

            Assert.AreEqual(SteamOpenIdStatus.Failed, result.Status,
                "this is the whole point: the claimed_id looked perfect");
        }

        [TestMethod]
        public async Task is_valid_true_has_to_be_a_line_of_its_own()
        {
            // A provider response that merely CONTAINS the text. A substring check would pass.
            var stub = new StubProvider("error:x-is_valid:true is not a verdict\nis_valid:false\n");

            var result = await SteamOpenIdProtocol.CompleteAsync(Assertion(), Origin, stub);

            Assert.AreEqual(SteamOpenIdStatus.Failed, result.Status);
        }

        [TestMethod]
        public async Task A_provider_that_errors_is_not_a_yes()
        {
            var stub = new StubProvider("is_valid:true", HttpStatusCode.InternalServerError);

            var result = await SteamOpenIdProtocol.CompleteAsync(Assertion(), Origin, stub);

            Assert.AreEqual(SteamOpenIdStatus.Failed, result.Status,
                "a 500 must not fall through to trusting the query string");
        }

        [TestMethod]
        public async Task A_claimed_id_from_somewhere_else_is_refused_even_if_verification_passes()
        {
            var stub = new StubProvider("is_valid:true");
            var assertion = Assertion();
            assertion["openid.claimed_id"] = "https://evil.example/openid/id/76561198000000001";

            var result = await SteamOpenIdProtocol.CompleteAsync(assertion, Origin, stub);

            Assert.AreEqual(SteamOpenIdStatus.Failed, result.Status);
        }

        [TestMethod]
        public async Task A_claimed_id_that_is_not_a_steam_id_is_refused()
        {
            var stub = new StubProvider("is_valid:true");
            foreach (var bad in new[]
                     {
                         "https://steamcommunity.com/openid/id/123",                  // too short
                         "https://steamcommunity.com/openid/id/765611980000000012",   // too long
                         "https://steamcommunity.com/openid/id/7656119800000000a",    // not digits
                         "https://steamcommunity.com/openid/id/",                     // nothing
                         "http://steamcommunity.com/openid/id/76561198000000001",     // not https
                     })
            {
                var assertion = Assertion();
                assertion["openid.claimed_id"] = bad;
                var result = await SteamOpenIdProtocol.CompleteAsync(assertion, Origin, stub);
                Assert.AreEqual(SteamOpenIdStatus.Failed, result.Status, bad);
            }
        }

        [TestMethod]
        public async Task A_return_to_pointing_elsewhere_is_refused_without_asking_Steam()
        {
            var stub = new StubProvider("is_valid:true");
            var assertion = Assertion();
            assertion["openid.return_to"] = "https://evil.example/Home/Logon";

            var result = await SteamOpenIdProtocol.CompleteAsync(assertion, Origin, stub);

            Assert.AreEqual(SteamOpenIdStatus.Failed, result.Status);
            Assert.AreEqual(0, stub.Calls, "refused before spending a request on it");
        }

        [TestMethod]
        public async Task An_openid_1_assertion_is_refused()
        {
            var stub = new StubProvider("is_valid:true");
            var assertion = Assertion();
            assertion["openid.ns"] = "http://openid.net/signon/1.1";

            var result = await SteamOpenIdProtocol.CompleteAsync(assertion, Origin, stub);

            Assert.AreEqual(SteamOpenIdStatus.Failed, result.Status);
            Assert.AreEqual(0, stub.Calls);
        }

        [TestMethod]
        public async Task Cancelling_at_Steam_is_not_a_failure()
        {
            var stub = new StubProvider("is_valid:true");
            var assertion = new Dictionary<string, string> { ["openid.mode"] = "cancel" };

            var result = await SteamOpenIdProtocol.CompleteAsync(assertion, Origin, stub);

            Assert.AreEqual(SteamOpenIdStatus.Canceled, result.Status);
            Assert.AreEqual(0, stub.Calls);
        }

        [TestMethod]
        public void The_referer_is_read_out_of_return_to()
        {
            Assert.AreEqual("/Forum", SteamOpenIdProtocol.ReadReferer(ReturnTo));
            Assert.AreEqual("/a b&c", SteamOpenIdProtocol.ReadReferer(
                Origin + "/Home/Logon?x=1&referer=" + Uri.EscapeDataString("/a b&c")));
            Assert.IsNull(SteamOpenIdProtocol.ReadReferer(Origin + "/Home/Logon"));
            Assert.IsNull(SteamOpenIdProtocol.ReadReferer(Origin + "/Home/Logon?other=1"));
        }

        /// <summary>Steam, as far as these tests are concerned. Records what it was asked.</summary>
        private sealed class StubProvider : HttpMessageHandler
        {
            private readonly string body;
            private readonly HttpStatusCode status;

            public StubProvider(string body, HttpStatusCode status = HttpStatusCode.OK)
            {
                this.body = body;
                this.status = status;
            }

            public int Calls { get; private set; }
            public string LastUrl { get; private set; }
            public Dictionary<string, string> LastForm { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Calls++;
                LastUrl = request.RequestUri.GetLeftPart(UriPartial.Path);
                Assert.AreEqual(HttpMethod.Post, request.Method, "check_authentication is a POST");

                var encoded = await request.Content.ReadAsStringAsync();
                LastForm = encoded.Split('&')
                    .Select(p => p.Split('='))
                    .ToDictionary(p => Uri.UnescapeDataString(p[0].Replace("+", " ")),
                                  p => Uri.UnescapeDataString(p[1].Replace("+", " ")));

                return new HttpResponseMessage(status) { Content = new StringContent(body) };
            }
        }
    }
}
