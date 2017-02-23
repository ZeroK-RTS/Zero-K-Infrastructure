using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChobbyLauncher
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class SteamP2PMessageAttribute : Attribute { }

    [SteamP2PMessage]
    public class SteamP2PNotifyJoin
    {
        public string JoinerName;
    }

    [SteamP2PMessage]
    public class SteamP2PRequestClientPort
    {
    }

    [SteamP2PMessage]
    public class SteamP2PClientPort
    {
        public string IP { get; set; }
        public int Port { get; set; }

    }

    [SteamP2PMessage]
    public class SteamP2PDirectConnectRequest
    {
        public string IP { get; set; }
        public int Port { get; set; }
        public string ScriptPassword { get; set; }
    }

}
