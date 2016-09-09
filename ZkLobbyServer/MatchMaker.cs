using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class MatchMaker
    {
        ZkLobbyServer server;


        List<MatchMakerSetup.Queue> QueueTypes = new List<MatchMakerSetup.Queue>();



        public MatchMaker(ZkLobbyServer server)
        {
            this.server = server;
            var db = new ZkDataContext();
        }
    }
}
