using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ZkLobbyServer.Api
{
    /// <summary>
    /// Serves <see cref="ILobbyServerApi"/> over HTTP, so the website can be somewhere else.
    ///
    /// **This endpoint is privileged.** Anyone who can reach it with the secret can kick players,
    /// speak in the moderator channel and redeem session tokens. Two consequences are built in
    /// rather than left to whoever deploys it:
    ///
    /// - It **will not start without a secret**. There is no "unset means open" mode, because
    ///   that is the configuration a hurried deployment ends up with.
    /// - The default prefix is **loopback**. Binding it to the world is possible and has to be
    ///   written down explicitly by the person doing it.
    ///
    /// Dispatch goes through <see cref="LobbyApiProtocol.Members"/>, which is built from the
    /// interface, so a request cannot name anything the interface does not declare. The invocation
    /// target is the <see cref="ILobbyServerApi"/> reference, never the concrete server.
    /// </summary>
    public class LobbyApiHost : IDisposable
    {
        /// <summary>Kept as a name callers already use; the value lives in LobbyApiConfiguration.</summary>
        public const string DefaultPrefix = LobbyApiConfiguration.DefaultPrefix;

        private readonly ILobbyServerApi api;
        private readonly string secret;
        private readonly HttpListener listener = new HttpListener();
        private readonly CancellationTokenSource stopping = new CancellationTokenSource();

        /// <summary>The prefix actually bound, which matters when the port was left to the OS.</summary>
        public string Prefix { get; }

        public LobbyApiHost(ILobbyServerApi api, string secret, string prefix = DefaultPrefix,
                            bool allowInsecureTransport = false)
        {
            this.api = api ?? throw new ArgumentNullException(nameof(api));

            if (string.IsNullOrWhiteSpace(secret))
                throw new ArgumentException(
                    "The lobby API refuses to listen without a shared secret. It can kick players and "
                    + "post as a moderator; an unauthenticated one is not a degraded mode, it is a hole.",
                    nameof(secret));
            this.secret = secret;

            Prefix = prefix.EndsWith("/") ? prefix : prefix + "/";
            LobbyApiProtocol.RequireTrustworthyTransport(Prefix, allowInsecureTransport);
            listener.Prefixes.Add(Prefix);
        }

        public void Start()
        {
            listener.Start();
            Task.Run(() => AcceptLoop());
            Trace.TraceInformation("Lobby API listening on {0}", Prefix);
        }

        private async Task AcceptLoop()
        {
            while (!stopping.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (Exception) when (stopping.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Lobby API accept failed: {0}", ex);
                    return;
                }

                // One request must not be able to stop the listener for everyone else.
                var _ = Task.Run(() => Handle(context));
            }
        }

        private async Task Handle(HttpListenerContext context)
        {
            try
            {
                var member = await Authorize(context).ConfigureAwait(false);
                if (member == null) return;

                var arguments = await ReadArguments(context).ConfigureAwait(false);
                if (arguments == null) return;

                var result = await Invoke(member, arguments).ConfigureAwait(false);
                Write(context, 200, new LobbyApiResponse { result = result });
            }
            catch (Exception ex)
            {
                // The caller gets nothing but "it failed". A stack trace over this boundary is a
                // map of the server, and the useful copy is the one in the server's own log.
                Trace.TraceError("Lobby API {0} failed: {1}", context.Request?.RawUrl, ex);
                TryWrite(context, 500, new LobbyApiResponse { error = "the call failed" });
            }
        }

        /// <summary>Returns the member to invoke, or null having already answered.</summary>
        private async Task<MethodInfo> Authorize(HttpListenerContext context)
        {
            await Task.Yield();

            if (context.Request.HttpMethod != "POST")
            {
                Write(context, 405, new LobbyApiResponse { error = "POST only" });
                return null;
            }

            var header = context.Request.Headers["Authorization"] ?? "";
            var prefix = LobbyApiProtocol.AuthorizationScheme + " ";
            var presented = header.StartsWith(prefix, StringComparison.Ordinal)
                ? header.Substring(prefix.Length)
                : null;

            if (!LobbyApiProtocol.SecretEquals(secret, presented))
            {
                // No detail, and the same answer for a missing header and a wrong secret.
                Write(context, 401, new LobbyApiResponse { error = "unauthorized" });
                return null;
            }

            var path = context.Request.Url.AbsolutePath;
            if (!path.StartsWith(LobbyApiProtocol.PathPrefix, StringComparison.Ordinal))
            {
                Write(context, 404, new LobbyApiResponse { error = "no such member" });
                return null;
            }

            var name = path.Substring(LobbyApiProtocol.PathPrefix.Length);
            MethodInfo member;
            if (!LobbyApiProtocol.Members.TryGetValue(name, out member))
            {
                // The interface is the allowlist; anything else does not exist as far as this is
                // concerned, and the answer does not distinguish "not a member" from "not allowed".
                Write(context, 404, new LobbyApiResponse { error = "no such member" });
                return null;
            }

            return member;
        }

        private async Task<JObject> ReadArguments(HttpListenerContext context)
        {
            if (context.Request.ContentLength64 > LobbyApiProtocol.MaxRequestBytes)
            {
                Write(context, 413, new LobbyApiResponse { error = "request too large" });
                return null;
            }

            string body;
            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                body = await reader.ReadToEndAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(body)) return new JObject();

            try
            {
                return JObject.Parse(body);
            }
            catch (Exception)
            {
                Write(context, 400, new LobbyApiResponse { error = "malformed arguments" });
                return null;
            }
        }

        private async Task<object> Invoke(MethodInfo member, JObject arguments)
        {
            var parameters = member.GetParameters();
            var values = new object[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var token = arguments[parameters[i].Name];
                if (token == null)
                {
                    // An absent argument takes the declared default, so a client written against an
                    // older signature keeps working rather than failing on a parameter it never knew.
                    values[i] = parameters[i].HasDefaultValue
                        ? parameters[i].DefaultValue
                        : DefaultOf(parameters[i].ParameterType);
                }
                else values[i] = token.ToObject(parameters[i].ParameterType);
            }

            var returned = member.Invoke(api, values);

            if (returned is Task task)
            {
                await task.ConfigureAwait(false);
                var type = task.GetType();
                if (type.IsGenericType) return type.GetProperty("Result").GetValue(task);
                return null;
            }

            return returned;
        }

        private static object DefaultOf(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

        private static void Write(HttpListenerContext context, int status, LobbyApiResponse response)
        {
            var bytes = Encoding.UTF8.GetBytes(LobbyApiProtocol.Serialize(response));
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.OutputStream.Close();
        }

        private static void TryWrite(HttpListenerContext context, int status, LobbyApiResponse response)
        {
            try { Write(context, status, response); } catch (Exception) { /* the client went away */ }
        }

        public void Dispose()
        {
            stopping.Cancel();
            try { listener.Stop(); } catch (Exception) { }
            try { listener.Close(); } catch (Exception) { }
        }
    }
}
