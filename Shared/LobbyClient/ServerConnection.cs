#region using

using System.Text;
using ZkData;

#endregion

namespace LobbyClient
{
    /// <summary>
    /// Handles communiction with server on low level
    /// </summary>
    public class ServerConnection: Connection
    {
        protected override ConnectionEventArgs ParseCommand(string line)
        {
            var command = new ConnectionEventArgs();
            if (line != null)
            {
                var args = line.Split(' '); // split arguments

                // prepare and send command recieved info
                command.Connection = this;
                command.Command = args[0];
                command.Result = ConnectionEventArgs.ResultTypes.Success;
                command.Parameters = new string[args.Length - 1];
                for (var j = 1; j < args.Length; ++j) command.Parameters[j - 1] = args[j];
            }
            else
            {
                command.Result = ConnectionEventArgs.ResultTypes.NetworkError;
                command.Parameters = new string[] { };
                command.Command = "";
            }
            return command;
        }

        /// <summary>
        /// Prepares byte array with command
        /// </summary>
        /// <param Name="command">command</param>
        /// <param Name="pars">command parameters</param>
        /// <returns></returns>
        protected override string PrepareCommand(string command, object[] pars)
        {
            var prepstring = command;
            for (var i = 0; i < pars.Length; ++i)
            {
                var ns = pars[i];
                string s;
                if (ns != null) s = ns.ToString(); else s = "";
                if (!string.IsNullOrEmpty(s)) prepstring += (s[0] == '\t' ? "" : " ") + s; // if parameter starts with \t it's sentence seperator and we will ommit space
            }
            //prepstring += '\n';
            return prepstring;
        }
    }
}