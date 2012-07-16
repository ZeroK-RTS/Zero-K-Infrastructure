using System.Linq;
using ZkData;

namespace ZeroKWeb.SpringieInterface
{
    public class PlayerJoinResult
    {
        public bool ForceSpec;
        public bool Kick;
        public string PrivateMessage;
        public string PublicMessage;
    }


    public class PlayerJoinHandler
    {
        public static PlayerJoinResult AutohostPlayerJoined(BattleContext context, int accountID) {
            var res = new PlayerJoinResult();
            var db = new ZkDataContext();
            AutohostMode mode = context.GetMode();
            if (mode == AutohostMode.Planetwars) {
                Planet planet = db.Galaxies.Single(x => x.IsDefault).Planets.Single(x => x.Resource.InternalName == context.Map);
                Account account = Account.AccountByLobbyID(db, accountID); // accountID is in fact lobbyID

                // conscription
                /*
                if (account.FactionID == null)
                {
                    var rand = new Random();
                    var faclist = db.Factions.ToList();
                    var fac = faclist[rand.Next(faclist.Count)];
                    account.FactionID = fac.FactionID;
                    db.Events.InsertOnSubmit(Global.CreateEvent("{0} was conscripted by {1}", account, fac));
                    db.SubmitChanges();
                    AuthServiceClient.SendLobbyMessage(account,
                                                       string.Format(
                                                           "You must be in a faction to play PlanetWars.  You were conscripted by {0}. To change your faction go to http://zero-k.info/Clans ",
                                                           fac.Name));
                    return string.Format("Sending {0} to {1}", account.Name, fac.Name);
                }
                 */

                if (account.Level < context.GetConfig().MinLevel) {
                    AuthServiceClient.SendLobbyMessage(account,
                                                       string.Format(
                                                           "Sorry, PlanetWars is competive online campaign for experienced players. You need to be at least level {0} to play here. To increase your level, play more games on other hosts or open multiplayer game and play against computer AI bots.  You can observe this game however.",
                                                           context.GetConfig().MinLevel));
                }

                if (account.Clan == null) {
                    //AuthServiceClient.SendLobbyMessage(account, "To play here, join a clan first http://zero-k.info/Clans");
                    res.PrivateMessage =
                        string.Format(
                            "{0} this is competetive PlanetWars campaign server. Join a clan to conquer the galaxy http://zero-k.info/Clans",
                            account.Name);
                    return res;
                }

                /*if (!account.Name.Contains(account.Clan.Shortcut))
                {
                    AuthServiceClient.SendLobbyMessage(account,
                                                       string.Format(
                                                           "Your name must contain clan tag {0}, rename for example by saying: \"/rename [{0}]{1}\" or \"/rename {0}_{1}\".",
                                                           account.Clan.Shortcut,
                                                           account.Name));
                    return string.Format("{0} cannot play, name must contain clan tag {1}", account.Name, account.Clan.Shortcut);
                }*/
                string owner = "";
                if (planet.Account != null) owner = planet.Account.Name;
                string facRoles = string.Join(",",
                                              account.AccountRolesByAccountID.Where(x => !x.RoleType.IsClanOnly).Select(x => x.RoleType.Name).ToList());
                if (!string.IsNullOrEmpty(facRoles)) facRoles += " of " + account.Faction.Name + ", ";

                string clanRoles = string.Join(",",
                                               account.AccountRolesByAccountID.Where(x => x.RoleType.IsClanOnly).Select(x => x.RoleType.Name).ToList());
                if (!string.IsNullOrEmpty(clanRoles)) clanRoles += " of " + account.Clan.ClanName;

                res.PublicMessage = string.Format("Greetings {0} {1}{2}, welcome to {3} planet {4} http://zero-k.info/PlanetWars/Planet/{5}",
                                                  account.Name,
                                                  facRoles,
                                                  clanRoles,
                                                  owner,
                                                  planet.Name,
                                                  planet.PlanetID);

                return res;
            }
            return null;
        }
    }
}