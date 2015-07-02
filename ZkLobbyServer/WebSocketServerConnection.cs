using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using vtortola.WebSockets;
using ZkData;

namespace ZkLobbyServer
{
    public abstract class WebSocketServerConnection: Connection
    {
        WebSocket wsc;

        protected override void InternalClose()
        {
            try
            {
                wsc.Close();
            }
            catch { }

            IsConnected = false;

            try
            {
                OnConnectionClosed(closeRequestedExplicitly);
            }
            catch (Exception ex)
            {
                Trace.TraceError("{0} error procesing OnConnectionClosed: {1}", ex);
            }
        }

        public async void RunOnAcceptedWebSocket(WebSocket wsc)
        {
            this.wsc = wsc;

            closeRequestedExplicitly = false;

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            token.Register(wsc.Close);

            IsConnected = true;

            try
            {
                await OnConnected();
            }
            catch (Exception ex)
            {
                Trace.TraceError("{0} error processing OnConnected: {1}", this, ex);
            }


            try {
                RemoteEndpointIP = wsc.RemoteEndpoint.Address.ToString();
                RemoteEndpointPort = wsc.RemoteEndpoint.Port;

                while (wsc.IsConnected && !token.IsCancellationRequested) {
                    var message = await wsc.ReadMessageAsync(token);
                    if (message.Length == 0) break;
                    using (var sr = new StreamReader(message, Encoding)) {
                        var line = await sr.ReadToEndAsync();
                        LogInput(line);
                        await OnLineReceived(line);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested) Trace.TraceWarning("{0} socket disconnected: {1}", this, ex.Message);
            }
            InternalClose();
        }


        public override async Task SendData(byte[] buffer)
        {
            if (IsConnected && wsc.IsConnected)
            {
                try
                {
                    using (var messageWriter = wsc.CreateMessageWriter(WebSocketMessageType.Binary)) await new MemoryStream(buffer).CopyToAsync(messageWriter);
                }
                catch (Exception ex)
                {
                    if (cancellationTokenSource != null && !cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        Trace.TraceWarning("{0} error sending command: {1}", this, ex.Message);
                        RequestClose();
                    }
                }
            }
        }
    }
}