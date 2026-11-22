using System;

namespace ZkLobbyServer.Api
{
    /// <summary>
    /// Which lobby server the website talks to, and how.
    ///
    /// **Settings live in the MiscVars table**, like every other secret this system has
    /// (see ZkData/Secrets.cs). Both halves already hold the database, so there is no new file to
    /// deploy, no environment to set, and nothing to keep in sync between two machines. It also
    /// means the secret is exactly as exposed as it was before: anyone who can read MiscVars could
    /// already read everything.
    ///
    /// - <c>LobbyApiUrl</c> - **unset means in-process**, which is what the site does today and
    ///   what it keeps doing if nobody touches anything. Setting it is the whole switch.
    /// - <c>LobbyApiSecret</c> - shared between the two. Required once the URL is set.
    /// - <c>LobbyApiListenPrefix</c> - where a standalone server listens. Loopback by default.
    ///
    /// **There is no half-configured state.** A URL without a secret throws at startup rather than
    /// falling back to in-process: falling back would start a second lobby server beside the one
    /// already running, and two servers sharing a database is a worse outcome than not starting.
    /// </summary>
    public static class LobbyApiConfiguration
    {
        public const string UrlKey = "LobbyApiUrl";
        public const string SecretKey = "LobbyApiSecret";
        public const string ListenPrefixKey = "LobbyApiListenPrefix";

        /// <summary>
        /// Where a standalone lobby server listens unless told otherwise. It lived on LobbyApiHost,
        /// which is the half that opens an HttpListener - and that made the CLIENT side of this
        /// file drag the server side behind it. The website's .NET 9 port links the client and has
        /// no business carrying a listener, so the constant moved to where it is a setting rather
        /// than an implementation detail.
        /// </summary>
        /// 8300, not 8200: <c>GlobalConst.LobbyServerPort</c> is 8200 in Local AND Live mode, so
        /// a default of 8200 is the port the player listener is about to bind. A standalone lobby
        /// server started without <c>LobbyApiListenPrefix</c> got as far as "Listening at port
        /// 8200" and then died on its own socket -
        /// <c>SocketException: Address already in use</c> out of HttpListener.Start.
        public const string DefaultPrefix = "http://127.0.0.1:8300/";

        /// <summary>
        /// <c>LobbyApiAllowInsecureTransport</c> - set to "true" to permit plaintext off loopback.
        /// The secret and the single sign-on token both cross this connection, so the default is
        /// no; this exists for a private segment where a certificate is not available, and it has
        /// to be set deliberately.
        /// </summary>
        public static bool AllowInsecureTransport(string configured) =>
            string.Equals(configured?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

        /// <summary>True when the website should NOT start a lobby server of its own.</summary>
        public static bool IsRemote(string url) => !string.IsNullOrWhiteSpace(url);

        /// <summary>
        /// The client for a remote server, or an exception saying what is missing. Never null and
        /// never a quiet fallback - see the class note on why.
        /// </summary>
        public static ILobbyServerApi CreateClient(string url, string secret, bool allowInsecureTransport = false)
        {
            if (!IsRemote(url))
                throw new InvalidOperationException(
                    "CreateClient was called with no " + UrlKey + ". Ask IsRemote first; with no URL "
                    + "the website runs the lobby server in its own process.");

            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException(
                    UrlKey + " is set to '" + url + "' but " + SecretKey + " is not. The lobby API "
                    + "kicks players and posts as a moderator, so it is not reachable without one. "
                    + "Set both MiscVars or neither.");

            return new RemoteLobbyServerApi(url, secret, handler: null, allowInsecureTransport: allowInsecureTransport);
        }

        /// <summary>
        /// Where a standalone lobby server listens. Loopback unless told otherwise, because the
        /// API is privileged and a default that reaches the world is a default nobody chose.
        /// </summary>
        public static string ListenPrefix(string configured) =>
            string.IsNullOrWhiteSpace(configured) ? DefaultPrefix : configured;
    }
}
