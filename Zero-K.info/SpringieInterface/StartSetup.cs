using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.SpringieInterface
{
    public class StartSetup
    {
        static bool listOnlyThatLevelsModules = false;  // may cause bugs

        /// <summary>
        /// Sets up all the things that Springie needs to know for the battle: how to balance, who to get extra commanders, what PlanetWars structures to create, etc.
        /// </summary>
        public static SpringBattleStartSetup GetSpringBattleStartSetup(BattleContext context) {
            try {
                AutohostMode mode = context.GetMode();
                var ret = new SpringBattleStartSetup();

                if (mode == AutohostMode.Planetwars)
                {
                    ret.BalanceTeamsResult = Balancer.BalanceTeams(context, true,null, null);
                    context.Players = ret.BalanceTeamsResult.Players;
                }
                
                var commanderTypes = new LuaTable();
                var db = new ZkDataContext();
                
                // calculate to whom to send extra comms
                var accountIDsWithExtraComms = new List<int>();
                if (mode == AutohostMode.Planetwars || mode == AutohostMode.Generic || mode == AutohostMode.GameFFA ||
                    mode == AutohostMode.Teams) {
                    IOrderedEnumerable<IGrouping<int, PlayerTeam>> groupedByTeam =
                        context.Players.Where(x => !x.IsSpectator).GroupBy(x => x.AllyID).OrderByDescending(x => x.Count());
                    IGrouping<int, PlayerTeam> biggest = groupedByTeam.FirstOrDefault();
                    if (biggest != null) {
                        foreach (var other in groupedByTeam.Skip(1)) {
                            int cnt = biggest.Count() - other.Count();
                            if (cnt > 0) {
                                foreach (Account a in
                                    other.Select(x => db.Accounts.First(y => y.AccountID == x.LobbyID)).OrderByDescending(x => x.Elo*x.EloWeight).Take(
                                        cnt)) accountIDsWithExtraComms.Add(a.AccountID);
                            }
                        }
                    }
                }

                bool is1v1 = context.Players.Where(x => !x.IsSpectator).ToList().Count == 2 && context.Bots.Count == 0;

                // write Planetwars details to modoptions (for widget)
                Faction attacker = null;
                Faction defender = null;
                Planet planet = null;
                if (mode == AutohostMode.Planetwars) {
                    planet = db.Galaxies.First(x => x.IsDefault).Planets.First(x => x.Resource.InternalName == context.Map);
                    attacker =
                        context.Players.Where(x => x.AllyID == 0 && !x.IsSpectator)
                            .Select(x => db.Accounts.First(y => y.AccountID == x.LobbyID))
                            .Where(x => x.Faction != null)
                            .Select(x => x.Faction)
                            .First();

                    defender = planet.Faction;

                    if (attacker == defender) defender = null;

                    ret.ModOptions.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "attackingFaction", Value = attacker.Shortcut });
                    if (defender != null) ret.ModOptions.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "defendingFaction", Value = defender.Shortcut });
                    ret.ModOptions.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "planet", Value = planet.Name });
                }

                // write player custom keys (level, elo, is muted, etc.)
                foreach (PlayerTeam p in context.Players) {
                    Account user = db.Accounts.Find(p.LobbyID);
                    if (user != null) {
                        var userParams = new List<SpringBattleStartSetup.ScriptKeyValuePair>();
                        ret.UserParameters.Add(new SpringBattleStartSetup.UserCustomParameters { LobbyID = p.LobbyID, Parameters = userParams });
                        
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "LobbyID", Value = user.AccountID.ToString() });
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "CountryCode", Value = user.Country });

                        bool userBanMuted = user.PunishmentsByAccountID.Any(x => !x.IsExpired && x.BanMute);
                        if (userBanMuted) userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "muted", Value = "1" });
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair
                                       { Key = "faction", Value = user.Faction != null ? user.Faction.Shortcut : "" });
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair
                                       { Key = "clan", Value = user.Clan != null ? user.Clan.Shortcut : "" });
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair
                                       { Key = "clanfull", Value = user.Clan != null ? user.Clan.ClanName : "" });
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "level", Value = user.Level.ToString() });
                        double elo =  mode == AutohostMode.Planetwars ? user.EffectivePwElo : (is1v1 ? user.Effective1v1Elo : user.EffectiveElo);
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "elo", Value = Math.Round(elo).ToString() }); // elo for ingame is just ordering for auto /take
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "avatar", Value = user.Avatar });
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "admin", Value = (user.IsZeroKAdmin ? "1" : "0") });

                        if (!p.IsSpectator) {
                            // set valid PW structure attackers
                            if (mode == AutohostMode.Planetwars)
                            {
                                bool allied = user.Faction != null && defender != null && user.Faction != defender &&
                                              defender.HasTreatyRight(user.Faction, x => x.EffectPreventIngamePwStructureDestruction == true, planet);

                                if (!allied && user.Faction != null && (user.Faction == attacker || user.Faction == defender)) {
                                    userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "canAttackPwStructures", Value = "1" });
                                }
                            }

                            var pu = new LuaTable();
                            bool userUnlocksBanned = user.PunishmentsByAccountID.Any(x => !x.IsExpired && x.BanUnlocks);
                            bool userCommandersBanned = user.PunishmentsByAccountID.Any(x => !x.IsExpired && x.BanCommanders);

                            if (!userUnlocksBanned) {
                                if (mode != AutohostMode.Planetwars || user.Faction == null) foreach (Unlock unlock in user.AccountUnlocks.Select(x => x.Unlock)) pu.Add(unlock.Code);
                                else {
                                    foreach (Unlock unlock in
                                        user.AccountUnlocks.Select(x => x.Unlock).Union(user.Faction.GetFactionUnlocks().Select(x => x.Unlock)).Where(x => x.UnlockType == UnlockTypes.Unit)) pu.Add(unlock.Code);
                                }
                            }

                            userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "unlocks", Value = pu.ToBase64String() });

                            if (accountIDsWithExtraComms.Contains(user.AccountID)) userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "extracomm", Value = "1" });

                            var pc = new LuaTable();

                            if (!userCommandersBanned) {
                                // set up commander data
                                foreach (Commander c in user.Commanders.Where(x => x.Unlock != null && x.ProfileNumber <= GlobalConst.CommanderProfileCount)) {
                                    try {
                                        if (string.IsNullOrEmpty(c.Name) || c.Name.Any(x => x == '"') )
                                        {
                                            c.Name = c.CommanderID.ToString();
                                        }
                                        LuaTable morphTable = new LuaTable();
                                        pc["[\"" + c.Name + "\"]"] = morphTable;

                                        // process decoration icons
                                        LuaTable decorations = new LuaTable();
                                        foreach (Unlock d in
                                                        c.CommanderDecorations.Where(x => x.Unlock != null).OrderBy(
                                                            x => x.SlotID).Select(x => x.Unlock))
                                        {
                                            CommanderDecorationIcon iconData = db.CommanderDecorationIcons.FirstOrDefault(x => x.DecorationUnlockID == d.UnlockID);
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
                                                    LuaTable entry = new LuaTable();
                                                    entry.Add("image", iconName);
                                                    decorations.Add("icon_" + iconPosition.ToLower(), entry);
                                                }
                                            }
                                            else decorations.Add(d.Code);
                                        }
                                        

                                        string prevKey = null;
                                        for (int i = 0; i <= GlobalConst.NumCommanderLevels; i++) {
                                            string key = string.Format("c{0}_{1}_{2}", user.AccountID, c.ProfileNumber, i);
                                            morphTable.Add(key);    // TODO: maybe don't specify morph series in player data, only starting unit

                                            var comdef = new LuaTable();
                                            commanderTypes[key] = comdef;

                                            comdef["chassis"] = c.Unlock.Code + i;

                                            var modules = new LuaTable();
                                            comdef["modules"] = modules;
                                            
                                            comdef["decorations"] = decorations;

                                            comdef["name"] = c.Name.Substring(0, Math.Min(25, c.Name.Length)) + " level " + i;

                                            //if (i < GlobalConst.NumCommanderLevels)
                                            //{
                                            //    comdef["next"] = string.Format("c{0}_{1}_{2}", user.AccountID, c.ProfileNumber, i+1);
                                            //}
                                            //comdef["owner"] = user.Name;

                                            if (i > 0)
                                            {
                                                comdef["cost"] = c.GetTotalMorphLevelCost(i);

                                                if (listOnlyThatLevelsModules)
                                                {
                                                    if (prevKey != null) comdef["prev"] = prevKey;
                                                    prevKey = key;
                                                    foreach (Unlock m in
                                                        c.CommanderModules.Where(x => x.CommanderSlot.MorphLevel == i && x.Unlock != null).OrderBy(
                                                            x => x.Unlock.UnlockType).ThenBy(x => x.SlotID).Select(x => x.Unlock)) modules.Add(m.Code);
                                                }
                                                else
                                                {
                                                    foreach (Unlock m in
                                                        c.CommanderModules.Where(x => x.CommanderSlot.MorphLevel <= i && x.Unlock != null).OrderBy(
                                                            x => x.Unlock.UnlockType).ThenBy(x => x.SlotID).Select(x => x.Unlock)) modules.Add(m.Code);
                                                }
                                            }
                                        }
                                    } catch (Exception ex) {
                                        Trace.TraceError(ex.ToString());
                                        throw new ApplicationException(
                                            string.Format("Error processing commander: {0} - {1} of player {2} - {3}",
                                                          c.CommanderID,
                                                          c.Name,
                                                          user.AccountID,
                                                          user.Name),
                                            ex);
                                    }
                                }
                            }
                            else userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "jokecomm", Value = "1" });

                            userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "commanders", Value = pc.ToBase64String() });
                        }
                    }
                }

                ret.ModOptions.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "commanderTypes", Value = commanderTypes.ToBase64String() });

                // set PW structures
                if (mode == AutohostMode.Planetwars)
                {
                    string owner = planet.Faction != null ? planet.Faction.Shortcut : "";

                    var pwStructures = new LuaTable();
                    foreach (PlanetStructure s in planet.PlanetStructures.Where(x => x.StructureType!= null && !string.IsNullOrEmpty(x.StructureType.IngameUnitName))) {
                        pwStructures.Add("s" + s.StructureTypeID,
                                         new LuaTable
                                         {
                                             { "unitname", s.StructureType.IngameUnitName },
                                             //{ "isDestroyed", s.IsDestroyed ? true : false },
                                             { "name", string.Format("{0} {1} ({2})", owner, s.StructureType.Name, s.Account!= null ? s.Account.Name:"unowned") },
                                             { "description", s.StructureType.Description }
                                         });
                    }
                    ret.ModOptions.Add(new SpringBattleStartSetup.ScriptKeyValuePair
                                       { Key = "planetwarsStructures", Value = pwStructures.ToBase64String() });
                }

                return ret;
            } catch (Exception ex) {
                Trace.TraceError(ex.ToString());
                throw;
            }
        }
    }
}
