using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                                                          AutohostMode.LowSkill,
                                                          AutohostMode.HighSkill,
                                                          AutohostMode.Planetwars,
                                                          AutohostMode.SmallTeams,
                                                          AutohostMode.Teams,
                                                          AutohostMode.GameFFA,
                                                          AutohostMode.GameChickens,
                                                          AutohostMode.Game1v1,
                                                      };
        public static bool SuppressJuggler = false;

        static PlayerJuggler() {
            var tas = Global.Nightwatch.Tas;

            tas.Extensions.JugglerConfigReceived += (args, config) =>
                {
                    if (args.UserName != GlobalConst.NightwatchName) {
                        Task.Factory.StartNew(() =>
                            {
                                try {
                                    using (var db = new ZkDataContext()) {
                                        var acc = Account.AccountByName(db, args.UserName);
                                        acc.MatchMakingActive = config.Active;
                                        var prefs = acc.Preferences;
                                        foreach (var item in config.Preferences) prefs[item.Mode] = item.Preference;
                                        acc.SetPreferences(prefs);
                                        db.SubmitAndMergeChanges();
                                        SendAccountConfig(acc);
                                        if (config.Active) tas.Say(TasClient.SayPlace.User, "Springie", "!juggle", false); // todo not very nice way to do that
                                    }
                                } catch (Exception ex) {
                                    Trace.TraceError(ex.ToString());
                                }
                            },
                                              TaskCreationOptions.LongRunning);
                    }
                };

            tas.ChannelUserAdded += (sender, args) =>
                {
                    var name = args.ServerParams[1];
                    var chan = args.ServerParams[0];
                    if (chan == ProtocolExtension.ExtensionChannelName && name != GlobalConst.NightwatchName) {
                        Task.Factory.StartNew(() =>
                            {
                                try {
                                    using (var db = new ZkDataContext()) {
                                        var acc = Account.AccountByName(db, name);
                                        SendAccountConfig(acc);
                                    }
                                } catch (Exception ex) {
                                    Trace.TraceError(ex.ToString());
                                }
                            },
                                              TaskCreationOptions.LongRunning);
                    }
                };

            // if player goes AFK disable his juggler
            tas.UserStatusChanged += (sender, args) =>
                {
                    var user = tas.ExistingUsers[args.ServerParams[0]];
                    if ((user.IsAway && DateTime.Now.Subtract(user.AwaySince ?? DateTime.MinValue).TotalMinutes > 30)) {
                        var conf = tas.Extensions.GetPublishedConfig(user.Name);
                        if (conf != null && conf.Active) {
                            using (var db = new ZkDataContext()) {
                                var acc = Account.AccountByName(db, user.Name);
                                acc.MatchMakingActive = false;
                                db.SubmitAndMergeChanges();
                                SendAccountConfig(acc);
                            }
                        }
                    }
                };

            tas.LoginAccepted += (sender, args) => { tas.JoinChannel("juggler"); };

            tas.JoinChannel("juggler");
        }


        public static JugglerResult JugglePlayers(List<JugglerAutohost> autohosts) {
            var ret = new JugglerResult();
            if (SuppressJuggler) return ret; // supressed dont do anything

            var bins = new List<Bin>();
            var db = new ZkDataContext();
            var sb = new StringBuilder();

            var tas = Global.Nightwatch.Tas;

            //only non passworded battles
            autohosts =
                autohosts.Where(
                    x => !tas.ExistingBattles.Values.Any(y => y.Founder.Name == x.LobbyContext.AutohostName && (y.IsPassworded || y.IsLocked))) //  
                         .ToList();


            List<int?> juggledLobbyIDs = new List<int?>();
            foreach (var u in tas.ExistingUsers.Values) {
               // if (u.IsInGame) continue; // dont move ingame 
                var battle = tas.ExistingBattles.Values.FirstOrDefault(y => y.Users.Any(z => z.Name == u.Name));
                if (battle != null) {
                    if (battle.IsLocked) continue; // dont move away from locked
                }
                juggledLobbyIDs.Add(u.LobbyID);
            }

            foreach (var ah in autohosts) {
                // remove those known to be playing actively
                if (ah.RunningGameStartContext != null) foreach (var id in ah.RunningGameStartContext.Players.Where(x => !x.IsSpectator && x.IsIngame).Select(x => x.LobbyID)) juggledLobbyIDs.Remove(id);
                //if (ah.LobbyContext != null) foreach (int id in ah.LobbyContext.Players.Where(x => x.IsIngame).Select(x => x.LobbyID)) juggledLobbyIDs.Remove(id);
            }

            var existing = tas.ExistingUsers.Values.Select(y => (int?)y.LobbyID).ToList();
            var allAccounts = db.Accounts.Where(x => existing.Contains(x.LobbyID)).GroupBy(x=>x.LobbyID ?? 0).ToDictionary(x => x.Key, x=>x.First());
            var juggledAccounts = db.Accounts.Where(x => juggledLobbyIDs.Contains(x.LobbyID) && x.MatchMakingActive).GroupBy(x=>x.LobbyID??0).ToDictionary(x => x.Key, x=>x.First());

            // make bins from non-running games with players by each type
            foreach (var grp in
                autohosts.Where(x => x.LobbyContext != null).GroupBy(x => x.LobbyContext.GetMode())) {
                var groupBins = new List<Bin>();
                // make bins from existing battles that are not running and have some players
                foreach (var ah in grp.Where(x => x.RunningGameStartContext == null && x.LobbyContext.Players.Any(y => !y.IsSpectator))) {
                    var bin = new Bin(ah);
                    var toAdd =
                        ah.LobbyContext.Players.Where(x => !x.IsSpectator && allAccounts.ContainsKey(x.LobbyID)).Select(x => x.LobbyID).ToList();
                    bin.ManuallyJoined.AddRange(toAdd);
                    groupBins.Add(bin);
                }

                if (groupBins.Count == 0 ||
                    groupBins.All(
                        x => x.Config.MaxToJuggle != null && x.Autohost.LobbyContext.Players.Count(y => !y.IsSpectator) >= x.Config.MaxToJuggle)) {
                    // no bins with players found or all full, add empty one
                    var firstEmpty = grp.FirstOrDefault(x => x.RunningGameStartContext == null && x.LobbyContext.Players.All(y => y.IsSpectator));
                    if (firstEmpty != null) {
                        var bin = new Bin(firstEmpty);
                        groupBins.Add(bin);
                    }
                }

                bins.AddRange(groupBins);
            }

            SetPriorities(bins, juggledAccounts, autohosts, allAccounts);

            sb.AppendLine("Original bins:");
            PrintBins(allAccounts, bins, sb);

            /*
            foreach (Bin b in bins.Where(x => x.Config.MinToJuggle > x.PlayerPriority.Count + x.ManuallyJoined.Count).ToList()) bins.Remove(b); // remove those that cant be possible handled
            sb.AppendLine("First purge:");
            PrintBins(allAccounts, bins, sb);
            */

            Bin todel = null;
            do {
                foreach (var b in bins) b.Assigned = new List<int>(b.ManuallyJoined);
                var priority = double.MaxValue;

                do {
                    var newPriority =
                        bins.SelectMany(x => x.PlayerPriority.Values)
                            .Where(x => x < priority)
                            .Select(x => (double?)x)
                            .OrderByDescending(x => x)
                            .FirstOrDefault();
                    if (newPriority == null) break; // no more priority to chec;
                    priority = newPriority.Value;

                    var moved = false;
                    do {
                        //one priority pass
                        moved = false;
                        foreach (var b in bins.OrderBy(x => BinOrder.IndexOf(x.Mode))) {
                            if (b.Config.MaxToJuggle != null && b.Assigned.Count >= b.Config.MaxToJuggle) continue;

                            var binElo = b.Assigned.Average(x => (double?)allAccounts[x].EffectiveElo);
                            var persons = b.PlayerPriority.Where(x => !b.Assigned.Contains(x.Key) && x.Value == priority).Select(x => x.Key).ToList();

                            if (b.Config.MaxEloDifference != null && binElo != null) persons.RemoveAll(x => Math.Abs(allAccounts[x].EffectiveElo - (binElo ?? 0)) > b.Config.MaxEloDifference);

                            if (binElo != null) persons = persons.OrderByDescending(x => Math.Abs(allAccounts[x].EffectiveElo - binElo.Value)).ToList();

                            foreach (var person in persons) {
                                var acc = allAccounts[person];
                                var current = bins.FirstOrDefault(x => x.Assigned.Contains(person));

                                var canMove = false;

                                if (current == null) canMove = true;
                                else {
                                    if (current != b) {
                                        if (acc.Preferences[current.Mode] < acc.Preferences[b.Mode]) canMove = true;
                                        if (acc.Preferences[current.Mode] == acc.Preferences[b.Mode]) if (b.Assigned.Count < b.Config.MinToJuggle && current.Assigned.Count >= current.Config.MinToJuggle + 1) canMove = true;
                                    }
                                }

                                if (canMove) {
                                    Move(bins, person, b);
                                    moved = true;
                                    break;
                                }
                            }
                        }
                    } while (moved);
                } while (true);

                // find first bin that cannot be started due to lack of people or first bin that can be merged into other bin and delete it
                todel =
                    bins.OrderBy(x => BinOrder.IndexOf(x.Mode))
                        .FirstOrDefault(
                            x =>
                            x.Assigned.Count < x.Config.MinToJuggle ||
                            (x.ManuallyJoined.Count < (x.Config.MergeSmallerThan ?? 0) && bins.Any(y => y != x && y.Mode == x.Mode)));

                if (todel != null) {
                    bins.Remove(todel);
                    MergeMauallyJoined(bins, todel, allAccounts);
                    sb.AppendLine("removing bin " + todel.Mode);
                    PrintBins(allAccounts, bins, sb);
                }
            } while (todel != null);

            sb.AppendLine("Final bins:");
            PrintBins(allAccounts, bins, sb);

            ret.PlayerMoves = new List<JugglerMove>();

            if (bins.Any()) {
                foreach (var b in bins) {
                    foreach (var a in b.Assigned) {
                        var acc = allAccounts[a];
                        var origAh = autohosts.FirstOrDefault(x => x.LobbyContext.Players.Any(y => y.Name == acc.Name && !y.IsSpectator));
                        if (origAh == null || origAh.LobbyContext.AutohostName != b.Autohost.LobbyContext.AutohostName) {
                            ret.PlayerMoves.Add(new JugglerMove
                                                {
                                                    Name = acc.Name,
                                                    TargetAutohost = b.Autohost.LobbyContext.AutohostName,
                                                    OriginalAutohost = origAh != null ? origAh.LobbyContext.AutohostName : null
                                                });
                            tas.ForceJoinBattle(acc.Name, b.Autohost.LobbyContext.AutohostName);
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

            return ret;
        }

        public static void SendAccountConfig(Account acc) {
            Global.Nightwatch.Tas.Extensions.PublishPlayerJugglerConfig(new ProtocolExtension.JugglerConfig(acc), acc.Name);
        }

        static void MergeMauallyJoined(List<Bin> bins, Bin deletedBin, Dictionary<int, Account> allAccounts) {
            foreach (var b in bins.Where(x => x.Mode == deletedBin.Mode)) {
                // find other bins instead of deleted
                foreach (var i in deletedBin.ManuallyJoined) {
                    // for each manually joined in deleted battle
                    if (!b.PlayerPriority.ContainsKey(i)) {
                        var acc = allAccounts[i];

                        // dont queue those who cannot join high/low skill host
                        if (b.Config.MinLevel != null && acc.Level < b.Config.MinLevel) continue;
                        if (b.Config.MaxLevel != null && acc.Level > b.Config.MaxLevel) continue;
                        if (b.Config.MinElo != null && acc.EffectiveElo < b.Config.MinElo) continue;
                        if (b.Config.MaxElo != null && acc.EffectiveElo > b.Config.MaxElo) continue;

                        if (b.Config.MaxToJuggle != null && b.ManuallyJoined.Count() >= b.Config.MaxToJuggle) continue; // full 1v1
                        if (b.Config.MaxEloDifference != null && b.ManuallyJoined.Count >= 1) {
                            var avgElo = b.ManuallyJoined.Average(x => allAccounts[x].EffectiveElo);
                            if (Math.Abs(acc.EffectiveElo - avgElo) > b.Config.MaxEloDifference) continue; //effective elo difference > 250 dont try to combine
                        }
                        b.PlayerPriority.Add(i, (double)GamePreference.Best); // add him to battle if he can play there 
                    }
                    else b.PlayerPriority[i] = (double)GamePreference.Best; // this case should not happen
                }
            }
        }


        static void Move(List<Bin> bins, int lobbyID, Bin target) {
            foreach (var b in bins) b.Assigned.Remove(lobbyID);

            if (!target.Assigned.Contains(lobbyID)) target.Assigned.Add(lobbyID);
        }

        static void PrintBins(Dictionary<int, Account> allAccounts, List<Bin> bins, StringBuilder sb) {
            foreach (var b in bins) {
                sb.AppendFormat("{0} {1}: {2}    - ({3})\n",
                                b.Mode,
                                b.Autohost.LobbyContext.AutohostName,
                                string.Join(",", b.Assigned.Select(x => allAccounts[x].Name)),
                                string.Join(",",
                                            b.PlayerPriority.OrderByDescending(x => x.Value)
                                             .Select(x => string.Format("{0}:{1}", allAccounts[x.Key].Name, x.Value))));
            }
        }

        static void ResetAssigned(List<Bin> bins) {
            foreach (var b in bins) b.Assigned = new List<int>(b.ManuallyJoined);
        }

        static void SetPriorities(List<Bin> bins,
                                  Dictionary<int, Account> juggledAccounts,
                                  List<JugglerAutohost> autohosts,
                                  Dictionary<int, Account> allAccounts) {
            foreach (var b in bins) {
                b.PlayerPriority.Clear();

                foreach (var a in juggledAccounts) {
                    var lobbyID = a.Key;
                    var battlePref = a.Value.MatchMakingActive ? (double)a.Value.Preferences[b.Mode] : (double)GamePreference.Never;
                    AutohostMode manualPref;

                    if (b.Config.MinLevel != null && a.Value.Level < b.Config.MinLevel) continue; // dont queue who cannot join PW
                    if (b.Config.MinElo != null && a.Value.EffectiveElo < b.Config.MinElo) continue; // dont queue those who cannot join high skill host

                    if (b.ManuallyJoined.Contains(lobbyID)) // was he there already
                        b.PlayerPriority[lobbyID] = battlePref; // player joined it already
                    else {
                        if (b.Config.MaxToJuggle != null && b.ManuallyJoined.Count() >= b.Config.MaxToJuggle) continue; // full 1v1
                        if (b.Config.MaxEloDifference != null && b.ManuallyJoined.Count >= 1) {
                            var avgElo = b.ManuallyJoined.Average(x => allAccounts[x].EffectiveElo);
                            if (Math.Abs(a.Value.EffectiveElo - avgElo) > b.Config.MaxEloDifference) continue; //effective elo difference > 250 dont try to combine
                        }

                        if (battlePref > (double)GamePreference.Never) b.PlayerPriority[lobbyID] = (int)battlePref;
                    }
                }
            }

            var state = new ProtocolExtension.JugglerState();
            state.TotalPlayers = juggledAccounts.Count;
            foreach (AutohostMode mode in Enum.GetValues(typeof(AutohostMode))) {
                var ingame =
                    autohosts.Where(x => x.RunningGameStartContext != null && x.LobbyContext.GetConfig().AutohostMode == mode)
                             .Sum(x => (int?)x.RunningGameStartContext.Players.Count(y => !y.IsSpectator)) ?? 0;
                var waitingJugglers =
                    bins.Where(x => x.Mode == mode)
                        .SelectMany(x => x.PlayerPriority)
                        .Where(x => x.Value > (double)GamePreference.Never)
                        .Select(x => x.Key);
                var waitingManual = bins.Where(x => x.Mode == mode).SelectMany(x => x.ManuallyJoined);
                state.TotalPlayers += waitingManual.Count();
                state.ModeCounts.Add(new ProtocolExtension.JugglerState.ModePair
                                     {
                                         Mode = mode,
                                         Count = waitingJugglers.Union(waitingManual).Distinct().Count(),
                                         Playing = ingame
                                     });
            }

            Global.Nightwatch.Tas.Extensions.PublishJugglerState(state);
        }


        public class Bin
        {
            public List<int> Assigned = new List<int>();
            public JugglerAutohost Autohost;
            public AutohostConfig Config;
            public List<int> ManuallyJoined = new List<int>();
            public AutohostMode Mode;
            public Dictionary<int, double> PlayerPriority = new Dictionary<int, double>();

            public Bin(JugglerAutohost autohost) {
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
        public string OriginalAutohost;
        public string TargetAutohost;
    }

    public class JugglerResult
    {
        public List<string> AutohostsToClose;
        public string Message;
        public List<JugglerMove> PlayerMoves;
    }
}