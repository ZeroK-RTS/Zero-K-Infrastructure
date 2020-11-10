using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using PlasmaShared;
using ZkData;
using Ratings;

namespace ZeroKWeb.SpringieInterface
{
    public class StartSetup
    {
        /// <summary>
        ///     Sets up all the things that Springie needs to know for the battle: how to balance, who to get extra commanders,
        ///     what PlanetWars structures to create, etc.
        /// </summary>
        public static LobbyHostingContext GetDedicatedServerStartSetup(LobbyHostingContext context)
        {
            var ret = context;
            try
            {
                var mode = context.Mode;

                var commProfiles = new LuaTable();
                var db = new ZkDataContext();

                // calculate to whom to send extra comms
                var accountIDsWithExtraComms = new Dictionary<int, int>();
                if (mode == AutohostMode.Planetwars || mode == AutohostMode.GameFFA || mode == AutohostMode.Teams)
                {
                    var groupedByTeam = context.Players.Where(x => !x.IsSpectator).GroupBy(x => x.AllyID).OrderByDescending(x => x.Count());
                    var biggest = groupedByTeam.FirstOrDefault();
                    if (biggest != null)
                    {
                        foreach (var other in groupedByTeam.Skip(1))
                        {
                            var cnt = biggest.Count() - other.Count();
                            if (cnt > 0)
                            {
                                // example case: 3 players on this team, 8 players on largest team
                                // 5 bonus comms to dole out to this team
                                // per_player = 1 (integer result of 5/3)
                                // remainder: 2, so now cnt = 2
                                // iterate over all players in this team
                                //  first player: cnt == 2, >0 so we give him a second extra comm
                                //  second player: cnt == 1, >0 so same deal
                                //  from now on cnt <= 0 so the last player only gets the one extra comm
                                int per_player = cnt / other.Count();
                                cnt = cnt % other.Count();
                                foreach (var a in other
                                    .Select(x => db.Accounts.First(y => y.AccountID == x.LobbyID))
                                    .OrderByDescending(x => x.GetRating(RatingCategory.Casual).Elo))
                                {
                                    accountIDsWithExtraComms.Add(a.AccountID, per_player + (cnt > 0 ? 1 : 0));
                                    cnt--;
                                }
                            }
                        }
                    }
                }


                // write Planetwars details to modoptions (for widget)
                Faction attacker = null;
                Faction defender = null;
                Planet planet = null;
                if (mode == AutohostMode.Planetwars)
                {
                    var galaxy = db.Galaxies.First(x => x.IsDefault);
                    planet = galaxy.Planets.First(x => x.Resource.InternalName == context.Map);
                    attacker =
                        context.Players.Where(x => x.AllyID == 0 && !x.IsSpectator)
                            .Select(x => db.Accounts.First(y => y.AccountID == x.LobbyID))
                            .Where(x => x.Faction != null)
                            .Select(x => x.Faction)
                            .First();

                    defender = planet.Faction;

                    if (attacker == defender) defender = null;

                    ret.ModOptions["attackingFaction"] = attacker.Shortcut;
                    ret.ModOptions["attackingFactionName"] = attacker.Name;
                    ret.ModOptions["attackingFactionColor"] = attacker.Color;
                    if (defender != null)
                    {
                        ret.ModOptions["defendingFaction"] = defender.Shortcut;
                        ret.ModOptions["defendingFactionName"] = defender.Name;
                        ret.ModOptions["defendingFactionColor"] = defender.Color;
                    }
                    else
                    {
                        ret.ModOptions["defendingFaction"] = "Mercenary";
                        ret.ModOptions["defendingFactionName"] = "Local militia";
                        ret.ModOptions["defendingFactionColor"] = "#CCCCCC";
                    }
                    ret.ModOptions["planet"] = planet.Name;
                    ret.ModOptions["pw_galaxyTurn"] = galaxy.Turn.ToString();

                    ret.ModOptions["pw_baseIP"] = GlobalConst.BaseInfluencePerBattle.ToString(CultureInfo.InvariantCulture);
                    ret.ModOptions["pw_dropshipIP"] = planet.GetEffectiveShipIpBonus(attacker).ToString(CultureInfo.InvariantCulture);
                    ret.ModOptions["pw_defenseIP"] = planet.GetEffectiveIpDefense().ToString(CultureInfo.InvariantCulture);
                    ret.ModOptions["pw_attackerIP"] = (planet.PlanetFactions.FirstOrDefault(x => x.FactionID == attacker.FactionID)?.Influence ?? 0).ToString(CultureInfo.InvariantCulture);
                    ret.ModOptions["pw_maxIP"] = GlobalConst.PlanetWarsMaximumIP.ToString(CultureInfo.InvariantCulture);
                    ret.ModOptions["pw_neededIP"] = GlobalConst.InfluenceToCapturePlanet.ToString(CultureInfo.InvariantCulture);
                    ret.ModOptions["pw_attackerWinLoseCC"] = GlobalConst.PlanetWarsAttackerWinLoseCcMultiplier.ToString(CultureInfo.InvariantCulture);
                    ret.ModOptions["pw_defenderWinKillCC"] = GlobalConst.PlanetWarsDefenderWinKillCcMultiplier.ToString(CultureInfo.InvariantCulture);
                }

                // write player custom keys (level, elo, is muted, etc.)
                foreach (var p in context.Players)
                {
                    var user = db.Accounts.Where(x => x.AccountID == p.LobbyID).Include(x => x.RelalationsByOwner).FirstOrDefault();
                    if (user != null)
                    {
                        var userParams = new Dictionary<string, string>();
                        ret.UserParameters[p.Name] = userParams;

                        userParams["LobbyID"] = user.AccountID.ToString();
                        userParams["CountryCode"] = user.HideCountry ? "??" : user.Country;

                        var userBanMuted = Punishment.GetActivePunishment(user.AccountID, null, null, null, x => x.BanMute) != null;
                        if (userBanMuted) userParams["muted"] = "1";
                        userParams["faction"] = user.Faction != null ? user.Faction.Shortcut : "";
                        userParams["clan"] = user.Clan != null ? user.Clan.Shortcut : "";
                        userParams["clanfull"] = user.Clan != null ? user.Clan.ClanName : "";

                        userParams["level"] = user.Level.ToString();
                        //userParams["mm_elo"] = Math.Round(user.EffectiveMmElo).ToString();
                        //userParams["casual_elo"] = Math.Round(user.EffectiveElo).ToString();

                        userParams["elo"] = Math.Round(user.GetRating(context.ApplicableRating).Elo).ToString();
                        userParams["elo_order"] = context.Players.Where(x => !x.IsSpectator)
                            .Select(x => db.Accounts.First(y => y.AccountID == x.LobbyID))
                            .Where(x => x.GetRating(context.ApplicableRating).Elo > user.GetRating(context.ApplicableRating).Elo)
                            .Count()
                            .ToString();

                        userParams["icon"] = user.GetIconName();
                        userParams["avatar"] = user.Avatar;
                        userParams["badges"] = string.Join(",", user.GetBadges());
                        userParams["admin"] = user.AdminLevel >= AdminLevel.Moderator ? "1" : "0";
                        userParams["room_boss"] = p.Name == context.FounderName ? "1" : "0";
                        if (p.PartyID.HasValue) userParams["PartyID"] = p.PartyID.ToString();

                        var userSpecChatBlocked = Punishment.GetActivePunishment(user.AccountID, null, null, null, x => x.BanSpecChat) != null; ;
                        userParams["can_spec_chat"] = userSpecChatBlocked ? "0" : "1";

                        userParams["ignored"] = string.Join(",", user.RelalationsByOwner.Where(x => x.Relation == Relation.Ignore).Select(x => x.Target.Name));
                        userParams["friends"] = string.Join(",", user.RelalationsByOwner.Where(x => x.Relation == Relation.Friend).Select(x => x.Target.Name));

                        if (!p.IsSpectator)
                        {
                            // set valid PW structure attackers
                            if (mode == AutohostMode.Planetwars)
                            {
                                userParams["pwRank"] = (user.AccountRolesByAccountID.Where(
                                            x =>
                                                !x.RoleType.IsClanOnly &&
                                                (x.RoleType.RestrictFactionID == null || x.RoleType.RestrictFactionID == user.FactionID)).OrderBy(x => x.RoleType.DisplayOrder).Select(x => (int?)x.RoleType.DisplayOrder).FirstOrDefault() ?? 999).ToString();


                                var allied = user.Faction != null && defender != null && user.Faction != defender &&
                                             defender.HasTreatyRight(user.Faction, x => x.EffectPreventIngamePwStructureDestruction == true, planet);

                                if (!allied && user.Faction != null && (user.Faction == attacker || user.Faction == defender))
                                {
                                    userParams["canAttackPwStructures"] = "1";
                                }

                                userParams["pwInstructions"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(GetPwInstructions(planet, user, db, attacker)));
                            }

                            if (accountIDsWithExtraComms.ContainsKey(user.AccountID)) userParams["extracomm"] = accountIDsWithExtraComms[user.AccountID].ToString();

                            var commProfileIDs = new LuaTable();
                            var userCommandersBanned = Punishment.GetActivePunishment(user.AccountID, null, null, null, x => x.BanCommanders) != null;
                            if (!userCommandersBanned)
                            {
                                // set up commander data
                                foreach (var c in user.Commanders.Where(x => x.Unlock != null && x.ProfileNumber <= GlobalConst.CommanderProfileCount)
                                    )
                                {
                                    try
                                    {
                                        var commProfile = new LuaTable();

                                        if (string.IsNullOrEmpty(c.Name) || c.Name.Any(x => x == '"'))
                                        {
                                            c.Name = c.CommanderID.ToString();
                                        }
                                        commProfiles.Add("c" + c.CommanderID, commProfile);
                                        commProfileIDs.Add("c" + c.CommanderID);

                                        // process decoration icons
                                        var decorations = new LuaTable();
                                        foreach (var d in
                                            c.CommanderDecorations.Where(x => x.Unlock != null).OrderBy(x => x.SlotID).Select(x => x.Unlock))
                                        {
                                            var iconData = db.CommanderDecorationIcons.FirstOrDefault(x => x.DecorationUnlockID == d.UnlockID);
                                            if (iconData != null)
                                            {
                                                string iconName = null, iconPosition = null;
                                                // FIXME: handle avatars and preset/custom icons
                                                if (iconData.IconType == (int)DecorationIconTypes.Faction)
                                                {
                                                    iconName = user.Faction != null ? user.Faction.Shortcut : null;
                                                }
                                                else if (iconData.IconType == (int)DecorationIconTypes.Clan)
                                                {
                                                    iconName = user.Clan != null ? user.Clan.Shortcut : null;
                                                }

                                                if (iconName != null)
                                                {
                                                    iconPosition = CommanderDecoration.GetIconPosition(d);
                                                    var entry = new LuaTable();
                                                    entry.Add("image", iconName);
                                                    decorations.Add("icon_" + iconPosition.ToLower(), entry);
                                                }
                                            }
                                            else decorations.Add(d.Code);
                                        }

                                        commProfile["name"] = LuaTable.SanitizeString(c.Name.Substring(0, Math.Min(25, c.Name.Length))) ?? "dummy";
                                        commProfile["chassis"] = c.Unlock.Code;
                                        commProfile["decorations"] = decorations;

                                        var modules = new LuaTable();
                                        commProfile["modules"] = modules;

                                        for (var i = 1; i <= GlobalConst.NumCommanderLevels; i++)
                                        {
                                            var modulesForLevel = new LuaTable();
                                            modules.Add(modulesForLevel);
                                            //var modulesOrdered = c.CommanderModules.Where(x => x.CommanderSlot.MorphLevel == i).ToList();
                                            var slots = db.CommanderSlots.ToList().Where(x => x.MorphLevel == i && (x.ChassisID == null || (x.ChassisID == c.ChassisUnlockID))).ToList();
                                            slots.Sort(delegate (CommanderSlot x, CommanderSlot y)
                                            {
                                                UnlockTypes type1 = x.UnlockType;
                                                UnlockTypes type2 = y.UnlockType;
                                                if (type1 == UnlockTypes.WeaponManualFire || type1 == UnlockTypes.WeaponBoth)
                                                    type1 = UnlockTypes.Weapon;
                                                if (type2 == UnlockTypes.WeaponManualFire || type2 == UnlockTypes.WeaponBoth)
                                                    type2 = UnlockTypes.Weapon;
                                                int result = type1.CompareTo(type2);
                                                if (result == 0) return x.CommanderSlotID.CompareTo(y.CommanderSlotID);
                                                else return result;
                                            });
                                            foreach (var slot in slots)
                                            {
                                                String value = String.Empty;
                                                var module = c.CommanderModules.FirstOrDefault(x => x.SlotID == slot.CommanderSlotID);
                                                if (module != null)
                                                    value = module.Unlock.Code;
                                                modulesForLevel.Add(value);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Trace.TraceError(ex.ToString());
                                        throw new ApplicationException(
                                            $"Error processing commander: {c.CommanderID} - {c.Name} of player {user.AccountID} - {user.Name}",
                                            ex);
                                    }
                                }
                            }
                            else userParams["jokecomm"] = "1";

                            userParams["commanders"] = commProfileIDs.ToBase64String();
                        }
                    }
                }
                ret.ModOptions["commanderTypes"] = commProfiles.ToBase64String();

                /* General-purpose identifier.
                 * Prefer the more specific ones below when possible */
                ret.ModOptions["serverType"] = "ZKLS";

                /* Access to commands normally accessible only by the host.
                 * Lua calls prepend the / on their own, but not the autohost,
                 * so /say doesn't need it, but the cheat command does */
                ret.ModOptions["cheatCommandPrefix"] = "say !hostsay /";

                /* The server is listening for SPRINGIE strings (the game can skip those otherwise).
                 * See https://github.com/ZeroK-RTS/Zero-K-Infrastructure/blob/master/Shared/LobbyClient/DedicatedServer.cs#L317 */
                ret.ModOptions["sendSpringieData"] = "1";

                // set PW structures
                if (mode == AutohostMode.Planetwars)
                {
                    var owner = planet.Faction != null ? planet.Faction.Shortcut : "";

                    var pwStructures = new LuaTable();
                    foreach (
                        var s in planet.PlanetStructures.Where(x => x.StructureType != null && !string.IsNullOrEmpty(x.StructureType.IngameUnitName)))
                    {
                        pwStructures.Add(s.StructureType.IngameUnitName,
                            new LuaTable
                            {
                                { "unitname", s.StructureType.IngameUnitName },
                                { "owner", s.Account?.Name },
                                { "canBeEvacuated", s.StructureType.IsIngameEvacuable },
                                { "canBeDestroyed", s.StructureType.IsIngameDestructible },
                                { "isInactive", !s.IsActive},
                                {
                                    "name", $"{owner} {s.StructureType.Name} ({(s.Account != null ? s.Account.Name : "unowned")})" },
                                { "description", s.StructureType.Description }
                            });
                    }
                    ret.ModOptions["planetwarsStructures"] = pwStructures.ToBase64String();
                }

                return ret;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                throw;
            }
        }

        private static string GetPwInstructions(Planet planet, Account user, ZkDataContext db, Faction attacker)
        {
            StringBuilder sb = new StringBuilder();
            var ipBase = GlobalConst.BaseInfluencePerBattle;
            var ipShips = planet.GetEffectiveShipIpBonus(attacker);
            var ipDefs = planet.GetEffectiveIpDefense();
            var attackerWinLoseCc = (ipShips + ipBase - ipDefs) * GlobalConst.PlanetWarsAttackerWinLoseCcMultiplier;
            var attackerLoseKillCc = (ipShips + ipBase - ipDefs) * GlobalConst.PlanetWarsDefenderWinKillCcMultiplier;

            attackerWinLoseCc = Math.Max(attackerWinLoseCc, 0);
            attackerLoseKillCc = Math.Max(attackerLoseKillCc, 0);

            var attackerIp = planet.PlanetFactions.FirstOrDefault(x => x.FactionID == attacker.FactionID)?.Influence;


            if (user.Faction == attacker)
            {
                sb.AppendFormat("You are attacking the {0} planet {1} and have {2:N1} of the {3:N1} influence required to conquer this planet.\n\n",
                    planet.Faction != null ? planet.Faction.Name : "neutral",
                    planet.Name,
                    attackerIp != null ? attackerIp : 0,
                    GlobalConst.InfluenceToCapturePlanet);

                sb.AppendFormat("If you win with your Command Center intact you will gain {0:N1} influence ({1:N1} base + {2:N1} from dropships - {3:N1} from defense). " +
                                "Protect your Command Center as, if you win without it, you will only gain {4:N1} influence.\n\n",
                    ipBase + ipShips - ipDefs,
                    ipBase,
                    ipShips,
                    ipDefs,
                    attackerWinLoseCc);

                sb.AppendFormat("If you are losing, try to kill enemy Command Center as destroying it gains you {0:N1} influence in defeat." +
                                " If you are defeated without destroying the enemy Command Center you will gain no influence.\n\n",
                                attackerLoseKillCc);

                if (planet.Faction != null)
                {
                    sb.AppendFormat("Destroy the planetary infrastructure as any structures destroyed here will be disabled on the strategic map." +
                                    " Destroy key structures first as the defenders may evacuate them." +
                                    " There is no need to delay victory as, if you win, all remaining structures are automatically destroyed.");
                }
            }
            else
            {
                sb.AppendFormat("You are defending the {0} planet {1} from {2}." +
                                " The attackers have {3:N1} of the {4:N1} influence required to conquer this planet.\n\n",
                    planet.Faction != null ? planet.Faction.Name : "neutral",
                    planet.Name,
                    attacker?.Shortcut,
                    attackerIp != null ? attackerIp : 0,
                    GlobalConst.InfluenceToCapturePlanet);

                sb.AppendFormat("If you win with your Command Center intact {0} will gain no influence." +
                                " Protect your Command Center as, if you win without it, the enemy will gain {1:N1} influence.\n\n",
                    attacker?.Shortcut,
                    attackerLoseKillCc);

                sb.AppendFormat("If you lose, {4} will gain {0:N1} influence ({1:N1} base + {2:N1} from dropships - {3:N1} from defense)." +
                                " If you are losing, try to kill the enemy Command Center, as they will only gain {5:N1} influence from victory.\n\n",
                    ipBase + ipShips - ipDefs,
                    ipBase,
                    ipShips,
                    ipDefs,
                    attacker?.Shortcut,
                    attackerWinLoseCc);

                if (planet.Faction != null)
                {
                    sb.AppendFormat("Protect your planetary infrastructure as any structures destroyed here will be disabled on the strategic map." +
                                    " Active wormhole generators allow you to periodically evacuate structures to safety." +
                                    " Hold out if you are losing as all remaining structures will be destroyed upon defeat." +
                                    " Do not destroy your own in anticipation of a loss as all structures are automatically disabled on newly conquered planets.");
                }
            }

            return sb.ToString();
        }
    }
}
