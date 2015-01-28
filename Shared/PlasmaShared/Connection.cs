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
                Trace.TraceError("Error procesing connection close {0}", ex);
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
                await OnConnected();
                while (!token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break; // disconnected cleanly
                    await OnLineReceived(line);
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested) Trace.TraceWarning("Socket disconnected: {0}", ex);

            }
            InternalClose();
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
                        Trace.TraceError("Error sending command {0}", ex);
                        RequestClose();
                    }
                }
            }
        }
    }
}