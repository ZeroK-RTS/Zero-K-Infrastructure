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

    public class JugglerConfig
    {
        public bool Active;
        public List<PreferencePair> Preferences = new List<PreferencePair>();

        public class PreferencePair
        {
            public AutohostMode Mode;
            public GamePreference Preference;
        }
    }

    public class JugglerState
    {
        public List<ModePair> ModeCounts = new List<ModePair>();
        public int TotalPlayers = 0;

        public class ModePair
        {
            public AutohostMode Mode;
            public int Count;
        }
    }

    public class PlayerJuggler
    {
        public static JugglerState LastState = new JugglerState();

        static readonly List<AutohostMode> BinOrder = new List<AutohostMode>
                                                      {
                                                          AutohostMode.Planetwars,
                                                          AutohostMode.BigTeams,
                                                          AutohostMode.MediumTeams,
                                                          AutohostMode.SmallTeams,
                                                          AutohostMode.GameFFA,
                                                          AutohostMode.GameChickens,
                                                          AutohostMode.Game1v1,
                                                      };


        public static bool CanMove(Account acc)
        {
            User user;
            if (Global.Nightwatch.Tas.ExistingUsers.TryGetValue(acc.Name, out user) && user.IsZkLobbyUser) return true;
            return false;
        }

        public static JugglerResult JugglePlayers(List<JugglerAutohost> autohosts)
        {
            var ret = new JugglerResult();
            

            var bins = new List<Bin>();
            var db = new ZkDataContext();
            var sb = new StringBuilder();
            var lobbyIds = new List<int?>();

            Dictionary<int, AutohostMode> manuallyPrefered = new Dictionary<int, AutohostMode>();
            
            var tas = Global.Nightwatch.Tas;

            autohosts = autohosts.Where(x => !tas.ExistingBattles.Values.Single(y => y.Founder.Name == x.LobbyContext.AutohostName).IsPassworded).ToList(); //only non pw battles

            foreach (var ah in autohosts)
            {
                if (ah.RunningGameStartContext == null) lobbyIds.AddRange(ah.LobbyContext.Players.Where(x => !x.IsSpectator).Select(x => (int?)x.LobbyID));
                    // game not running add all nonspecs
                else
                {
                    var notPlaying =
                        ah.LobbyContext.Players.Where(
                            x => !x.IsSpectator && !x.IsIngame && !ah.RunningGameStartContext.Players.Any(y => y.LobbyID == x.LobbyID && !y.IsSpectator)).Select(
                                x => (int?)x.LobbyID).ToList();
                    // game running, add all those that are not playing and are not specs
                    lobbyIds.AddRange(notPlaying);
                }
            }

            var roomLessLobbyID = tas.ExistingUsers.Values.Where(x => !x.IsInGame).Select(x => (int?)x.LobbyID).ToList();

            var juggledAccounts = db.Accounts.Where(x => lobbyIds.Contains(x.LobbyID) || (roomLessLobbyID.Contains(x.LobbyID) && x.MatchMakingActive)).ToDictionary(x => x.LobbyID ?? 0);

            // make bins from non-running games with players by each type
            foreach (var grp in
                autohosts.Where(x => x.LobbyContext != null).GroupBy(x => x.LobbyContext.GetMode()))
            {
                List<Bin> groupBins = new List<Bin>();
                
                // set manual preference for all juggleable people who are in games
                foreach (var ah in grp) {
                    foreach (var u in ah.LobbyContext.Players.Where(x=>juggledAccounts.ContainsKey(x.LobbyID))) {
                        manuallyPrefered[u.LobbyID] = grp.Key;
                    }
                }

                // make bins from existing battles that are not running and have some players
                foreach (var ah in grp.Where(x => x.RunningGameStartContext == null && x.LobbyContext.Players.Any(y => !y.IsSpectator)))
                {
                    var bin = new Bin(ah);
                    var toAdd = ah.LobbyContext.Players.Where(x => !x.IsSpectator && juggledAccounts.ContainsKey(x.LobbyID)).Select(x => x.LobbyID).ToList();
                    bin.ManuallyJoined.AddRange(toAdd);
                    groupBins.Add(bin);
                }

                if (groupBins.Count == 0) { // no bins with players found, add empty one
                    var firstEmpty = grp.First(x => x.RunningGameStartContext == null && x.LobbyContext.Players.All(y => y.IsSpectator));
                    var bin = new Bin(firstEmpty);
                    groupBins.Add(bin);
                }
                
                // remove all but biggest below merge limit
                var biggest = groupBins.OrderByDescending(x => x.ManuallyJoined.Count).First();
                groupBins.RemoveAll(x => x != biggest && x.ManuallyJoined.Count < (x.Config.MergeSmallerThan??0));
                
                bins.AddRange(groupBins);
            }

            
            SetPriorities(bins, juggledAccounts, manuallyPrefered);

            sb.AppendLine("Original bins:");
            PrintBins(juggledAccounts, bins, sb);

            foreach (var b in bins.Where(x => x.Config.MinToJuggle > x.PlayerPriority.Count).ToList()) bins.Remove(b); // remove those that cant be possible handled
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
                            if (b.Config.MaxToJuggle != null && b.Assigned.Count >= b.Config.MaxToJuggle) continue;

                            var binElo = b.Assigned.Average(x => (double?)juggledAccounts[x].EffectiveElo);
                            var persons =
                                b.PlayerPriority.Where(x => !b.Assigned.Contains(x.Key) && x.Value == priority && CanMove(juggledAccounts[x.Key])).
                                    Select(x => x.Key).ToList();
                            
                            if (b.Config.MaxEloDifference != null && binElo != null) persons.RemoveAll(x => Math.Abs(juggledAccounts[x].EffectiveElo - (binElo ?? 0)) > b.Config.MaxEloDifference);

                            if (binElo != null) persons = persons.OrderByDescending(x => Math.Abs(juggledAccounts[x].EffectiveElo - binElo.Value)).ToList();

                            foreach (var person in persons)
                            {
                                var acc = juggledAccounts[person];
                                var current = bins.FirstOrDefault(x => x.Assigned.Contains(person));

                                var saveBattleRule = false;
                                if (current != null && current != b && acc.Preferences[current.Mode] <= acc.Preferences[b.Mode]) if (b.Assigned.Count < b.Config.MinToJuggle && current.Assigned.Count >= current.Config.MinToJuggle + 1) saveBattleRule = true;

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
                todel = bins.OrderBy(x => BinOrder.IndexOf(x.Mode)).FirstOrDefault(x => x.Assigned.Count < x.Config.MinToJuggle);

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
                SplitBins(autohosts, juggledAccounts, sb, bins);

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
                            string reason = "because you weren't in a valid battle or your battle was split into two smaller";
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
                            // AuthServiceClient.SendLobbyMessage(acc, string.Format("You were moved to {0}, {1}. To change your preferences, please go to home page. http://zero-k.info", b.Autohost.LobbyContext.AutohostName, reason));
                        }
                    }


                }

                /*ret.AutohostsToClose = new List<string>();
                foreach (var ah in
                    autohosts.Where(
                        x => x.RunningGameStartContext == null && !bins.Any(y => y.Autohost == x) && x.LobbyContext.Players.Any(y => !y.IsSpectator))) ret.AutohostsToClose.Add(ah.LobbyContext.AutohostName);*/
            }



            ret.Message = sb.ToString();
            
            return new JugglerResult();
            
            //return ret;
        }

        static void SplitBins(List<JugglerAutohost> autohosts, Dictionary<int, Account> juggledAccounts, StringBuilder sb, List<Bin> bins)
        {
            // split too big bins -> move top players to another autohost
            foreach (var b in bins.ToList())
            {
                if (b.Assigned.Count > b.Config.SplitBiggerThan)
                {
                    sb.AppendLine("Splitting " + b.Autohost.LobbyContext.AutohostName);
                    var splitTo = autohosts.FirstOrDefault(x => x.LobbyContext.GetMode() == b.Mode && x.RunningGameStartContext == null && x != b.Autohost);
                        //find first one that isnt running and isnt bin -> no players for it planned
                    if (splitTo != null)
                    {
                        sb.AppendLine("Splitting to " + splitTo.LobbyContext.AutohostName);
                        var eloList = b.Assigned.Select(x => juggledAccounts[x]).Where(CanMove).OrderBy(x => x.EffectiveElo).ToList();
                        var toMove = eloList.Take(b.Assigned.Count/2).ToList();
                        if (toMove.Count >= b.Config.MinToJuggle)
                        {
                            var target = new Bin(splitTo);
                            target.Assigned.AddRange(toMove.Select(x => x.LobbyID ?? 0));
                            bins.Add(target);
                            b.Assigned.RemoveAll(x => toMove.Any(y => y.LobbyID == x));
                        }
                    }
                }
            }
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
                if (b.Config.DontMoveManuallyJoined == true) b.Assigned = new List<int>(b.ManuallyJoined);
                else
                {
                    b.Assigned.Clear();
                    foreach (var id in b.ManuallyJoined) if (!CanMove(juggledPlayers[id])) b.Assigned.Add(id); 
                }
            }
        }

        static void SetPriorities(List<Bin> bins, Dictionary<int, Account> juggledAccounts, Dictionary<int, AutohostMode> manuallyPrefered)
        {
            foreach (var b in bins)
            {
                b.PlayerPriority.Clear();

                foreach (var a in juggledAccounts)
                {
                    var lobbyID = a.Key;
                    var battlePref = (double)a.Value.Preferences[b.Mode];
                    AutohostMode manualPref;
                    if (manuallyPrefered.TryGetValue(lobbyID, out manualPref) && manualPref == b.Mode) battlePref += 0.5; // player joined manually same type add 0.5

                    if (b.Config.MinLevel != null && a.Value.Level < b.Config.MinLevel) continue; // dont queue who cannot join PW
                    
                    if (b.ManuallyJoined.Contains(lobbyID)) // was he there already
                        b.PlayerPriority[lobbyID] = battlePref; // player joined it already
                    else
                    {
                        if (CanMove(a.Value))
                        {
                            if (b.Config.MaxToJuggle != null && b.ManuallyJoined.Count() >= b.Config.MaxToJuggle) continue; // full 1v1
                            if (b.Config.MaxEloDifference != null && b.ManuallyJoined.Count >=1)
                            {
                                var avgElo = b.ManuallyJoined.Average(x=>juggledAccounts[x].EffectiveElo);
                                if (Math.Abs(a.Value.EffectiveElo - avgElo) > b.Config.MaxEloDifference) continue; //effective elo difference > 250 dont try to combine
                            }

                            if (battlePref > (double)GamePreference.Never+0.6) b.PlayerPriority[lobbyID] = (int)battlePref;
                        }
                    }
                }
            }

            var state  = new JugglerState();
            state.TotalPlayers = juggledAccounts.Count;
            foreach (var grp in bins.GroupBy(x => x.Mode).Select(x => new { Mode = x.Key, Count = x.Sum(y => y.PlayerPriority.Count(z => z.Value > (double)GamePreference.Never)) })) {
                state.ModeCounts.Add(new JugglerState.ModePair() {Mode = grp.Mode, Count = grp.Count});
            }
            LastState = state;
        }


        public class Bin
        {
            public List<int> Assigned = new List<int>();
            public JugglerAutohost Autohost;
            public List<int> ManuallyJoined = new List<int>();
            public AutohostMode Mode;
            public AutohostConfig Config;
            public Dictionary<int, double> PlayerPriority = new Dictionary<int, double>();

            public Bin(JugglerAutohost autohost)
            {
                Autohost = autohost;
                Mode = autohost.LobbyContext.GetMode();
                Config = autohost.LobbyContext.GetConfig();
            }

            public class PlayerEntry
            {
                public Account Account;
                public Bin CurrentBin;
            }
        }

        public static JugglerConfig GetPlayerConfig(string login)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.First(x => x.Name == login);
            var ret = new JugglerConfig();
            ret.Active = acc.MatchMakingActive;
            foreach (var pref in acc.Preferences) {
                ret.Preferences.Add(new JugglerConfig.PreferencePair() { Mode = pref.Key, Preference = pref.Value});
            }
            return ret;
        }

        public static void SetPlayerConfig(string login, string password, JugglerConfig config)
        {
            var acc = AuthServiceClient.VerifyAccountPlain(login, password);
            var db = new ZkDataContext();
            acc = db.Accounts.Single(x => x.AccountID == acc.AccountID);
            acc.MatchMakingActive = config.Active;
            var prefs = acc.Preferences;
            foreach (var item in config.Preferences) {
                prefs[item.Mode] = item.Preference;
            }
            acc.SetPreferences(prefs);
            db.SubmitChanges(); 
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