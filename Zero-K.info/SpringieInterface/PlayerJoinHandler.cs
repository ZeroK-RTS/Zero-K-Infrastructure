using System.Linq;
using LobbyClient;
using MumbleIntegration;
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
                Planet planet = db.Galaxies.Single(x => x.IsDefault).Planets.SingleOrDefault(x => x.Resource.InternalName == context.Map);
                if (planet == null) {
                    res.PublicMessage = "Invalid map";
                    return res;
                }
                Account account = Account.AccountByLobbyID(db, accountID); // accountID is in fact lobbyID

                if (account != null)
                {
                    if (account.Level < context.GetConfig().MinLevel)
                    {
                        res.PrivateMessage = string.Format("Sorry, PlanetWars is competive online campaign for experienced players. You need to be at least level {0} to play here. To increase your level, play more games on other hosts or open multiplayer game and play against computer AI bots.  You can still spectate this game, however.",
                                                               context.GetConfig().MinLevel);
                        res.ForceSpec = true;
                        return res;
                    }

                    /*
                    if (account.Faction == null)
                    {
                        res.PrivateMessage =
                            string.Format(
                                "{0} this is competitive PlanetWars campaign server. Join a clan to conquer the galaxy http://zero-k.info/Factions",
                                account.Name);
                        return res;
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
            }
            Account acc = Account.AccountByLobbyID(db, accountID); // accountID is in fact lobbyID

            if (acc != null)
            {
                AutohostConfig config = context.GetConfig();
                if (acc.Level < config.MinLevel)
                {
                    res.PrivateMessage = string.Format("Sorry, you need to be at least level {0} to play here. To increase your level, play more games on other hosts or open multiplayer game and play against computer AI bots. You can still spectate this game, however.",
                                                           config.MinLevel);
                    res.ForceSpec = true;
                    return res;
                }
                else if (acc.Level > config.MaxLevel)
                {
                    res.PrivateMessage = string.Format("Sorry, your level must be {0} or lower to play here. Pick on someone your own size! You can still spectate this game, however.",
                                                           config.MaxLevel);
                    res.ForceSpec = true;
                    return res;
                }

                // FIXME: use 1v1 Elo for 1v1
                if (acc.EffectiveElo < config.MinElo)
                {
                    res.PrivateMessage = string.Format("Sorry, you need to have an Elo rating of at least {0} to play here. Win games against human opponents to raise your Elo. You can still spectate this game, however.",
                                                           config.MinElo);
                    res.ForceSpec = true;
                    return res;
                }
                else if (acc.EffectiveElo > config.MaxElo)
                {
                    res.PrivateMessage = string.Format("Sorry, your Elo rating must be {0} or lower to play here. Pick on someone your own size! You can still spectate this game, however.",
                                                           config.MaxElo);
                    res.ForceSpec = true;
                    return res;
                }
            }

            return null;
        }
    }
}