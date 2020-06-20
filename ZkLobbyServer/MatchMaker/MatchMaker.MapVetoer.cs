using LobbyClient;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Linq;
using System.Linq;
using ZkData;

namespace ZkLobbyServer
{
    public partial class MatchMaker
    {
        public class MapVetoer
        {
            public ProposedBattle Battle { get; set; }
            public ZkLobbyServer Server { get; set; }

            public string SelectMap(List<string> candidates)
            {
                List<User> players = Battle.Players.Select(x => x.LobbyUser).ToList();
                List<string> filteredCandidates = CandidatesAfterBans(candidates, GetPlayerMapBans(players));

                Random r = new Random();
                if (filteredCandidates.Count == 0)
                {
                    Server.UserLogSay($"Warning: could not find one valid candidate after bans for game with {Battle.Size} players. " +
                        $"This really should not happen, ignoring bans for this game."); ;
                    return candidates[r.Next(candidates.Count)];
                } else
                {
                    Server.UserLogSay($"Received {filteredCandidates.Count} candidate maps after applying bans on {candidates.Count} maps.");
                    return filteredCandidates[r.Next(filteredCandidates.Count)];
                }
            }

            private List<string> CandidatesAfterBans(List<string> candidates, IEnumerable<IGrouping<int, AccountMapBan>> bans) {
                var bannedMaps = new HashSet<string>();

                // Ban up to X% of all maps in the pool, and distribute those bans evenly across all players, rounding down.
                // This could be static configuration on the queue itself but the map ban logic may take into account the actual
                // ban distribution of each player in the future and this keeps all the banning logic outside the main MatchMaker class.
                var maximumBans = GlobalConst.MaximumPercentageOfBannedMaps * candidates.Count;
                var bansPerPlayer = Math.Truncate(maximumBans / Battle.Size);
                Server.UserLogSay($"Banning {bansPerPlayer} maps for each of {Battle.Size} plyers.");

                // For each player in the battle, go through their bans and add the first X bans not already used by another player.
                // For team games, this does not always result in the most amount of bans possible depending on the order player bans
                // are processed, but still guarantees every player will not see at least X of the maps they have banned.
                foreach (var userBans in bans)
                {
                    var bansUsed = 0;
                    foreach(var ban in userBans)
                    {
                        var mapName = ban.Resource.InternalName;
                        if (bansUsed < bansPerPlayer && !bannedMaps.Contains(mapName))
                        {
                            bansUsed++;
                            bannedMaps.Add(mapName);
                        }
                    }
                }

                var allCandidates = new HashSet<string>(candidates);
                return allCandidates.Except(bannedMaps).ToList();
            }

            private IEnumerable<IGrouping<int, AccountMapBan>> GetPlayerMapBans(List<User> players)
            {
                var db = new ZkDataContext();
                var accountIDs = players.Select(x => x.AccountID);

                return db.AccountMapBans
                    .Where(x => accountIDs.Contains(x.AccountID))
                    .Include(x => x.Resource)
                    .OrderBy(x => x.Rank)
                    .GroupBy(x => x.AccountID);
            }
        }
    }
}
