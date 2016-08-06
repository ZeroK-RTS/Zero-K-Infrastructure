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

namespace ZkLobbyServer
{
    public partial class ServerBattle
    {
        const int MaxMapListLength = 100; //400

        List<string> engineListCache = new List<string>();
        public const int engineListTimeout = 600; //hold existing enginelist for at least 10 minutes before re-check for update
        DateTime engineListDate = new DateTime(0);

        readonly List<string> toNotify = new List<string>();

        public bool AllReadyAndSynced(out List<string> usname)
        {
            usname = new List<string>();
            foreach (var p in Users.Values)
            {
                if (p.IsSpectator) continue;
                if (p.SyncStatus != SyncStatuses.Synced) usname.Add(p.Name);
            }
            return usname.Count == 0;
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
                    Users.Values.FirstOrDefault(
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

            foreach (var p in Users.Values)
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




        public void ComBalance(TasSayEventArgs e, string[] words)
        {
            var teamCount = 0;
            if (words.Length > 0) Int32.TryParse(words[0], out teamCount);

            RunServerBalance(false, teamCount == 0 ? (int?)null : teamCount, false);
        }


        public void ComCBalance(TasSayEventArgs e, string[] words)
        {
            var teamCount = 2;
            if (words.Length > 0) Int32.TryParse(words[0], out teamCount);
            else teamCount = 2;
            RunServerBalance(false, teamCount, true);
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

            ConnectedUser usr;
            if (server.ConnectedUsers.TryGetValue(usrlist[0], out usr))
            {
                usr.Process(new UpdateUserBattleStatus() { Name = usr.Name, IsSpectator = true });
            }
            Respond(e, "Forcing " + usrlist[0] + " to spectator");
        }

        public void ComForceSpectatorAfk(TasSayEventArgs e, string[] words)
        {
            foreach (var u in Users.Values) if (!u.IsSpectator && u.LobbyUser.IsAway) ComForceSpectator(e, new[] { u.Name });
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

            SayBattle("please wait, game is about to start");
            StopVote();
            lastSplitPlayersCountCalled = 0;
            StartGame();
        }


        public class KickedPlayer
        {
            public string Name;
            public DateTime TimeOfKicked = DateTime.UtcNow;
        }

        public List<KickedPlayer> kickedPlayers = new List<KickedPlayer>();

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


            if (!kickedPlayers.Any(x => x.Name == usrlist[0])) kickedPlayers.Add(new KickedPlayer() { Name = usrlist[0] });
            if (spring.IsRunning) spring.Kick(usrlist[0]);

            server.ConnectedUsers[FounderName].Process(new KickFromBattle() { BattleID = BattleID, Name = usrlist[0] });
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

                var mapi =  cache.GetResourceDataByInternalName(vals[0]);
                if (mapi != null)
                {
                    FounderUser.Process(new BattleUpdate() {Header = new BattleHeader() {BattleID = BattleID, Map = } })
                    throw new NotImplementedException();
                    //tas.ChangeMap(mapi.InternalName);
                }
            }
            else Respond(e, "Cannot find such map.");
        }

        public void ComMapRemote(TasSayEventArgs e, params string[] words)
        {
            if (HasRights("map", e, true)) ComMap(e, words);
            else if (HasRights("votemap", e, true)) RunCommand(e, "votemap", words);
            else Respond(e, "You do not have rights to change map");
        }


        public void ComPredict(TasSayEventArgs e, string[] words)
        {
            var b = this;
            var grouping = b.Users.Values.Where(u => !u.IsSpectator).GroupBy(u => u.AllyNumber).ToList();
            bool is1v1 = grouping.Count == 2 && grouping[0].Count() == 1 && grouping[1].Count() == 1;
            IGrouping<int, UserBattleStatus> oldg = null;
            foreach (var g in grouping)
            {
                if (oldg != null)
                {
                    var t1elo = oldg.Average(x => (is1v1 ? x.LobbyUser.Effective1v1Elo : x.LobbyUser.EffectiveElo));
                    var t2elo = g.Average(x => (is1v1 ? x.LobbyUser.Effective1v1Elo : x.LobbyUser.EffectiveElo));
                    Respond(e,
                            String.Format("team {0} has {1}% chance to win over team {2}",
                                          oldg.Key + 1,
                                          ZkData.Utils.GetWinChancePercent(t2elo - t1elo),
                                          g.Key + 1));
                }
                oldg = g;
            }
        }


        public void ComRehost(TasSayEventArgs e, string[] words)
        {
            /*if (spring.IsRunning)
            {
                Respond(e, "Cannot rehost while game is running");
                return;
            }*/
            if (words.Length == 0) OpenBattleRoom(null, null);
            else
            {
                string[] mods;
                int[] indexes;
                if (FilterMods(words, out mods, out indexes) == 0) Respond(e, "cannot find such game");
                else OpenBattleRoom(mods[0], null);
            }
        }

        public void ComResetOptions(TasSayEventArgs e, string[] words)
        {
            FounderUser.Process(new SetModOptions() { Options = new Dictionary<string, string>() });
            Respond(e, "Game options reset to defaults");
        }

        public void ComRing(TasSayEventArgs e, string[] words)
        {
            var usrlist = new List<string>();

            if (words.Length == 0)
            {
                // ringing idle
                foreach (var p in Users.Values)
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
                FounderUser.Process(new Say() { User = e.UserName, Text = "wants your attention", IsEmote = true, Ring = true, Place = SayPlace.Battle});
                rang += s + ", ";
            }

            //if (words.Length == 0 && usrlist.Count > 7) SayBattle("ringing all unready");
            //else SayBattle("ringing " + rang);
        }


        // user and rank info



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
                if (mode != AutohostMode.None)
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

            this.StartGame();
        }

        public void ComUpdateRapidMod(TasSayEventArgs e, string[] words)
        {
            if (string.IsNullOrEmpty(words[0]))
            {
                UpdateRapidMod(config.AutoUpdateRapidTag);
            }
            else UpdateRapidMod(words[0]);
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

        internal static int FilterMods(string[] words, ServerBattle ah, out string[] vals, out int[] indexes)
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


        internal static int FilterUsers(string[] words, ServerBattle ah, Spring spring, out string[] vals, out int[] indexes)
        {
            var b = ah;
            var i = 0;
            var temp = b.Users.Values.Select(u => u.Name).ToList();
            if (spring.IsRunning) foreach (var u in spring.StartContext.Players)
                {
                    if (!temp.Contains(u.Name)) temp.Add(u.Name);
                }
            return Filter(temp.ToArray(), words, out vals, out indexes);
        }


        public Dictionary<string, string> GetOptionsDictionary(TasSayEventArgs e, string[] words)
        {
            var s = Utils.Glue(words);
            var ret = new Dictionary<string, string>();
            var pairs = s.Split(new[] { ',' });
            if (pairs.Length == 0 || pairs[0].Length == 0)
            {
                Respond(e, "requires key=value format");
                return ret;
            }
            foreach (var pair in pairs)
            {
                var parts = pair.Split(new[] { '=' }, 2);
                if (parts.Length != 2)
                {
                    Respond(e, "requires key=value format");
                    return ret;
                }
                var key = parts[0].Trim(); //Trim() to make "key = value format" ignore whitespace 
                var val = parts[1].Trim();

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
                            ret[key] = val;
                        }
                        else Respond(e, "Value " + val + " is not valid for this option");

                        break;
                    }
                }
                if (!found)
                {
                    Respond(e, "No option called " + key + " found");
                    return ret;
                }
            }
            return ret;
        }

        public bool RunServerBalance(bool isGameStart, int? allyTeams, bool? clanWise)
        {
            try
            {
                var serv = GlobalConst.GetSpringieService();
                var context = GetContext();
                context.mode = mode;

                var balance = serv.BalanceTeams(context, isGameStart, allyTeams, clanWise);

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
throw new NotImplementedException();
            /*
            if (!string.IsNullOrEmpty(balance.Message)) SayBattle(balance.Message, false);
            if (balance.Players != null && balance.Players.Count > 0)
            {

                foreach (var user in Users.Values.Where(x => !x.IsSpectator && !balance.Players.Any(y => y.Name == x.Name))) tas.ForceSpectator(user.Name); // spec those that werent in response
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
            }*/
        }

        void ComHelp(TasSayEventArgs e, string[] words)
        {
            var ulevel = GetUserLevel(e);
            Respond(e, "---");
            foreach (var c in Commands.Commands) if (c.Level <= ulevel) Respond(e, " !" + c.Name + " " + c.HelpText);
            Respond(e, "---");
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
                Respond(e, "---");
                for (var i = 0; i < vals.Length; ++i) Respond(e, indexes[i] + ": " + vals[i]);
                Respond( e, "---");
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
                Respond(e, "---");
                for (var i = 0; i < vals.Length; ++i) Respond(e, indexes[i] + ": " + vals[i]);
                Respond(e, "---");
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


        void ComSetEngine(TasSayEventArgs e, string[] words)
        {
            throw new NotImplementedException();
            /*
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
                    var springCheck = Program.main.Downloader.GetAndSwitchEngine(specificVer, springPaths);//will trigger springPaths.SpringVersionChanged event
                    if (springCheck == null) ; //Respond(e, "Engine available");
                    else
                        Respond(e, "Downloading engine. " + springCheck.IndividualProgress + "%");

                    return;
                }).Start();
            }*/
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
            throw new NotImplementedException();
            /*
            if (spring.IsRunning)
            {
                Respond(e, "Cannot set options while the game is running");
                return;
            }
            var ret = GetOptionsDictionary(e, words);
            if (ret.Count > 0)
            {
                tas.UpdateModOptions(ret);
                Respond(e, "Options set");
            }*/
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

        static int FilterMaps(string[] words, ServerBattle ah, out string[] vals, out int[] indexes)
        {
            var result = ah.cache.FindResourceData(words, ResourceType.Map);
            vals = result.Select(x => x.InternalName).ToArray();
            indexes = result.Select(x => x.ResourceID).ToArray();
            return vals.Length;
        }


        int FilterUsers(string[] words, out string[] vals, out int[] indexes)
        {
            return FilterUsers(words, this, spring, out vals, out indexes);
        }


        void SayLines(TasSayEventArgs e, string what)
        {
            foreach (var line in what.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)) Respond(e, line);
        }

        public void ServerVerifyMap(bool pickNew)
        {
            try
            {
                if (!spring.IsRunning)
                {
                    var serv = GlobalConst.GetSpringieService();

                    Task.Factory.StartNew(() =>
                    {
                        RecommendedMapResult map;
                        try
                        {
                            map = serv.GetRecommendedMap(GetContext(), pickNew);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError(ex.ToString());
                            return;
                        }
                        if (map != null && map.MapName != null)
                        {
                            if (MapName != map.MapName)
                            {
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


        public void ComAddUser(TasSayEventArgs e, string[] words)
        {
            if (words.Length != 1) Respond(e, "Specify password");
            if (spring.IsRunning) spring.AddUser(e.UserName, words[0]);
        }

    }
}
