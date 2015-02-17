#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using ZkData;

#endregion

namespace LobbyClient
{
    /// <summary>
    ///     Event arguments used in many Connection events
    /// </summary>
    public class ConnectionEventArgs : EventArgs
    {
        string command = "";

        public string Command { get { return command; } set { command = value; } }

        public Connection Connection { get; set; }

        public string[] Parameters { get; set; }


        public ConnectionEventArgs() { }

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
    public class ServerConnection: Connection
    {
        public event EventHandler<ConnectionEventArgs> CommandRecieved;
        public event EventHandler<EventArgs<KeyValuePair<string, object[]>>> CommandSent = delegate { };
        public event EventHandler Connected;
        public event EventHandler ConnectionClosed;


        public override Task OnConnectionClosed(bool wasRequested)
        {
            return Task.Run(() => { if (ConnectionClosed != null) ConnectionClosed(this, EventArgs.Empty); });
        }

        public override Task OnConnected()
        {
            return Task.Run(() => { if (Connected != null) Connected(this, EventArgs.Empty); });
        }

        public override Task OnLineReceived(string line)
        {
            return Task.Run(() => {
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
            });
        }

        public async Task SendCommand(string command, params object[] parameters)
        {
            if (IsConnected)
            {
                try
                {
                    var buffer = Encoding.GetBytes(PrepareCommand(command, parameters));
                    await SendData(buffer);
                    CommandSent(this, new EventArgs<KeyValuePair<string, object[]>>(new KeyValuePair<string, object[]>(command, parameters)));
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error sending command {0}", ex);
                }
            }
        }

        protected ConnectionEventArgs ParseCommand(string line)
        {
            var command = new ConnectionEventArgs();
            if (line != null) {
                var args = line.Split(' '); // split arguments

                // prepare and send command recieved info
                command.Connection = this;
                command.Command = args[0];
                command.Parameters = new string[args.Length - 1];
                for (var j = 1; j < args.Length; ++j) command.Parameters[j - 1] = args[j];
                return command;
            } 
            return null;
        }

        /// <summary>
        /// Prepares byte array with command
        /// </summary>
        /// <param Name="command">command</param>
        /// <param Name="pars">command parameters</param>
        /// <returns></returns>
        protected string PrepareCommand(string command, object[] pars)
        {
            var sb = new StringBuilder();
            sb.Append(command);
            foreach (var ns in pars) {
                string s;
                if (ns != null) s = ns.ToString(); else s = "";
                
                if (!string.IsNullOrEmpty(s) && s[0] == '\t') sb.Append(s);// if parameter starts with \t it's sentence seperator and we will ommit space
                else {
                    sb.Append(' ');
                    sb.Append(s);
                }
            }
            sb.Append('\n');
            return sb.ToString();
        }
    }
}