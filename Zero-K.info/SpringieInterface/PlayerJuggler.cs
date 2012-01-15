using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LobbyClient;
using PlasmaShared;
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


        public static bool CanMove(Account acc)
        {
            User user;
            if (Global.Nightwatch.Tas.ExistingUsers.TryGetValue(acc.Name, out user) && !user.IsZkLobbyUser) return false;
            return true;
        }

        public static JugglerResult JugglePlayers(List<JugglerAutohost> autohosts)
        {
            var ret = new JugglerResult();
            var bins = new List<Bin>();
            var db = new ZkDataContext();
            var sb = new StringBuilder();
            var lobbyIds = new List<int?>();

            foreach (var ah in autohosts)
            {
                if (ah.RunningGameStartContext == null) lobbyIds.AddRange(ah.LobbyContext.Players.Where(x => !x.IsSpectator).Select(x => (int?)x.LobbyID));
                    // game not running add all nonspecs
                else
                {
                    var notPlaying =
                        ah.LobbyContext.Players.Where(
                            x => !x.IsSpectator && !ah.RunningGameStartContext.Players.Any(y => y.LobbyID == x.LobbyID && !y.IsSpectator)).Select(
                                x => (int?)x.LobbyID).ToList();
                    // game running, add all those that are not playing and are not specs
                    lobbyIds.AddRange(notPlaying);
                }
            }

            var juggledAccounts = db.Accounts.Where(x => lobbyIds.Contains(x.LobbyID)).ToDictionary(x => x.LobbyID ?? 0);

            // make bins from non-running games with players by each type
            foreach (var grp in
                autohosts.Where(x => x.LobbyContext != null).GroupBy(x => x.LobbyContext.GetMode()))
            {
                List<Bin> groupBins = new List<Bin>();

                foreach (var ah in grp.Where(x => x.RunningGameStartContext == null && x.LobbyContext.Players.Any(y => !y.IsSpectator)))
                {
                    var bin = new Bin() { Autohost = ah, Mode = grp.Key };
                    bin.ManuallyJoined.AddRange(
                        ah.LobbyContext.Players.Where(x => !x.IsSpectator && juggledAccounts.ContainsKey(x.LobbyID)).Select(x => x.LobbyID));
                    groupBins.Add(bin);
                }

                if (groupBins.Count == 0) { // no bins with players found, add empty one
                    var firstEmpty = grp.First(x => x.RunningGameStartContext == null && x.LobbyContext.Players.All(y => y.IsSpectator));
                    var bin = new Bin() { Autohost = firstEmpty, Mode = grp.Key };
                    groupBins.Add(bin);
                }
                var biggest = groupBins.OrderByDescending(x => x.ManuallyJoined.Count).First();
                foreach (var ah in grp.Where(x=>x.RunningGameStartContext != null)) { // iterate through running and assign players there to biggest bin of same class
                    biggest.ManuallyJoined.AddRange(ah.LobbyContext.Players.Where(x => !x.IsSpectator && juggledAccounts.ContainsKey(x.LobbyID)).Select(x => x.LobbyID));
                }

                bins.AddRange(groupBins);
            }

            
            SetPriorities(bins, juggledAccounts);

            sb.AppendLine("Original bins:");
            PrintBins(juggledAccounts, bins, sb);

            foreach (var b in bins.Where(x => x.MinPlayers > juggledAccounts.Count).ToList()) bins.Remove(b); // remove those that cant be possible handled
            sb.AppendLine("First purge:");
            PrintBins(juggledAccounts, bins, sb);

            Bin todel = null;
            do
            {
                ResetAssigned(bins, juggledAccounts);
                var priority = double.MaxValue;

                do
                {
                    var newPriority =
                        bins.SelectMany(x => x.PlayerPriority.Values).Where(x => x < priority).Select(x => (double?)x).OrderByDescending(x => x).
                            FirstOrDefault();
                    if (newPriority == null) break; // no more priority to chec;
                    priority = newPriority.Value;

                    var moved = false;
                    do
                    {
                        //one priority pass
                        moved = false;
                        foreach (var b in bins.OrderBy(x => BinOrder.IndexOf(x.Mode)))
                        {
                            if (b.Assigned.Count >= b.MaxPlayers) continue;

                            var binElo = b.Assigned.Average(x => (double?)juggledAccounts[x].EffectiveElo);
                            var persons =
                                b.PlayerPriority.Where(x => !b.Assigned.Contains(x.Key) && x.Value == priority && CanMove(juggledAccounts[x.Key])).
                                    Select(x => x.Key).ToList();
                            if (binElo != null) persons = persons.OrderByDescending(x => Math.Abs(juggledAccounts[x].EffectiveElo - binElo.Value)).ToList();

                            foreach (var person in persons)
                            {
                                var acc = juggledAccounts[person];
                                var current = bins.FirstOrDefault(x => x.Assigned.Contains(person));

                                var saveBattleRule = false;
                                if (current != null && current != b && acc.Preferences[current.Mode] <= acc.Preferences[b.Mode]) if (b.Assigned.Count < b.MinPlayers && current.Assigned.Count >= current.MinPlayers + 1) saveBattleRule = true;

                                if (current == null || saveBattleRule)
                                {
                                    Move(bins, person, b);
                                    moved = true;
                                    break;
                                }
                            }
                        }
                    } while (moved);
                } while (true);

                // find first bin that cannot be started due to lack of people and remove it 
                todel = bins.OrderBy(x => BinOrder.IndexOf(x.Mode)).FirstOrDefault(x => x.Assigned.Count < x.MinPlayers);

                if (todel != null)
                {
                    bins.Remove(todel);
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
                        if (origAh == null || origAh.LobbyContext.AutohostName != b.Autohost.LobbyContext.AutohostName)
                        {
                            ret.PlayerMoves.Add(new JugglerMove() { Name = acc.Name, TargetAutohost = b.Autohost.LobbyContext.AutohostName });
                            string reason = "because you weren't in a valid battle";
                            if (origAh != null) {
                                var origMode = origAh.LobbyContext.GetMode();
                                if (acc.Preferences[origMode] < acc.Preferences[b.Mode])
                                {
                                    reason = string.Format("because you like {0} more than {1}", b.Mode.Description(), origMode.Description());
                                }
                                else {
                                    if (!bins.Any(x => x.Autohost == origAh))
                                    {
                                        reason = string.Format("because your game is not yet possible due to lack of players");
                                    }
                                    else {
                                        reason = string.Format("because you like {0} same as {1} and {0} was missing a player and you were the best match", b.Mode.Description(), origMode.Description());
                                    }
                                }
                            }
                            AuthServiceClient.SendLobbyMessage(acc, string.Format("You were moved to {0}, {1}. To change your preference, please go to home page.", b.Autohost.LobbyContext.AutohostName, reason));
                        }
                    }
                }

                /*ret.AutohostsToClose = new List<string>();
                foreach (var ah in
                    autohosts.Where(
                        x => x.RunningGameStartContext == null && !bins.Any(y => y.Autohost == x) && x.LobbyContext.Players.Any(y => !y.IsSpectator))) ret.AutohostsToClose.Add(ah.LobbyContext.AutohostName);*/
            }

            ret.Message = sb.ToString();
            return ret;
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
                sb.AppendFormat("{0} {1}: {2}    - ({3})\n",
                                b.Mode,
                                b.Autohost.LobbyContext.AutohostName,
                                string.Join(",", b.Assigned.Select(x => juggledAccounts[x].Name)),
                                string.Join(",",
                                            b.PlayerPriority.OrderByDescending(x => x.Value).Select(
                                                x => string.Format("{0}:{1}", juggledAccounts[x.Key].Name, x.Value))));
            }
            sb.AppendFormat("Free people: {0}\n",
                            string.Join(",", juggledAccounts.Where(x => !bins.Any(y => y.Assigned.Contains(x.Key))).Select(x => x.Value.Name)));
        }

        static void ResetAssigned(List<Bin> bins, Dictionary<int, Account> juggledPlayers)
        {
            foreach (var b in bins)
            {
                if (b.Mode == AutohostMode.Game1v1) b.Assigned = new List<int>(b.ManuallyJoined);
                else
                {
                    b.Assigned.Clear();
                    foreach (var id in b.ManuallyJoined) if (!CanMove(juggledPlayers[id])) b.Assigned.Add(id); // todo non zkl are not moveable yet, remove later
                }
            }
        }

        static void SetPriorities(List<Bin> bins, Dictionary<int, Account> juggledAccounts)
        {
            foreach (var b in bins)
            {
                b.PlayerPriority.Clear();

                foreach (var a in juggledAccounts)
                {
                    var lobbyID = a.Key;
                    var battlePref = a.Value.Preferences[b.Mode];

                    if (b.Mode == AutohostMode.Planetwars && a.Value.Level < GlobalConst.MinPlanetWarsLevel) continue; // dont queue who cannot join PW

                    if (b.ManuallyJoined.Contains(lobbyID)) // was he there already
                        b.PlayerPriority[lobbyID] = (int)battlePref + 0.5; // player joined, he gets +0.5 for his normal preference;
                    else
                    {
                        if (b.Mode == AutohostMode.Game1v1 && b.ManuallyJoined.Count() >= 2) continue; // full 1v1
                        if (b.Mode == AutohostMode.Game1v1 && b.ManuallyJoined.Count == 1 &&
                            Math.Abs(a.Value.EffectiveElo - juggledAccounts[b.ManuallyJoined[0]].EffectiveElo) > 250) continue; //effective elo difference > 250 dont try to combine

                        if (battlePref > GamePreference.Never) b.PlayerPriority[lobbyID] = (int)battlePref;
                    }
                }
            }
        }


        public class Bin
        {
            public List<int> Assigned = new List<int>();
            public JugglerAutohost Autohost;
            public List<int> ManuallyJoined = new List<int>();
            public int MaxPlayers
            {
                get
                {
                    if (Mode == AutohostMode.Game1v1) return 2;
                    else return 32;
                }
            }
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
                            return 6;
                        case AutohostMode.GameTeams:
                            return 4;
                        case AutohostMode.GameChickens:
                            return 3;
                    }
                    return 0;
                }
            }
            public AutohostMode Mode;
            public Dictionary<int, double> PlayerPriority = new Dictionary<int, double>();

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