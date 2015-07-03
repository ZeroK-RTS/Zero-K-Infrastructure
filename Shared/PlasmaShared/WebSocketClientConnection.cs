using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace ZkData
{
    /*
    public abstract class WebSocketClientConnection: Connection
    {
        protected WebSocket wsc;


        public Task Connect(string host, int port, string bindingIp = null)
        {
            return InternalRun(host, port);
        }


        protected override void InternalClose()
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

        

        public override async Task SendData(byte[] buffer)
        {
            if (IsConnected) {
                try {
                    wsc.Send(buffer, 0, buffer.Length);
                } catch (Exception ex) {
                    if (cancellationTokenSource != null && !cancellationTokenSource.Token.IsCancellationRequested) {
                        Trace.TraceWarning("{0} error sending command: {1}", this, ex.Message);
                        RequestClose();
                    }
                }
            }
        }


        protected async Task InternalRun(string host, int port)
        {
            closeRequestedExplicitly = false;

            cancellationTokenSource = new CancellationTokenSource();

            var token = cancellationTokenSource.Token;

            wsc = new WebSocket(string.Format("ws://{0}:{1}",host,port)); // note bindingIp not supported

            token.Register(() => wsc.Close());

            wsc.Opened += (sender, args) => {
                IsConnected = true;
                OnConnected().Wait(token);
            };
            wsc.MessageReceived += (sender, args) => {
                LogInput(args.Message);
                OnLineReceived(args.Message).Wait(token);
            };
            wsc.Error += (sender, args) => {
                Trace.TraceError("Error in websocket client: {0}", args.Exception);
            };
            wsc.Closed += (sender, args) => { if (IsConnected) InternalClose();};
            

            RemoteEndpointIP = host;
            RemoteEndpointPort = port;
            
            wsc.Open();
            
        }
    }*/
}