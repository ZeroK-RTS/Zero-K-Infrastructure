using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;
//using LobbyClient;
//using NightWatch;
using CaTracker;
using PlasmaShared;
using ZkData.UnitSyncLib;
using ZeroKWeb;
using ZkData;
using Encoder = System.Drawing.Imaging.Encoder;

namespace Fixer
{
    public static class Program
    {
        const int MaxBanHours = 24 * 36525;   // 100 years

        public static string GenerateMACAddress()
        {
            var sBuilder = new StringBuilder();
            var r = new Random();
            int number;
            byte b;
            for (int i = 0; i < 6; i++)
            {
                number = r.Next(0, 255);
                b = Convert.ToByte(number);
                if (i == 0)
                {
                    b = setBit(b, 6); //--> set locally administered
                    b = unsetBit(b, 7); // --> set unicast 
                }
                sBuilder.Append(number.ToString("X2"));
            }
            return sBuilder.ToString().ToUpper();
        }

        private static byte setBit(byte b, int BitNumber)
        {
            if (BitNumber < 8 && BitNumber > -1)
            {
                return (byte)(b | (byte)(0x01 << BitNumber));
            }
            else
            {
                throw new InvalidOperationException(
                "Der Wert für BitNumber " + BitNumber.ToString() + " war nicht im zulässigen Bereich! (BitNumber = (min)0 - (max)7)");
            }
        }

        private static byte unsetBit(byte b, int BitNumber)
        {
            if (BitNumber < 8 && BitNumber > -1)
            {
                return (byte)(b | (byte)(0x00 << BitNumber));
            }
            else
            {
                throw new InvalidOperationException(
                "Der Wert für BitNumber " + BitNumber.ToString() + " war nicht im zulässigen Bereich! (BitNumber = (min)0 - (max)7)");
            }
        }

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static void TestCRC()
        {
            for (int i = 0; i < 8192; i++)
            {
                string mac = GenerateMACAddress() + "lobby.springrts.com";
                byte[] mac2 = Encoding.ASCII.GetBytes(mac);
                uint hash1 = LobbyClient.Crc.Crc32(mac2);
                uint hash2 = LobbyClient.Crc.Crc32Old(mac2);
                if (hash1 != hash2)
                {
                    Console.WriteLine("MISMATCH: {0} vs. {1} (MAC {2})", hash1, hash2, mac);
                }
            }
        }

        public static void GetNICs()
        {
            var nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface nic in nics)
            {
                System.Console.WriteLine("{0} | type {1}", nic.GetPhysicalAddress(), nic.NetworkInterfaceType);
            }
        }

        public enum OptionType
        {
            Undefined = 0,
            Bool = 1,
            List = 2,
            Number = 3,
            String = 4,
            Section = 5
        }
        public static OptionType Type { get; set; }

        public static bool GetPair(string Value, out string result)
        {
            result = "";
            Type = OptionType.Number;
            switch (Type)
            {
                case OptionType.Bool:
                    if (Value != "0" && Value != "1") return false;
                    return true;

                case OptionType.Number:
                    double d;
                    if (!double.TryParse(Value, out d)) return false;
                    return true;

                case OptionType.String:
                    return true;
            }

            return false;
        }

        public static void FixStuff()
        {
            var client = MissionEditor2.MissionServiceClientFactory.MakeClient();
            var list = client.ListMissionInfos();
            foreach (Mission m in list)
            {
                Console.WriteLine(m.Name);
                Console.WriteLine(m.Description);
            }
        }

        public static void AddClanLeader()
        {
            var db = new ZkDataContext();
            var role = db.RoleTypes.First(x => x.IsClanOnly);
            foreach (var clan in db.Clans.Where(x => !x.IsDeleted))
            {
                if (!clan.AccountRoles.Any(y => y.RoleType.IsClanOnly))
                {
                    var picked = clan.Accounts.Where(x => x.LastLogin >= DateTime.UtcNow.AddDays(-7)).OrderByDescending(x => x.Level).FirstOrDefault();
                    if (picked == null) clan.Accounts.OrderByDescending(x => x.LastLogin).FirstOrDefault();
                    if (picked != null)
                    {
                        clan.AccountRoles.Add(new AccountRole()
                        {
                            AccountID = picked.AccountID,
                            ClanID = clan.ClanID,
                            Inauguration = DateTime.UtcNow,
                            RoleTypeID = role.RoleTypeID
                        });
                    }
                }
            }
            db.SubmitAndMergeChanges();
        }

        [STAThread]
        static void Main(string[] args)
        {

            var cloner = new DbCloner("zero-k", "zero-k-mig", "Data Source=omega.licho.eu,100;Initial Catalog=zero-k-mig;Persist Security Info=True;User ID=zero-k;Password=zkdevpass1;MultipleActiveResultSets=true");
            var order = cloner.GetOrderedTables();
            //var cols = cloner.GetTableColumns("AutohostConfig");
            //cloner.CloneAllTables();


            //var db = new ZkDataContext(false);
            //db.Database.CreateIfNotExists();

            //PlanetwarsFixer.StartGalaxy(24,3919,3925);
            //AddClanLeader();
            return;
            //TestPwMatch();
            //FixStuff();

            //var guid = Guid.NewGuid().ToString();

            //var pp = new PayPalInterface();
            //pp.ImportPaypalHistory(Directory.GetCurrentDirectory());

            //Test1v1Elo();
            //GenerateTechs();

            //FixDemoEngineVersion();

            //ImportSpringiePlayers();
            //RecalculateBattleElo();
            //FixMaps();

            //PickHomworldOwners();

            //PlanetwarsFixer.PurgeGalaxy(24, false, true);
            //PlanetwarsFixer.RandomizeMaps(24);
            //SetPlanetTeamSizes();
            
            //RandomizePlanetOwners(24);
            //GenerateStructures(24);
            PlanetwarsFixer.GenerateArtefacts(24, new int[] { 3940, 3949, 3954, 3929, 3956 });

            //SwapPlanetOwners(3948, 3955);
            //SwapPlanetOwners(3973, 3932);
            //PlanetwarsFixer.AddWormholes();
            //PlanetwarsFixer.RemoveTechStructures(true, true);
            //StartGalaxy(24);

            //TestPrediction();
            //FixMissionScripts();

            //AnalyzeModuleUsagePatterns();
            //AnalyzeCommUsagePatterns();

            //UpdateMissionProgression(12);
            //PrintNightwatchSubscriptions("zkadmin");

            //RecountForumVotes();
            //GetForumKarmaLadder();
            //GetForumKarmaVotes();
            //GetForumVotesByVoter(161294, 5340);
            //DeleteUserVotes(189201, null);  //neon    
            //GetClanStackWinRate(465, 2000); //Mean
            //GetForumVotesByUserVoterAgnostic(161294);
            //GetAverageElo();
        }

        static void SetPlanetTeamSizes()
        {
            var db = new ZkDataContext(true);
            var gal = db.Galaxies.First(x => x.IsDefault);
            var planets = gal.Planets.ToList().OrderBy(x=>x.Resource.MapDiagonal).ToList();
            var cnt = planets.Count;
            int num = 0;
            foreach (var p in planets)
            {
                //if (num < cnt*0.15) p.TeamSize = 1;else 
                if (num < cnt*0.80) p.TeamSize = 2;
                //else if (num < cnt*0.85) p.TeamSize = 3;
                else p.TeamSize = 3;
                num++;
            }
            db.SubmitAndMergeChanges();
        }

        public static void RecalculateKudos()
        {
            var db = new ZkDataContext(true);
            foreach (var acc in db.Accounts.Where(x => x.KudosPurchases.Any() || x.ContributionsByAccountID.Any())) acc.Kudos = acc.KudosGained - acc.KudosSpent;
            db.SubmitAndMergeChanges();
        }

        
        public static void FixHashes()
        {
            var db = new ZkDataContext();
            foreach (var r in db.Resources.Include(x=>x.ResourceSpringHashes))
            {
                var h84 = r.ResourceSpringHashes.Where(x => x.SpringVersion == "84").Select(x => x.SpringHash).SingleOrDefault();
                var h840 = r.ResourceSpringHashes.Where(x => x.SpringVersion == "84.0").Select(x => x.SpringHash).SingleOrDefault();

                if (h84 != h840)
                {
                    var entry = r.ResourceSpringHashes.SingleOrDefault(x => x.SpringVersion == "84.0");
                    if (h84 != 0)
                    {
                        if (entry == null)
                        {
                            entry = new ResourceSpringHash() { SpringVersion = "84.0" };
                            r.ResourceSpringHashes.Add(entry);
                        }
                        entry.SpringHash = h84;
                    }
                    else
                    {
                        if (entry != null) db.ResourceSpringHashes.DeleteOnSubmit(entry);
                    }
                }
            }
            db.SubmitChanges();
        }

        public static void CountUserIDs()
        {
            var db = new ZkDataContext();
            var userIDs = db.AccountUserIDs.ToList();
            var uniqueIDs = userIDs.Select(x => x.UserID).Distinct().ToList();
            Dictionary<long, int> userIDCounts = new Dictionary<long, int>();
            System.Console.WriteLine("{0} userIDs, {1} uniques", userIDs.Count, uniqueIDs.Count);
            foreach (long userID in uniqueIDs)
            {
                int count = userIDs.Count(x => x.UserID == userID);
                userIDCounts[userID] = count;
            }
            var sortedIDs = uniqueIDs.OrderByDescending(x => userIDCounts[x]).Take(20).ToList();
            foreach (int userID in sortedIDs)
            {
                System.Console.WriteLine("{0}: {1}", userID, userIDCounts[userID]);
            }
        }

        public static void SetFFATeams()
        {
            var db = new ZkDataContext();
            foreach (var m in db.Resources.Where(x => x.FeaturedOrder != null && x.TypeID == ResourceType.Map))
            {
                var lg = m.SpringBattlesByMapResourceID.Take(100).ToList();
                double cnt = lg.Count;
                if (cnt == 0) continue;
                ;
                if (lg.Count(x => x.HasBots) / (double)cnt > 0.5)
                {
                    m.MapIsChickens = true;
                }

                if (lg.Count(x => x.PlayerCount == 2) / cnt > 0.4)
                {
                    m.MapIs1v1 = true;
                }

                var teams =
                    m.SpringBattlesByMapResourceID.Take(100).GroupBy(
                        x => x.SpringBattlePlayers.Where(y => !y.IsSpectator).Select(y => y.AllyNumber).Distinct().Count()).OrderByDescending(
                            x => x.Count()).Select(x => x.Key).FirstOrDefault();
                if (teams > 2)
                {
                    m.MapIsFfa = true;
                    m.MapFFAMaxTeams = teams;
                }
                else { }



            }
            db.SubmitChanges();

        }


        public static void FixDemoFiles()
        {
            var db = new ZkDataContext();
            foreach (var sb in db.SpringBattles)
            {
                //sb.ReplayFileName = sb.ReplayFileName.Replace("http://springdemos.licho.eu/","http://zero-k.info/replays/");
            }
            //db.SubmitChanges();

        }

        public static void FixMissionScripts()
        {
            var db = new ZkDataContext();
            var missions = db.Missions.ToList();
            foreach (Mission mission in missions)
            {
                mission.Script = Regex.Replace(mission.Script, "GameType=([^;]+);", (m) => { return string.Format("GameType={0};", mission.NameWithVersion); });
            }
            db.SubmitChanges();
        }

        static void GetModuleUsage(int moduleID, List<Commander> comms, ZkDataContext db)
        {
            var modules = db.CommanderModules.Where(x => comms.Contains(x.Commander) && x.ModuleUnlockID == moduleID).ToList();
            int numModules = 0;
            Unlock wantedModule = db.Unlocks.FirstOrDefault(x => x.UnlockID == moduleID);
            int moduleCost = (int)wantedModule.MetalCost;
            numModules = modules.Count;

            System.Console.WriteLine("MODULE: " + wantedModule.Name);
            System.Console.WriteLine("Instances: " + numModules);
            System.Console.WriteLine("Total cost: " + numModules * moduleCost);
            System.Console.WriteLine();
        }

        public static void AnalyzeModuleUsagePatterns()
        {
            var db = new ZkDataContext();
            var modules = db.Unlocks.Where(x => x.UnlockType == UnlockTypes.Weapon).ToList();

            var players = db.Accounts.Where(x => x.Elo >= 1700 && DateTime.Compare(x.LastLogin.AddMonths(3), DateTime.UtcNow) > 0).ToList();
            System.Console.WriteLine("Number of accounts to process: " + players.Count);
            var comms = db.Commanders.Where(x => players.Contains(x.AccountByAccountID)).ToList();
            System.Console.WriteLine("Number of comms to process: " + comms.Count);
            System.Console.WriteLine();


            foreach (Unlock module in modules)
            {
                GetModuleUsage(module.UnlockID, comms, db);
            }
        }

        public static void AnalyzeCommUsagePatterns()
        {
            var db = new ZkDataContext();
            var chasses = db.Unlocks.Where(x => x.UnlockType == UnlockTypes.Chassis).ToList();

            var players = db.Accounts.Where(x => x.Elo >= 1700 && DateTime.Compare(x.LastLogin.AddMonths(3), DateTime.UtcNow) > 0).ToList();
            System.Console.WriteLine("Number of accounts to process: " + players.Count);
            System.Console.WriteLine();

            foreach (Unlock chassis in chasses)
            {
                var comms = db.Commanders.Where(x => players.Contains(x.AccountByAccountID) && x.ChassisUnlockID == chassis.UnlockID).ToList();
                int chassisCount = comms.Count;
                System.Console.WriteLine("CHASSIS " + chassis.Name + " : " + chassisCount);
            }
        }

        public static void ProgressCampaign(ZkDataContext db, Account acc, Mission mission, bool completeNext = false, string missionVars = "")
        {
            CampaignPlanet planet = db.CampaignPlanets.FirstOrDefault(p => p.MissionID == mission.MissionID);
            if (planet != null)
            {
                AccountCampaignProgress progress = acc.AccountCampaignProgress.FirstOrDefault(x => x.PlanetID == planet.PlanetID && x.CampaignID == planet.CampaignID);
                bool alreadyCompleted = false;
                int accountID = acc.AccountID;
                int campID = planet.CampaignID;
                Campaign camp = planet.Campaign;
                List<CampaignPlanet> unlockedPlanets = new List<CampaignPlanet>();
                List<CampaignJournal> unlockedJournals = new List<CampaignJournal>();

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

                // now we unlock planets and journal entries
                // first mark this planet as completed - but only if it's already unlocked
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
                                progress2 = new AccountCampaignProgress() { AccountID = accountID, CampaignID = campID, PlanetID = toUnlock.PlanetID, IsCompleted = completeNext, IsUnlocked = true };
                                db.AccountCampaignProgress.InsertOnSubmit(progress2);
                                unlockedPlanets.Add(toUnlock);
                            }
                            else if (!progress2.IsUnlocked)
                            {
                                progress2.IsUnlocked = true;
                                unlockedPlanets.Add(toUnlock);
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
                        AccountCampaignJournalProgress jp = journal.AccountCampaignJournalProgress.FirstOrDefault(x => x.AccountID == accountID);
                        if (jp == null)
                        {
                            jp = new AccountCampaignJournalProgress() { AccountID = accountID, CampaignID = campID, JournalID = journal.JournalID, IsUnlocked = true };
                            db.AccountCampaignJournalProgress.InsertOnSubmit(jp);
                            unlockedJournals.Add(journal);
                        }
                        else if (!jp.IsUnlocked)
                        {
                            jp.IsUnlocked = true;
                            unlockedJournals.Add(journal);
                        }
                    }
                }

                if (!alreadyCompleted)
                {
                    System.Console.WriteLine("Planet completed: {0}", planet);
                    foreach (CampaignJournal journal in db.CampaignJournals.Where(x => x.CampaignID == campID && x.CampaignPlanet == planet && x.UnlockOnPlanetCompletion))
                    {
                        unlockedJournals.Add(journal);
                    }
                }
                foreach (CampaignPlanet unlocked in unlockedPlanets)
                {
                    System.Console.WriteLine("Planet unlocked: {0}", unlocked);
                    foreach (CampaignJournal journal in db.CampaignJournals.Where(x => x.CampaignID == campID && x.CampaignPlanet == unlocked && x.UnlockOnPlanetUnlock))
                    {
                        unlockedJournals.Add(journal);
                    }
                }
                foreach (CampaignJournal uj in unlockedJournals)
                {
                    System.Console.WriteLine("{1} - Journal entry unlocked: {0}", uj, uj.CampaignPlanet);
                }
                db.SubmitChanges();
            }
        }

        public static void UpdateMissionProgression(int planetID, bool completeNext = false, string missionVars = "")
        {
            ZkDataContext db = new ZkDataContext();
            var accp = db.AccountCampaignProgress.Where(x => x.PlanetID == planetID && x.IsCompleted).ToList();
            foreach (var prog in accp)
            {
                Account acc = prog.Account;
                System.Console.WriteLine(acc);
                ProgressCampaign(db, acc, prog.CampaignPlanet.Mission, completeNext, missionVars);
            }
        }

        public static void PrintNightwatchSubscriptions(string channel)
        {
            ZkDataContext db = new ZkDataContext();
            var subscriptions = db.LobbyChannelSubscriptions.Where(x => x.Channel == channel);
            foreach (var entry in subscriptions)
            {
                Account acc = entry.Account;
                System.Console.WriteLine(acc);
            }
        }

        public static void GetClanStackWinRate(int clanID, int minElo)
        {
            int won = 0, lost = 0;
            ZkDataContext db = new ZkDataContext();
            Clan clan = db.Clans.FirstOrDefault(x => x.ClanID == clanID);
            var battles = db.SpringBattles.Where(x => x.SpringBattlePlayers.Count(p => p.Account.ClanID == clanID && p.Account.Elo >= minElo && !p.IsSpectator) >= 2
                && x.StartTime.CompareTo(DateTime.Now.AddMonths(-1)) > 0 && x.ResourceByMapResourceID.MapIsSpecial != true && !x.IsFfa).ToList();
            System.Console.WriteLine(clan.ClanName + ", " + battles.Count);
            foreach (SpringBattle battle in battles)
            {
                int onWinners = 0, onLosers = 0;
                var players = battle.SpringBattlePlayers.Where(p => p.Account.ClanID == clanID && p.Account.Elo >= minElo && !p.IsSpectator).ToList();
                foreach (SpringBattlePlayer player in players)
                {
                    if (player.IsInVictoryTeam) onWinners++;
                    else onLosers++;
                }
                if (onWinners > onLosers)
                {
                    won++;
                    System.Console.WriteLine("won " + battle.SpringBattleID);
                }
                else if (onLosers > onWinners)
                {
                    lost++;
                    System.Console.WriteLine("lost " + battle.SpringBattleID);
                }
                //else System.Console.WriteLine("OMG, B" + battle.SpringBattleID);
            }
            System.Console.WriteLine(won + ", " + lost);
        }

        public static void GetAverageElo()
        {
            int count = 0;
            double eloSum = 0;
            ZkDataContext db = new ZkDataContext();
            foreach (Account acc in db.Accounts.Where(x => x.SpringBattlePlayers.Any(g => g.SpringBattle.StartTime > DateTime.UtcNow.AddMonths(-1)) && x.EloWeight == GlobalConst.EloWeightMax))
            {
                System.Console.WriteLine(String.Format("{0}: {1}", acc.Name, acc.EffectiveElo));
                eloSum += acc.EffectiveElo;
                count++;
            }
            double average = eloSum / count;
            System.Console.WriteLine(String.Format("Average: {0}, Total players: {1}", average, count));
        }

        static void FixDemoEngineVersion()
        {
            var db = new ZkDataContext();
            foreach (var b in db.SpringBattles.Where(x => x.Title.Contains("[engine")))
            {
                var match = Regex.Match(b.Title, @"\[engine([^\]]+)\]");
                if (match.Success)
                {
                    var eng = match.Groups[1].Value;
                    if (eng != b.EngineVersion) b.EngineVersion = eng;
                }
            }
            db.SubmitChanges();
        }

        public class EloEntry
        {
            public double Elo = 1500;
            public int Cnt = 0;

        }

        public static void Test1v1Elo()
        {
            var db = new ZkDataContext();
            Dictionary<Account, EloEntry> PlayerElo = new Dictionary<Account, EloEntry>();

            int cnt = 0;
            foreach (var sb in db.SpringBattles.Where(x => !x.IsMission && !x.HasBots && !x.IsFfa && x.PlayerCount == 2).OrderBy(x => x.SpringBattleID))
            {
                cnt++;

                double winnerElo = 0;
                double loserElo = 0;

                var losers = sb.SpringBattlePlayers.Where(x => !x.IsSpectator && !x.IsInVictoryTeam).Select(x => new { Player = x, x.Account }).ToList();
                var winners = sb.SpringBattlePlayers.Where(x => !x.IsSpectator && x.IsInVictoryTeam).Select(x => new { Player = x, x.Account }).ToList();

                if (losers.Count != 1 || winners.Count != 1)
                {
                    continue;
                }

                foreach (var r in winners)
                {
                    EloEntry el;
                    if (!PlayerElo.TryGetValue(r.Account, out el)) el = new EloEntry();
                    winnerElo += el.Elo;
                }
                foreach (var r in losers)
                {
                    EloEntry el;
                    if (!PlayerElo.TryGetValue(r.Account, out el)) el = new EloEntry();
                    loserElo += el.Elo;
                }

                winnerElo = winnerElo / winners.Count;
                loserElo = loserElo / losers.Count;

                var eWin = 1 / (1 + Math.Pow(10, (loserElo - winnerElo) / 400));
                var eLose = 1 / (1 + Math.Pow(10, (winnerElo - loserElo) / 400));

                var sumCount = losers.Count + winners.Count;
                var scoreWin = Math.Sqrt(sumCount / 2.0) * 32 * (1 - eWin);
                var scoreLose = Math.Sqrt(sumCount / 2.0) * 32 * (0 - eLose);

                foreach (var r in winners)
                {
                    var change = (float)(scoreWin);
                    EloEntry elo;
                    if (!PlayerElo.TryGetValue(r.Account, out elo))
                    {
                        elo = new EloEntry();
                        PlayerElo[r.Account] = elo;
                    }
                    elo.Elo += change;
                    elo.Cnt++;
                }

                foreach (var r in losers)
                {
                    var change = (float)(scoreLose);
                    EloEntry elo;
                    if (!PlayerElo.TryGetValue(r.Account, out elo))
                    {
                        elo = new EloEntry();
                        PlayerElo[r.Account] = elo;
                    }
                    elo.Elo += change;
                    elo.Cnt++;
                }
            }


            Console.WriteLine("Total battles: {0}", cnt);
            Console.WriteLine("Name;1v1Elo;TeamElo;1v1Played;TeamPlayed");
            foreach (var entry in PlayerElo.Where(x => x.Value.Cnt > 40).OrderByDescending(x => x.Value.Elo))
            {
                Console.WriteLine("{0};{1:f0};{2:f0};{3};{4}", entry.Key.Name, entry.Value.Elo, entry.Key.EffectiveElo, entry.Value.Cnt, entry.Key.SpringBattlePlayers.Count(x => !x.IsSpectator && x.SpringBattle.PlayerCount > 2));
            }

        }

        public static void TestPrediction()
        {
            var db = new ZkDataContext();
            var cnt = 0;
            var winPro = 0;
            var winLessNub = 0;
            var winMoreVaried = 0;
            var winPredicted = 0;


            foreach (var sb in db.SpringBattles.Where(x => !x.IsMission && !x.HasBots && !x.IsFfa && x.IsEloProcessed && x.PlayerCount >= 8 && !x.Events.Any()).OrderByDescending(x => x.SpringBattleID))
            {

                var losers = sb.SpringBattlePlayers.Where(x => !x.IsSpectator && !x.IsInVictoryTeam).Select(x => new { Player = x, x.Account }).ToList();
                var winners = sb.SpringBattlePlayers.Where(x => !x.IsSpectator && x.IsInVictoryTeam).Select(x => new { Player = x, x.Account }).ToList();

                if (losers.Count == 0 || winners.Count == 0 || losers.Count == winners.Count) continue;

                if (winners.Select(x => x.Account.EffectiveElo).Max() > losers.Select(x => x.Account.EffectiveElo).Max()) winPro++;
                if (winners.Select(x => x.Account.EffectiveElo).Min() > losers.Select(x => x.Account.EffectiveElo).Min()) winLessNub++;

                if (winners.Select(x => x.Account.EffectiveElo).StdDev() > losers.Select(x => x.Account.EffectiveElo).StdDev()) winMoreVaried++;

                var winnerElo = winners.Select(x => x.Account.EffectiveElo).Average();
                var loserElo = losers.Select(x => x.Account.EffectiveElo).Average();

                var eWin = 1 / (1 + Math.Pow(10, (loserElo - winnerElo) / 400));
                var eLose = 1 / (1 + Math.Pow(10, (winnerElo - loserElo) / 400));

                if (eWin > eLose) winPredicted++;


                cnt++;
                if (cnt == 200) break;
            }

            Console.WriteLine("prwin: {0},  lessnubwin: {1},  morevaried: {2},  count: {3}, predicted:{4}", winPro, winLessNub, winMoreVaried, cnt, winPredicted);
        }

        static void RecalculateBattleElo()
        {
            using (var db = new ZkDataContext())
            {
                foreach (var b in db.SpringBattles.Where(x => !x.IsEloProcessed).ToList())
                {
                    Console.WriteLine(b.SpringBattleID);
                    b.CalculateAllElo();
                }
                db.SubmitChanges();
            }
        }

        static void ImportSpringiePlayers()
        {
            var db = new ZkDataContext();
            foreach (var line in File.ReadLines("springie.csv").AsParallel())
            {
                var m = Regex.Match(line, "\"[0-9]+\";\"([^\"]+)\";\"[0-9]+\";\"[0-9]+\";\"([^\"]+)\";\"([^\"]+)\"");
                if (m.Success)
                {
                    string name = m.Groups[1].Value;
                    double elo = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                    double w = double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
                    if (elo != 1500 || w != 1)
                    {
                        foreach (var a in db.Accounts.Where(x => x.Name == name))
                        {
                            a.Elo = (float)elo;
                            a.EloWeight = (float)w;
                        }
                        Console.WriteLine(name);
                    }
                }
            }
            db.SubmitChanges();
        }

        static void FixMaps()
        {
            try
            {
                var db = new ZkDataContext();
                foreach (var r in db.Resources.Where(x => x.LastChange == null)) r.LastChange = DateTime.UtcNow;
                db.SubmitChanges();
                return;

                foreach (var resource in db.Resources.Where(x => x.TypeID == ResourceType.Map))//&&x.MapSizeSquared == null))
                {
                    var file = String.Format("{0}/{1}.metadata.xml.gz", @"d:\zero-k.info\www\Resources", resource.InternalName.EscapePath());
                    var map = (Map)new XmlSerializer(typeof(Map)).Deserialize(new MemoryStream(File.ReadAllBytes(file).Decompress()));

                    resource.MapWidth = map.Size.Width / 512;
                    resource.MapHeight = map.Size.Height / 512;

                    if (string.IsNullOrEmpty(resource.AuthorName))
                    {

                        if (!string.IsNullOrEmpty(map.Author)) resource.AuthorName = map.Author;
                        else
                        {
                            Console.WriteLine("regex test");
                            var m = Regex.Match(map.Description, "by ([\\w]+)", RegexOptions.IgnoreCase);
                            if (m.Success) resource.AuthorName = m.Groups[1].Value;
                        }
                    }
                    Console.WriteLine("author: " + resource.AuthorName);


                    if (resource.MapIsSpecial == null) resource.MapIsSpecial = map.ExtractorRadius > 120 || map.MaxWind > 40;
                    resource.MapSizeSquared = (map.Size.Width / 512) * (map.Size.Height / 512);
                    resource.MapSizeRatio = (float)map.Size.Width / map.Size.Height;

                    var minimap = String.Format("{0}/{1}.minimap.jpg", @"d:\zero-k.info\www\Resources", resource.InternalName.EscapePath());

                    using (var im = Image.FromFile(minimap))
                    {
                        int w, h;

                        if (resource.MapSizeRatio > 1)
                        {
                            w = 96;
                            h = (int)(w / resource.MapSizeRatio);
                        }
                        else
                        {
                            h = 96;
                            w = (int)(h * resource.MapSizeRatio);
                        }

                        using (var correctMinimap = new Bitmap(w, h, PixelFormat.Format24bppRgb))
                        {
                            using (var graphics = Graphics.FromImage(correctMinimap))
                            {
                                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                graphics.DrawImage(im, 0, 0, w, h);
                            }

                            var jgpEncoder = ImageCodecInfo.GetImageEncoders().First(x => x.FormatID == ImageFormat.Jpeg.Guid);
                            var encoderParams = new EncoderParameters(1);
                            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 100L);

                            var target = String.Format("{0}/{1}.thumbnail.jpg", @"d:\zero-k.info\www\Resources", resource.InternalName.EscapePath());
                            correctMinimap.Save(target, jgpEncoder, encoderParams);
                        }
                    }
                    Console.WriteLine(string.Format("{0}", resource.InternalName));
                }
                db.SubmitChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void MassBan(string name, int startIndex, int endIndex, string reason, int banHours, bool banSite = false, bool banLobby = true, bool banIP = false, bool banID = false)
        {
            ZkDataContext db = new ZkDataContext();
            for (int i = startIndex; i <= endIndex; i++)
            {
                Account acc = db.Accounts.FirstOrDefault(x => x.Name == name + i);
                if (acc != null)
                {
                    int? userID = banID ? (int?)acc.AccountUserIDs.OrderByDescending(x => x.LastLogin).FirstOrDefault().UserID : null;
                    string userIP = banIP ? acc.AccountIPs.OrderByDescending(x => x.LastLogin).FirstOrDefault().IP : null;
                    System.Console.WriteLine(acc.Name, userID, userIP);
                    Punishment punishment = new Punishment
                    {
                        Time = DateTime.UtcNow,
                        Reason = reason,
                        BanSite = banSite,
                        BanLobby = banLobby,
                        BanExpires = DateTime.UtcNow.AddHours(banHours),
                        BanIP = userIP,
                        CreatedAccountID = 5806,
                        UserID = userID,
                    };
                    acc.PunishmentsByAccountID.Add(punishment);
                }
            }
            db.SubmitChanges();
        }


        public static void TestPwMatch()
        {
            Global.Nightwatch = new Nightwatch(Directory.GetCurrentDirectory());
            Global.Nightwatch.Start();
            Global.PlanetWarsMatchMaker = new PlanetWarsMatchMaker(Global.Nightwatch.Tas);
            var db = new ZkDataContext();
            var gal = db.Galaxies.First(x => x.IsDefault);
            Global.PlanetWarsMatchMaker.AddAttackOption(gal.Planets.Skip(1).First());
            /*
            Utils.StartAsync(() =>
            {
                Thread.Sleep(30000);
                challenge = new AttackOption();
                challenge.Attackers.Add(tas.ExistingUsers["Licho"]);
                challenge.Defenders.Add(tas.ExistingUsers["gajop"]);
                AcceptChallenge();
            });*/

            
            Console.ReadLine();

        }
    }
}
