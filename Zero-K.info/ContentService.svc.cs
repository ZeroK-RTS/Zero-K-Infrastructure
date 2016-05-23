﻿using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.Linq;
using Microsoft.Linq.Translations;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "ContentService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select ContentService.svc or ContentService.svc.cs at the Solution Explorer and start debugging.
    public class ContentService : IContentService
    {
        
        public DownloadFileResult DownloadFile(string internalName)
        {
            return PlasmaServer.DownloadFile(internalName);
        }

        
        public List<ResourceData> FindResourceData(string[] words, ResourceType? type = null)
        {
            var db = new ZkDataContext();
            var ret = db.Resources.AsQueryable();
            if (type == ResourceType.Map) ret = ret.Where(x => x.TypeID == ResourceType.Map);
            if (type == ResourceType.Mod) ret = ret.Where(x => x.TypeID == ResourceType.Mod);
            string joinedWords = string.Join(" ", words);
            var test = ret.Where(x => x.InternalName == joinedWords);
            if (test.Any()) return test.OrderByDescending(x => -x.FeaturedOrder).AsEnumerable().Select(PlasmaServer.ToResourceData).ToList();
            int i;
            if (words.Length == 1 && int.TryParse(words[0], out i)) ret = ret.Where(x => x.ResourceID == i);
            else
            {
                foreach (var w in words)
                {
                    var w1 = w;
                    ret = ret.Where(x => SqlFunctions.PatIndex("%" + w1 + "%", x.InternalName) > 0);
                }
            }
            ret = ret.Where(x => x.ResourceContentFiles.Any(y => y.LinkCount > 0));
            return ret.OrderByDescending(x => -x.FeaturedOrder).Take(400).ToList().Select(PlasmaServer.ToResourceData).ToList();
        }


        
        public List<string> GetEloTop10()
        {
            DateTime ladderTimeout = DateTime.UtcNow.AddDays(-GlobalConst.LadderActivityDays);
            using (var db = new ZkDataContext())

                return
                db.Accounts.Where(x => x.SpringBattlePlayers.Any(y => y.SpringBattle.StartTime > ladderTimeout && y.SpringBattle.PlayerCount == 2 && y.SpringBattle.HasBots == false && y.EloChange != null && !y.IsSpectator).OrderByDescending(
                    x => x.Effective1v1Elo).WithTranslations().Select(x => x.Name).Take(10).ToList();
        }




        /// <summary>
        /// Finds resource by either md5 or internal name
        /// </summary>
        /// <param name="md5"></param>
        /// <param name="internalName"></param>
        /// <returns></returns>
        
        public ResourceData GetResourceData(string md5, string internalName)
        {
            return PlasmaServer.GetResourceData(md5, internalName);
        }

        
        public ResourceData GetResourceDataByInternalName(string internalName)
        {
            var db = new ZkDataContext();
            var entry = db.Resources.SingleOrDefault(x => x.InternalName == internalName);
            if (entry != null) return PlasmaServer.ToResourceData(entry);
            else return null;
        }

        
        public ResourceData GetResourceDataByResourceID(int resourceID)
        {
            var db = new ZkDataContext();
            return PlasmaServer.ToResourceData(db.Resources.Single(x => x.ResourceID == resourceID));
        }


        
        public List<ResourceData> GetResourceList(DateTime? lastChange, out DateTime currentTime)
        {
            return PlasmaServer.GetResourceList(lastChange, out currentTime);
        }


        
        public ScriptMissionData GetScriptMissionData(string name)
        {
            using (var db = new ZkDataContext())
            {
                var m = db.Missions.Single(x => x.Name == name && x.IsScriptMission);
                return new ScriptMissionData()
                {
                    MapName = m.Map,
                    ModTag = m.ModRapidTag,
                    StartScript = m.Script,
                    ManualDependencies = m.ManualDependencies != null ? new List<string>(m.ManualDependencies.Split('\n')) : null,
                    Name = m.Name
                };
            }
        }


        
        public void NotifyMissionRun(string login, string missionName)
        {
            missionName = Mission.GetNameWithoutVersion(missionName);
            using (var db = new ZkDataContext())
            {
                db.Missions.Single(x => x.Name == missionName).MissionRunCount++;
                Account.AccountByName(db, login).MissionRunCount++;
                db.SubmitChanges();
            }
        }


        
        public ReturnValue RegisterResource(int apiVersion,
                                                         string springVersion,
                                                         string md5,
                                                         int length,
                                                         ResourceType resourceType,
                                                         string archiveName,
                                                         string internalName,
                                                         byte[] serializedData,
                                                         List<string> dependencies,
                                                         byte[] minimap,
                                                         byte[] metalMap,
                                                         byte[] heightMap,
                                                         byte[] torrentData)
        {
            return PlasmaServer.RegisterResource(apiVersion,
                                                 springVersion,
                                                 md5,
                                                 length,
                                                 resourceType,
                                                 archiveName,
                                                 internalName,
                                                 serializedData,
                                                 dependencies,
                                                 minimap,
                                                 metalMap,
                                                 heightMap,
                                                 torrentData);
        }

        
        public void SubmitMissionScore(string login, string passwordHash, string missionName, int score, int gameSeconds, string missionVars = "")
        {
            missionName = Mission.GetNameWithoutVersion(missionName);

            using (var db = new ZkDataContext())
            {
                var acc = AuthServiceClient.VerifyAccountHashed(login, passwordHash);
                if (acc == null) throw new ApplicationException("Invalid login or password");

                acc.Xp += GlobalConst.XpForMissionOrBots;

                var mission = db.Missions.Single(x => x.Name == missionName);

                if (score != 0 || mission.RequiredForMultiplayer)
                {
                    var scoreEntry = mission.MissionScores.FirstOrDefault(x => x.AccountID == acc.AccountID);
                    if (scoreEntry == null)
                    {
                        scoreEntry = new MissionScore() { MissionID = mission.MissionID, AccountID = acc.AccountID, Score = int.MinValue };
                        mission.MissionScores.Add(scoreEntry);
                    }

                    if (score > scoreEntry.Score)
                    {
                        var max = mission.MissionScores.Max(x => (int?)x.Score);
                        if (max == null || max <= score)
                        {
                            mission.TopScoreLine = login;
                            acc.Xp += 150; // 150 for getting top score
                        }
                        scoreEntry.Score = score;
                        scoreEntry.Time = DateTime.UtcNow;
                        scoreEntry.MissionRevision = mission.Revision;
                        scoreEntry.GameSeconds = gameSeconds;
                    }
                }

                acc.CheckLevelUp();
                db.SubmitChanges();

                if (!acc.CanPlayMultiplayer)
                {
                    if (
                        db.Missions.Where(x => x.RequiredForMultiplayer)
                            .All(y => y.MissionScores.Any(z => z.AccountID == acc.AccountID)))
                    {
                        acc.CanPlayMultiplayer = true;
                        db.SaveChanges();
                        Global.Server.PublishAccountUpdate(acc);
                        Global.Server.GhostPm(acc.Name, "Congratulations! You are now authorized to play MultiPlayer games!");
                    }
                }


                // ====================
                // campaign stuff
                ProgressCampaign(acc.AccountID, mission.MissionID, missionVars);
            }
        }





        
        public bool VerifyAccountData(string login, string password)
        {
            var acc = AuthServiceClient.VerifyAccountPlain(login, password);
            if (acc == null) return false;
            return true;
        }

        
        
        public AccountInfo GetAccountInfo(string login, string password)
        {
            var acc = AuthServiceClient.VerifyAccountPlain(login, password);
            if (acc == null) return null;
            else return new AccountInfo()
            {
                Name = acc.Name,
                Country = acc.Country,
                Aliases = acc.Aliases,
                ZeroKAccountID = acc.AccountID,
                ZeroKLevel = acc.Level,
                ClanID = acc.ClanID ?? 0,
                ClanName = acc.Clan != null ? acc.Clan.ClanName : null,
                IsZeroKAdmin = acc.IsZeroKAdmin,
                Avatar = acc.Avatar,
                Elo = (float)acc.Elo,
                EffectiveElo = acc.EffectiveElo,
                EloWeight = (float)acc.EloWeight,
                FactionID = acc.FactionID ?? 0,
                FactionName = acc.Faction != null ? acc.Faction.Name : null,
                SpringieLevel = acc.GetEffectiveSpringieLevel(),
                LobbyID = acc.LobbyID ?? 0
            };
        }


        public static void ProgressCampaign(int accountID, int missionID, string missionVars = "")
        {
            ZkDataContext db = new ZkDataContext();
            CampaignPlanet planet = db.CampaignPlanets.FirstOrDefault(p => p.MissionID == missionID);
            if (planet != null)
            {
                Account acc = db.Accounts.First(x => x.AccountID == accountID);
                bool alreadyCompleted = false;
                int campID = planet.CampaignID;
                Campaign camp = planet.Campaign;
                List<int> unlockedPlanetIDs = new List<int>();
                List<int> unlockedJournalIDs = new List<int>();
                //List<CampaignPlanet> unlockedPlanets = new List<CampaignPlanet>();
                //List<CampaignJournal> unlockedJournals = new List<CampaignJournal>();

                // start with processing the mission vars, if there are any
                byte[] missionVarsAsByteArray = System.Convert.FromBase64String(missionVars);
                string missionVarsDecoded = System.Text.Encoding.UTF8.GetString(missionVarsAsByteArray);
                foreach (string kvpRaw in missionVarsDecoded.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string kvpRaw2 = kvpRaw.Trim();
                    string key = "", value = "";
                    string[] kvpSplit = kvpRaw2.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (kvpSplit.Length == 2)
                    {
                        key = kvpSplit[0].Trim();
                        value = kvpSplit[1].Trim();
                    }
                    else
                    {
                        throw new Exception("Invalid key-value pair in decoded mission vars: " + missionVarsDecoded);
                    }
                    if (!(string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value)))
                    {
                        CampaignVar cv = camp.CampaignVars.First(x => x.KeyString == key);
                        AccountCampaignVar acv = acc.AccountCampaignVars.FirstOrDefault(x => x.CampaignID == campID && x.VarID == cv.VarID);
                        if (acv == null)
                        {
                            db.AccountCampaignVars.InsertOnSubmit(new AccountCampaignVar() { AccountID = accountID, CampaignID = campID, VarID = cv.VarID, Value = value });
                        }
                        else acv.Value = value;
                    }
                }

                //reload DB - this allows the vars submitted this session to be used by the following code
                db.SubmitChanges();
                db = new ZkDataContext();
                acc = db.Accounts.First(x => x.AccountID == accountID);
                planet = db.CampaignPlanets.FirstOrDefault(p => p.MissionID == missionID);
                camp = planet.Campaign;

                // now we unlock planets and journal entries
                // first mark this planet as completed - but only if it's already unlocked
                AccountCampaignProgress progress = acc.AccountCampaignProgress.FirstOrDefault(x => x.PlanetID == planet.PlanetID && x.CampaignID == planet.CampaignID);
                if (progress != null)
                {
                    alreadyCompleted = progress.IsCompleted;
                }
                else if (planet.StartsUnlocked)
                {
                    progress = new AccountCampaignProgress() { AccountID = accountID, CampaignID = campID, PlanetID = planet.PlanetID, IsCompleted = false, IsUnlocked = true };
                    db.AccountCampaignProgress.InsertOnSubmit(progress);
                }

                if (progress != null && planet.IsUnlocked(accountID))
                {
                    progress.IsCompleted = true;


                    // unlock planets made available by completing this one
                    var links = camp.CampaignLinks.Where(x => x.UnlockingPlanetID == planet.PlanetID);
                    foreach (CampaignLink link in links)
                    {
                        CampaignPlanet toUnlock = link.PlanetToUnlock;
                        bool proceed = true;
                        var requiredVars = toUnlock.CampaignPlanetVars;
                        if (requiredVars.Count() == 0) proceed = true;
                        else
                        {
                            foreach (CampaignPlanetVar variable in requiredVars)
                            {
                                AccountCampaignVar accountVar = acc.AccountCampaignVars.FirstOrDefault(x => x.CampaignID == campID && x.VarID == variable.RequiredVarID);
                                if (!(accountVar != null && accountVar.Value == variable.RequiredValue))
                                {
                                    proceed = false;
                                    break;  // failed to meet var requirement, stop here
                                }
                            }
                        }

                        if (proceed)    // met requirements for unlocking planet
                        {
                            AccountCampaignProgress progress2 = toUnlock.AccountCampaignProgress.FirstOrDefault(x => x.CampaignID == campID && x.AccountID == accountID);
                            if (progress2 == null)
                            {
                                progress2 = new AccountCampaignProgress() { AccountID = accountID, CampaignID = campID, PlanetID = toUnlock.PlanetID, IsCompleted = false, IsUnlocked = true };
                                db.AccountCampaignProgress.InsertOnSubmit(progress2);
                                unlockedPlanetIDs.Add(toUnlock.PlanetID);
                            }
                            else if (!progress2.IsUnlocked)
                            {
                                progress2.IsUnlocked = true;
                                unlockedPlanetIDs.Add(toUnlock.PlanetID);
                            }
                        }
                    }
                }
                // unlock journals
                var journalsWithVars = db.CampaignJournals.Where(x => x.CampaignID == campID && x.CampaignJournalVars.Any());
                foreach (CampaignJournal journal in journalsWithVars)
                {
                    bool proceed = true;

                    var requiredVars = journal.CampaignJournalVars.Where(x => x.CampaignID == campID).ToList();
                    foreach (CampaignJournalVar variable in requiredVars)
                    {
                        AccountCampaignVar accountVar = acc.AccountCampaignVars.FirstOrDefault(x => x.CampaignID == campID && x.VarID == variable.RequiredVarID);
                        if (!(accountVar != null && accountVar.Value == variable.RequiredValue))
                        {
                            proceed = false;
                            break;  // failed to meet var requirement, stop here
                        }
                    }

                    if (proceed)    // met requirements for unlocking journal
                    {
                        System.Console.WriteLine(journal.Title);
                        AccountCampaignJournalProgress jp = journal.AccountCampaignJournalProgress.FirstOrDefault(x => x.AccountID == accountID);
                        if (jp == null)
                        {
                            jp = new AccountCampaignJournalProgress() { AccountID = accountID, CampaignID = campID, JournalID = journal.JournalID, IsUnlocked = true };
                            db.AccountCampaignJournalProgress.InsertOnSubmit(jp);
                            unlockedJournalIDs.Add(journal.JournalID);
                        }
                        else if (!jp.IsUnlocked)
                        {
                            jp.IsUnlocked = true;
                            unlockedJournalIDs.Add(journal.JournalID);
                        }
                    }
                }
                db.SubmitChanges();
                db = new ZkDataContext();
                planet = db.CampaignPlanets.FirstOrDefault(p => p.MissionID == missionID);

                //throw new Exception("DEBUG: Campaign " + campID + ", planet " + planet.PlanetID);
                if (!alreadyCompleted)
                {
                    db.CampaignEvents.InsertOnSubmit(Global.CreateCampaignEvent(accountID, campID, "Planet completed: {0}", planet));
                    foreach (CampaignJournal journal in db.CampaignJournals.Where(x => x.CampaignID == campID && x.PlanetID == planet.PlanetID && x.UnlockOnPlanetCompletion))
                    {
                        unlockedJournalIDs.Add(journal.JournalID);
                    }
                }
                foreach (int unlockedID in unlockedPlanetIDs)
                {
                    CampaignPlanet unlocked = db.CampaignPlanets.FirstOrDefault(x => x.PlanetID == unlockedID && x.CampaignID == campID);
                    db.CampaignEvents.InsertOnSubmit(Global.CreateCampaignEvent(accountID, campID, "Planet unlocked: {0}", unlocked));
                    foreach (CampaignJournal journal in db.CampaignJournals.Where(x => x.CampaignID == campID && x.PlanetID == unlocked.PlanetID && x.UnlockOnPlanetUnlock))
                    {
                        unlockedJournalIDs.Add(journal.JournalID);
                    }
                }
                foreach (int ujid in unlockedJournalIDs)
                {
                    CampaignJournal uj = db.CampaignJournals.FirstOrDefault(x => x.JournalID == ujid && x.CampaignID == campID);
                    db.CampaignEvents.InsertOnSubmit(Global.CreateCampaignEvent(accountID, campID, "{1} - Journal entry unlocked: {0}", uj, uj.CampaignPlanet));
                }
                db.SubmitChanges();
            }
        }
    }
}
