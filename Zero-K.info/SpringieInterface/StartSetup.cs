using System;
using System.Collections.Generic;
using System.Linq;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.SpringieInterface
{
    public class SpringBattleStartSetup
    {
        public List<ScriptKeyValuePair> ModOptions = new List<ScriptKeyValuePair>();
        public List<UserCustomParameters> UserParameters = new List<UserCustomParameters>();

        public class ScriptKeyValuePair
        {
            public string Key;
            public string Value;
        }

        public class UserCustomParameters
        {
            public int LobbyID;
            public List<ScriptKeyValuePair> Parameters = new List<ScriptKeyValuePair>();
        }
    }


    public class StartSetup
    {
        public static SpringBattleStartSetup GetSpringBattleStartSetup(BattleContext context)
        {
            try
            {
                var mode = context.GetMode();
                var ret = new SpringBattleStartSetup();
                var commanderTypes = new LuaTable();
                var db = new ZkDataContext();

                var accountIDsWithExtraComms = new List<int>();
                // calculate to whom to send extra comms
                if (mode == AutohostMode.Planetwars || mode== AutohostMode.BigTeams|| mode== AutohostMode.MediumTeams || mode == AutohostMode.GameFFA || mode == AutohostMode.SmallTeams)
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
                                    other.Select(x => db.Accounts.First(y => y.LobbyID == x.LobbyID)).OrderByDescending(x => x.Elo*x.EloWeight).Take(
                                        cnt)) accountIDsWithExtraComms.Add(a.AccountID);
                            }
                        }
                    }
                }

                foreach (var p in context.Players.Where(x => !x.IsSpectator))
                {
                    var user = db.Accounts.FirstOrDefault(x => x.LobbyID == p.LobbyID);
                    if (user != null)
                    {
                        var userParams = new List<SpringBattleStartSetup.ScriptKeyValuePair>();
                        ret.UserParameters.Add(new SpringBattleStartSetup.UserCustomParameters { LobbyID = p.LobbyID, Parameters = userParams });

                        var pu = new LuaTable();
                        var userUnlocksBanned = user.Punishments.Any(x => x.BanExpires > DateTime.UtcNow && x.BanUnlocks);
                        var userCommandersBanned = user.Punishments.Any(x => x.BanExpires > DateTime.UtcNow && x.BanCommanders);
                        var userBanMuted = user.Punishments.Any(x => x.BanExpires > DateTime.UtcNow && x.BanMute);

                        if (!userUnlocksBanned)
                        {
                            if (mode != AutohostMode.Planetwars || user.ClanID == null) foreach (var unlock in user.AccountUnlocks.Select(x => x.Unlock)) pu.Add(unlock.Code);
                            else
                            {
                                foreach (var unlock in
                                    user.AccountUnlocks.Select(x => x.Unlock).Union(Galaxy.ClanUnlocks(db, user.ClanID).Select(x => x.Unlock))) pu.Add(unlock.Code);
                            }
                        }

                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "unlocks", Value = pu.ToBase64String() });
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair()
                                       { Key = "faction", Value = user.Faction != null ? user.Faction.Shortcut : "" });
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair()
                                       { Key = "clan", Value = user.Clan != null ? user.Clan.Shortcut : "" });
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "level", Value = user.Level.ToString() });
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "elo", Value = user.EffectiveElo.ToString() });
                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "avatar", Value = user.Avatar });
                        if (userBanMuted) userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "muted", Value = "1" });


                        if (accountIDsWithExtraComms.Contains(user.AccountID)) userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "extracomm", Value = "1" });

                        var pc = new LuaTable();

                        if (!userCommandersBanned)
                        {
                            foreach (var c in user.Commanders.Where(x=>x.Unlock !=null))
                            {
                                try
                                {
                                    if (string.IsNullOrEmpty(c.Name) || c.Name.Any(x=>!Char.IsLetterOrDigit(x) && x!= ' ')) c.Name = c.CommanderID.ToString();
                                    var morphTable = new LuaTable();
                                    pc["[\"" + c.Name + "\"]"] = morphTable;
                                    for (var i = 1; i <= 4; i++)
                                    {
                                        var key = string.Format("c{0}_{1}_{2}", user.AccountID, c.CommanderID, i);
                                        morphTable.Add(key);

                                        var comdef = new LuaTable();
                                        commanderTypes[key] = comdef;

                                        comdef["chassis"] = c.Unlock.Code + i;

                                        var modules = new LuaTable();
                                        comdef["modules"] = modules;

                                        comdef["cost"] = c.GetTotalMorphLevelCost(i);

                                        comdef["name"] = c.Name.Substring(0, Math.Min(25, c.Name.Length)) + " level " + i;

                                        foreach (var m in
                                            c.CommanderModules.Where(x => x.CommanderSlot.MorphLevel <= i && x.Unlock != null).OrderBy(x => x.Unlock.UnlockType).ThenBy(x => x.SlotID).Select(
                                                x => x.Unlock)) modules.Add(m.Code);
                                    }
                                }
                                catch (Exception ex) {
                                    throw new ApplicationException(string.Format("Error processing commander: {0} - {1} of player {2} - {3}",c.CommanderID, c.Name, user.AccountID, user.Name),ex);
                                }
                            }
                        }
                        else userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "jokecomm", Value = "1" });

                        userParams.Add(new SpringBattleStartSetup.ScriptKeyValuePair() { Key = "commanders", Value = pc.ToBase64String() });
                    }
                }

                ret.ModOptions.Add(new SpringBattleStartSetup.ScriptKeyValuePair { Key = "commanderTypes", Value = commanderTypes.ToBase64String() });
                if (mode == AutohostMode.Planetwars)
                {
                    var planet = db.Galaxies.Single(x => x.IsDefault).Planets.Single(x => x.Resource.InternalName == context.Map);

                    var owner = "";
                    var second = "";
                    var factionInfluences = planet.GetFactionInfluences().Where(x => x.Influence > 0).ToList();
                    var firstEntry = factionInfluences.FirstOrDefault();
                    var ownerAccount = planet.Account;
                    var secondEntry = factionInfluences.Skip(1).FirstOrDefault();
                    if (ownerAccount != null) owner = string.Format("{0} of {1}", ownerAccount.Clan.Shortcut, ownerAccount.Faction.Name);
                    if (secondEntry != null && firstEntry != null)
                        second = string.Format("{0} needs {1} influence - ",
                                               secondEntry.Faction.Shortcut,
                                               firstEntry.Influence - secondEntry.Influence);

                    var pwStructures = new LuaTable();
                    foreach (var s in planet.PlanetStructures.Where(x => !x.IsDestroyed && !string.IsNullOrEmpty(x.StructureType.IngameUnitName)))
                    {
                        pwStructures.Add("s" + s.StructureTypeID,
                                         new LuaTable()
                                         {
                                             { "unitname", s.StructureType.IngameUnitName },
                                             //{ "isDestroyed", s.IsDestroyed ? true : false },
                                             { "name", owner + s.StructureType.Name },
                                             { "description", second + s.StructureType.Description }
                                         });
                    }
                    ret.ModOptions.Add(new SpringBattleStartSetup.ScriptKeyValuePair
                                       { Key = "planetwarsStructures", Value = pwStructures.ToBase64String() });
                }

                return ret;
            }
            catch (Exception ex)
            {
                var db = new ZkDataContext();
                var licho = db.Accounts.SingleOrDefault(x => x.AccountID == 5986);
                if (licho != null) foreach (var line in ex.ToString().Lines()) AuthServiceClient.SendLobbyMessage(licho, line);
                throw;
            }
        }
    }
}