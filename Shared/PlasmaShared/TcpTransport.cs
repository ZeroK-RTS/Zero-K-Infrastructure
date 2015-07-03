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
        protected CancellationTokenSource cancellationTokenSource;
        protected bool closeRequestedExplicitly;


        StreamReader reader;
        NetworkStream stream;
        protected TcpClient tcp;
        public static Encoding Encoding = new UTF8Encoding(false);
        public virtual bool IsConnected { get; protected set; }


        public Task Connect(string host, int port, string bindingIp = null)
        {
            return InternalRun(null, host, port, bindingIp);
        }

        public Task RunOnExistingTcp(TcpClient tcp)
        {
            return InternalRun(tcp);
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


        protected async Task InternalRun(TcpClient existingTcp, string host = null, int? port = null, string bindingIp = null)
        {
            closeRequestedExplicitly = false;

            cancellationTokenSource = new CancellationTokenSource();

            var token = cancellationTokenSource.Token;

            if (existingTcp == null) {
                if (bindingIp == null) tcp = new TcpClient();
                else tcp = new TcpClient(new IPEndPoint(IPAddress.Parse(bindingIp), 0));
            } else tcp = existingTcp;

            token.Register(() => tcp.Close());

            try {
                if (existingTcp == null) await tcp.ConnectAsync(host, port.Value);
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


        public Func<Task> OnConnected { get; set; }
        public Func<bool, Task> OnConnectionClosed { get; set; }
        public Func<string, Task> OnCommandReceived { get; set; }

        public async Task SendCommand(string command)
        {
            byte[] buffer = Encoding.GetBytes(command);
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

        public string RemoteEndpointAddress { get; private set; }
        public int RemoteEndpointPort { get; private set; }
    }
}