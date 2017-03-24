using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
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
                var accountIDsWithExtraComms = new List<int>();
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
                                foreach (var a in
                                    other.Select(x => db.Accounts.First(y => y.AccountID == x.LobbyID))
                                        .OrderByDescending(x => x.Elo*x.EloWeight)
                                        .Take(cnt)) accountIDsWithExtraComms.Add(a.AccountID);
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
                    planet = db.Galaxies.First(x => x.IsDefault).Planets.First(x => x.Resource.InternalName == context.Map);
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
                }

                // write player custom keys (level, elo, is muted, etc.)
                foreach (var p in context.Players)
                {
                    var user = db.Accounts.Where(x=>x.AccountID == p.LobbyID).Include(x=>x.RelalationsByOwner).FirstOrDefault();
                    if (user != null)
                    {
                        var userParams = new Dictionary<string, string>();
                        ret.UserParameters[p.Name] = userParams;

                        userParams["LobbyID"] = user.AccountID.ToString();
                        userParams["CountryCode"] = user.Country;

                        var userBanMuted = Punishment.GetActivePunishment(user.AccountID, null, null, x => x.BanMute) != null;
                        if (userBanMuted) userParams["muted"] = "1";
                        userParams["faction"] = user.Faction != null ? user.Faction.Shortcut : "";
                        userParams["clan"] = user.Clan != null ? user.Clan.Shortcut : "";
                        userParams["clanfull"] = user.Clan != null ? user.Clan.ClanName : "";

                        userParams["level"] = user.Level.ToString();
                        //userParams["mm_elo"] = Math.Round(user.EffectiveMmElo).ToString();
                        //userParams["casual_elo"] = Math.Round(user.EffectiveElo).ToString();

                        userParams["elo"] = Math.Round(RatingSystems.DisableRatingSystems ? user.BestEffectiveElo : user.GetBestRating().Elo).ToString();
                        
                        userParams["icon"] = user.GetIconName();
                        userParams["avatar"] = user.Avatar;
                        userParams["badges"] = string.Join(",", user.GetBadges());
                        userParams["admin"] = user.AdminLevel >= AdminLevel.Moderator ? "1" : "0";
                        if (p.PartyID.HasValue) userParams["PartyID"] = p.PartyID.ToString();

                        var userSpecChatBlocked = Punishment.GetActivePunishment(user.AccountID, null, null, x => x.BanSpecChat) != null; ;
                        userParams["can_spec_chat"] = userSpecChatBlocked ? "0" : "1";

                        userParams["ignored"] = string.Join(",", user.RelalationsByOwner.Where(x => x.Relation == Relation.Ignore).Select(x=>x.Target.Name));
                        userParams["friends"] = string.Join(",", user.RelalationsByOwner.Where(x => x.Relation == Relation.Friend).Select(x=>x.Target.Name));
                        
                        if (!p.IsSpectator)
                        {
                            // set valid PW structure attackers
                            if (mode == AutohostMode.Planetwars)
                            {
                                userParams["pwRank"] = (user.AccountRolesByAccountID.Where(
                                            x =>
                                                !x.RoleType.IsClanOnly &&
                                                (x.RoleType.RestrictFactionID == null || x.RoleType.RestrictFactionID == user.FactionID)).OrderBy(x=>x.RoleType.DisplayOrder).Select(x => (int?)x.RoleType.DisplayOrder).FirstOrDefault() ?? 999).ToString();


                                var allied = user.Faction != null && defender != null && user.Faction != defender &&
                                             defender.HasTreatyRight(user.Faction, x => x.EffectPreventIngamePwStructureDestruction == true, planet);

                                if (!allied && user.Faction != null && (user.Faction == attacker || user.Faction == defender))
                                {
                                    userParams["canAttackPwStructures"] = "1";
                                }

                                userParams["pwInstructions"] = GetPwInstructions(planet, user, db, attacker);
                            }

                            if (accountIDsWithExtraComms.Contains(user.AccountID)) userParams["extracomm"] = "1";

                            var commProfileIDs = new LuaTable();
                            var userCommandersBanned = Punishment.GetActivePunishment(user.AccountID, null, null, x => x.BanCommanders) != null; 
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

                                        commProfile["name"] = c.Name.Substring(0, Math.Min(25, c.Name.Length));
                                        commProfile["chassis"] = c.Unlock.Code;
                                        commProfile["decorations"] = decorations;

                                        var modules = new LuaTable();
                                        commProfile["modules"] = modules;

                                        for (var i = 1; i <= GlobalConst.NumCommanderLevels; i++)
                                        {
                                            var modulesForLevel = new LuaTable();
                                            modules.Add(modulesForLevel);
                                            var modulesOrdered = c.CommanderModules.Where(x => x.CommanderSlot.MorphLevel == i).ToList();
                                            var slots = db.CommanderSlots.ToList().Where(x => x.MorphLevel == i && (x.ChassisID == null || (x.ChassisID == c.ChassisUnlockID))).ToList();
                                            slots.Sort(delegate (CommanderSlot x, CommanderSlot y)
                                            {
                                                UnlockTypes type1 = x.UnlockType;
                                                UnlockTypes type2 = x.UnlockType;
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

                // set PW structures
                if (mode == AutohostMode.Planetwars)
                {
                    var owner = planet.Faction != null ? planet.Faction.Shortcut : "";

                    var pwStructures = new LuaTable();
                    foreach (
                        var s in planet.PlanetStructures.Where(x => x.StructureType != null && !string.IsNullOrEmpty(x.StructureType.IngameUnitName)))
                    {
                        pwStructures.Add("s" + s.StructureTypeID,
                            new LuaTable
                            {
                                { "unitname", s.StructureType.IngameUnitName },
                                { "owner", s.Account?.Name },
                                { "canBeEvacuated", s.StructureType.IsIngameEvacuable },
                                { "canBeDestroyed", s.StructureType.IsIngameDestructible },
                                //{ "isDestroyed", s.IsDestroyed ? true : false },
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
            var attackerWinLoseCc = (ipShips + ipBase)*GlobalConst.PlanetWarsAttackerWinLoseCcMultiplier - ipDefs;
            var attackerLoseKillCc = (ipShips + ipBase) * GlobalConst.PlanetWarsDefenderWinKillCcMultiplier - ipDefs;

            var attackerIp = planet.PlanetFactions.FirstOrDefault(x => x.FactionID == attacker.FactionID)?.Influence;


            if (user.Faction == attacker)
            {
                sb.AppendFormat("You are attacking {0} planet {1}\n", planet.Faction != null ? planet.Faction.Name : "neutral", planet.Name);
                sb.AppendFormat("You have {0} of the {1} influence needed to conquer this planet.\n",
                    attackerIp,
                    GlobalConst.InfluenceToCapturePlanet);

                sb.AppendFormat("If you win you will gain {0} influence ({1} base + {2} from dropships - {3} from defense)\n",
                    ipBase + ipShips - ipDefs,
                    ipBase,
                    ipShips,
                    ipDefs);

                sb.AppendFormat("If you are losing, try to kill enemy command center for {0} influence\n", attackerLoseKillCc);
                sb.AppendFormat("If you are winning, protect your command center. If you lose it, you will only get {0} influence here\n", attackerWinLoseCc);
                sb.AppendFormat("You can also destroy enemy PlanetWars structures to disable them on the strategic map\n");
            }
            else
            {
                sb.AppendFormat("You are defending {0} planet {1}\n", planet.Faction != null ? planet.Faction.Name : "neutral", planet.Name);
                sb.AppendFormat("Attackers have {0} of the {1} influence needed to conquer this planet.\n",
                    attackerIp,
                    GlobalConst.InfluenceToCapturePlanet);

                sb.AppendFormat("If you win, attacker will get nothing, but they can disable your PlanetWars structures by destroying them here.\n");
                sb.AppendFormat("You can prevent structure destruction by evacuation them.\n");

                sb.AppendFormat("If you lose, {4} will gain {0} influence ({1} base + {2} from dropships - {3} from defense)\n",
                    ipBase + ipShips - ipDefs,
                    ipBase,
                    ipShips,
                    ipDefs, attacker?.Shortcut);


                sb.AppendFormat("If you are losing, try to kill enemy command center, they will only gain {0} influence\n", attackerWinLoseCc);
                sb.AppendFormat("If you are winning, protect your command center. If you win but lose it, they will still get {0} influence here\n", attackerLoseKillCc);
            }

            return sb.ToString();
        }
    }
}