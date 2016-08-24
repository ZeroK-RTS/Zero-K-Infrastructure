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

        
        public void ComResetOptions(Say e, string[] words)
        {
            throw new NotImplementedException();
            //FounderUser.Process(new SetModOptions() { Options = new Dictionary<string, string>() });
            //Respond(e, "Game options reset to defaults");
        }



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
