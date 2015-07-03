using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using vtortola.WebSockets;
using ZkData;

namespace ZkLobbyServer
{
    /*public abstract class WebSocketTransport: ITransport
    {
        protected CancellationTokenSource cancellationTokenSource;
        bool closeRequestedExplicitly;
        WebSocket wsc;
        public static Encoding Encoding = new UTF8Encoding(false);


        public async void RunOnAcceptedWebSocket(WebSocket wsc)
        {
            this.wsc = wsc;

            closeRequestedExplicitly = false;

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            token.Register(wsc.Close);

            IsConnected = true;

            try {
                await OnConnected();
            } catch (Exception ex) {
                Trace.TraceError("{0} error processing OnConnected: {1}", this, ex);
            }

            try {
                RemoteEndpointAddress = wsc.RemoteEndpoint.Address.ToString();
                RemoteEndpointPort = wsc.RemoteEndpoint.Port;

                while (wsc.IsConnected && !token.IsCancellationRequested) {
                    var message = await wsc.ReadMessageAsync(token);
                    if (message != null) {
                        if (message.Length == 0) break;
                        using (var sr = new StreamReader(message, Encoding)) {
                            var line = await sr.ReadToEndAsync();
                            await OnCommandReceived(line);
                        }
                    }
                }
            } catch (Exception ex) {
                if (!token.IsCancellationRequested) Trace.TraceWarning("{0} socket disconnected: {1}", this, ex.Message);
            }
            InternalClose();
        }


        public async Task SendCommand(byte[] buffer) {}

        protected void InternalClose()
        {
            try {
                wsc.Close();
            } catch {}

            IsConnected = false;

            try {
                OnConnectionClosed(closeRequestedExplicitly);
            } catch (Exception ex) {
                Trace.TraceError("{0} error procesing OnConnectionClosed: {1}", ex);
            }
        }

        public bool IsConnected { get; private set; }
        public Func<string, Task> OnCommandReceived { get; set; }
        public Func<Task> OnConnected { get; set; }
        public Func<bool, Task> OnConnectionClosed { get; set; }
        public string RemoteEndpointAddress { get; private set; }
        public int RemoteEndpointPort { get; private set; }

        public void RequestClose()
        {
            IsConnected = false;
            closeRequestedExplicitly = true;
            if (cancellationTokenSource != null) //in case never connected yet
                cancellationTokenSource.Cancel();
        }

        public async Task SendLine(string command)
        {
            if (IsConnected && wsc.IsConnected) {
                try {
                    var buffer = Encoding.GetBytes(command);
                    using (var messageWriter = wsc.CreateMessageWriter(WebSocketMessageType.Binary)) await new MemoryStream(buffer).CopyToAsync(messageWriter);
                } catch (Exception ex) {
                    if (cancellationTokenSource != null && !cancellationTokenSource.Token.IsCancellationRequested) {
                        Trace.TraceWarning("{0} error sending command: {1}", this, ex.Message);
                        RequestClose();
                    }
                }
            }
        }
    }*/
}