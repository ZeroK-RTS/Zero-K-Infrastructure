#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

#endregion

namespace PlasmaShared
{
    /// <summary>
    /// Event arguments used in many Connection events
    /// </summary>
    public class ConnectionEventArgs: EventArgs
    {
        public enum ResultTypes
        {
            Success,
            NetworkError,
            NotSet
        } ;

        string command = "";
        ResultTypes result = ResultTypes.NotSet;

        public string Command { get { return command; } set { command = value; } }

        public Connection Connection { get; set; }

        public string[] Parameters { get; set; }

        public ResultTypes Result { get { return result; } set { result = value; } }

        public ConnectionEventArgs() {}

        public ConnectionEventArgs(Connection connection, string command, string[] parameters)
        {
            Connection = connection;
            this.command = command;
            Parameters = parameters;
        }
    }

    /// <summary>
    /// Handles communiction with server on low level
    /// </summary>
    public abstract class Connection: IDisposable
    {
        bool isConnected;
        bool isConnecting;
        readonly object myLock = new object();
        protected Byte[] readBuffer;
        int readPosition;
        protected TcpClient tcp;

        public bool IsConnected { get { return isConnected; } }

        public bool IsConnecting { get { return isConnecting; } }

        public event EventHandler<ConnectionEventArgs> CommandRecieved;
        public event EventHandler<EventArgs<KeyValuePair<string, object[]>>> CommandSent = delegate { };
        public event EventHandler Connected;
        public event EventHandler ConnectionClosed;

        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// Closes connection to remote server
        /// </summary>
        public void Close()
        {
            lock (myLock)
            {
                CommandRecieved = null;
                Connected = null;
                isConnected = false;
                isConnecting = false;
                try
                {
                    tcp.GetStream().Close();
                    tcp.Close();
                }
                catch {}

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

        public void Connect(string host, int port, string bindingIp = null)
        {
            readPosition = 0;
          if (bindingIp == null )  tcp = new TcpClient();
          else tcp = new TcpClient(new IPEndPoint(IPAddress.Parse(bindingIp),0));
            try
            {
                isConnecting = true;
                tcp.LingerState.Enabled = false;
                tcp.BeginConnect(host, port, ConnectCallback, this);
            }
            catch
            {
                Close();
            }
        }


        public void SendCommand(string command, params object[] parameters)
        {
            if (IsConnected)
            {
                try
                {
                    CommandSent(this, new EventArgs<KeyValuePair<string, object[]>>(new KeyValuePair<string, object[]>(command, parameters)));
                    var buffer = PrepareCommand(command, parameters);
                    tcp.GetStream().BeginWrite(buffer, 0, buffer.Length, CommandSentCallback, this);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error sending command {0}", ex);
                    Close();
                    return;
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
            catch
            {
                serv.Close();
            }
        }

        static void ConnectCallback(IAsyncResult res)
        {
            var con = res.AsyncState as Connection;
            try
            {
                con.tcp.EndConnect(res);
                con.readBuffer = new byte[con.tcp.ReceiveBufferSize];
                con.readPosition = 0;
                con.tcp.GetStream().BeginRead(con.readBuffer, 0, con.readBuffer.Length, DataRecieveCallback, con);
                con.isConnected = true;
                con.isConnecting = false;
                try
                {
                    if (con.Connected != null) con.Connected(con, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error in connect callback {0}", ex);
                }
            }
            catch
            {
                con.Close();
            }
        }


        static void DataRecieveCallback(IAsyncResult res)
        {
            var server = (Connection)res.AsyncState;
            if (server.IsConnected)
            {
                try
                {
                    var read = server.tcp.GetStream().EndRead(res); // actual data read - this blocks
                    server.readPosition += read;
                    if (read == 0)
                    {
                        server.Close();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error while recieving data form server {0}", ex);
                    // there was error while reading - stream is broken
                    server.Close();
                    return;
                }

                // check data for new line - isolating commands from it
                for (var i = server.readPosition - 1; i >= 0; --i)
                {
                    if (server.readBuffer[i] == '\n')
                    {
                        // new line found - convert to string and parse commands

                        var recData = Encoding.UTF8.GetString(server.readBuffer, 0, i + 1); // convert recieved bytes to string

                        // cycle through lines of data
                        foreach (var line in recData.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            // for each line = command do

                            ConnectionEventArgs command = null;
                            try
                            {
                                command = server.ParseCommand(line);
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError("Error parsing command {0} {1}", line, ex);
                                throw;
                            }

                            if (command != null) if (server.CommandRecieved != null) server.CommandRecieved(server, command);
                        }

                        // copy remaining data (not ended by \n yet) to the beginning of buffer
                        for (var x = 0; x < server.readPosition - i - 1; ++x) server.readBuffer[x] = server.readBuffer[x + i + 1];
                        server.readPosition = server.readPosition - i - 1;
                        break;
                    }
                }

                // prepare to read more data
                var rembuf = server.readBuffer.Length - server.readPosition;
                if (rembuf <= 0)
                {
                    // read buffer too small, increase it
                    var n = new byte[server.readBuffer.Length*2];
                    server.readBuffer.CopyTo(n, 0);
                    server.readBuffer = n;
                    rembuf = server.readBuffer.Length - server.readPosition;
                }

                try
                {
                    server.tcp.GetStream().BeginRead(server.readBuffer, server.readPosition, rembuf, DataRecieveCallback, server);
                }
                catch (Exception ex)
                {
                    // there was error while reading - stream is broken
                    Trace.TraceError("Error recieving data {0}", ex);
                    server.Close();
                    return;
                }
            }
        }

        protected abstract ConnectionEventArgs ParseCommand(string line);
        protected abstract byte[] PrepareCommand(string command, object[] pars);
    }
}