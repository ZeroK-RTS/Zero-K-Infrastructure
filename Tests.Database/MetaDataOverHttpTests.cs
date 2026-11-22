using System;
using System.IO;
using System.Net;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaShared;
using ZkData;
using ZkData.UnitSyncLib;

namespace Tests.Database
{
    /// <summary>
    /// Map and game metadata reaching a lobby server that does not share the website's disk.
    ///
    /// ServerBattle asks for both every time a battle opens, and the only source used to be
    /// GlobalConst.SiteDiskPath - the website's own directory, which the lobby server saw because
    /// it ran inside the website's IIS worker. Out of process it sees nothing there, and the
    /// lookups answered null without saying so; a player running !listoptions is told "this game
    /// has no options".
    ///
    /// The fallback needs no new API: the website already publishes these exact bytes at
    /// {ResourceBaseUrl}/{name}.metadata.xml.gz, the game client downloads them from there in
    /// MetaDataCache itself, and both paths hand the result to the same deserialiser. These tests
    /// stand a real HttpListener where the site would be and check the server-side lookup reaches
    /// it - and, just as importantly, that the disk still wins when it has the file.
    /// </summary>
    [TestClass]
    public class MetaDataOverHttpTests
    {
        private const string ModName = "zk:test-metadata";

        private static byte[] AMod(string mutator)
        {
            return MetaDataCache.SerializeAndCompressMetaData(new Mod
            {
                Name = ModName,
                Mutator = mutator,
                Options = new Option[0],
            });
        }

        /// <summary>A stand-in for the website, serving one file at the path the client uses.</summary>
        private sealed class FakeSite : IDisposable
        {
            private readonly HttpListener listener = new HttpListener();
            public readonly string BaseUrl;
            public int Requests;

            public FakeSite(byte[] payload, int port)
            {
                BaseUrl = "http://127.0.0.1:" + port;
                listener.Prefixes.Add(BaseUrl + "/");
                listener.Start();
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    while (listener.IsListening)
                    {
                        HttpListenerContext context;
                        try { context = listener.GetContext(); }
                        catch { return; }

                        Interlocked.Increment(ref Requests);
                        if (payload == null) context.Response.StatusCode = 404;
                        else context.Response.OutputStream.Write(payload, 0, payload.Length);
                        context.Response.Close();
                    }
                });
            }

            public void Dispose() { try { listener.Stop(); } catch { } }
        }

        private static T WithSite<T>(string siteDiskPath, string baseUrl, Func<T> body)
        {
            var originalPath = GlobalConst.SiteDiskPath;
            var originalUrl = GlobalConst.ResourceBaseUrl;
            GlobalConst.SiteDiskPath = siteDiskPath;
            GlobalConst.ResourceBaseUrl = baseUrl + "/Resources";
            MetaDataCache.ServerClearMetaDataCache();
            try { return body(); }
            finally
            {
                GlobalConst.SiteDiskPath = originalPath;
                GlobalConst.ResourceBaseUrl = originalUrl;
                MetaDataCache.ServerClearMetaDataCache();
            }
        }

        [TestMethod]
        public void Metadata_is_fetched_from_the_site_when_it_is_not_on_disk()
        {
            using (var site = new FakeSite(AMod("from-http"), 18731))
            {
                var mod = WithSite("", site.BaseUrl, () => MetaDataCache.ServerGetMod(ModName));
                Assert.IsNotNull(mod, "nothing came back, so the HTTP fallback did not run");
                Assert.AreEqual("from-http", mod.Mutator);
            }
        }

        /// <summary>
        /// ServerBattle asks on every battle it opens, and an InternalName is one immutable
        /// version of a resource - so the second ask must not be a second request.
        /// </summary>
        [TestMethod]
        public void A_fetched_resource_is_not_fetched_again()
        {
            using (var site = new FakeSite(AMod("from-http"), 18732))
            {
                WithSite("", site.BaseUrl, () =>
                {
                    Assert.IsNotNull(MetaDataCache.ServerGetMod(ModName));
                    Assert.IsNotNull(MetaDataCache.ServerGetMod(ModName));
                    Assert.IsNotNull(MetaDataCache.ServerGetMod(ModName));
                    return 0;
                });
                Assert.AreEqual(1, site.Requests, "the site was asked more than once for the same resource");
            }
        }

        /// <summary>
        /// The disk is the fast path and has to stay it: a lobby server beside the website should
        /// not start making HTTP requests to it for files it can already read.
        /// </summary>
        [TestMethod]
        public void The_disk_is_used_when_it_has_the_file_and_the_site_is_not_asked()
        {
            var root = Path.Combine(Path.GetTempPath(), "zk-meta-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, GlobalConst.ResourceFolder));
            File.WriteAllBytes(Path.Combine(root, GlobalConst.ResourceFolder, ModName.EscapePath() + ".metadata.xml.gz"),
                AMod("from-disk"));
            try
            {
                using (var site = new FakeSite(AMod("from-http"), 18733))
                {
                    var mod = WithSite(root, site.BaseUrl, () => MetaDataCache.ServerGetMod(ModName));
                    Assert.IsNotNull(mod);
                    Assert.AreEqual("from-disk", mod.Mutator, "the HTTP copy won over a file that was right there");
                    Assert.AreEqual(0, site.Requests, "the site was asked for a file the disk already had");
                }
            }
            finally { Directory.Delete(root, true); }
        }

        /// <summary>A site that answers 404 leaves the caller with null, not an exception.</summary>
        [TestMethod]
        public void A_resource_no_site_has_is_still_answered_with_null()
        {
            using (var site = new FakeSite(null, 18734))
            {
                Assert.IsNull(WithSite("", site.BaseUrl, () => MetaDataCache.ServerGetMod(ModName)));
            }
        }
    }
}
