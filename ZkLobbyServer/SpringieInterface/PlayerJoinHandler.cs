using System.Linq;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.SpringieInterface
{
    public class PlayerJoinHandler
    {
        /// <summary>
        /// Writes join messages and tells <see cref="Springie"/> to force spectate player if needed
        /// </summary>
        public static PlayerJoinResult AutohostPlayerJoined(BattleContext context, int accountID) {
            var res = new PlayerJoinResult();
            var db = new ZkDataContext();
            AutohostMode mode = context.GetMode();

            if (mode == AutohostMode.Planetwars) {
                Planet planet = db.Galaxies.Single(x => x.IsDefault).Planets.SingleOrDefault(x => x.Resource.InternalName == context.Map);
                if (planet == null) {
                    res.PublicMessage = "Invalid map";
                    return res;
                }
                Account account = db.Accounts.Find(accountID); // accountID is in fact lobbyID

                if (account != null) {
                    string owner = "";
                    if (planet.Account != null) owner = planet.Account.Name;
                    string facRoles = string.Join(",",
                                                  account.AccountRolesByAccountID.Where(x => !x.RoleType.IsClanOnly).Select(x => x.RoleType.Name).ToList());
                    if (!string.IsNullOrEmpty(facRoles)) facRoles += " of " + account.Faction.Name + ", ";

                    string clanRoles = string.Join(",",
                                                   account.AccountRolesByAccountID.Where(x => x.RoleType.IsClanOnly).Select(x => x.RoleType.Name).ToList());
                    if (!string.IsNullOrEmpty(clanRoles)) clanRoles += " of " + account.Clan.ClanName;

                    res.PublicMessage = string.Format("Greetings {0} {1}{2}, welcome to {3} planet {4} {6}/PlanetWars/Planet/{5}",
                                                      account.Name,
                                                      facRoles,
                                                      clanRoles,
                                                      owner,
                                                      planet.Name,
                                                      planet.PlanetID,
                                                      GlobalConst.BaseSiteUrl);

                    return res;
                }
            }

            return null;
        }
    }
}