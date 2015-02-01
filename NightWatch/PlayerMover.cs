using System.Linq;
using LobbyClient;
using ZkData;

namespace CaTracker
{
    public class PlayerMover
    {
        readonly TasClient tas;

        public PlayerMover(TasClient tas)
        {
            this.tas = tas;
            tas.Said += tas_Said;
        }

        void tas_Said(object sender, TasSayEventArgs e)
        {
            if (e.UserName.Contains("Nightwatch")) return;

            if (e.Place == SayPlace.User)
            {
                if (e.Text.StartsWith("!move"))
                {
                    var db = new ZkDataContext();
                    var acc = db.Accounts.Find(tas.ExistingUsers[e.UserName].AccountID);
                    if (acc.IsZeroKAdmin)
                    {
                        var parts = e.Text.Split(' ');
                        if (parts.Length != 3) tas.Say(SayPlace.User, e.UserName, "!move [from] [to]", false);
                        else
                        {
                            var from = tas.ExistingBattles.Values.FirstOrDefault(x => x.Founder.Name == parts[1]);
                            var to = tas.ExistingBattles.Values.FirstOrDefault(x => x.Founder.Name == parts[2]);
                            if (from != null && to != null)
                            {
                                foreach (var b in from.Users.Values) if (!b.LobbyUser.IsInGame && b.Name != from.Founder.Name) tas.ForceJoinBattle(b.Name, to.BattleID);
                            }
                            else tas.Say(SayPlace.User, e.UserName, "Not a valid battle host name", false);
                        }
                    }
                }
                // split players evenly into two games by median elo -> expand to specify proportion to shunt?
                // TODO: split players and specs separately
                else if (e.Text.StartsWith("!splitplayers"))
                {
                    var db = new ZkDataContext();
                    var acc = db.Accounts.Find(tas.ExistingUsers[e.UserName].AccountID);
                    if (acc.IsZeroKAdmin)
                    {
                        var parts = e.Text.Split(' ');
                        if (parts.Length != 3) tas.Say(SayPlace.User, e.UserName, "!splitplayers [from] [to]", false);
                        else
                        {
                            var from = tas.ExistingBattles.Values.FirstOrDefault(x => x.Founder.Name == parts[1]);
                            var to = tas.ExistingBattles.Values.FirstOrDefault(x => x.Founder.Name == parts[2]);
                            if (from != null && to != null)
                            {
                                var list = from.Users.Values.Where(x=>!x.LobbyUser.IsInGame && x.Name != from.Founder.Name && !x.IsSpectator).OrderBy(x => x.LobbyUser.EffectiveElo);
                                var toMove = list.Take(list.Count() / 2);
                                foreach (var b in toMove) tas.ForceJoinBattle(b.Name, to.BattleID);
                            }
                            else tas.Say(SayPlace.User, e.UserName, "Not a valid battle host name", false);
                        }
                    }
                }
            }
        }
    }
}