using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ZkData
{
    public abstract class TcpConnection: Connection
    {
        StreamReader reader;
        NetworkStream stream;
        protected TcpClient tcp;

        public Task RunOnExistingTcp(TcpClient tcp)
        {
            return InternalRun(tcp);
        }


        public override Task Connect(string host, int port, string bindingIp = null)
        {
            return InternalRun(null, host, port, bindingIp);
        }


        protected override void InternalClose()
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

        public override async Task SendData(byte[] buffer)
        {
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

                RemoteEndpointIP = ((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString();
                RemoteEndpointPort = ((IPEndPoint)tcp.Client.RemoteEndPoint).Port;

                try {
                    await OnConnected();
                } catch (Exception ex) {
                    Trace.TraceError("{0} error processing OnConnected: {1}", this, ex);
                }

                while (!token.IsCancellationRequested) {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break; // disconnected cleanly
                    LogInput(line);
                    await OnLineReceived(line);
                }
            } catch (Exception ex) {
                if (!token.IsCancellationRequested) Trace.TraceWarning("{0} socket disconnected: {1}", this, ex.Message);
            }
            InternalClose();
        }
    }
}