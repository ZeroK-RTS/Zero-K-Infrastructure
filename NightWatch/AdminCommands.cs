using System.Linq;
using LobbyClient;
using ZkData;

namespace CaTracker
{
    public class AdminCommands
    {
        readonly TasClient tas;

        public static string[] adminCommands = { "kick", "op", "changeaccountpass", "chanserv" };

        public AdminCommands(TasClient tas)
        {
            this.tas = tas;
            tas.Said += tas_Said;
        }

        void tas_Said(object sender, TasSayEventArgs e)
        {
            if (e.UserName.Contains("Nightwatch")) return;

            if (e.Place == TasSayEventArgs.Places.Normal)
            {
                foreach (string command in adminCommands)
                {
                    if (e.Text.StartsWith("!" + command))
                    {
                        var db = new ZkDataContext();
                        var acc = db.Accounts.Find(tas.ExistingUsers[e.UserName].LobbyID);
                        if (!(acc.IsZeroKAdmin || acc.IsLobbyAdministrator)) return;
                        break;
                    }
                }

                if (e.Text.StartsWith("!kick"))
                {
                    var parts = e.Text.Split(' ');
                    if (!(parts.Length >= 2)) tas.Say(TasClient.SayPlace.User, e.UserName, "!kick [player] [reason]", false);
                    else
                    {
                        var player = tas.ExistingUsers.FirstOrDefault(x => x.Key == parts[1]).Key;
                        var reasonParts = ((string[])parts.Clone()).ToList();
                        reasonParts.RemoveRange(0, 2);
                        string reason = string.Join(" ", reasonParts) ?? "";
                        if (player != null)
                        {
                            tas.AdminKickFromLobby(player, reason);
                        }
                        else tas.Say(TasClient.SayPlace.User, e.UserName, "Not a valid player name", false);
                    }
                }
                else if (e.Text.StartsWith("!op"))
                {
                    var parts = e.Text.Split(' ');
                    if (parts.Length != 3) tas.Say(TasClient.SayPlace.User, e.UserName, "!op [player] [channel]", false);
                    else
                    {
                        var player = tas.ExistingUsers.FirstOrDefault(x => x.Key == parts[1]).Key;
                        var channel = parts[2];
                        if (player != null)
                        {
                            tas.Say(TasClient.SayPlace.User, "ChanServ", string.Format("!op {0} {1}", channel, player), false);
                            tas.Say(TasClient.SayPlace.User, e.UserName, string.Format("DEBUG: !op {0} {1}", channel, player), false);
                        }
                        else tas.Say(TasClient.SayPlace.User, e.UserName, "Not a valid player name", false);
                    }
                }
                else if (e.Text.StartsWith("!changeaccountpass"))
                {
                    var parts = e.Text.Split(' ');
                    if (parts.Length != 3) tas.Say(TasClient.SayPlace.User, e.UserName, "!changeaccountpass [player] [password (plaintext)]", false);
                    else
                    {
                        var password = ZkData.Utils.HashLobbyPassword(parts[2]);
                        tas.SendRaw(string.Format("CHANGEACCOUNTPASS {0} {1}", parts[1], password));
                        tas.Say(TasClient.SayPlace.User, e.UserName, string.Format("DEBUG: CHANGEACCOUNTPASS {0} {1}", parts[1], password), false);
                    }
                }
                else if (e.Text.StartsWith("!chanserv"))
                {
                    var command = e.Text.Substring(9).TrimStart();
                    tas.Say(TasClient.SayPlace.User, "ChanServ", "!" + command, false);
                }
            }
        }
    }
}