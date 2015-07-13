using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace ZkData
{
    public class WebSocketClientTransport : ITransport
    {
        Func<Task> OnConnected { get; set; }
        Func<bool, Task> OnConnectionClosed { get; set; }
        Func<string, Task> OnLineReceived { get; set; }

        bool closeRequestedExplicitly;
        protected WebSocket wsc;
        public static Encoding Encoding = new UTF8Encoding(false);

        public WebSocketClientTransport(string host, int port, string bindingIp = null)
        {
            RemoteEndpointAddress = host;
            RemoteEndpointPort = port;
        }


        protected void InternalClose()
        {
            try
            {
                if (wsc.State == WebSocketState.Open || wsc.State == WebSocketState.None)
                {
                    wsc.Close();
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
            }
            catch { }
        }


        public bool IsConnected { get; private set; }

        public async Task ConnectAndRun(Func<string, Task> onLineReceived, Func<Task> onConnected, Func<bool, Task> onConnectionClosed)
        {
            this.OnLineReceived = onLineReceived;
            this.OnConnected = onConnected;
            this.OnConnectionClosed = onConnectionClosed;

            closeRequestedExplicitly = false;

            wsc = new WebSocket(string.Format("ws://{0}:{1}", RemoteEndpointAddress, RemoteEndpointPort)); // note bindingIp not supported
            wsc.EnableAutoSendPing = true;
            wsc.Opened += (sender, args) =>
            {
                IsConnected = true;
                OnConnected();
            };
            wsc.MessageReceived += (sender, args) => { OnLineReceived(args.Message); };
            wsc.Error += (sender, args) => { Trace.TraceError("Error in websocket client: {0}", args.Exception); };
            wsc.Closed += (sender, args) => { if (IsConnected) InternalClose(); };

            wsc.Open();
            
        }

        public string RemoteEndpointAddress { get; private set; }
        public int RemoteEndpointPort { get; private set; }

        public void RequestClose()
        {
            IsConnected = false;
            closeRequestedExplicitly = true;
            InternalClose();
        }

        public async Task SendLine(string command)
        {
            if (IsConnected)
            {
                try
                {
                    var buffer = Encoding.GetBytes(command);
                    wsc.Send(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    if (IsConnected)
                    {
                        Trace.TraceWarning("{0} error sending command: {1}", this, ex.Message);
                        RequestClose();
                    }
                }
            }
        }
    }
}