using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZkLobbyServer
{
    public class MatchMakerBattle : AutoClosedServerBatle
    {
        public MatchMaker.ProposedBattle Prototype { get; private set; }

        public MatchMakerBattle(ZkLobbyServer server, MatchMaker.ProposedBattle bat, string mapname, bool applyHandicap) : base(server, null)
        {
            bool isLadder = DynamicConfig.Instance.LadderSeasonOngoing && bat.Size == 2; // only 1v1
            ApplicableRating = isLadder ? RatingCategory.Ladder : RatingCategory.MatchMaking; // FIXME should be both if season ongoing, but accepts only 1 AFAICS
            IsMatchMakerBattle = true;
            EngineVersion = server.Engine;
            ModName = server.Game;
            FounderName = "MatchMaker #" + BattleID;
            Mode = bat.QueueType.Mode;

            Title = "MM " + BattleID + ": " + bat.QueueType.Name;
            try {
                Title += ", Rank " + Ratings.Ranks.RankNames[bat.Players.Select(x => x.LobbyUser.Rank).Max()];
            } catch (Exception ex) {
                Trace.TraceError(ex.ToString());
            }

            MaxPlayers = bat.Size;
            Prototype = bat;
            MapName = mapname;

            foreach (var pe in bat.Players) Users[pe.Name] = new UserBattleStatus(pe.Name, pe.LobbyUser, GenerateClientScriptPassword(pe.Name));
            
            if (ModOptions == null) ModOptions = new Dictionary<string, string>();

            // proper way to send some extra start setup data
            if (bat.QueueType.Mode != AutohostMode.GameChickens)
                SetCompetitiveModoptions();
            if (applyHandicap)
                SetHandicapModoptions();
            ModOptions["MatchMakerType"] = bat.QueueType.Name;

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
