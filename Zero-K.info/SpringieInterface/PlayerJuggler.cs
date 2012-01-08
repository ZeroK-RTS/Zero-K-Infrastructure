using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public class PlayerEntry {
            public Account Account;
            public Bin CurrentBin;
        }

        public class Bin {
            public JugglerAutohost Autohost;
            public List<int> HighPriority = new List<int>();
            public List<int> NormalPriority = new List<int>();
            public List<int> Assigned = new List<int>();
        }


        public static JugglerResult JugglePlayers(List<JugglerAutohost> autohosts)
        {
            var ret = new JugglerResult();
            var bins = new List<Bin>();
            var db= new ZkDataContext();
            
            List<int?> lobbyIds = new List<int?>();

            foreach (var ah in autohosts) {
                if (ah.RunningGameStartContext == null) lobbyIds.AddRange(ah.LobbyContext.Players.Where(x=>!x.IsSpectator).Select(x=>(int?)x.LobbyID)); // game not running add all nonspecs
                else lobbyIds.AddRange(ah.LobbyContext.Players.Where(x=>!x.IsSpectator && !ah.RunningGameStartContext.Players.Any(y=>y.LobbyID==x.LobbyID && !y.IsSpectator)).Select(x=>(int?)x.LobbyID)); // game running, add all those that are not playing and are not specs
            }

            var juggledAccounts = db.Accounts.Where(x=>lobbyIds.Contains(x.LobbyID)).ToDictionary(x=>x.LobbyID??0);


            foreach (var grp in autohosts.Where(x => x.RunningGameStartContext == null && x.LobbyContext != null && x.LobbyContext.Players.Any(y=>!y.IsSpectator)).GroupBy(x => x.LobbyContext.GetMode())) {
                if (grp.Key == AutohostMode.Game1v1)
                {
                    // make bins from all 1v1 autohost
                    foreach (var ah in grp) {
                        var bin = new Bin() { Autohost = ah };
                        bin.Assigned.AddRange(ah.LobbyContext.Players.Where(x => !x.IsSpectator && juggledAccounts.ContainsKey(x.LobbyID)).Select(x=>x.LobbyID));
                        bins.Add(bin); 
                    }
                }
                else {
                    //make one bin from biggest ah of other type
                    var biggest = grp.OrderByDescending(x => x.LobbyContext.Players.Count(y => !y.IsSpectator)).First();
                    var bin = new Bin() { Autohost = biggest };
                    foreach (var ah in grp) bin.Assigned.AddRange(ah.LobbyContext.Players.Where(x => !x.IsSpectator && juggledAccounts.ContainsKey(x.LobbyID)).Select(x => x.LobbyID)); // add all valid players from all ah to this bin
                    
                    bins.Add(bin); 
                }
            }

            var sb = new StringBuilder();
            foreach (var b in bins) {
                sb.AppendFormat("{0}: {1}\n", b.Autohost.LobbyContext.AutohostName, string.Join(",", b.Assigned.Select(x => juggledAccounts[x].Name)));
            }
            sb.AppendFormat("Free people: {0}\n", string.Join(",", juggledAccounts.Where(x => !bins.Any(y => y.Assigned.Contains(x.Key))).Select(x => x.Value.Name)));
            
            ret.Message = sb.ToString();
            return ret;
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