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
    public class MatchMakerBattle : ServerBattle
    {
        public MatchMaker.ProposedBattle Prototype { get; private set; }

        public MatchMakerBattle(ZkLobbyServer server, MatchMaker.ProposedBattle bat) : base(server, null)
        {
            IsMatchMakerBattle = true;
            EngineVersion = server.Engine;
            ModName = server.Game;
            FounderName = "MatchMaker #" + BattleID;
            Title = "MatchMaker " + BattleID;
            Mode = bat.QueueType.Mode;
            MaxPlayers = bat.Size;
            Prototype = bat;

            foreach (var pe in bat.Players) Users[pe.Name] = new UserBattleStatus(pe.Name, pe.LobbyUser, Guid.NewGuid().ToString());
            
            if (ModOptions == null) ModOptions = new Dictionary<string, string>();
            if (bat.QueueType.Mode != AutohostMode.GameChickens) ModOptions["mutespec"] = "mute"; // mute spectators

            ValidateAndFillDetails();
        }

        public override void ValidateBattleStatus(UserBattleStatus ubs)
        {
            if (Prototype.Players.Any(y => y.Name == ubs.Name))
            {
                ubs.IsSpectator = false;
                ubs.AllyNumber = 0;
            }
            else
            {
                ubs.IsSpectator = true;
            }
        }

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