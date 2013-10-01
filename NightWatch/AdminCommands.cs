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
                            var reason = parts[2] ?? "";
                            if (player != null)
                            {
                                tas.AdminKickFromLobby(parts[1], reason);
                            }
                            else tas.Say(TasClient.SayPlace.User, e.UserName, "Not a valid player name", false);
                        }
                    }
                }
            }
        }
    }
}