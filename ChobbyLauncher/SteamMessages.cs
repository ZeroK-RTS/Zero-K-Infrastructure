using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChobbyLauncher
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class SteamP2PMessageAttribute : Attribute { }

    /// <summary>
    /// sent to establish p2p connection
    /// </summary>
    [SteamP2PMessage]
    public class Dummy
    {
    }


    [SteamP2PMessage]
    public class SteamP2PNotifyJoin
    {
        public string JoinerName;
    }

    [SteamP2PMessage]
    public class SteamP2PRequestPrepareProxy
    {
        public int Channel { get; set; }
    }

    [SteamP2PMessage]
    public class SteamP2PConfirmCreateProxy
    {
        public int Channel { get; set; }

    }

    [SteamP2PMessage]
    public class SteamP2PDirectConnectRequest:SteamConnectSpring
    {
    }

}
