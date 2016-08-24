#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZeroKWeb.SpringieInterface;
using ZkData;
#endregion

namespace ZkLobbyServer
{
    public partial class ServerBattle
    {
        public const int MaxMapListLength = 100; //400

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
            if (HostedMod.Mission != null)
            {
                alliances = 0;
                allyno = 0;
                // TODO HACK implement mission commshare
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




        public void ComBalance(Say e, string[] words)
        {
            var teamCount = 0;
            if (words.Length > 0) Int32.TryParse(words[0], out teamCount);

            RunServerBalance(false, teamCount == 0 ? (int?)null : teamCount, false);
        }


        public void ComCBalance(Say e, string[] words)
        {
            var teamCount = 2;
            if (words.Length > 0) Int32.TryParse(words[0], out teamCount);
            else teamCount = 2;
            RunServerBalance(false, teamCount, true);
        }


        public void ComExit(Say e, string[] words)
        {
            if (spring.IsRunning) SayBattle("exiting game");
            else Respond(e, "cannot exit, not in game");
            spring.ExitGame();
        }


        public void ComForce(Say e, string[] words)
        {
            if (spring.IsRunning)
            {
                SayBattle("forcing game start by " + e.User);
                spring.ForceStart();
            }
            else Respond(e, "cannot force, game not started");
        }

        public void ComForceSpectator(Say e, string[] words)
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

        public void ComForceSpectatorAfk(Say e, string[] words)
        {
            foreach (var u in Users.Values) if (!u.IsSpectator && u.LobbyUser.IsAway) ComForceSpectator(e, new[] { u.Name });
        }

        public void ComForceStart(Say e, string[] words)
        {
            SayBattle("please wait, game is about to start");
            StopVote();
            StartGame();
        }


        public class KickedPlayer
        {
            public string Name;
            public DateTime TimeOfKicked = DateTime.UtcNow;
        }

        public List<KickedPlayer> kickedPlayers = new List<KickedPlayer>();

        public void ComKick(Say e, string[] words)
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



        public void ComPredict(Say e, string[] words)
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


        public void ComResetOptions(Say e, string[] words)
        {
            throw new NotImplementedException();
            //FounderUser.Process(new SetModOptions() { Options = new Dictionary<string, string>() });
            //Respond(e, "Game options reset to defaults");
        }

        public async Task ComRing(Say e, string[] words)
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
                await server.GhostSay(new Say() { User = e.User, Target = s, Text = e.User + " wants your attention", IsEmote = true, Ring = true, Place = SayPlace.User });
                rang += s + ", ";
            }

            //if (words.Length == 0 && usrlist.Count > 7) SayBattle("ringing all unready");
            //else SayBattle("ringing " + rang);
        }


        // user and rank info



        public void ComStart(Say e, string[] words)
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

            if (this.Mode != AutohostMode.None)
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
                if (this.Mode != AutohostMode.None)
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

            this.StartGame();
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



        public Dictionary<string, string> GetOptionsDictionary(Say e, string[] words)
        {
            return new Dictionary<string, string>();
            // TODO hack reimplement
            /*
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
            return ret;*/
        }

        public bool RunServerBalance(bool isGameStart, int? allyTeams, bool? clanWise)
        {
            try
            {
                var context = GetContext();
                context.mode = Mode;
                var balance = Balancer.BalanceTeams(context, isGameStart, allyTeams, clanWise);
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

/*        void ComHelp(Say e, string[] words)
        {
            var ulevel = GetUserLevel(e);
            Respond(e, "---");
            foreach (var c in Commands.Commands) if (c.Level <= ulevel) Respond(e, " !" + c.Name + " " + c.HelpText);
            Respond(e, "---");
        }*/



        void ComListOptions(Say e, string[] words)
        {
         /*   var mod = hostedMod;
            if (mod.Options.Length == 0) Respond(e, "this mod has no options");
            else foreach (var opt in mod.Options) Respond(e, opt.ToString());*/
        }


        void ComNotify(Say e, string[] words)
        {
            if (!toNotify.Contains(e.User)) toNotify.Add(e.User);
            Respond(e, "I will notify you when the game ends.");
        }

        void ComSetGameTitle(Say e, string[] words)
        {
            throw new NotImplementedException();
            /*
            if (words.Length == 0) Respond(e, "this command needs one parameter - new game title");
            else
            {
                config.Title = Utils.Glue(words);
                Respond(e, "game title changed");
            }*/
        }

        void ComSetMaxPlayers(Say e, string[] words)
        {
            throw new NotImplementedException();
            /*
            if (words.Length == 0) Respond(e, "this command needs one parameter - number of players");
            else
            {
                int plr;
                Int32.TryParse(words[0], out plr);
                if (plr < 1) plr = 1;
                if (plr > Spring.MaxTeams) plr = Spring.MaxTeams;
                config.MaxPlayers = plr;
                Respond(e, "server size changed");
            }*/
        }


        void ComSetOption(Say e, string[] words)
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

        void ComSetPassword(Say e, string[] words)
        {
            throw new NotImplementedException();
            /*if (words.Length == 0)
            {
                config.Password = "";
                Respond(e, "password removed");
            }
            else
            {
                config.Password = words[0];
                Respond(e, "password changed");
            }*/
        }

        void ComTransmit(Say e, string[] words)
        {
            if (words.Length == 0)
            {
                Respond(e, "This command needs 1 parameter (transmit text)");
                return;
            }
            if (spring.IsRunning) spring.SayGame(String.Format("[{0}]{1}", e.User, "!transmit " + Utils.Glue(words)));
        }

        public int FilterUsers(string[] words, out string[] vals, out int[] indexes)
        {
            return FilterUsers(words, this, spring, out vals, out indexes);
        }


        public void ComAddUser(Say e, string[] words)
        {
            if (words.Length != 1) Respond(e, "Specify password");
            if (spring.IsRunning) spring.AddUser(e.User, words[0]);
        }

    }
}
