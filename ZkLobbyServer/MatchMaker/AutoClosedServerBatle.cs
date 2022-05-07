using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZeroKWeb.SpringieInterface;
using ZkData;

namespace ZkLobbyServer
{
    public class AutoClosedServerBatle: ServerBattle {
        public AutoClosedServerBatle(ZkLobbyServer server, string founder): base(server, founder) {}

        protected override async Task OnDedicatedExited(SpringBattleContext springBattleContext)
        {
            try
            {
                StopVote();
                RunningSince = null;
                IsInGame = false;
                isZombie = true;

                var playerNames = spring.Context.ActualPlayers.Where(x => x.Name != null).Select(x => x.Name).ToList();

                bool result = BattleResultHandler.SubmitSpringBattleResult(springBattleContext, server, debriefingMessage =>
                {
                    debriefingMessage.ChatChannel = "debriefing_" + debriefingMessage.ServerBattleID;
                    // join people to channel
                    Task.WhenAll(
                        playerNames.Select(x => server.ConnectedUsers.Get(x))
                            .Where(x => x != null)
                            .Select(x => x.Process(new JoinChannel() { ChannelName = debriefingMessage.ChatChannel })));
                    server.Broadcast(playerNames, debriefingMessage);
                });
                
                await server.RemoveBattle(this);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing battle exited: {0}", ex);
            }
        }
    }
}