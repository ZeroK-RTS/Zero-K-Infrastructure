using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZkData
{
    public class TcpTransport: ITransport
    {
        Func<string, Task> OnCommandReceived { get; set; }
        Func<Task> OnConnected { get; set; }
        Func<bool, Task> OnConnectionClosed { get; set; }
        protected CancellationTokenSource cancellationTokenSource;
        protected bool closeRequestedExplicitly;


        StreamReader reader;
        NetworkStream stream;
        protected TcpClient tcp;
        public static Encoding Encoding = new UTF8Encoding(false);

        public TcpTransport(TcpClient existingTcp)
        {
            this.tcp = existingTcp;
        }

        public TcpTransport(string host, int port, string bindingIp = null)
        {
            if (bindingIp == null) tcp = new TcpClient(new IPEndPoint(IPAddress.Parse("0.0.0.0"),0));
            else tcp = new TcpClient(new IPEndPoint(IPAddress.Parse(bindingIp), 0));
            RemoteEndpointAddress = host;
            RemoteEndpointPort = port;
        }

        protected void InternalClose()
        {
            try {
                tcp.Close();
            } catch {}

            IsConnected = false;

            try {
                OnConnectionClosed(closeRequestedExplicitly);
            } catch (Exception ex) {
                Trace.TraceError("{0} error procesing OnConnectionClosed: {1}", ex);
            }
        }

        public virtual bool IsConnected { get; protected set; }


        /// <summary>
        ///     Closes connection to remote server
        /// </summary>
        public void RequestClose()
        {
            IsConnected = false;
            closeRequestedExplicitly = true;
            if (cancellationTokenSource != null) //in case never connected yet
                cancellationTokenSource.Cancel();
        }


        public async Task SendLine(string command)
        {
            var buffer = Encoding.GetBytes(command);
            if (IsConnected) {
                try {
                    await stream.WriteAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
                } catch (Exception ex) {
                    if (cancellationTokenSource != null && !cancellationTokenSource.Token.IsCancellationRequested) {
                        Trace.TraceWarning("{0} error sending command: {1}", this, ex.Message);
                        RequestClose();
                    }
                }
            }
        }

        public async Task ConnectAndRun(Func<string, Task> onLineReceived, Func<Task> onConnected, Func<bool, Task> onConnectionClosed)
        {
            OnCommandReceived = onLineReceived;
            OnConnected = onConnected;
            OnConnectionClosed = onConnectionClosed;
            closeRequestedExplicitly = false;

            cancellationTokenSource = new CancellationTokenSource();

            var token = cancellationTokenSource.Token;

            token.Register(() => tcp.Close());

            try {
                if (!tcp.Connected) await tcp.ConnectAsync(RemoteEndpointAddress, RemoteEndpointPort);
                stream = tcp.GetStream();
                reader = new StreamReader(stream, Encoding);
                IsConnected = true;

                RemoteEndpointAddress = ((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString();
                RemoteEndpointPort = ((IPEndPoint)tcp.Client.RemoteEndPoint).Port;

                try {
                    await OnConnected();
                } catch (Exception ex) {
                    Trace.TraceError("{0} error processing OnConnected: {1}", this, ex);
                }

                while (!token.IsCancellationRequested) {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break; // disconnected cleanly
                    await OnCommandReceived(line);
                }
            } catch (Exception ex) {
                if (!token.IsCancellationRequested) Trace.TraceWarning("{0} socket disconnected: {1}", this, ex.Message);
            }
            InternalClose();
        }

        public string RemoteEndpointAddress { get; private set; }
        public int RemoteEndpointPort { get; private set; }
    }
}