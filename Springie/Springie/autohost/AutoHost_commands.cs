#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LobbyClient;
using PlasmaShared.ContentService;
using PlasmaShared.SpringieInterfaceReference;
using AutohostMode = PlasmaShared.SpringieInterfaceReference.AutohostMode;

#endregion

namespace Springie.autohost
{
    public partial class AutoHost
    {
        const int MaxMapListLength = 400;

        readonly List<string> toNotify = new List<string>();

        public bool AllReadyAndSynced(out List<string> usname)
        {
            usname = new List<string>();
            foreach (var p in tas.MyBattle.Users)
            {
                if (p.IsSpectator) continue;
                if (p.SyncStatus != SyncStatuses.Synced) usname.Add(p.Name);
            }
            return usname.Count == 0;
        }

        public bool AllUniqueTeams(out List<string> username)
        {
            var teams = new List<int>();
            username = new List<string>();
            foreach (var p in tas.MyBattle.Users)
            {
                if (p.IsSpectator) continue;
                if (teams.Contains(p.TeamNumber)) username.Add(p.Name);
                else teams.Add(p.TeamNumber);
            }
            return username.Count == 0;
        }


        public void BalanceTeams(int teamCount, bool clanwise)
        {
            try
            {
                var b = tas.MyBattle;

                if (hostedMod.IsMission)
                {
                    var freeSlots = GetFreeSlots();
                    foreach (var u in b.Users.Where(x => !x.IsSpectator).ToList())
                    {
                        var curSlot = hostedMod.MissionSlots.FirstOrDefault(x => x.IsHuman && x.TeamID == u.TeamNumber && x.AllyID == u.AllyNumber);
                        if (curSlot != null && curSlot.IsRequired)
                        {
                            if (u.TeamColor != curSlot.Color) tas.ForceColor(u.Name, curSlot.Color);
                        }
                        else
                        {
                            var slot = freeSlots.FirstOrDefault();
                            if (slot == null)
                            {
                                if (curSlot == null) tas.ForceSpectator(u.Name);
                            }
                            else if (slot.IsRequired || curSlot == null)
                            {
                                tas.ForceAlly(u.Name, slot.AllyID);
                                tas.ForceTeam(u.Name, slot.TeamID);
                                tas.ForceColor(u.Name, slot.Color);
                                freeSlots = freeSlots.Skip(1);
                            }
                        }
                    }

                    // remove extra bots 
                    foreach (var bot in b.Bots.Where(x => x.owner != tas.UserName)) tas.RemoveBot(bot.Name);
                    return;
                }

                var ranker = new List<UsRank>();
                foreach (var u in b.Users) if (!u.IsSpectator) ranker.Add(new UsRank(ranker.Count, u.LobbyUser.EffectiveElo, clanwise ? (u.LobbyUser.Clan ?? "") : "", u));
                var totalPlayers = ranker.Count;

                var rand = new Random();

                if (teamCount < 1) teamCount = 1;
                if (teamCount > ranker.Count) teamCount = ranker.Count;

                var teamUsers = new List<UsRank>[teamCount];
                for (var i = 0; i < teamUsers.Length; ++i) teamUsers[i] = new List<UsRank>();
                var teamSums = new double[teamCount];

                var teamClans = new List<string>[teamCount];
                for (var i = 0; i < teamClans.Length; ++i) teamClans[i] = new List<string>();

                var clans = "";
                // remove clans that have less than 2 members - those are irelevant
                foreach (var u in ranker)
                {
                    if (u.Clan != "")
                    {
                        if (ranker.FindAll(delegate(UsRank x) { return x.Clan == u.Clan; }).Count < 2) u.Clan = "";
                        else clans += u.Clan + ", ";
                    }
                }
                if (clans != "") SayBattle("those clan are being balanced: " + clans);

                // this cycle performs actual user adding to teams
                var cnt = 0;
                while (ranker.Count > 0)
                {
                    var minsum = Double.MaxValue;
                    var minid = 0;
                    for (var i = 0; i < teamCount; ++i)
                    {
                        var l = teamUsers[i];
                        // pick only current "row" and find the one with least sum
                        if (l.Count == cnt/teamCount)
                        {
                            if (teamSums[i] < minsum)
                            {
                                minid = i;
                                minsum = teamSums[i];
                            }
                        }
                    }

                    var candidates = new List<UsRank>();

                    // get list of clans assigned to other teams
                    var assignedClans = new List<string>();
                    for (var i = 0; i < teamClans.Length; ++i) if (i != minid) assignedClans.AddRange(teamClans[i]);

                    // first try to get some with same clan
                    if (teamClans[minid].Count > 0) candidates.AddRange(ranker.Where(x => x.Clan != "" && teamClans[minid].Contains(x.Clan)));

                    // we dont have any candidates try to get clanner from unassigned clan
                    if (candidates.Count == 0) candidates.AddRange(ranker.Where(x => x.Clan != "" && !assignedClans.Contains(x.Clan)));

                    // we still dont have any candidates try to get anyone
                    if (candidates.Count == 0) candidates.AddRange(ranker);

                    var maxElo = Double.MinValue;
                    var maxUsers = new List<UsRank>();
                    // get candidate which increases team elo most (round elo to tens to add some randomness)
                    foreach (var c in candidates)
                    {
                        var newElo = ((teamUsers[minid].Sum(x => x.Elo) + Math.Round(c.Elo/10)*10))/(teamUsers.Count() + 1);
                        if (newElo > maxElo)
                        {
                            maxUsers.Clear();
                            maxUsers.Add(c);
                            maxElo = newElo;
                        }
                        else if (newElo == maxElo) maxUsers.Add(c);
                    }
                    var pickedUser = maxUsers[rand.Next(maxUsers.Count)];

                    teamUsers[minid].Add(pickedUser);
                    teamSums[minid] = maxElo;

                    if (pickedUser.Clan != "")
                    {
                        // if we work with clans add user's clan to clan list for his team
                        if (!teamClans[minid].Contains(pickedUser.Clan)) teamClans[minid].Add(pickedUser.Clan);
                    }

                    ranker.Remove(pickedUser);

                    cnt++;
                }

                // alliances for allinace permutations
                var allys = new List<int>();
                for (var i = 0; i < teamCount; ++i) allys.Add(i);

                var t = "";

                for (var i = 0; i < teamCount; ++i)
                {
                    // permute one alliance
                    var rdindex = rand.Next(allys.Count);
                    var allynum = allys[rdindex];
                    allys.RemoveAt(rdindex);

                    if (teamUsers[i].Count > 0)
                    {
                        if (i > 0) t += ":";
                        t += (allynum + 1) + "=" + Math.Round(teamSums[i]);
                    }

                    foreach (var u in teamUsers[i]) tas.ForceAlly(u.User.Name, allynum);
                }

                t += ")";

                SayBattle(String.Format("{0} players balanced {2} to {1} teams (ratings {3}",
                                        totalPlayers,
                                        teamCount,
                                        clanwise ? "respecting clans" : "",
                                        t));
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleException(ex, "Error balancing teams");
            }
        }



        public bool BalancedTeams(out int allyno, out int alliances)
        {
            if (hostedMod.IsMission)
            {
                alliances = 0;
                allyno = 0;
                var invalidUser =
                    tas.MyBattle.Users.FirstOrDefault(
                        x => !x.IsSpectator && !hostedMod.MissionSlots.Any(y => y.IsHuman && y.TeamID == x.TeamNumber && y.AllyID == x.AllyNumber));
                if (invalidUser != null)
                {
                    SayBattle(String.Format("User {0} is not in proper mission slot", invalidUser.Name));
                    return false;
                }

                var slot = GetFreeSlots().FirstOrDefault();
                if (slot == null || !slot.IsRequired) return true;
                else
                {
                    SayBattle(String.Format("Mission slot {0}/{1} (team {2}, id {3}) needs player",
                                            slot.AllyName,
                                            slot.TeamName,
                                            slot.AllyID,
                                            slot.TeamID));
                    allyno = slot.AllyID;
                    return false;
                }
            }

            var counts = new int[16];
            allyno = 0;

            foreach (var p in tas.MyBattle.Users)
            {
                if (p.IsSpectator) continue;
                counts[p.AllyNumber]++;
            }

            alliances = counts.Count(x => x > 0);

            var tsize = 0;
            for (var i = 0; i < counts.Length; ++i)
            {
                if (counts[i] != 0)
                {
                    if (tsize == 0) tsize = counts[i];
                    else if (tsize != counts[i])
                    {
                        allyno = i;
                        return false;
                    }
                }
            }
            return true;
        }

        DateTime lockedUntil = DateTime.MinValue;

        public void ComLock(TasSayEventArgs e, string[] words)
        {
            if (words != null && words.Length == 1) {
                int timer;
                if (int.TryParse(words[0], out timer)) {
                    if (timer < 0) timer = 0;
                    if (timer > 120) timer = 120;
                    lockedUntil = DateTime.UtcNow.AddSeconds(timer);
                }
            }
            tas.ChangeLock(true);
        }

        public void ComUnlock(TasSayEventArgs e, string[] words)
        {
            if (DateTime.UtcNow < lockedUntil) Respond(e, string.Format("Lock is timed, wait {0} seconds", (int)lockedUntil.Subtract(DateTime.UtcNow).TotalSeconds));
            else {
                tas.ChangeLock(false);
                lockedUntil = DateTime.MinValue;
            }
        }


        public void ComAddBox(TasSayEventArgs e, string[] words)
        {
            if (words.Length < 4)
            {
                Respond(e, "This command needs at least 4 parameters");
                return;
            }
            int x, y, w, h;
            if (!Int32.TryParse(words[0], out x) || !Int32.TryParse(words[1], out y) || !Int32.TryParse(words[2], out w) ||
                !Int32.TryParse(words[3], out h))
            {
                Respond(e, "All parameters must be numbers");
                return;
            }
            var numrect = 0;
            if (words.Length > 4) Int32.TryParse(words[4], out numrect);

            if (numrect == 0)
            {
                numrect = tas.MyBattle.GetFirstEmptyRectangle();
                if (numrect == -1)
                {
                    Respond(e, "Cannot add more boxes");
                    return;
                }
                numrect++;
            }
            tas.AddBattleRectangle(numrect - 1, new BattleRect(x*2, y*2, (x + w)*2, (y + h)*2));
        }

        public void ComAlly(TasSayEventArgs e, string[] words)
        {
            if (words.Length < 2)
            {
                Respond(e, "this command needs 2 parameters (ally number and player name)");
                return;
            }
            var allyno = 0;
            if (!Int32.TryParse(words[0], out allyno) || --allyno < 0 || allyno >= Spring.MaxAllies)
            {
                Respond(e, "invalid ally number");
                return;
            }
            string[] usrs;
            int[] idx;
            if (FilterUsers(Utils.ShiftArray(words, -1), out usrs, out idx) == 0) Respond(e, "no such player found");
            else
            {
                SayBattle("Forcing " + usrs[0] + " to alliance " + (allyno + 1));
                tas.ForceAlly(usrs[0], allyno);
            }
        }


        public void ComBalance(TasSayEventArgs e, string[] words)
        {
            var teamCount = 0;
            if (words.Length > 0) Int32.TryParse(words[0], out teamCount);

            if (SpawnConfig == null) RunServerBalance(false, teamCount == 0 ? (int?)null : teamCount, false);
            else
            {
                if (teamCount == 0) teamCount = 2;
                ComFix(e, words);
                BalanceTeams(teamCount, false);
            }
        }

        public void ComBoss(TasSayEventArgs e, string[] words)
        {
            if (words.Length == 0)
            {
                if (bossName == "")
                {
                    Respond(e, "there is currently no active boss");
                    return;
                }
                SayBattle("boss " + bossName + " removed");
                bossName = "";
                return;
            }
            else
            {
                string[] usrs;
                int[] idx;
                if (FilterUsers(words, out usrs, out idx) == 0)
                {
                    Respond(e, "no such player found");
                    return;
                }
                
                if (usrs[0] == tas.UserName)
                {
                    Respond(e, "you flatter me, but no");
                    return;
                }

                SayBattle("New boss is " + usrs[0]);
                bossName = usrs[0];
            }
        }

        public void ComCBalance(TasSayEventArgs e, string[] words)
        {
            var teamCount = 2;
            if (words.Length > 0) Int32.TryParse(words[0], out teamCount);
            else teamCount = 2;
            if (SpawnConfig == null) RunServerBalance(false, teamCount, true);
            else
            {
                ComFix(e, words);
                BalanceTeams(teamCount, true);
            }
        }

        public void ComClearBox(TasSayEventArgs e, string[] words)
        {
            if (words.Length == 0) foreach (var i in tas.MyBattle.Rectangles.Keys.ToList()) tas.RemoveBattleRectangle(i);
            else
            {
                var numrect = 0;
                if (!Int32.TryParse(words[0], out numrect)) Respond(e, "paramater must by a number of rectangle");
                tas.RemoveBattleRectangle(numrect - 1);
            }
        }

        public void ComCorners(TasSayEventArgs e, string[] words)
        {
            if (words.Length != 2)
            {
                Respond(e, "This command needs 2 parameters");
                return;
            }
            if (words[0] != "a" && words[0] != "b") Respond(e, "first parameter must be 'a' or 'b'");
            else
            {
                int perc;
                Int32.TryParse(words[1], out perc);
                if (perc < 0 || perc > 50) Respond(e, "second parameter must be between 0 and 50");
                else
                {
                    var p = perc/100.0;
                    if (words[0] == "a")
                    {
                        tas.AddBattleRectangle(0, new BattleRect(0, 0, p, p));
                        tas.AddBattleRectangle(1, new BattleRect(1 - p, 1 - p, 1, 1));
                        tas.AddBattleRectangle(2, new BattleRect(1 - p, 0, 1, p));
                        tas.AddBattleRectangle(3, new BattleRect(0, 1 - p, p, 1));
                    }
                    else
                    {
                        tas.AddBattleRectangle(0, new BattleRect(1 - p, 0, 1, p));
                        tas.AddBattleRectangle(1, new BattleRect(0, 1 - p, p, 1));
                        tas.AddBattleRectangle(2, new BattleRect(0, 0, p, p));
                        tas.AddBattleRectangle(3, new BattleRect(1 - p, 1 - p, 1, 1));
                    }
                }
            }
        }


        public void ComExit(TasSayEventArgs e, string[] words)
        {
            if (spring.IsRunning) SayBattle("exiting game");
            else Respond(e, "cannot exit, not in game");
            spring.ExitGame();
        }


        /// <summary>
        /// fixes ids
        /// </summary>
        /// <param name="e"></param>
        /// <param name="words">if param is "silent" does not advertise id fixing</param>
        /// <returns>true if id teams were already fixed</returns>
        public bool ComFix(TasSayEventArgs e, params string[] words)
        {
            var b = tas.MyBattle;
            var groups =
                b.Users.Where(x => !x.IsSpectator && x.SyncStatus != SyncStatuses.Unknown).GroupBy(x => x.TeamNumber).Where(g => g.Count() > 1);
            if (groups.Count() > 0)
            {
                var id = 0;
                foreach (var u in b.Users.Where(x => !x.IsSpectator && x.SyncStatus != SyncStatuses.Unknown)) tas.ForceTeam(u.Name, id++);
                if (words == null || words.Length == 0 || words[0] != "silent") SayBattle("team numbers fixed");
                return false;
            }
            else return true;
        }


        public void ComFixColors(TasSayEventArgs e, string[] words)
        {
            var cols = new List<MyCol>();

            if (hostedMod.IsMission)
            {
                ForceMissionColors();
                return;
            }

            var b = tas.MyBattle;
            foreach (var u in b.Users) if (!u.IsSpectator) cols.Add((MyCol)u.TeamColor);
            var arcols = cols.ToArray();

            MyCol.FixColors(arcols, 30000);

            var changed = false;
            var cnt = 0;
            foreach (var u in b.Users)
            {
                if (!u.IsSpectator)
                {
                    if (u.TeamColor != (int)arcols[cnt])
                    {
                        tas.ForceColor(u.Name, (int)arcols[cnt]);
                        changed = true;
                    }
                    cnt++;
                }
            }
            if (changed) SayBattle("colors fixed");
        }

        public void ComForce(TasSayEventArgs e, string[] words)
        {
            if (spring.IsRunning)
            {
                SayBattle("forcing game start by " + e.UserName);
                spring.ForceStart();
            }
            else Respond(e, "cannot force, game not started");
        }

        public void ComForceSpectator(TasSayEventArgs e, string[] words)
        {
            if (words.Length == 0)
            {
                Respond(e, "You must specify player name");
                return;
            }

            int[] indexes;
            string[] usrlist;
            if (FilterUsers(words, out usrlist, out indexes) == 0)
            {
                Respond(e, "Cannot find such player");
                return;
            }

            tas.ForceSpectator(usrlist[0]);
            Respond(e, "Forcing " + usrlist[0] + " to spectator");
        }

        public void ComForceSpectatorAfk(TasSayEventArgs e, string[] words)
        {
            var b = tas.MyBattle;
            if (b != null)
            {
                foreach (var u in b.Users)
                {
                    User u2;
                    if (u.Name != tas.UserName && !u.IsSpectator && tas.GetExistingUser(u.Name, out u2)) if (u2.IsAway) ComForceSpectator(e, new[] { u.Name });
                }
            }
        }

        public void ComForceStart(TasSayEventArgs e, string[] words)
        {
            int allyno;
            int alliances;
            if (hostedMod.IsMission && !BalancedTeams(out allyno, out alliances))
            {
                SayBattle("Cannot start, mission slots are not correct");
                return;
            }
            /*string usname;
      if (!AllReadyAndSynced(out usname)) {
        SayBattle("cannot start, " + usname + " not ready and synced");
        return;
      }*/
            if (SpawnConfig == null && config != null && config.Mode == AutohostMode.Planetwars)
            {
                if (RunServerBalance(true, null, null))
                {
                    SayBattle("please wait, game is about to start");
                    StopVote();
                    lastSplitPlayersCountCalled = 0;
                    tas.StartGame();
                }
            }
            else
            {
                SayBattle("please wait, game is about to start");
                StopVote();
                lastSplitPlayersCountCalled = 0;
                tas.StartGame();
            }
        }

        public void ComJuggle(TasSayEventArgs e, string[] words)
        {
            Respond(e, Program.main.JugglePlayers());
        }

        public void ComKick(TasSayEventArgs e, string[] words)
        {
            if (words.Length == 0)
            {
                Respond(e, "You must specify player name");
                return;
            }

            int[] indexes;
            string[] usrlist;
            if (FilterUsers(words, out usrlist, out indexes) == 0)
            {
                if (spring.IsRunning) spring.Kick(Utils.Glue(words));
                Respond(e, "Cannot find such player");
                return;
            }

            if (usrlist[0] == tas.UserName)
            {
                Respond(e, "won't kick myself, not in suicidal mood today");
                return;
            }

            if (spring.IsRunning) spring.Kick(usrlist[0]);
            tas.Kick(usrlist[0]);
        }


        public void ComMap(TasSayEventArgs e, params string[] words)
        {
            if (spring.IsRunning)
            {
                Respond(e, "Cannot change map while the game is running");
                return;
            }
            if (words.All(String.IsNullOrEmpty))
            {
                ServerVerifyMap(true);
                //Respond(e, "You must specify a map name");
                return;
            }

            string[] vals;
            int[] indexes;
            if (FilterMaps(words, out vals, out indexes) > 0)
            {
                SayBattle("changing map to " + vals[0]);
                var mapi = cache.GetResourceDataByInternalName(vals[0]);
                if (mapi != null)
                {
                    tas.ChangeMap(mapi.InternalName,
                                  mapi.SpringHashes.Where(x => x.SpringVersion == springPaths.SpringVersion).Select(x => x.SpringHash).FirstOrDefault());
                }
            }
            else Respond(e, "Cannot find such map.");
        }

        public void ComMove(TasSayEventArgs e, string[] words)
        {
            if (words.Length < 1)
            {
                Respond(e, "<target hostname>");
                return;
            }
            var host = words[0];

            if (!tas.ExistingBattles.Values.Any(x=>x.Founder.Name ==host)) {
                Respond(e,string.Format("Host {0} not found", words[0]));
                return;
            }

            var serv = new SpringieService();
            var moves = new List<MovePlayerEntry>();
            foreach (var u in tas.MyBattle.Users.Where(x=>x.LobbyUser.Name != tas.MyBattle.Founder.Name)) {
                moves.Add(new MovePlayerEntry()
                          {
                              PlayerName = u.Name,
                              BattleHost = host
                          });
            }
            serv.MovePlayers(tas.UserName,tas.UserPassword, moves.ToArray());
        }


        public void ComPredict(TasSayEventArgs e, string[] words)
        {
            var b = tas.MyBattle;
            var grouping = b.Users.Where(u => !u.IsSpectator).GroupBy(u => u.AllyNumber);

            IGrouping<int, UserBattleStatus> oldg = null;
            foreach (var g in grouping)
            {
                if (oldg != null)
                {
                    var t1elo = oldg.Average(x => x.LobbyUser.EffectiveElo);

                    var t2elo = g.Average(x => x.LobbyUser.EffectiveElo);
                    Respond(e,
                            String.Format("team {0} has {1}% chance to win over team {2}",
                                          oldg.Key + 1,
                                          PlasmaShared.Utils.GetWinChancePercent(t2elo - t1elo),
                                          g.Key + 1));
                }
                oldg = g;
            }
        }


        public void ComRandom(TasSayEventArgs e, string[] words)
        {
            ComFix(e, words);
            var b = tas.MyBattle;

            var actUsers = new List<UserBattleStatus>();
            foreach (var u in b.Users) if (!u.IsSpectator) actUsers.Add(u);

            var teamCount = 0;
            if (words.Length > 0) Int32.TryParse(words[0], out teamCount);
            else teamCount = 2;
            if (teamCount < 2) teamCount = 2;
            if (teamCount > actUsers.Count) teamCount = 2;
            var r = new Random();

            var al = 0;
            while (actUsers.Count > 0)
            {
                var index = r.Next(actUsers.Count);
                tas.ForceAlly(actUsers[index].Name, al);
                actUsers.RemoveAt(index);
                al++;
                al = al%teamCount;
            }
            SayBattle("players assigned to " + teamCount + " random teams");
        }

        public void ComRehost(TasSayEventArgs e, string[] words)
        {
            if (words.Length == 0) Start(null, null);
            else
            {
                string[] mods;
                int[] indexes;
                if (FilterMods(words, out mods, out indexes) == 0) Respond(e, "cannot find such mod");
                else Start(mods[0], null);
            }
        }

        public void ComRing(TasSayEventArgs e, string[] words)
        {
            var usrlist = new List<string>();

            if (words.Length == 0)
            {
                // ringing idle
                foreach (var p in tas.MyBattle.Users)
                {
                    if (p.IsSpectator) continue;
                    if ((p.SyncStatus != SyncStatuses.Synced) && (!spring.IsRunning || !spring.IsPlayerReady(p.Name))) usrlist.Add(p.Name);
                }
            }
            else
            {
                string[] vals;
                int[] indexes;
                FilterUsers(words, out vals, out indexes);
                usrlist = new List<string>(vals);
            }

            var rang = "";
            foreach (var s in usrlist)
            {
                tas.Ring(s);
                rang += s + ", ";
            }

            if (words.Length == 0 && usrlist.Count > 7) SayBattle("ringing all unready");
            else SayBattle("ringing " + rang);
        }


        // user and rank info


        public void ComSay(TasSayEventArgs e, string[] words)
        {
            if (words.Length == 0)
            {
                Respond(e, "This command needs 1 parameter (say text)");
                return;
            }
            SayBattle("[" + e.UserName + "]" + Utils.Glue(words));
        }


        public void ComSplit(TasSayEventArgs e, string[] words)
        {
            if (words.Length != 2)
            {
                Respond(e, "This command needs 2 parameters");
                return;
            }
            if (words[0] != "h" && words[0] != "v") Respond(e, "first parameter must be 'h' or 'v'");
            else
            {
                int perc;
                Int32.TryParse(words[1], out perc);
                if (perc < 0 || perc > 50) Respond(e, "second parameter must be between 0 and 50");
                else
                {
                    if (words[0] == "h")
                    {
                        tas.AddBattleRectangle(0, new BattleRect(0, 0, 1.0, perc/100.0));
                        tas.AddBattleRectangle(1, new BattleRect(0, 1.0 - perc/100.0, 1.0, 1.0));
                    }
                    else
                    {
                        tas.AddBattleRectangle(0, new BattleRect(0, 0, perc/100.0, 1.0));
                        tas.AddBattleRectangle(1, new BattleRect(1.0 - perc/100.0, 0, 1.0, 1.0));
                    }
                    tas.RemoveBattleRectangle(2);
                    tas.RemoveBattleRectangle(3);
                }
            }
        }

        public void ComSplitPlayers(TasSayEventArgs e, string[] words)
        {
            if (tas.MyBattle != null && !spring.IsRunning) {
                var serv = new SpringieService();
                serv.SplitAutohost(tas.MyBattle.GetContext(), tas.UserPassword);
            }
        }


        public void ComSpringie(TasSayEventArgs e, string[] words)
        {
            var b = tas.MyBattle;

            var running = DateTime.Now.Subtract(Program.startupTime);
            running = new TimeSpan((int)running.TotalHours, running.Minutes, running.Seconds);

            var started = DateTime.Now.Subtract(spring.GameStarted);
            started = new TimeSpan((int)started.TotalHours, started.Minutes, started.Seconds);

            Respond(e, tas.UserName + " (" + MainConfig.SpringieVersion + ") running for " + running);
            Respond(e, "players: " + (b.Users.Count - b.NonSpectatorCount) + "/" + b.MaxPlayers);
            Respond(e, "mod: " + b.ModName);
            Respond(e, "map: " + b.MapName);
            Respond(e,
                    "game " + (spring.IsRunning ? "running since " : "not running, last started ") +
                    (spring.GameStarted != DateTime.MinValue ? started + " ago" : "never"));
        }





        public void ComStart(TasSayEventArgs e, string[] words)
        {
            if (spring.IsRunning) {
                Respond(e, "Game already running");
                return;
            }
            /*if (activePoll != null)
            {
                Respond(e, "Poll is active");
                return;
            }*/

            List<string> usname;
            if (!AllReadyAndSynced(out usname))
            {
                SayBattle("cannot start, " + Utils.Glue(usname.ToArray()) + " does not have the map/game yet");
                return;
            }

            if (SpawnConfig != null)
            {
                int allyno;
                int alliances;
                if (!BalancedTeams(out allyno, out alliances))
                {
                    SayBattle("cannot start, alliance " + (allyno + 1) + " not fair. Use !forcestart to override");
                    return;
                }
            }
            else
            {
                if (config != null && config.Mode != AutohostMode.None)
                {
                    if (!RunServerBalance(true, null, null))
                    {
                        SayBattle("Cannot start a game atm");
                        return;
                    }

                }
            }

            SayBattle("please wait, game is about to start");
            StopVote();
            lastSplitPlayersCountCalled = 0;
            tas.StartGame();
        }

        public void ComTeam(TasSayEventArgs e, string[] words)
        {
            if (words.Length < 2)
            {
                Respond(e, "this command needs 2 parameters (team number and player name)");
                return;
            }
            var teamno = 0;
            if (!Int32.TryParse(words[0], out teamno) || --teamno < 0 || teamno >= Spring.MaxTeams)
            {
                Respond(e, "invalid team number");
                return;
            }
            string[] usrs;
            int[] idx;
            if (FilterUsers(Utils.ShiftArray(words, -1), out usrs, out idx) == 0) Respond(e, "no such player found");
            else
            {
                SayBattle("Forcing " + usrs[0] + " to team " + (teamno + 1));
                tas.ForceTeam(usrs[0], teamno);
            }
        }

        public void ComTeamColors(TasSayEventArgs e, string[] words)
        {
            if (hostedMod.IsMission)
            {
                ForceMissionColors();
                return;
            }

            var players = tas.MyBattle.Users.Where(u => !u.IsSpectator).ToArray();
            var alliances = players.GroupBy(u => u.AllyNumber).ToArray();
            var teamCounts = alliances.Select(g => g.Count()).ToArray();
            var colors = TeamColorMaker.GetTeamColors(teamCounts);
            var changed = false;
            for (var allianceIndex = 0; allianceIndex < alliances.Length; allianceIndex++)
            {
                var alliance = alliances[allianceIndex].ToArray();
                for (var teamIndex = 0; teamIndex < alliance.Length; teamIndex++)
                {
                    var user = alliance[teamIndex];
                    var newColor = (int)(MyCol)colors[allianceIndex][teamIndex];
                    if (user.TeamColor == newColor) continue;
                    tas.ForceColor(user.Name, newColor);
                    changed = true;
                }
            }
            if (changed) SayBattle("team colors set");
        }

        internal static int Filter(string[] source, string[] words, out string[] resultVals, out int[] resultIndexes)
        {
            int i;

            // search by direct index
            if (words.Length == 1)
            {
                if (Int32.TryParse(words[0], out i))
                {
                    if (i >= 0 && i < source.Length)
                    {
                        resultVals = new[] { source[i] };
                        resultIndexes = new[] { i };
                        return 1;
                    }
                }
            }

            // search by direct word
            var glued = Utils.Glue(words);
            for (i = 0; i < source.Length; ++i)
            {
                if (String.Compare(source[i], glued, true) == 0)
                {
                    resultVals = new[] { source[i] };
                    resultIndexes = new[] { i };
                    return 1;
                }
            }

            var res = new List<string>();
            var resi = new List<int>();

            for (i = 0; i < words.Length; ++i) words[i] = words[i].ToLower();
            for (i = 0; i < source.Length; ++i)
            {
                if (source[i] + "" == "") continue;
                var item = source[i];
                var isok = true;
                for (var j = 0; j < words.Length; ++j)
                {
                    if (!item.ToLower().Contains(words[j]))
                    {
                        isok = false;
                        break;
                    }
                }
                if (isok)
                {
                    res.Add(item);
                    resi.Add(i);
                }
            }

            resultVals = res.ToArray();
            resultIndexes = resi.ToArray();

            return res.Count;
        }

        public int FilterMaps(string[] words, out string[] vals, out int[] indexes)
        {
            return FilterMaps(words, this, out vals, out indexes);
        }

        internal static int FilterMods(string[] words, AutoHost ah, out string[] vals, out int[] indexes)
        {
            var result = ah.cache.FindResourceData(words, ResourceType.Mod);
            vals = result.Select(x => x.InternalName).ToArray();
            indexes = result.Select(x => x.ResourceID).ToArray();
            return vals.Length;
        }

        public int FilterMods(string[] words, out string[] vals, out int[] indexes)
        {
            return FilterMods(words, this, out vals, out indexes);
        }


        internal static int FilterUsers(string[] words, TasClient tas, Spring spring, out string[] vals, out int[] indexes)
        {
            var b = tas.MyBattle;
            var i = 0;
            var temp = b.Users.Select(u => u.Name).ToList();
            if (spring.IsRunning) foreach (var u in spring.StartContext.Players) {
                if (!temp.Contains(u.Name)) temp.Add(u.Name);
            }
            return Filter(temp.ToArray(), words, out vals, out indexes);
        }


        public string GetOptionsString(TasSayEventArgs e, string[] words)
        {
            var s = Utils.Glue(words);
            var result = "";
            var pairs = s.Split(new[] { ',' });
            if (pairs.Length == 0 || pairs[0].Length == 0)
            {
                Respond(e, "requires key=value format");
                return "";
            }
            foreach (var pair in pairs)
            {
                var parts = pair.Split(new[] { '=' }, 2);
                if (parts.Length != 2)
                {
                    Respond(e, "requires key=value format");
                    return "";
                }
                var key = parts[0];
                var val = parts[1];

                var found = false;
                var mod = hostedMod;
                foreach (var o in mod.Options)
                {
                    if (o.Key == key)
                    {
                        found = true;
                        string res;
                        if (o.GetPair(val, out res))
                        {
                            if (result != "") result += "\t";
                            result += res;
                        }
                        else Respond(e, "Value " + val + " is not valid for this option");

                        break;
                    }
                }
                if (!found)
                {
                    Respond(e, "No option called " + key + " found");
                    return "";
                }
            }
            return result;
        }

        public bool RunServerBalance(bool isGameStart, int? allyTeams, bool? clanWise)
        {
            try
            {
                if (tas.MyBattle == null) return false;
                var serv = new SpringieService();

                serv.Timeout = 10000;
                var balance = serv.BalanceTeams(tas.MyBattle.GetContext(), isGameStart, allyTeams, clanWise);
                if (!string.IsNullOrEmpty(balance.Message)) SayBattle(balance.Message, false);
                if (balance.Players != null && balance.Players.Length > 0)
                {
                    foreach (var user in tas.MyBattle.Users.Where(x => !x.IsSpectator && !balance.Players.Any(y => y.Name == x.Name))) tas.ForceSpectator(user.Name); // spec those that werent in response
                    foreach (var user in balance.Players.Where(x => x.IsSpectator)) tas.ForceSpectator(user.Name);
                    foreach (var user in balance.Players.Where(x => !x.IsSpectator))
                    {
                        tas.ForceTeam(user.Name, user.TeamID);
                        tas.ForceAlly(user.Name, user.AllyID);
                    }
                }

                if (balance.DeleteBots) foreach (var b in tas.MyBattle.Bots) tas.RemoveBot(b.Name);
                if (balance.Bots != null && balance.Bots.Length > 0)
                {
                    foreach (var b in tas.MyBattle.Bots.Where(x => !balance.Bots.Any(y => y.BotName == x.Name && y.Owner == x.owner))) tas.RemoveBot(b.Name);

                    foreach (var b in balance.Bots)
                    {
                        var existing = tas.MyBattle.Bots.FirstOrDefault(x => x.owner == b.Owner && x.Name == b.BotName);
                        if (existing != null)
                        {
                            var upd = existing.Clone();
                            upd.AllyNumber = b.AllyID;
                            upd.TeamNumber = b.TeamID;
                            tas.UpdateBot(existing.Name, upd, existing.TeamColor);
                        }
                        else
                        {
                            var botStatus = tas.MyBattleStatus.Clone();
                            botStatus.TeamNumber = b.TeamID;
                            botStatus.AllyNumber = b.AllyID;
                            tas.AddBot(b.BotName.Replace(" ", "_"), botStatus, botStatus.TeamColor, b.BotAI);
                        }
                    }
                }

                return balance.CanStart;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return false;
            }
        }


        void ComAdmins(TasSayEventArgs e, string[] words)
        {
            tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
            foreach (var u in tas.ExistingUsers.Values.Where(x => x.SpringieLevel > 1)) tas.Say(TasClient.SayPlace.User, e.UserName, " " + u.Name + " (level " + u.SpringieLevel + ")", false);
            tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
        }

        void ComHelp(TasSayEventArgs e, string[] words)
        {
            var ulevel = GetUserLevel(e);
            tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
            foreach (var c in Commands.Commands) if (c.Level <= ulevel) tas.Say(TasClient.SayPlace.User, e.UserName, " !" + c.Name + " " + c.HelpText, false);
            tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
        }


        void ComHelpAll(TasSayEventArgs e, string[] words)
        {
            var copy = new List<CommandConfig>(Commands.Commands);
            copy.Sort(delegate(CommandConfig a, CommandConfig b)
                {
                    if (a.Level != b.Level) return a.Level.CompareTo(b.Level);
                    else return a.Name.CompareTo(b.Name);
                });

            tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
            foreach (var c in copy) tas.Say(TasClient.SayPlace.User, e.UserName, "Level " + c.Level + " --> !" + c.Name + " " + c.HelpText, false);
            tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
        }

        void ComListMaps(TasSayEventArgs e, string[] words)
        {
            string[] vals;
            int[] indexes;
            int count;
            if ((count = FilterMaps(words, out vals, out indexes)) > 0)
            {
                if (count > MaxMapListLength)
                {
                    Respond(e, String.Format("This has {0} results, please narrow down your search", count));
                    return;
                }
                tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
                for (var i = 0; i < vals.Length; ++i) tas.Say(TasClient.SayPlace.User, e.UserName, indexes[i] + ": " + vals[i], false);
                tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
            }
            else Respond(e, "no such map found");
        }

        void ComListMods(TasSayEventArgs e, string[] words)
        {
            string[] vals;
            int[] indexes;
            int count;
            if ((count = FilterMods(words, out vals, out indexes)) > 0)
            {
                if (count > MaxMapListLength)
                {
                    Respond(e, String.Format("This has {0} results, please narrow down your search", count));
                    return;
                }
                tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
                for (var i = 0; i < vals.Length; ++i) tas.Say(TasClient.SayPlace.User, e.UserName, indexes[i] + ": " + vals[i], false);
                tas.Say(TasClient.SayPlace.User, e.UserName, "---", false);
            }
            else Respond(e, "no such mod found");
        }

        void ComListOptions(TasSayEventArgs e, string[] words)
        {
            var mod = hostedMod;
            if (mod.Options.Length == 0) Respond(e, "this mod has no options");
            else foreach (var opt in mod.Options) Respond(e, opt.ToString());
        }


        void ComNotify(TasSayEventArgs e, string[] words)
        {
            if (!toNotify.Contains(e.UserName)) toNotify.Add(e.UserName);
            Respond(e, "I will notify you when game ends");
        }

        void ComResetOptions(TasSayEventArgs e, string[] words)
        {
            foreach (var opt in tas.MyBattle.ModOptions)
            {
                var entry = hostedMod.Options.FirstOrDefault(x => x.Key.ToLower() == opt.Key.ToLower());
                if (entry != null && entry.Default != opt.Value)
                {
                    string str;
                    entry.GetPair(entry.Default, out str);
                    tas.SetScriptTag(str);
                }
            }

            Respond(e, "Game options reset to defaults");
        }

        void ComSaveBoxes(TasSayEventArgs e, string[] words)
        {
            try
            {
                var serv = new SpringieService();
                serv.StoreBoxes(tas.MyBattle.GetContext(),
                                tas.MyBattle.Rectangles.Select(x =>
                                    {
                                        double left;
                                        double top;
                                        double right;
                                        double bottom;
                                        x.Value.ToFractions(out left, out top, out right, out bottom);
                                        return new RectInfo()
                                               {
                                                   Number = x.Key,
                                                   X = (int)(left*100),
                                                   Y = (int)(top*100),
                                                   Width = (int)((right - left)*100),
                                                   Height = (int)((bottom - top)*100)
                                               };
                                    }).ToArray());
                Respond(e, "Saved");
            }
            catch (Exception ex)
            {
                Respond(e, ex.ToString());
            }
        }

        void ComSetEngine(TasSayEventArgs e, string[] words)
        {
            if (words.Length != 1)
            {
                Respond(e, "Specify engine version");
                return;
            }
            else
            {
                var version = words[0];
                requestedEngineChange = version;
                Respond(e, "Preparing engine change to " + version);
                Program.main.Downloader.GetAndSwitchEngine(version);
            }
        }


        void ComSetGameTitle(TasSayEventArgs e, string[] words)
        {
            if (words.Length == 0) Respond(e, "this command needs one parameter - new game title");
            else
            {
                config.Title = Utils.Glue(words);
                Respond(e, "game title changed");
            }
        }

        void ComSetMaxPlayers(TasSayEventArgs e, string[] words)
        {
            if (words.Length == 0) Respond(e, "this command needs one parameter - number of players");
            else
            {
                int plr;
                Int32.TryParse(words[0], out plr);
                if (plr < 1) plr = 1;
                if (plr > Spring.MaxTeams) plr = Spring.MaxTeams;
                config.MaxPlayers = plr;
                Respond(e, "server size changed");
            }
        }


        void ComSetOption(TasSayEventArgs e, string[] words)
        {
            var ret = GetOptionsString(e, words);
            if (ret != "")
            {
                tas.SetScriptTag(ret);
                Respond(e, "Options set");
            }
        }

        void ComSetPassword(TasSayEventArgs e, string[] words)
        {
            if (words.Length == 0)
            {
                config.Password = "";
                Respond(e, "password remoded");
            }
            else
            {
                config.Password = words[0];
                Respond(e, "password changed");
            }
        }

        void ComTransmit(TasSayEventArgs e, string[] words)
        {
            if (words.Length == 0)
            {
                Respond(e, "This command needs 1 parameter (transmit text)");
                return;
            }
            if (spring.IsRunning) spring.SayGame(String.Format("[{0}]{1}", e.UserName, "!transmit " + Utils.Glue(words)));
        }

        static int FilterMaps(string[] words, AutoHost ah, out string[] vals, out int[] indexes)
        {
            var result = ah.cache.FindResourceData(words, ResourceType.Map);
            vals = result.Select(x => x.InternalName).ToArray();
            indexes = result.Select(x => x.ResourceID).ToArray();
            return vals.Length;
        }


        int FilterUsers(string[] words, out string[] vals, out int[] indexes)
        {
            return FilterUsers(words, tas, spring, out vals, out indexes);
        }

        void ForceMissionColors()
        {
            var b = tas.MyBattle;
            foreach (var u in b.Users.Where(x => !x.IsSpectator))
            {
                var slot = hostedMod.MissionSlots.FirstOrDefault(x => x.IsHuman && x.TeamID == u.TeamNumber && x.AllyID == u.AllyNumber);
                if (slot != null && slot.Color != u.TeamColor) tas.ForceColor(u.Name, slot.Color);
            }
        }


        void SayLines(TasSayEventArgs e, string what)
        {
            foreach (var line in what.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)) tas.Say(TasClient.SayPlace.User, e.UserName, line, false);
        }

        void ServerVerifyMap(bool pickNew)
        {
            try
            {
                if (tas.MyBattle != null && !spring.IsRunning)
                {
                    var serv = new SpringieService();
                    serv.Timeout = 15000;
                    serv.GetRecommendedMapCompleted += (sender, args) =>
                    {
                        if (!args.Cancelled && args.Error == null) {
                            var map = args.Result;
                            if (map != null && map.MapName != null && tas.MyBattle != null)
                            {
                                if (tas.MyBattle.MapName != map.MapName)
                                {
                                    ComMap(TasSayEventArgs.Default, map.MapName);
                                    SayBattle(map.Message);
                                }
                            }    
                        
                        }
                    };
                    serv.GetRecommendedMapAsync(tas.MyBattle.GetContext(), pickNew);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        class UsRank
        {
            public string Clan;
            public readonly double Elo;
            public readonly int Id;
            public readonly UserBattleStatus User;

            public UsRank(int id, double elo, string clan, UserBattleStatus user)
            {
                Id = id;
                Elo = elo;
                User = user;
                Clan = clan;
            }
        }

    }
}