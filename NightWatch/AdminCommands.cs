using System.Linq;
using LobbyClient;
using ZkData;

namespace CaTracker
{
    public class AdminCommands
    {
        readonly TasClient tas;

        public AdminCommands(TasClient tas)
        {
            this.tas = tas;
            tas.Said += tas_Said;
        }

        void tas_Said(object sender, TasSayEventArgs e)
        {
            if (e.Place == TasSayEventArgs.Places.Normal)
            {
                if (e.Text.StartsWith("!kick"))
                {
                    var db = new ZkDataContext();
                    var acc = Account.AccountByLobbyID(db, tas.ExistingUsers[e.UserName].LobbyID);
                    if (acc.IsZeroKAdmin || acc.IsLobbyAdministrator)
                    {
                        var parts = e.Text.Split(' ');
                        if (!(parts.Length >= 2)) tas.Say(TasClient.SayPlace.User, e.UserName, "!kick [player] [reason]", false);
                        else
                        {
                            var player = tas.ExistingUsers.FirstOrDefault(x => x.Key == parts[1]).Key;
                            var reasonParts = ((string[])parts.Clone()).ToList();
                            reasonParts.RemoveRange(0, 2);
                            string reason = string.Concat(reasonParts) ?? "";
                            if (player != null)
                            {
                                tas.AdminKickFromLobby(player, reason);
                            }
                            else tas.Say(TasClient.SayPlace.User, e.UserName, "Not a valid player name", false);
                        }
                    }
                }
                else if (e.Text.StartsWith("!op"))
                {
                    var db = new ZkDataContext();
                    var acc = Account.AccountByLobbyID(db, tas.ExistingUsers[e.UserName].LobbyID);
                    if (acc.IsZeroKAdmin || acc.IsLobbyAdministrator)
                    {
                        var parts = e.Text.Split(' ');
                        if (parts.Length != 3) tas.Say(TasClient.SayPlace.User, e.UserName, "!op [player] [channel]", false);
                        else
                        {
                            var player = tas.ExistingUsers.FirstOrDefault(x => x.Key == parts[1]).Key;
                            var channel = tas.ExistingChannels.FirstOrDefault(x => x.Key == "#" + parts[2]).Key;
                            if (player != null)
                            {
                                tas.Say(TasClient.SayPlace.User, "ChanServ", string.Format("!op {0} {1}", channel, player), false);
                                tas.Say(TasClient.SayPlace.User, e.UserName, string.Format("DEBUG: !op {0} {1}", channel, player), false);
                            }
                            else tas.Say(TasClient.SayPlace.User, e.UserName, "Not a valid player name", false);
                        }
                    }
                }
            }
        }
    }
}