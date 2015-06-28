#region using

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace ZkData
{
    /// <summary>
    ///     Handles communiction with server on low level
    /// </summary>
    public abstract class Connection
    {
        protected TcpClient tcp;
        public bool IsConnected { get; private set; }
        public static Encoding Encoding = new UTF8Encoding(false);

        public event EventHandler<string> Input = delegate { };
        public event EventHandler<string> Output = delegate { }; // outgoing command and arguments

        bool closeRequestedExplicitly;

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


        private void InternalClose()
        {
            try
            {
                tcp.Close();
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

        StreamReader reader;
        CancellationTokenSource cancellationTokenSource;
        NetworkStream stream;

        public abstract Task OnConnected();
        public abstract Task OnConnectionClosed(bool wasRequested);
        public abstract Task OnLineReceived(string line);
        

        public Task Connect(string host, int port, string bindingIp = null)
        {
            return InternalRun(null, host, port, bindingIp);
        }

        public Task RunOnExistingTcp(TcpClient tcp)
        {
            return InternalRun(tcp);
        }

        public string RemoteEndpointIP { get; private set; }
        public int RemoteEndpointPort { get; private set; }

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


            try
            {
                if (existingTcp == null) await tcp.ConnectAsync(host, port.Value);
                stream = tcp.GetStream();
                reader = new StreamReader(stream, Encoding);
                IsConnected = true;

                RemoteEndpointIP = ((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString();
                RemoteEndpointPort = ((IPEndPoint)tcp.Client.RemoteEndPoint).Port;

                try {
                    await OnConnected();
                } catch (Exception ex) {
                    Trace.TraceError("{0} error processing OnConnected: {1}",this,ex);
                }

                while (!token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break; // disconnected cleanly
                    Input(this, line);
                    await OnLineReceived(line);
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested) Trace.TraceWarning("{0} socket disconnected: {1}", this, ex.Message);

            }
            InternalClose();
        }

        public Task SendString(string line)
        {
            Output(this, line.TrimEnd('\n'));
            return SendData(Encoding.GetBytes(line));
        }

        
        public async Task SendData(byte[] buffer)
        {
            if (IsConnected)
            {
                try
                {
                    await stream.WriteAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
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

        public override string ToString()
        {
            return string.Format("[{0}:{1}]", RemoteEndpointIP, RemoteEndpointPort);
        }
    }
}