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

        public List<MatchMakerSetup.Queue> QueueTypes = new List<MatchMakerSetup.Queue>();

        
        public MatchMaker(ZkLobbyServer server)
        {
            this.server = server;
            using (var db = new ZkDataContext())
            {
                QueueTypes.Add(new MatchMakerSetup.Queue()
                {
                    Name = "1v1",
                    Description = "Duels with reasonable skill difference",
                    MaxFriendCount = 0,
                    Maps =
                        db.Resources.Where(x => x.MapSupportLevel >= MapSupportLevel.MatchMaker && x.MapIs1v1 == true && x.TypeID == ResourceType.Map)
                            .Select(x => x.InternalName)
                            .ToList()
                });

                QueueTypes.Add(new MatchMakerSetup.Queue()
                {
                    Name = "Teams",
                    Description = "Small teams 2v2 to 4v4 with reasonable skill difference",
                    MaxFriendCount = 3,
                    Maps =
                        db.Resources.Where(
                                x => x.MapSupportLevel >= MapSupportLevel.MatchMaker && x.MapIsTeams == true && x.TypeID == ResourceType.Map)
                            .Select(x => x.InternalName)
                            .ToList()
                });
            }
        }
    }
}
