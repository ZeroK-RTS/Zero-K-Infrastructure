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
    public class WebSocketServerTransport: ITransport
    {
        Func<Task> OnConnected { get; set; }
        Func<bool, Task> OnConnectionClosed { get; set; }
        Func<string, Task> OnLineReceived { get; set; }
        protected CancellationTokenSource cancellationTokenSource;
        bool closeRequestedExplicitly;
        readonly WebSocket wsc;
        public static Encoding Encoding = new UTF8Encoding(false);

        public WebSocketServerTransport(WebSocket acceptedWebsocket)
        {
            this.wsc = acceptedWebsocket;
        }


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

        public async Task ConnectAndRun(Func<string, Task> onLineReceived, Func<Task> onConnected, Func<bool, Task> onConnectionClosed)
        {
            this.OnLineReceived = onLineReceived;
            this.OnConnected = onConnected;
            this.OnConnectionClosed = onConnectionClosed;

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
                        var ms = new MemoryStream();
                        await message.CopyToAsync(ms);
                        var line = Encoding.GetString(ms.ToArray());
                        await OnLineReceived(line);
                    }
                }
            } catch (Exception ex) {
                if (!token.IsCancellationRequested) Trace.TraceWarning("{0} socket disconnected: {1}", this, ex.Message);
            }
            InternalClose();
        }

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
                    //var buffer = Encoding.GetBytes(command);
                    using (var messageWriter = wsc.CreateMessageWriter(WebSocketMessageType.Text)) await new StreamWriter(messageWriter, Encoding).WriteAsync(command);

                    /*var buf = Encoding.GetBytes(command);
                    lock (this) 
                        using (var messageWriter = wsc.CreateMessageWriter(WebSocketMessageType.Text)) messageWriter.Write(buf,0, buf.Length);*/
                    
                } catch (Exception ex) {
                    if (cancellationTokenSource != null && !cancellationTokenSource.Token.IsCancellationRequested) {
                        Trace.TraceWarning("{0} error sending command: {1}", this, ex.Message);
                        RequestClose();
                    }
                }
            }
        }
    }
}