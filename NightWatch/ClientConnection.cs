using System.Net.Sockets;
using System.Text;

namespace CaTracker
{
    /// <summary>
    /// Handles communiction with clients using client protocol
    /// </summary>
    public class ClientConnection: ServerConnection
    {
        public ClientConnection() {}

        public ClientConnection(string host, int port): base(host, port) {}
        public ClientConnection(TcpClient cli): base(cli) {}


        protected override ServerConnectionEventArgs ParseCommand(string line)
        {
            var args = line.Split('|'); // split arguments
            var command = new ServerConnectionEventArgs();
            command.ServerConnection = this;
            command.Command = args[0];
            command.Result = ServerConnectionEventArgs.ResultTypes.Success;
            command.Parameters = new string[args.Length - 1];
            for (var j = 1; j < args.Length; ++j) command.Parameters[j - 1] = MyUnescape(args[j]);
            return command;
        }

        protected override byte[] PrepareCommand(string command, object[] pars)
        {
            var prepstring = command + "|";
            for (var i = 0; i < pars.Length; ++i)
            {
                var s = pars[i].ToString();
                prepstring += MyEscape(s) + "|"; // if parameter starts with \t it's sentence seperator and we will ommit space
            }
            prepstring += '\n';
            return ASCIIEncoding.ASCII.GetBytes(prepstring);
        }

        static string MyEscape(string input)
        {
            return input.Replace("|", "&divider&");
        }

        static string MyUnescape(string input)
        {
            return input.Replace("&divider&", "|");
        }
    }
}