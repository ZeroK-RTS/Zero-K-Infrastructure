#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZkData;
#endregion

namespace Springie.autohost
{
    public partial class AutoHost
    {
        const int MaxMapListLength = 100; //400

        List<string> engineListCache = new List<string>();
        public const int engineListTimeout = 600; //hold existing enginelist for at least 10 minutes before re-check for update
        DateTime engineListDate = new DateTime(0);

        readonly List<string> toNotify = new List<string>();

        public bool AllReadyAndSynced(out List<string> usname)
        {
            usname = new List<string>();
            foreach (var p in tas.MyBattle.Users.Values)
            {
                if (p.IsSpectator) continue;
                if (p.SyncStatus != SyncStatuses.Synced) usname.Add(p.Name);
            }
            return usname.Count == 0;
        }



        public void BalanceTeams(int teamCount, bool clanwise)
        {
            try
            {
                var b = tas.MyBattle;

                if (hostedMod.IsMission)
                {
                    var freeSlots = GetFreeSlots();
                    foreach (var u in b.Users.Values.Where(x => !x.IsSpectator).ToList())
                    {
                        var curSlot = hostedMod.MissionSlots.FirstOrDefault(x => x.IsHuman && x.TeamID == u.TeamNumber && x.AllyID == u.AllyNumber);
                        if (curSlot != null && curSlot.IsRequired)
                        {
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
                                freeSlots = freeSlots.Skip(1);
                            }
                        }
                    }

                    // remove extra bots 
                    foreach (var bot in b.Bots.Values.Where(x => x.owner != tas.UserName)) tas.RemoveBot(bot.Name);
                    return;
                }

                //fill ranker table with players
                var ranker = new List<UsRank>();
                foreach (var u in b.Users.Values) if (!u.IsSpectator) ranker.Add(new UsRank(ranker.Count, u.LobbyUser.EffectiveElo, clanwise ? (u.LobbyUser.Clan ?? "") : "", u));
                var totalPlayers = ranker.Count;

                var rand = new Random();

                //sanity check for teamCount (1<teamCount<playerCount)
                if (teamCount < 1) teamCount = 1;
                if (teamCount > ranker.Count) teamCount = ranker.Count;

                //initialize teamSums & teamUsers & teamClans table (no value set yet)
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
                        if (l.Count == cnt / teamCount)
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
                        var newElo = ((teamUsers[minid].Sum(x => x.Elo) + Math.Round(c.Elo / 10) * 10)) / (teamUsers.Count() + 1);
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
                Trace.TraceError("Error balancing teams: {0}",ex);
            }
        }



        public bool BalancedTeams(out int allyno, out int alliances)
        {
            if (hostedMod.IsMission)
            {
                alliances = 0;
                allyno = 0;
                SayBattle(String.Format("Mod {0} has {1} mission slots", hostedMod.Name, hostedMod.MissionSlots.Count()));
                bool err = false;
                var invalidUser =
                    tas.MyBattle.Users.Values.FirstOrDefault(
                        x => !x.IsSpectator && !hostedMod.MissionSlots.Any(y => y.IsHuman && y.TeamID == x.TeamNumber && y.AllyID == x.AllyNumber));
                if (invalidUser != null)
                {
                    SayBattle(String.Format("User {0} is not in proper mission slot", invalidUser.Name));
                    SayBattle(String.Format("Current slot: {0}", invalidUser.TeamNumber));
                    err = true;
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
                    err = true;
                }
                if (err) return false;
            }

            var counts = new int[16];
            allyno = 0;

            foreach (var p in tas.MyBattle.Users.Values)
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
            tas.AddBattleRectangle(numrect - 1, new BattleRect(x * 2, y * 2, (x + w) * 2, (y + h) * 2));
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
                BalanceTeams(teamCount, true);
            }
        }

        public void ComClearBox(TasSayEventArgs e, string[] words)
        {
            if (words.Length == 0) foreach (var i in tas.MyBattle.Rectangles.Keys.ToList()) tas.RemoveBattleRectangle(i);
            else
            {
                var numrect = 0;
                if (!Int32.TryParse(words[0], out numrect)) Respond(e, "parameter must be a number of rectangle");
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
                    var p = perc / 100.0;
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
                foreach (var u in b.Users.Values)
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

            SayBattle("please wait, game is about to start");
            StopVote();
            lastSplitPlayersCountCalled = 0;
            tas.StartGame();
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
                    tas.ChangeMap(mapi.InternalName);
                }
            }
            else Respond(e, "Cannot find such map.");
        }

        public void ComPlanet(TasSayEventArgs e, params string[] words)
        {
            if (spring.IsRunning)
            {
                Respond(e, "Cannot attack different planet while the game is running");
                return;
            }
            if (words.All(String.IsNullOrEmpty))
            {
                ServerVerifyMap(true);
                //Respond(e, "You must specify a map name");
                return;
            }

            // FIXME get list of valid planets
            /*
            string[] vals;
            int[] indexes;
            if (FilterMaps(words, out vals, out indexes) > 0)
            {
                SayBattle("changing planet to " + vals[0]);
                var mapi = cache.GetResourceDataByInternalName(vals[0]);
                if (mapi != null)
                {
                    tas.ChangeMap(mapi.InternalName,
                                  mapi.SpringHashes.Where(x => x.SpringVersion == springPaths.SpringVersion).Select(x => x.SpringHash).FirstOrDefault());
                }
            }
            else Respond(e, "Invalid planet ID.");
             */
        }



        public void ComMove(TasSayEventArgs e, string[] words)
        {
            if (words.Length < 1)
            {
                Respond(e, "<target hostname>");
                return;
            }
            var host = words[0];

            if (!tas.ExistingBattles.Values.Any(x => x.Founder.Name == host))
            {
                Respond(e, string.Format("Host {0} not found", words[0]));
                return;
            }

            var serv = GlobalConst.GetSpringieService();
            var moves = new List<MovePlayerEntry>();
            foreach (var u in tas.MyBattle.Users.Values.Where(x => x.LobbyUser.Name != tas.MyBattle.Founder.Name))
            {
                moves.Add(new MovePlayerEntry()
                          {
                              PlayerName = u.Name,
                              BattleHost = host
                          });
            }
            serv.MovePlayers(tas.UserName, tas.UserPassword, moves);
        }


        public void ComPredict(TasSayEventArgs e, string[] words)
        {
            var b = tas.MyBattle;
            var grouping = b.Users.Values.Where(u => !u.IsSpectator).GroupBy(u => u.AllyNumber).ToList();
            bool is1v1 = grouping.Count == 2 && grouping[0].Count() == 1 && grouping[1].Count() == 1;
            IGrouping<int, UserBattleStatus> oldg = null;
            foreach (var g in grouping)
            {
                if (oldg != null)
                {
                    // FIXME use 1v1 elo for 1v1 prediction
                    var t1elo = oldg.Average(x => x.LobbyUser.EffectiveElo);
                    var t2elo = g.Average(x => x.LobbyUser.EffectiveElo);
                    Respond(e,
                            String.Format("team {0} has {1}% chance to win over team {2}",
                                          oldg.Key + 1,
                                          ZkData.Utils.GetWinChancePercent(t2elo - t1elo),
                                          g.Key + 1));
                }
                oldg = g;
            }
        }


        public void ComRandom(TasSayEventArgs e, string[] words)
        {
            var b = tas.MyBattle;

            var actUsers = new List<UserBattleStatus>();
            foreach (var u in b.Users.Values) if (!u.IsSpectator) actUsers.Add(u);

            var teamCount = 0;
            var teamnum = new List<int>();
            if (words.Length > 0) Int32.TryParse(words[0], out teamCount);
            else teamCount = 2;
            if (teamCount < 2) teamCount = 2;
            if (teamCount > actUsers.Count)
            {
                for (int i = 0; i < teamCount; i++) teamnum.Add(i);
                teamCount = actUsers.Count;
            }
            var r = new Random();

            var al = -1;
            var index = -1;
            while (actUsers.Count > 0)
            {
                if (teamnum.Count > 0)
                {
                    index++;
                    al = r.Next(teamnum.Count);
                    teamnum.RemoveAt(al);
                }
                else
                {
                    index = r.Next(actUsers.Count);
                    al++;
                    al = al % teamCount;
                }
                tas.ForceAlly(actUsers[index].Name, al);
                actUsers.RemoveAt(index);
            }
            SayBattle("players assigned to " + teamCount + " random teams");
        }

        public void ComRehost(TasSayEventArgs e, string[] words)
        {
            /*if (spring.IsRunning)
            {
                Respond(e, "Cannot rehost while game is running");
                return;
            }*/
            if (words.Length == 0) OpenBattleRoom(null, null, false);
            else
            {
                string[] mods;
                int[] indexes;
                if (FilterMods(words, out mods, out indexes) == 0) Respond(e, "cannot find such game");
                else OpenBattleRoom(mods[0], null, false);
            }
        }

        public void ComResetOptions(TasSayEventArgs e, string[] words)
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

        public void ComRing(TasSayEventArgs e, string[] words)
        {
            var usrlist = new List<string>();

            if (words.Length == 0)
            {
                // ringing idle
                foreach (var p in tas.MyBattle.Users.Values)
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
                        tas.AddBattleRectangle(0, new BattleRect(0, 0, 1.0, perc / 100.0));
                        tas.AddBattleRectangle(1, new BattleRect(0, 1.0 - perc / 100.0, 1.0, 1.0));
                    }
                    else
                    {
                        tas.AddBattleRectangle(0, new BattleRect(0, 0, perc / 100.0, 1.0));
                        tas.AddBattleRectangle(1, new BattleRect(1.0 - perc / 100.0, 0, 1.0, 1.0));
                    }
                    tas.RemoveBattleRectangle(2);
                    tas.RemoveBattleRectangle(3);
                }
            }
        }

        public void ComSplitPlayers(TasSayEventArgs e, string[] words)
        {
            if (tas.MyBattle != null && !spring.IsRunning)
            {
                this.SayBattle("Splitting room into two by the skill level, keeping clans together");
                var serv = GlobalConst.GetSpringieService();
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
            if (spring.IsRunning)
            {
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
            var temp = b.Users.Values.Select(u => u.Name).ToList();
            if (spring.IsRunning) foreach (var u in spring.StartContext.Players)
                {
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
                var serv = GlobalConst.GetSpringieService();

                var balance = serv.BalanceTeams(tas.MyBattle.GetContext(), isGameStart, allyTeams, clanWise);
                
                ApplyBalanceResults(balance);

                return balance.CanStart;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return false;
            }
        }

        public void ApplyBalanceResults(BalanceTeamsResult balance)
        {
            if (!string.IsNullOrEmpty(balance.Message)) SayBattle(balance.Message, false);
            if (balance.Players != null && balance.Players.Count > 0)
            {
                
                foreach (var user in tas.MyBattle.Users.Values.Where(x => !x.IsSpectator && !balance.Players.Any(y => y.Name == x.Name))) tas.ForceSpectator(user.Name); // spec those that werent in response
                foreach (var user in balance.Players.Where(x => x.IsSpectator)) tas.ForceSpectator(user.Name);

                bool comsharing = false;
                bool coopOptExists = tas.MyBattle.ModOptions.Any(x => x.Key.ToLower() == "coop");
                if (coopOptExists)
                {
                    KeyValuePair<string, string> comsharing_modoption = tas.MyBattle.ModOptions.FirstOrDefault(x => x.Key.ToLower() == "coop");
                    if (comsharing_modoption.Value != "0" && comsharing_modoption.Value != "false") comsharing = true;
                }
                foreach (var user in balance.Players.Where(x => !x.IsSpectator))
                {
                    tas.ForceTeam(user.Name, comsharing ? user.AllyID : user.TeamID);
                    tas.ForceAlly(user.Name, user.AllyID);
                }
            }

            if (balance.DeleteBots) foreach (var b in tas.MyBattle.Bots.Keys) tas.RemoveBot(b);
            if (balance.Bots != null && balance.Bots.Count > 0)
            {
                foreach (var b in tas.MyBattle.Bots.Values.Where(x => !balance.Bots.Any(y => y.BotName == x.Name && y.Owner == x.owner))) tas.RemoveBot(b.Name);

                foreach (var b in balance.Bots)
                {
                    var existing = tas.MyBattle.Bots.Values.FirstOrDefault(x => x.owner == b.Owner && x.Name == b.BotName);
                    if (existing != null)
                    {
                        tas.UpdateBot(existing.Name, b.BotAI, b.AllyID, b.TeamID);
                    }
                    else
                    {
                        tas.AddBot(b.BotName.Replace(" ", "_"), b.BotAI, b.AllyID, b.TeamID);
                    }
                }
            }
        }


        void ComAdmins(TasSayEventArgs e, string[] words)
        {
            tas.Say(SayPlace.User, e.UserName, "---", false);
            foreach (var u in tas.ExistingUsers.Values.Where(x => x.SpringieLevel >= 3)) tas.Say(SayPlace.User, e.UserName, " " + u.Name + " (level " + u.SpringieLevel + ")", false);
            tas.Say(SayPlace.User, e.UserName, "---", false);
        }

        void ComHelp(TasSayEventArgs e, string[] words)
        {
            var ulevel = GetUserLevel(e);
            tas.Say(SayPlace.User, e.UserName, "---", false);
            foreach (var c in Commands.Commands) if (c.Level <= ulevel) tas.Say(SayPlace.User, e.UserName, " !" + c.Name + " " + c.HelpText, false);
            tas.Say(SayPlace.User, e.UserName, "---", false);
        }


        void ComHelpAll(TasSayEventArgs e, string[] words)
        {
            var copy = new List<CommandConfig>(Commands.Commands);
            copy.Sort(delegate(CommandConfig a, CommandConfig b)
                {
                    if (a.Level != b.Level) return a.Level.CompareTo(b.Level);
                    else return a.Name.CompareTo(b.Name);
                });

            tas.Say(SayPlace.User, e.UserName, "---", false);
            foreach (var c in copy) tas.Say(SayPlace.User, e.UserName, "Level " + c.Level + " --> !" + c.Name + " " + c.HelpText, false);
            tas.Say(SayPlace.User, e.UserName, "---", false);
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
                tas.Say(SayPlace.User, e.UserName, "---", false);
                for (var i = 0; i < vals.Length; ++i) tas.Say(SayPlace.User, e.UserName, indexes[i] + ": " + vals[i], false);
                tas.Say(SayPlace.User, e.UserName, "---", false);
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
                tas.Say(SayPlace.User, e.UserName, "---", false);
                for (var i = 0; i < vals.Length; ++i) tas.Say(SayPlace.User, e.UserName, indexes[i] + ": " + vals[i], false);
                tas.Say(SayPlace.User, e.UserName, "---", false);
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
            Respond(e, "I will notify you when the game ends.");
        }

        void ComSaveBoxes(TasSayEventArgs e, string[] words)
        {
            try
            {
                var serv = GlobalConst.GetSpringieService();
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
                                                   X = (int)(left * 100),
                                                   Y = (int)(top * 100),
                                                   Width = (int)((right - left) * 100),
                                                   Height = (int)((bottom - top) * 100)
                                               };
                                    }).ToList());
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
                string partVersion = words[0];
                string specificVer = null;
                ZkData.Utils.SafeThread(() =>
                {
                    specificVer = engineListCache.Find(x => x.StartsWith(partVersion));
                    if (specificVer == null && DateTime.Now.Subtract(engineListDate).TotalSeconds > engineListTimeout) //no result & old list
                    {
                        engineListCache = PlasmaDownloader.EngineDownload.GetEngineList(); //get entire list online
                        engineListDate = DateTime.Now;
                        specificVer = engineListCache.Find(x => x.StartsWith(partVersion));
                    }
                    if (specificVer == null) //still no result
                    {
                        Respond(e, "No such engine version");
                        return;
                    }
                    requestedEngineChange = specificVer; //in autohost.cs
                    Respond(e, "Preparing engine change to " + specificVer);
                    var springCheck = Program.main.Downloader.GetAndSwitchEngine(specificVer);
                    if (springCheck == null) ; //Respond(e, "Engine available");
                    else
                        Respond(e, "Downloading engine. " + springCheck.IndividualProgress + "%");

                    return;
                }).Start();
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
            if (spring.IsRunning)
            {
                Respond(e, "Cannot set options while the game is running");
                return;
            }
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
                Respond(e, "password removed");
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


        void SayLines(TasSayEventArgs e, string what)
        {
            foreach (var line in what.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)) tas.Say(SayPlace.User, e.UserName, line, false);
        }

        public void ServerVerifyMap(bool pickNew)
        {
            try
            {
                if (tas.MyBattle != null && !spring.IsRunning)
                {
                    var serv = GlobalConst.GetSpringieService();

                    Task.Factory.StartNew(() => {
                        RecommendedMapResult map;
                        try {
                            map = serv.GetRecommendedMap(tas.MyBattle.GetContext(), pickNew);
                        } catch (Exception ex) {
                            Trace.TraceError(ex.ToString());
                            return;
                        }
                        if (map != null && map.MapName != null && tas.MyBattle != null) {
                            if (tas.MyBattle.MapName != map.MapName) {
                                ComMap(TasSayEventArgs.Default, map.MapName);
                                SayBattle(map.Message);
                            }
                        }

                    });
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

        /////-------------KARMARKAR & KARP PARTITION ALGORITHM---------------/////
        //KARMARKAR & KARP Partition Algorithm is a Heuristic algorithm that split a set of integers
        //into 2 different set with an equal (or almost equal) sum. The advantage of this algorithm
        //over a brute force checking is it is really cheap and yield the same exact result as brute
        //force checking. 
        //
        //This version of this algorithm use recursive search for exact result. Benchmarked running
        //time on AMD Athlon(tm) XP 2800+ at 2 GHz** was
        //  0.00 second for 30 integers on EASY problem.
        //  120 second for 70 integers on EASY problem.
        //  27 second for 30 integers on HARD problem.
        //
        //EXAMPLE of usage:
        //  int[] eloList = new int[] {2144,2063,2010,2007,1889,1884,1783,1714,1629,1621};
        //  int[] team1 = new int[1];
        //  int[] team2 = new int[1];
        //  KarmarkarKarpPartitioning(eloList, out team1, out team2);
        //  for (int i = 0; i < team1.Length; i++)
        //      System.Diagnostics.Trace.TraceInformation("Team1 {0}", team1[i]);
        //  for (int i = 0; i < team2.Length; i++)
        //      System.Diagnostics.Trace.TraceInformation("Team2 {0}", team2[i]);
        //OUTPUT:
        //  Team1 1783 
        //  Team1 1629 
        //  Team1 1889 
        //  Team1 2007 
        //  Team1 2063
        //  
        //  Team2 1884 
        //  Team2 2010 
        //  Team2 1621 
        //  Team2 1714 
        //  Team2 2144
        //
        //SOME NOTE:
        //1) The function will not work with integer count less than 3. Will output a table with
        //   -1 in this case.
        //2) The "noRecursion" tag allow a purely KK algorithm that do not output an exact value. 
        //   Good for finding approximate solution for really hard problem which otherwise take too
        //   long to solve. See**
        //   
        //SOURCE:
        //"The Differencing Method of Set Partitioning" (1982) by Nanrendrar Karmarkar and Richard M. Karp 
        //**"Heuristics and Exact Methods for Number Partitioning" (2008) by Joao Pedro Pedroso and Mikio Kubo 

        public void KarmarkarKarpPartitioning(int[] numberList, out int[] _team1, out int[] _team2, bool noRecursion = false)
        {
            //initialize table/value
            int numberCount = numberList.Length;
            int[] team1 = new int[numberCount];
            int[] team2 = new int[numberCount];
            for (int i = 0; i < numberCount; i++)
            {
                team1[i] = -1;
                team2[i] = -1;
            }
            int team1index = 0;
            int team2index = 0;
            _team1 = team1;
            _team2 = team2;

            //skip number list with less than 4 member
            if (numberCount < 3)
                return;

            System.Array.Sort(numberList); //sort in ascending order (big value on end of table)

            //copy number list
            int[] numberList1 = new int[numberCount];
            for (int j = 0; j < numberCount; j++)
                numberList1[j] = numberList[j];

            //perform differencing
            int[] differencingResult = DifferencingTree(numberList1, false, noRecursion);

            //explore right branch of differencing tree if no optimal result found
            if (differencingResult[differencingResult.Length - 1] > 1 && numberCount > 4 && !noRecursion)
            {
                int[] numberList2 = new int[numberCount];
                for (int j = 0; j < numberCount; j++)
                    numberList2[j] = numberList[j];
                int[] differencingResult2 = DifferencingTree(numberList, true);
                if (differencingResult[differencingResult.Length - 1] > differencingResult2[differencingResult2.Length - 1])
                    differencingResult = differencingResult2;
            }

            //partition number list (reconstruct list using operation results from differencing operation)
            //intialize value
            int difference = differencingResult[differencingResult.Length - 1];
            int team1sum = difference;
            int team2sum = 0;
            team1index = 1;
            team2index = 0;
            team1[0] = difference;

            //loop, step-by-step fill value into 2 new table using value from operation results
            for (int i = differencingResult.Length - 1; i > 0; i = i - 3)
            {
                difference = differencingResult[i];
                bool found = false;
                for (int j = team1index - 1; j >= 0; j--)
                {
                    if (team1[j] == difference)
                    {
                        team1sum = team1sum - difference;
                        team1index = team1index - 1;
                        team1[j] = team1[team1index];
                        team1[team1index] = -1;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    for (int j = team2index - 1; j >= 0; j--)
                    {
                        if (team2[j] == difference)
                        {
                            team2sum = team2sum - difference;
                            team2index = team2index - 1;
                            team2[j] = team2[team2index];
                            team2[team2index] = -1;
                            break;
                        }
                    }
                }

                int firstLargeValue = differencingResult[i - 2];
                int secondLargeValue = differencingResult[i - 1];

                if (team1sum < team2sum)
                {
                    team1sum = team1sum + firstLargeValue;
                    team1[team1index] = firstLargeValue;
                    team1index = team1index + 1;
                }
                else
                {
                    team2sum = team2sum + firstLargeValue;
                    team2[team2index] = firstLargeValue;
                    team2index = team2index + 1;
                }
                if (team1sum < team2sum)
                {
                    team1sum = team1sum + secondLargeValue;
                    team1[team1index] = secondLargeValue;
                    team1index = team1index + 1;
                }
                else
                {
                    team2sum = team2sum + secondLargeValue;
                    team2[team2index] = secondLargeValue;
                    team2index = team2index + 1;
                }
            }

            _team1 = new int[team1index];
            _team2 = new int[team2index];
            for (int j = 0; j < team1index; j++)
                _team1[j] = team1[j];
            for (int j = 0; j < team2index; j++)
                _team2[j] = team2[j];

            //FINISH
        }

        public int[] DifferencingTree(int[] numberList, bool goToPlusSide, bool noRecursion = false)
        {
            //do differencing
            int numberCount = numberList.Length;
            int firstLargeValue = numberList[numberCount - 1];
            int secondLargeValue = numberList[numberCount - 2];
            int difference = 0;
            if (!goToPlusSide)
                difference = firstLargeValue - secondLargeValue;
            else
                difference = firstLargeValue + secondLargeValue;

            //clear last 2 content
            numberList[numberCount - 1] = -1;
            numberList[numberCount - 2] = -1;
            numberCount = numberCount - 2;

            //insert "differenced" value into (sorted) number list
            numberCount = numberCount + 1;
            int i = numberCount - 1;
            while (i >= 0)
            {
                if (i == 0 || difference >= numberList[i - 1])
                {
                    numberList[i] = difference;
                    break;
                }
                else if (i > 0) numberList[i] = numberList[i - 1];
                i--;
            }

            //put operation result into secondary table.
            int expectedSize = numberCount * 3;
            int[] differencingResult = new int[expectedSize];
            differencingResult[0] = firstLargeValue;
            differencingResult[1] = secondLargeValue;
            differencingResult[2] = difference;

            if (numberCount > 1)
            {
                //copy number list
                int[] numberList1 = new int[numberCount];
                for (int j = 0; j < numberCount; j++)
                    numberList1[j] = numberList[j];

                //go deeper
                int[] differencingResultChild = DifferencingTree(numberList1, false, noRecursion);

                //go to right side of tree if optimal result not yet obtained
                if (differencingResultChild[differencingResultChild.Length - 1] > 1 && numberCount > 4 && !noRecursion)
                {
                    int[] numberList2 = new int[numberCount];
                    for (int j = 0; j < numberCount; j++)
                        numberList2[j] = numberList[j];
                    int[] differencingResultChild2 = DifferencingTree(numberList2, true);
                    if (differencingResultChild[differencingResultChild.Length - 1] > differencingResultChild2[differencingResultChild2.Length - 1])
                        differencingResultChild = differencingResultChild2;
                }

                //copy operation result from branches into secondary table.
                int differencingResultIndex = 3;
                for (int j = 0; j < differencingResultChild.Length; j++)
                {
                    differencingResult[j + differencingResultIndex] = differencingResultChild[j];
                }
            }

            //return operation results to parent
            return differencingResult;
        }
        /////-------------END PARTITION ALGORITHM---------------/////


        public void ComAddUser(TasSayEventArgs e, string[] words)
        {
            if (words.Length != 1) Respond(e,"Specify password");
            if (spring.IsRunning) spring.AddUser(e.UserName, words[0]);
        }

    }
}
