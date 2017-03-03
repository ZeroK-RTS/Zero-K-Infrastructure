using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
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

                var debriefingMessage = BattleResultHandler.SubmitSpringBattleResult(springBattleContext, server);
                debriefingMessage.ChatChannel = "debriefing_" + debriefingMessage.ServerBattleID;

                // join people to channel
                await
                    Task.WhenAll(
                        spring.Context.ActualPlayers.Where(x=>x.Name != null).Select(x => server.ConnectedUsers.Get(x.Name))
                            .Where(x => x != null)
                            .Select(x => x.Process(new JoinChannel() { ChannelName = debriefingMessage.ChatChannel })));


                await server.Broadcast(Users.Keys, debriefingMessage);
                await server.RemoveBattle(this);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing battle exited: {0}", ex);
            }
        }
    }
}