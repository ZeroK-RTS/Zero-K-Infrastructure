using System.Collections.Generic;
using System.Linq;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZkLobbyServer
{
    public class MatchMakerBattle : AutoClosedServerBatle
    {
        public MatchMaker.ProposedBattle Prototype { get; private set; }

        public MatchMakerBattle(ZkLobbyServer server, MatchMaker.ProposedBattle bat, string mapname) : base(server, null)
        {
            ApplicableRating = RatingCategory.MatchMaking;
            IsMatchMakerBattle = true;
            EngineVersion = server.Engine;
            ModName = server.Game;
            FounderName = "MatchMaker #" + BattleID;
            Mode = bat.QueueType.Mode;

            Title = "MM " + BattleID + ": " + bat.QueueType.Name;
            if (Mode == AutohostMode.Game1v1) {
                // possibly it could say the matchup in teams as well? Risks too long title though.
                Title += " " + bat.Players[0].Name + " vs " + bat.Players[1].Name;
            }
            if (Mode == AutohostMode.Game1v1 || Mode == AutohostMode.Teams) {
                using (var db = new ZkDataContext())
                {
                    try {
                        float totalElo = 0.0f;
                        foreach (var pe in bat.Players) {
                            var acc = db.Accounts.First(x => x.Name == pe.Name);
                            totalElo += acc.GetBestRating().Elo;
                        }
                        totalElo /= bat.Players.Count();
                        Title += ", avg skill " + totalElo.ToString("N0");
                    } catch (Exception ex) {
                        Trace.TraceError(ex.ToString());
                    }
                }
            }

            MaxPlayers = bat.Size;
            Prototype = bat;
            MapName = mapname;

            foreach (var pe in bat.Players) Users[pe.Name] = new UserBattleStatus(pe.Name, pe.LobbyUser, GenerateClientScriptPassword(pe.Name));
            
            if (ModOptions == null) ModOptions = new Dictionary<string, string>();

            // hacky way to send some extra start setup data
            if (bat.QueueType.Mode != AutohostMode.GameChickens) ModOptions["mutespec"] = "mute";
            ModOptions["MatchMakerType"] = bat.QueueType.Name;
            ModOptions["MinSpeed"] = "1";
            ModOptions["MaxSpeed"] = "1";

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
    }
}
