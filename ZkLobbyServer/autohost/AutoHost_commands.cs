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



        public Dictionary<string, string> GetOptionsDictionary(Say e, string s)
        {
            var ret = new Dictionary<string, string>();
            if (HostedModInfo == null) return ret; 

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
                var mod = HostedModInfo;
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


        public int FilterUsers(string[] words, out string[] vals, out int[] indexes)
        {
            return FilterUsers(words, this, spring, out vals, out indexes);
        }

    }
}
