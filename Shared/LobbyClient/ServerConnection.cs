#region using

using System;
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