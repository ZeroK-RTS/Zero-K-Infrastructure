#region using

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

#endregion

namespace CaTracker
{
    /// <summary>
    /// Event arguments used in many ServerConnection events
    /// </summary>
    public class ServerConnectionEventArgs: EventArgs
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

        public string[] Parameters { get; set; }

        public ResultTypes Result { get { return result; } set { result = value; } }

        public ServerConnection ServerConnection { get; set; }

        public ServerConnectionEventArgs() {}

        public ServerConnectionEventArgs(ServerConnection serverConnection, string command, string[] parameters)
        {
            ServerConnection = serverConnection;
            this.command = command;
            Parameters = parameters;
        }
    }


    /// <summary>
    /// Handles communiction with server on low level
    /// </summary>
    public class ServerConnection: IDisposable
    {
        //protected NetworkStream stream;

        bool isWriting = false;
        protected Byte[] readBuffer;
        int readPosition;
        protected TcpClient tcp;
        Queue<Byte[]> writeQueue = new Queue<byte[]>();

        /// <summary>
        /// Raised when command is recieved from the server
        /// </summary>
        public event EventHandler<ServerConnectionEventArgs> CommandRecieved;

        /// <summary>
        /// raised when connection is closed 
        /// </summary>
        public event EventHandler ConnectionClosed;

        public ServerConnection() {}

        /// <summary>
        /// Creates object and connects to TA server
        /// </summary>
        /// <param name="host">server host</param>
        /// <param name="port">server port</param>
        public ServerConnection(string host, int port)
        {
            Connect(host, port);
        }


        public ServerConnection(TcpClient cli)
        {
            Connect(cli);
        }

        public void Dispose()
        {
            Close();
        }

        bool isDisposed;

        /// <summary>
        /// Closes connection to remote server
        /// </summary>
        public void Close()
        {
            isDisposed = true;
            //if (tcp != null) tcp.Close();
            var pom = ConnectionClosed;
            if (pom != null) ConnectionClosed(this, EventArgs.Empty);
        }


        /// <summary>
        /// Connects to TA server and resets internal data
        /// </summary>
        /// <param name="host">server host</param>
        /// <param name="port">server port</param>
        public void Connect(string host, int port)
        {
            try
            {
                tcp = new TcpClient(host, port);
                Init();
            }
            catch
            {
                Close();
            }
        }

        public void Connect(TcpClient client)
        {
            tcp = client;
            Init();
        }

        /// <summary>
        /// Sends command to server
        /// </summary>
        /// <param name="command">command</param>
        /// <param name="parameters">command parameters</param>
        public void SendCommand(string command, params string[] parameters)
        {
            if (isDisposed) return;
            var stream = tcp.GetStream();
            if (stream != null && stream.CanWrite)
            {
                var buffer = PrepareCommand(command, parameters);
                lock (writeQueue)
                {
                    writeQueue.Enqueue(buffer);

                    if (writeQueue.Count > 0 && !isWriting)
                    {
                        var buf = writeQueue.Dequeue();
                        try
                        {
                            isWriting = true;
                            stream.BeginWrite(buf, 0, buf.Length, CommandSentCallback, this);
                        }
                        catch
                        {
                            Close();
                            return;
                        }
                    }
                }
            }
        }

        static void CommandSentCallback(IAsyncResult res)
        {
            var con = (ServerConnection)res.AsyncState;
            try
            {
                if (con.isDisposed) return;
                var stream = con.tcp.GetStream();
                stream.EndWrite(res);
                lock (con.writeQueue)
                {
                    if (con.writeQueue.Count > 0)
                    {
                        var buf = con.writeQueue.Dequeue();
                        stream.BeginWrite(buf, 0, buf.Length, CommandSentCallback, con);
                    }
                    else con.isWriting = false;
                }
            }
            catch
            {
                con.Close();
                return;
            }
        }


        static void DataRecieveCallback(IAsyncResult res)
        {
            var server = (ServerConnection)res.AsyncState;
            if (server.isDisposed) return;
            NetworkStream stream;
            try
            {
                stream = server.tcp.GetStream();
                var cnt = stream.EndRead(res); // actual data read - this blocks
                server.readPosition += cnt;
                if (cnt == 0)
                {
                    server.Close();
                    return;
                }
            }
            catch
            {
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

                    var recData = Encoding.ASCII.GetString(server.readBuffer, 0, i + 1); // convert recieved bytes to string
                    //Console.WriteLine(recData);
                    // cycle through lines of data
                    foreach (var line in recData.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        // for each line = command do

                        var command = server.ParseCommand(line);
                        if (server.CommandRecieved != null) server.CommandRecieved(server, command);
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
                stream.BeginRead(server.readBuffer, server.readPosition, rembuf, DataRecieveCallback, server);
            }
            catch
            {
                // there was error while reading - stream is broken
                server.Close();
                return;
            }
        }

        void Init()
        {
            readBuffer = new byte[tcp.ReceiveBufferSize];
            readPosition = 0;
            tcp.GetStream().BeginRead(readBuffer, 0, readBuffer.Length, DataRecieveCallback, this);
        }

        protected virtual ServerConnectionEventArgs ParseCommand(string line)
        {
            var args = line.Split(' '); // split arguments
            var command = new ServerConnectionEventArgs();
            command.ServerConnection = this;
            command.Command = args[0];
            command.Result = ServerConnectionEventArgs.ResultTypes.Success;
            command.Parameters = new string[args.Length - 1];
            for (var j = 1; j < args.Length; ++j) command.Parameters[j - 1] = args[j];
            return command;
        }

        /// <summary>
        /// Prepares byte array with command
        /// </summary>
        /// <param name="command">command</param>
        /// <param name="pars">command parameters</param>
        /// <returns></returns>
        protected virtual byte[] PrepareCommand(string command, object[] pars)
        {
            var prepstring = command;
            for (var i = 0; i < pars.Length; ++i)
            {
                var s = pars[i].ToString();

                prepstring += (s[0] == '\t' ? "" : " ") + s; // if parameter starts with \t it's sentence seperator and we will ommit space
            }
            prepstring += '\n';
            return Encoding.ASCII.GetBytes(prepstring);
        }
    }
}