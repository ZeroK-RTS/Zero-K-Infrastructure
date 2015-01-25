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
    ///     Event arguments used in many Connection events
    /// </summary>
    public class ConnectionEventArgs: EventArgs
    {
        public enum ResultTypes
        {
            Success,
            NetworkError,
            NotSet
        };

        string command = "";
        ResultTypes result = ResultTypes.NotSet;

        public string Command { get { return command; } set { command = value; } }

        public Connection Connection { get; set; }

        public string[] Parameters { get; set; }

        public ResultTypes Result { get { return result; } set { result = value; } }

        public ConnectionEventArgs() {}

        public ConnectionEventArgs(Connection connection, string command, string[] parameters) {
            Connection = connection;
            this.command = command;
            Parameters = parameters;
        }
    }

    /// <summary>
    ///     Handles communiction with server on low level
    /// </summary>
    public abstract class Connection: IDisposable
    {
        readonly object myLock = new object();
        protected TcpClient tcp;

        public bool IsConnected { get; private set; }
        public bool IsConnecting { get; private set; }

        public static Encoding Encoding = new UTF8Encoding(false);


        public event EventHandler<ConnectionEventArgs> CommandRecieved;
        public event EventHandler<EventArgs<KeyValuePair<string, object[]>>> CommandSent = delegate { };
        public event EventHandler Connected;
        public event EventHandler ConnectionClosed;

        public void Dispose() {
            InternalClose();
        }

        /// <summary>
        ///     Closes connection to remote server
        /// </summary>
        public void RequestClose() {
            cancellationTokenSource.Cancel();
        }

        private void InternalClose()
        {
            bool callClosing = false;
            lock (myLock)
            {
                if (IsConnected || IsConnecting)
                {
                    callClosing = true;
                    IsConnected = false;
                    IsConnecting = false;
                    CommandRecieved = null;
                    Connected = null;
                }
            }

            if (callClosing)
            {
                try
                {
                    tcp.GetStream().Close();
                    tcp.Close();
                }
                catch { }

                try
                {
                    if (ConnectionClosed != null) ConnectionClosed(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error procesing connection close {0}", ex);
                }
                ConnectionClosed = null;
            }
        }

        StreamReader reader;
        CancellationTokenSource cancellationTokenSource;


        public async Task Connect(string host, int port, string bindingIp = null, bool executeOnCallerThread = true)
        {
            cancellationTokenSource = new CancellationTokenSource();

            var token = cancellationTokenSource.Token;

            if (bindingIp == null) tcp = new TcpClient();
            else tcp = new TcpClient(new IPEndPoint(IPAddress.Parse(bindingIp), 0));
            try
            {
                token.Register(() => tcp.Close());

                IsConnecting = true;
                await tcp.ConnectAsync(host, port).ConfigureAwait(executeOnCallerThread); // see http://blog.stephencleary.com/2012/07/dont-block-on-async-code.html
                var stream = tcp.GetStream();
                reader = new StreamReader(stream, Encoding);
                IsConnected = true;
                IsConnecting = false;
                if (Connected != null) Connected(this, EventArgs.Empty);
                while (!token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(executeOnCallerThread); ;

                    ConnectionEventArgs command = null;
                    try
                    {
                        command = ParseCommand(line);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error parsing command {0} {1}", line, ex);
                        throw;
                    }

                    if (command != null) if (CommandRecieved != null) CommandRecieved(this, command);
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested) Trace.TraceWarning("Socket disconnected: {0}", ex);

            }
            InternalClose();
        }




        public void SendCommand(string command, params object[] parameters)
        {
            if (IsConnected)
            {
                try
                {
                    CommandSent(this, new EventArgs<KeyValuePair<string, object[]>>(new KeyValuePair<string, object[]>(command, parameters)));
                    var buffer = Encoding.GetBytes(PrepareCommand(command, parameters));
                    tcp.GetStream().BeginWrite(buffer, 0, buffer.Length, CommandSentCallback, this);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error sending command {0}", ex);
                    RequestClose();
                }
            }
        }

        static void CommandSentCallback(IAsyncResult res)
        {
            var serv = res.AsyncState as Connection;
            try
            {
                serv.tcp.GetStream().EndWrite(res);
            }
            catch (Exception ex) 
            {
                Trace.TraceError("Eror finalizing write: {0}",ex);
                serv.RequestClose();
            }
        }

        protected abstract ConnectionEventArgs ParseCommand(string line);
        protected abstract string PrepareCommand(string command, object[] pars);
    }
}