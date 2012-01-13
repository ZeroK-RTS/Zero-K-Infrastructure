using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LobbyClient;
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
        List<JugglerAutohost> autohosts;
        Dictionary<int, Account> juggledAccounts;
        List<Bin> bins;
        Dictionary<int, bool> canBeMoved;

        public PlayerJuggler(List<JugglerAutohost> autohosts) {
            this.autohosts = autohosts;

        }


        public JugglerResult JugglePlayers()
        {
            var ret = new JugglerResult();
            bins = new List<Bin>();
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

            juggledAccounts = db.Accounts.Where(x => lobbyIds.Contains(x.LobbyID)).ToDictionary(x => x.LobbyID ?? 0);
            canBeMoved = new Dictionary<int, bool>();
            canBeMoved = juggledAccounts.ToDictionary(x=>x.Key, x=> {
                                                                        User user;
                                                                        Global.Nightwatch.Tas.ExistingUsers.TryGetValue(x.Value.Name,out user);    
                                                                        return user == null || !user.IsZkLobbyUser;
                });


            foreach (var grp in
                autohosts.Where(x => x.RunningGameStartContext == null && x.LobbyContext != null && x.LobbyContext.Players.Any(y => !y.IsSpectator)).
                    GroupBy(x => x.LobbyContext.GetMode()))
            {
                /*if (grp.Key == AutohostMode.Game1v1)
                {
                    // make bins from all 1v1 autohost*/
                    foreach (var ah in grp)
                    {
                        var bin = new Bin() { Autohost = ah, Mode = ah.LobbyContext.GetMode() };
                        bin.ManuallyJoined.AddRange(
                            ah.LobbyContext.Players.Where(x => !x.IsSpectator && juggledAccounts.ContainsKey(x.LobbyID)).Select(x => x.LobbyID));
                        bins.Add(bin);
                    }
                /*}
                else
                {
                    //make one bin from biggest ah of other type
                    var biggest = grp.OrderByDescending(x => x.LobbyContext.Players.Count(y => !y.IsSpectator)).First();
                    var bin = new Bin() { Autohost = biggest, Mode = biggest.LobbyContext.GetMode() };
                    foreach (var ah in autohosts.Where(x => x.LobbyContext.GetMode() == bin.Mode))
                    {
                        bin.ManuallyJoined.AddRange(
                            ah.LobbyContext.Players.Where(x => !x.IsSpectator && juggledAccounts.ContainsKey(x.LobbyID)).Select(x => x.LobbyID));
                        // add all valid players from all ahof this type to this bin
                    }

                    bins.Add(bin);
                }*/
            }

            foreach (var b in bins.Where(x => x.MinPlayers > juggledAccounts.Count).ToList()) bins.Remove(b); // remove those that cant be possible handled

            SetPriorities(bins, juggledAccounts);
            
            sb.AppendLine("Original bins:");
            PrintBins(sb);

            Bin todel = null;
            do 
            {
                ResetAssigned();
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
                            var persons = b.PlayerPriority.Where(x => !b.Assigned.Contains(x.Key) && x.Value == priority && canBeMoved.ContainsKey(x.Key)).Select(x => x.Key);
                            if (binElo != null) persons = persons.OrderByDescending(x => Math.Abs(juggledAccounts[x].EffectiveElo - binElo.Value));

                            foreach (var person in persons)
                            {
                                var acc = juggledAccounts[person];
                                var current = bins.FirstOrDefault(x => x.Assigned.Contains(person));

                                var saveBattleRule = false;
                                if (current != null && current != b && acc.Preferences[current.Mode] <= acc.Preferences[b.Mode]) if (b.Assigned.Count < b.MinPlayers && current.Assigned.Count >= current.MinPlayers + 1) saveBattleRule = true;

                                if (current == null || saveBattleRule)
                                {
                                    Move(person, b);
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
                    PrintBins(sb);
                }
            } while (todel != null);

            sb.AppendLine("Final bins:");
            PrintBins(sb);

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

                /*ret.AutohostsToClose = new List<string>();
                foreach (var ah in
                    autohosts.Where(
                        x => x.RunningGameStartContext == null && !bins.Any(y => y.Autohost == x) && x.LobbyContext.Players.Any(y => !y.IsSpectator))) ret.AutohostsToClose.Add(ah.LobbyContext.AutohostName);*/
            }

            ret.Message = sb.ToString();
            return ret;
        }


        void Move(int lobbyID, Bin target)
        {
            foreach (var b in bins) b.Assigned.Remove(lobbyID);

            if (!target.Assigned.Contains(lobbyID)) target.Assigned.Add(lobbyID);
        }

        void PrintBins(StringBuilder sb)
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

        void ResetAssigned()
        {
            foreach (var b in bins)
            {
                if (b.Mode == AutohostMode.Game1v1) b.Assigned = new List<int>(b.ManuallyJoined);
                else {
                    b.Assigned.Clear();
                    foreach (var id in b.ManuallyJoined)
                    {
                        if (!canBeMoved.ContainsKey(id))  b.Assigned.Add(id);  // todo non zkl are not moveable yet, remove later
                    }
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