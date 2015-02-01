#region using

using System;
using System.Collections.Generic;
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

        public abstract void OnConnectionClosed(bool wasRequested);

        private void InternalClose()
        {
            try
            {
                tcp.GetStream().Close();
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

        public abstract void OnConnected();

        public async Task Connect(string host, int port, string bindingIp = null)
        {
            closeRequestedExplicitly = false;

            cancellationTokenSource = new CancellationTokenSource();

            var token = cancellationTokenSource.Token;

            if (bindingIp == null) tcp = new TcpClient();
            else tcp = new TcpClient(new IPEndPoint(IPAddress.Parse(bindingIp), 0));
            token.Register(() => tcp.Close());
            

            try
            {
                await tcp.ConnectAsync(host, port).ConfigureAwait(false); // see http://blog.stephencleary.com/2012/07/dont-block-on-async-code.html
                stream = tcp.GetStream();
                reader = new StreamReader(stream, Encoding);
                IsConnected = true;
                OnConnected();
                while (!token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    OnLineReceived(line);
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested) Trace.TraceWarning("Socket disconnected: {0}", ex);

            }
            InternalClose();
        }

        public async Task Accept(TcpClient tcp)
        {
            closeRequestedExplicitly = false;
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            token.Register(() => tcp.Close());

            try
            {
                stream = tcp.GetStream();
                reader = new StreamReader(stream, Encoding);
                IsConnected = true;
                OnConnected();
                while (!token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    OnLineReceived(line);
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested) Trace.TraceWarning("Socket disconnected: {0}", ex);

            }
            InternalClose();
        }

        public abstract void OnLineReceived(string line);

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