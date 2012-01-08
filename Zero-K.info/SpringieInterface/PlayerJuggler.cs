using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZkData;

namespace ZeroKWeb.SpringieInterface
{
    public class JugglerAutohost
    {
        public BattleContext LobbyContext;
        public BattleContext RunningGameStartContext;
    }

    public class PlayerJuggler
    {
        static readonly List<AutohostMode> BinOrder = new List<AutohostMode>
                                                      {
                                                          AutohostMode.Game1v1,
                                                          AutohostMode.GameFFA,
                                                          AutohostMode.Planetwars,
                                                          AutohostMode.GameTeams,
                                                          AutohostMode.GameChickens
                                                      };


        public static JugglerResult JugglePlayers(List<JugglerAutohost> autohosts)
        {
            var ret = new JugglerResult();
            var bins = new List<Bin>();
            var db = new ZkDataContext();
            var sb = new StringBuilder();
            var lobbyIds = new List<int?>();

            foreach (var ah in autohosts)
            {
                sb.AppendLine("ah:" + ah.LobbyContext.AutohostName);

                if (ah.RunningGameStartContext == null) lobbyIds.AddRange(ah.LobbyContext.Players.Where(x => !x.IsSpectator).Select(x => (int?)x.LobbyID));
                    // game not running add all nonspecs
                else
                {
                    var notPlaying = ah.LobbyContext.Players.Where(
                            x => !x.IsSpectator && !ah.RunningGameStartContext.Players.Any(y => y.LobbyID == x.LobbyID && !y.IsSpectator)).Select(
                                x => (int?)x.LobbyID).ToList();
                    // game running, add all those that are not playing and are not specs
                    lobbyIds.AddRange(notPlaying);
                    sb.AppendLine("not playing: " + string.Join(",", notPlaying));
                }
            }

            var juggledAccounts = db.Accounts.Where(x => lobbyIds.Contains(x.LobbyID)).ToDictionary(x => x.LobbyID ?? 0);

            foreach (var grp in
                autohosts.Where(x => x.RunningGameStartContext == null && x.LobbyContext != null && x.LobbyContext.Players.Any(y => !y.IsSpectator)).
                    GroupBy(x => x.LobbyContext.GetMode()))
            {
                if (grp.Key == AutohostMode.Game1v1)
                {
                    // make bins from all 1v1 autohost
                    foreach (var ah in grp)
                    {
                        var bin = new Bin() { Autohost = ah, Mode = ah.LobbyContext.GetMode() };
                        bin.Assigned.AddRange(
                            ah.LobbyContext.Players.Where(x => !x.IsSpectator && juggledAccounts.ContainsKey(x.LobbyID)).Select(x => x.LobbyID));
                        bins.Add(bin);
                    }
                }
                else
                {
                    //make one bin from biggest ah of other type
                    var biggest = grp.OrderByDescending(x => x.LobbyContext.Players.Count(y => !y.IsSpectator)).First();
                    var bin = new Bin() { Autohost = biggest, Mode = biggest.LobbyContext.GetMode() };
                    foreach (var ah in autohosts.Where(x => x.LobbyContext.GetMode() == bin.Mode))
                    {
                        bin.Assigned.AddRange(
                            ah.LobbyContext.Players.Where(x => !x.IsSpectator && juggledAccounts.ContainsKey(x.LobbyID)).Select(x => x.LobbyID));
                        // add all valid players from all ahof this type to this bin
                    }

                    bins.Add(bin);
                }
            }

            

            SetBinLists(bins, juggledAccounts);
            foreach (var b in bins) b.Assigned.Clear();

            sb.AppendLine("Original bins:");
            PrintBins(juggledAccounts, bins, sb);

            Bin todel = null;
            do
            {
                var moved = false;

                // high priority pass
                do
                {
                    moved = false;
                    foreach (var b in bins.OrderBy(x => BinOrder.IndexOf(x.Mode)))
                    {
                        var person = b.HighPriority.FirstOrDefault(x => !b.Assigned.Contains(x));
                        if (person != 0)
                        {
                            var acc = juggledAccounts[person];
                            var current = bins.FirstOrDefault(x => x.Assigned.Contains(person));

                            var biggerBattleRule = false;
                            if (current != null)
                            {
                                if (b.Assigned.Count < b.MinPlayers && current.Assigned.Count < current.MinPlayers) biggerBattleRule = b.Assigned.Count > current.Assigned.Count;
                                else if (b.Assigned.Count < b.MinPlayers && current.Assigned.Count >= current.MinPlayers + 1) biggerBattleRule = true;
                            }

                            if (current == null || acc.Preferences[current.Mode] != GamePreference.Prefers || biggerBattleRule)
                            {
                                Move(bins, person, b);
                                moved = true;
                            }
                        }
                    }
                } while (moved);

                //sb.AppendLine("h-pass bins:");
                //PrintBins(juggledAccounts, bins, sb);

                // normal pass
                do
                {
                    moved = false;
                    foreach (var b in bins.OrderBy(x => BinOrder.IndexOf(x.Mode)))
                    {
                        var person = b.NormalPriority.FirstOrDefault(x => !b.Assigned.Contains(x));
                        if (person != 0)
                        {
                            var current = bins.FirstOrDefault(x => x.Assigned.Contains(person));
                            var biggerBattleRule = false;
                            if (current != null)
                            {
                                if (b.Assigned.Count < b.MinPlayers && current.Assigned.Count < current.MinPlayers) biggerBattleRule = b.Assigned.Count > current.Assigned.Count;
                                else if (b.Assigned.Count < b.MinPlayers && current.Assigned.Count >= current.MinPlayers + 1) biggerBattleRule = true;
                            }

                            if (current == null || biggerBattleRule)
                            {
                                Move(bins, person, b);
                                moved = true;
                            }
                        }
                    }
                } while (moved);

                todel = bins.OrderBy(x => BinOrder.IndexOf(x.Mode)).FirstOrDefault(x => x.Assigned.Count < x.MinPlayers);
                if (todel != null)
                {
                    bins.Remove(todel);
                    SetBinLists(bins, juggledAccounts);
                    sb.AppendLine("removing bin " + todel.Mode);
                    PrintBins(juggledAccounts, bins, sb);
                }
            } while (todel != null);

            sb.AppendLine("Final bins:");
            PrintBins(juggledAccounts, bins, sb);

            if (bins.Any())
            {
                ret.PlayerMoves = new List<JugglerMove>();
                foreach (var b in bins)
                {
                    foreach (var a in b.Assigned)
                    {
                        var acc = juggledAccounts[a];
                        var origAh = autohosts.FirstOrDefault(x => x.LobbyContext.Players.Any(y => y.Name == acc.Name));
                        if (origAh == null || origAh.LobbyContext.AutohostName != b.Autohost.LobbyContext.AutohostName) ret.PlayerMoves.Add(new JugglerMove() { Name = acc.Name, TargetAutohost = b.Autohost.LobbyContext.AutohostName });
                    }
                }

                ret.AutohostsToClose = new List<string>();
                foreach (
                    var ah in
                        autohosts.Where(
                            x =>
                            x.RunningGameStartContext == null && !bins.Any(y => y.Autohost == x) && x.LobbyContext.Players.Any(y => !y.IsSpectator))) ret.AutohostsToClose.Add(ah.LobbyContext.AutohostName);
            }

            ret.Message = sb.ToString();
            return ret;
        }

        static Dictionary<int, GamePreference> GetCurrentPrefs(List<Bin> bins, Dictionary<int, Account> juggledAccounts)
        {
            var currentPrefs = new Dictionary<int, GamePreference>();
            foreach (var a in juggledAccounts)
            {
                currentPrefs[a.Key] = GamePreference.Neutral; // no bin -> take any
                var hisBin = bins.FirstOrDefault(x => x.Assigned.Contains(a.Key));
                if (hisBin != null) currentPrefs[a.Key] = a.Value.Preferences[hisBin.Mode];
            }
            return currentPrefs;
        }

        static void Move(List<Bin> bins, int lobbyID, Bin target)
        {
            foreach (var b in bins) b.Assigned.Remove(lobbyID);

            if (!target.Assigned.Contains(lobbyID)) target.Assigned.Add(lobbyID);
        }

        static void PrintBins(Dictionary<int, Account> juggledAccounts, List<Bin> bins, StringBuilder sb)
        {
            foreach (var b in bins)
            {
                sb.AppendFormat("{0} {1}: {2}    - (High: {3})   (Low: {4})\n",
                                b.Mode,
                                b.Autohost.LobbyContext.AutohostName,
                                string.Join(",", b.Assigned.Select(x => juggledAccounts[x].Name)),
                                string.Join(",", b.HighPriority.Select(x => juggledAccounts[x].Name)),
                                string.Join(",", b.NormalPriority.Select(x => juggledAccounts[x].Name)));
            }
            sb.AppendFormat("Free people: {0}\n",
                            string.Join(",", juggledAccounts.Where(x => !bins.Any(y => y.Assigned.Contains(x.Key))).Select(x => x.Value.Name)));
        }

        static void SetBinLists(List<Bin> bins, Dictionary<int, Account> juggledAccounts)
        {
            var currentPrefs = GetCurrentPrefs(bins, juggledAccounts);

            foreach (var b in bins)
            {
                b.HighPriority.Clear();
                b.NormalPriority.Clear();

                foreach (var a in juggledAccounts)
                {
                    var lobbyID = a.Key;

                    if (b.Mode == AutohostMode.Planetwars && a.Value.Level < GlobalConst.MinPlanetWarsLevel) continue; // dont queue who cannot join PW

                    if (b.Assigned.Contains(lobbyID)) // he is there already
                    {
                        if (a.Value.Preferences[b.Mode] != GamePreference.Dislike) b.HighPriority.Add(lobbyID); // if he disliked only add existing as neutral, otherwise high
                        else b.NormalPriority.Add(lobbyID);
                    }
                    else
                    {
                        if (b.Mode == AutohostMode.Game1v1 && b.Assigned.Count() >= 2) continue; // full 1v1
                        if (b.Mode == AutohostMode.Game1v1 && b.Assigned.Count == 1 &&
                            Math.Abs(a.Value.EffectiveElo - juggledAccounts[b.Assigned[0]].EffectiveElo) > 250) continue; //effective elo difference > 250 dont try to combine

                        if (a.Value.Preferences[b.Mode] > currentPrefs[lobbyID]) b.HighPriority.Add(lobbyID);
                        else if (a.Value.Preferences[b.Mode] == currentPrefs[lobbyID]) b.NormalPriority.Add(lobbyID);
                    }
                }
            }

            // set those who dont have any bin preference to be neutral with all bins
            foreach (var a in juggledAccounts) if (!bins.Any(x => x.HighPriority.Contains(a.Key) || x.NormalPriority.Contains(a.Key))) foreach (var b in bins) b.NormalPriority.Add(a.Key);
        }

        public class Bin
        {
            public List<int> Assigned = new List<int>();
            public JugglerAutohost Autohost;
            public List<int> HighPriority = new List<int>();
            public int MinPlayers
            {
                get
                {
                    switch (Mode)
                    {
                        case AutohostMode.Game1v1:
                            return 2;
                        case AutohostMode.GameFFA:
                            return 3;
                        case AutohostMode.Planetwars:
                            return 4;
                        case AutohostMode.GameTeams:
                            return 4;
                        case AutohostMode.GameChickens:
                            return 2;
                    }
                    return 0;
                }
            }
            public AutohostMode Mode;
            public List<int> NormalPriority = new List<int>();

            public class PlayerEntry
            {
                public Account Account;
                public Bin CurrentBin;
            }
        }
    }

    public class JugglerMove
    {
        public string Name;
        public string TargetAutohost;
    }

    public class JugglerResult
    {
        public List<string> AutohostsToClose;
        public string Message;
        public List<JugglerMove> PlayerMoves;
    }
}