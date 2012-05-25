using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
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
                                                          AutohostMode.Planetwars,
                                                          AutohostMode.BigTeams,
                                                          AutohostMode.MediumTeams,
                                                          AutohostMode.SmallTeams,
                                                          AutohostMode.GameFFA,
                                                          AutohostMode.GameChickens,
                                                          AutohostMode.Game1v1,
                                                      };
        private static List<JugglerMove> LastPlayerMoves
            ;

        static PlayerJuggler()
        {
            TasClient tas = Global.Nightwatch.Tas;  

            tas.JoinChannel("juggler");

            tas.Extensions.JugglerConfigReceived += (args, config) =>
                {
                    if (args.UserName != GlobalConst.NightwatchName)
                    {
                        Task.Factory.StartNew(() =>
                            {
                                try
                                {
                                    using (var db = new ZkDataContext())
                                    {
                                        var acc = Account.AccountByName(db, args.UserName);
                                        acc.MatchMakingActive = config.Active;
                                        var prefs = acc.Preferences;
                                        foreach (var item in config.Preferences) prefs[item.Mode] = item.Preference;
                                        acc.SetPreferences(prefs);
                                        db.SubmitAndMergeChanges();
                                        SendAccountConfig(acc);
                                    }
                                }
                                catch (Exception ex) {
                                    Trace.TraceError(ex.ToString());
                                }
                            },TaskCreationOptions.LongRunning);
                    }
                };

            tas.ChannelUserAdded += (sender, args) => {
                var name = args.ServerParams[1];
                var chan = args.ServerParams[0];
                if (chan == ProtocolExtension.ExtensionChannelName && name != GlobalConst.NightwatchName) 
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        using (var db = new ZkDataContext())
                        {
                            var acc = Account.AccountByName(db, name);
                            SendAccountConfig(acc);
                        }
                    }
                    catch (Exception ex) {
                        Trace.TraceError(ex.ToString());
                    }

                }, TaskCreationOptions.LongRunning);

            };

            tas.BattleUserJoined += (sender, args) =>
            {
                tas.Say(TasClient.SayPlace.Channel, "juggler",string.Format("{0} joined {1}", args.UserName, args.BattleID), false);
                var entry = LastPlayerMoves.FirstOrDefault(x => x.Name == args.UserName);
                if (entry != null) {
                    Battle joinedBattle;
                    tas.Say(TasClient.SayPlace.Channel, "juggler", string.Format("last entry {0}: {1} -> {2}", entry.Name, entry.OriginalAutohost, entry.TargetAutohost), false);
                    if (tas.ExistingBattles.TryGetValue(args.BattleID, out joinedBattle) && joinedBattle.Founder.Name != entry.TargetAutohost) {
                        tas.ForceJoinBattle(args.UserName, entry.TargetAutohost);
                    }
                }
            };

        }

        public static void SendAccountConfig(Account acc) {
            Global.Nightwatch.Tas.Extensions.PublishPlayerJugglerConfig(new ProtocolExtension.JugglerConfig(acc), acc.Name);                
        }



        public static bool CanMove(Account acc)
        {
            return true;
            
            User user;
            if (Global.Nightwatch.Tas.ExistingUsers.TryGetValue(acc.Name, out user) && user.IsZkLobbyUser) return true;
            if (string.IsNullOrEmpty(acc.LobbyVersion) || !acc.LobbyVersion.Contains("SpringLobby")) return true;
            else return false;
        }

        public static JugglerResult JugglePlayers(List<JugglerAutohost> autohosts)
        {
            var ret = new JugglerResult();

            var bins = new List<Bin>();
            var db = new ZkDataContext();
            var sb = new StringBuilder();
            var lobbyIds = new List<int?>();

            var manuallyPrefered = new Dictionary<int, AutohostMode>();

            var tas = Global.Nightwatch.Tas;

            autohosts =
                autohosts.Where(x => !tas.ExistingBattles.Values.Any(y => y.Founder.Name == x.LobbyContext.AutohostName && y.IsPassworded)).ToList();
            //only non pw battles

						var ingames = tas.ExistingUsers.Where(x => x.Value.IsInGame).Select(x => x.Value.LobbyID).ToDictionary(x=>x);
            foreach (var ah in autohosts)
            {
							if (ah.RunningGameStartContext == null) lobbyIds.AddRange(ah.LobbyContext.Players.Where(x => !x.IsSpectator).Select(x => (int?)x.LobbyID));
                    // game not running add all nonspecs
                else
                {
                    /*var notPlaying =
                        ah.LobbyContext.Players.Where(
                            x =>
                            !x.IsSpectator && !x.IsIngame && !ingames.ContainsKey(x.LobbyID) && 
                            !ah.RunningGameStartContext.Players.Any(y => y.LobbyID == x.LobbyID && !y.IsSpectator && y.IsIngame)).Select(
                                x => (int?)x.LobbyID).ToList();
                    // game running, add all those that are not playing and are not specs
                     lobbyIds.AddRange(notPlaying);*/


                }
            }

            var roomLessLobbyID = tas.ExistingUsers.Values.Where(x => !x.IsInGame).Select(x => (int?)x.LobbyID).ToList();
            foreach (var ah in autohosts)
            {
                // safeguard - remove those known to be playing or speccing
                if (ah.RunningGameStartContext != null) foreach (var id in ah.RunningGameStartContext.Players.Where(x => !x.IsSpectator && x.IsIngame).Select(x => x.LobbyID)) roomLessLobbyID.Remove(id);
                if (ah.LobbyContext != null) foreach (var id in ah.LobbyContext.Players.Where(x => x.IsIngame).Select(x => x.LobbyID)) roomLessLobbyID.Remove(id);
            }

            var juggledAccounts =
                db.Accounts.Where(x => lobbyIds.Contains(x.LobbyID) || (roomLessLobbyID.Contains(x.LobbyID) && x.MatchMakingActive)).ToDictionary(
                    x => x.LobbyID ?? 0);

            // make bins from non-running games with players by each type
            foreach (var grp in
                autohosts.Where(x => x.LobbyContext != null).GroupBy(x => x.LobbyContext.GetMode()))
            {
                var groupBins = new List<Bin>();

                // set manual preference for all juggleable people who are in games
                foreach (var ah in grp) foreach (var u in ah.LobbyContext.Players.Where(x => juggledAccounts.ContainsKey(x.LobbyID))) manuallyPrefered[u.LobbyID] = grp.Key;

                // make bins from existing battles that are not running and have some players
                foreach (var ah in grp.Where(x => x.RunningGameStartContext == null && x.LobbyContext.Players.Any(y => !y.IsSpectator)))
                {
                    var bin = new Bin(ah);
                    var toAdd =
                        ah.LobbyContext.Players.Where(x => !x.IsSpectator && juggledAccounts.ContainsKey(x.LobbyID)).Select(x => x.LobbyID).ToList();
                    bin.ManuallyJoined.AddRange(toAdd);
                    groupBins.Add(bin);
                }

                if (groupBins.Count == 0)
                {
                    // no bins with players found, add empty one
                    var firstEmpty = grp.FirstOrDefault(x => x.RunningGameStartContext == null && x.LobbyContext.Players.All(y => y.IsSpectator));
                    if (firstEmpty != null)
                    {
                        var bin = new Bin(firstEmpty);
                        groupBins.Add(bin);
                    }
                }

                // remove all but biggest below merge limit
                var biggest = groupBins.OrderByDescending(x => x.ManuallyJoined.Count).FirstOrDefault();
                groupBins.RemoveAll(x => x != biggest && x.ManuallyJoined.Count < (x.Config.MergeSmallerThan ?? 0));

                bins.AddRange(groupBins);
            }

            /*
            // those who manually joined a game and have preference set to never -> upgrade prefernece to ok
            foreach (var kvp in manuallyPrefered) {
                var acc = juggledAccounts[kvp.Key];
                if (acc.Preferences[kvp.Value] <= GamePreference.Never) {
                   acc.Preferences[kvp.Value] =  GamePreference.Ok;
                   acc.SetPreferences(acc.Preferences);
                   tas.Extensions.PublishPlayerJugglerConfig(new ProtocolExtension.JugglerConfig(acc), acc.Name );
                }
            }
            db.SubmitChanges();
            */


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

            ret.PlayerMoves = new List<JugglerMove>();

            if (bins.Any())
            {
                SplitBins(autohosts, juggledAccounts, sb, bins);
                sb.AppendLine("After split:");
                PrintBins(juggledAccounts, bins, sb);

                
                foreach (var b in bins)
                {
                    foreach (var a in b.Assigned)
                    {
                        var acc = juggledAccounts[a];
                        var origAh = autohosts.FirstOrDefault(x => x.LobbyContext.Players.Any(y => y.Name == acc.Name));
                        if (origAh == null || origAh.LobbyContext.AutohostName != b.Autohost.LobbyContext.AutohostName)
                        {
                            ret.PlayerMoves.Add(new JugglerMove() { Name = acc.Name, TargetAutohost = b.Autohost.LobbyContext.AutohostName, OriginalAutohost = origAh != null ?  origAh.LobbyContext.AutohostName:null});
                            tas.ForceJoinBattle(acc.Name,  b.Autohost.LobbyContext.AutohostName);
                        }
                    }
                }

                /*ret.AutohostsToClose = new List<string>();
                foreach (var ah in
                    autohosts.Where(
                        x => x.RunningGameStartContext == null && !bins.Any(y => y.Autohost == x) && x.LobbyContext.Players.Any(y => !y.IsSpectator))) ret.AutohostsToClose.Add(ah.LobbyContext.AutohostName);*/
            }

            ret.Message = sb.ToString();

            tas.Say(TasClient.SayPlace.Channel, "juggler", ret.Message, false);

            LastPlayerMoves = new List<JugglerMove>(ret.PlayerMoves);

            foreach (var entry in LastPlayerMoves) {
                tas.Say(TasClient.SayPlace.Channel, "juggler", string.Format("{0}: {1}->{2}", entry.Name, entry.OriginalAutohost, entry.TargetAutohost), false);
            }



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
                if (b.Config.DontMoveManuallyJoined == true) b.Assigned = new List<int>(b.ManuallyJoined);
                else
                {
                    b.Assigned.Clear();
                    foreach (var id in b.ManuallyJoined) b.Assigned.Add(id);
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
                    var battlePref = a.Value.MatchMakingActive ? (double)a.Value.Preferences[b.Mode] : (double)GamePreference.Never;
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
                            if (b.Config.MaxEloDifference != null && b.ManuallyJoined.Count >= 1)
                            {
                                var avgElo = b.ManuallyJoined.Average(x => juggledAccounts[x].EffectiveElo);
                                if (Math.Abs(a.Value.EffectiveElo - avgElo) > b.Config.MaxEloDifference) continue; //effective elo difference > 250 dont try to combine
                            }

                            if (battlePref > (double)GamePreference.Never) b.PlayerPriority[lobbyID] = (int)battlePref;
                        }
                    }
                }
            }

            var state = new ProtocolExtension.JugglerState();
            state.TotalPlayers = juggledAccounts.Count;
            foreach (var grp in
                bins.GroupBy(x => x.Mode).Select(
                    x => new { Mode = x.Key, Count = x.Sum(y => y.PlayerPriority.Count(z => z.Value > (double)GamePreference.Never)) })) state.ModeCounts.Add(new ProtocolExtension.JugglerState.ModePair() { Mode = grp.Mode, Count = grp.Count });

            Global.Nightwatch.Tas.Extensions.PublishJugglerState(state);
        }

        static void SplitBins(List<JugglerAutohost> autohosts, Dictionary<int, Account> juggledAccounts, StringBuilder sb, List<Bin> bins)
        {
            // split too big bins -> move top players to another autohost
            foreach (var b in new List<Bin>(bins))
            {
                if (b.Assigned.Count > (b.Config.SplitBiggerThan ?? 99))
                {
                    sb.AppendLine("Splitting " + b.Autohost.LobbyContext.AutohostName);
                    var splitTo =
                        autohosts.FirstOrDefault(x => x.LobbyContext.GetMode() == b.Mode && x.RunningGameStartContext == null && x != b.Autohost);
                    //find first one that isnt running and isnt bin -> no players for it planned
                    if (splitTo != null)
                    {
                        sb.AppendLine("Splitting to " + splitTo.LobbyContext.AutohostName);
                        var target = new Bin(splitTo);
                        bins.Add(target);

                        int moved = 0;
                        int toMove = b.Assigned.Count / 2;

                        // split while keeping clan groups together

                        foreach (var clanGrp in b.Assigned.Select(x => juggledAccounts[x]).Where(CanMove).GroupBy(x => x.ClanID ?? x.LobbyID).OrderByDescending(x => x.Average(y => y.EffectiveElo))) {
                            target.Assigned.AddRange(clanGrp.Select(x=>x.LobbyID??0));
                            b.Assigned.RemoveAll(x => clanGrp.Any(y => y.LobbyID == x));
                            moved += clanGrp.Count();
                            if (moved >= toMove) break;
                        }
                    }
                }
            }
        }


        public class Bin
        {
            public List<int> Assigned = new List<int>();
            public JugglerAutohost Autohost;
            public AutohostConfig Config;
            public List<int> ManuallyJoined = new List<int>();
            public AutohostMode Mode;
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
    }

    public class JugglerMove
    {
        public string Name;
        public string TargetAutohost;
        public string OriginalAutohost;
    }

    public class JugglerResult
    {
        public List<string> AutohostsToClose;
        public string Message;
        public List<JugglerMove> PlayerMoves;
    }
}