﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Linq;
using System.Diagnostics;
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
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Xml.Serialization;
using AutoRegistrator;
using EntityFramework.Extensions;
//using LobbyClient;
//using NightWatch;
using LobbyClient;
using Microsoft.Linq.Translations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlasmaDownloader;
using PlasmaDownloader.Packages;
using PlasmaShared;
using ZkData.UnitSyncLib;
using ZkData;
using Encoder = System.Drawing.Imaging.Encoder;
using PlasmaDownloader = PlasmaDownloader.PlasmaDownloader;
using Ratings;
using ZeroKWeb;
using AutoRegistrator = ZeroKWeb.AutoRegistrator;
using ZkLobbyServer = ZkLobbyServer.ZkLobbyServer;

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
                uint hash1 = Crc.Crc32(mac2);
                uint hash2 = Crc.Crc32Old(mac2);
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

        public static void FixDuplicatedAccounts()
        {
            GlobalConst.Mode = ModeType.Test;
            var db = new ZkDataContext();
            var duplicates = db.Accounts.GroupBy(x => x.Name).Where(x => x.Count() > 1).ToList();
            foreach (var duplicateGroup in duplicates)
            {
                var keep = duplicateGroup.OrderByDescending(x => x.SpringBattlePlayers.Count()).ThenByDescending(x => x.LastLogin).First();
                foreach (var todel in duplicateGroup.ToList())
                    if (keep.AccountID != todel.AccountID)
                    {
                        try
                        {
                            db.ForumThreadLastReads.DeleteAllOnSubmit(todel.ForumThreadLastReads.ToList());
                            db.AccountBattleAwards.DeleteAllOnSubmit(todel.AccountBattleAwards.ToList());
                            db.Accounts.DeleteOnSubmit(todel);
                            db.SaveChanges();
                            Console.WriteLine("Deleted {0}", todel.Name);
                        }
                        catch (Exception ex)
                        {
                            using (var db2 = new ZkDataContext())
                            {
                                var acc = db2.Accounts.Find(todel.AccountID);
                                acc.IsDeleted = true;
                                acc.Name = string.Format("___DELETED___{0}", todel.AccountID); // hacky way to avoid duplicates
                                db2.SaveChanges();
                            }
                            Console.WriteLine("Failed to delete {0}", todel.Name);
                        }
                    }
            }
        }

        public static void RemoveUnusedMaps()
        {
            var db = new ZkDataContext();
            var mintime = DateTime.Now.AddMonths(-12);

            var maps = db.Resources.Where(x => x.TypeID == ResourceType.Map)
                .Select(x => new { Map = x, Count = x.SpringBattlesByMapResourceID.Count(y => y.StartTime >= mintime) })
                .Where(x => x.Map.MapSupportLevel >= MapSupportLevel.Supported || x.Count > 1 && x.Map.ResourceContentFiles.Any(y=>y.LinkCount >0))
                .Select(x => x.Map.ResourceContentFiles.OrderByDescending(y => y.LinkCount).Select(y=>y.FileName.ToLower()).FirstOrDefault()).ToList();

            
            var files = Directory.GetFiles(Path.Combine(GlobalConst.SiteDiskPath, "autoregistrator", "maps")).Select(Path.GetFileName).ToList();

            foreach (var f in files.Where(x => !maps.Contains(x.ToLower())))
            {
                var todel = Path.Combine(GlobalConst.SiteDiskPath, "autoregistrator", "maps", f);
                Console.WriteLine($"{f}");
                File.Delete(todel);
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


        public static void RenameAccount(int accountID, string newName)
        {
            var db = new ZkDataContext();
            Account acc = db.Accounts.FirstOrDefault(x => x.AccountID == accountID);
            string oldName = acc.Name;
            acc.SetName(newName);
            Console.WriteLine("Renaming {0} to {1}", oldName, newName);
            db.SaveChanges();
        }

        public static void FixStuff()
        {
            //RenameAccount(359399, "IcyIcyIce2");
            //RenameAccount(235316, "IcyIcyIce");

            //AddClanLeader(530, 5806);
            //
            //UpdateMissionProgression(13);
            /*
            var db = new ZkDataContext();
            int accountID = 5806;
            List<CampaignJournal> unlockedJournals = new List<CampaignJournal>();
            CampaignPlanet planet = db.CampaignPlanets.First(x => x.PlanetID == 13);
            int campID = planet.CampaignID;
            db.CampaignEvents.InsertOnSubmit(Global.CreateCampaignEvent(accountID, campID, "Planet completed: {0}", planet));
            //foreach (CampaignJournal journal in db.CampaignJournals.Where(x => x.CampaignID == campID && x.PlanetID == planet.PlanetID && x.UnlockOnPlanetCompletion))
            //{
            //    unlockedJournals.Add(journal);
            //}
            //foreach (CampaignJournal uj in unlockedJournals)
            //{
            //    db.CampaignEvents.InsertOnSubmit(Global.CreateCampaignEvent(accountID, campID, "{1} - Journal entry unlocked: {0}", uj, uj.CampaignPlanet));
            //}
            db.SubmitChanges();
            */
        }
        public static void AddClanLeader(int clanID, int? leaderID)
        {
            var db = new ZkDataContext();
            var role = db.RoleTypes.First(x => x.IsClanOnly);
            Clan clan = db.Clans.FirstOrDefault(x => x.ClanID == clanID);

            if (!clan.AccountRoles.Any(y => y.RoleType.IsClanOnly))
            {
                Account picked = null;
                if (leaderID != null) picked = db.Accounts.FirstOrDefault(x => x.AccountID == leaderID);
                if (picked == null) picked = clan.Accounts.Where(x => x.LastLogin >= DateTime.UtcNow.AddDays(-7)).OrderByDescending(x => x.Level).FirstOrDefault();
                if (picked == null) picked = clan.Accounts.OrderByDescending(x => x.LastLogin).FirstOrDefault();
                if (picked != null)
                {
                    if (picked.ClanID != clanID)
                    {
                        picked.ClanID = clanID;
                    }

                    clan.AccountRoles.Add(new AccountRole()
                    {
                        AccountID = picked.AccountID,
                        ClanID = clan.ClanID,
                        Inauguration = DateTime.UtcNow,
                        RoleTypeID = role.RoleTypeID
                    });
                    clan.IsDeleted = false;
                }
            }
            db.SaveChanges();
        }

        public static void AddClanLeaders()
        {
            var db = new ZkDataContext();
            foreach (var clan in db.Clans.Where(x => !x.IsDeleted))
            {
                AddClanLeader(clan.ClanID, null);
            }
            //db.SubmitAndMergeChanges();
        }


        public static void GetGameStats(DateTime from)
        {
            //GlobalConst.Mode = ModeType.Live;
            var db = new ZkDataContext();
            var bats = db.SpringBattles.Where(x => x.StartTime >= from && x.Duration >= 60 * 5).ToList();
            Console.WriteLine("Battles from {0}", from);
            var total = bats.Count;
            Console.WriteLine("Total: {0}", total);
            var breakdown = bats.GroupBy(x => x.PlayerCount).OrderBy(x => x.Key).Select(x => new
            {
                Size = x.Key,
                Count = x.Count()
            }).ToList();

            foreach (var b in breakdown)
            {
                Console.WriteLine("Size: {0}    Battles: {1}", b.Size, b.Count);
            }



        }




        public class MiniBat
        {
            public int ID;
            public int Duration;
            public int MapID;
            public List<List<int>> Players = new List<List<int>>();
        }

        public static void SaveMiniBats(string path) {
            var js = JsonSerializer.Create();
            using (var fs = File.OpenWrite(path)) 
            using (var tw = new StreamWriter(fs)) js.Serialize(tw, GetMiniBats());
        }

        public static void DeleteOldUsers() {
            //GlobalConst.Mode = ModeType.Live;
            var dbo = new ZkDataContext();

            var limit = DateTime.Now.AddMonths(-1);
            var acid = dbo.Accounts.Where(x=>!x.IsBot && !x.SpringBattles.Any() && !x.ForumThreads.Any() && !x.SpringBattlePlayers.Any(z=>!z.IsSpectator) && x.MissionRunCount == 0 && !x.ForumPosts.Any() && !x.ContributionsByAccountID.Any() && x.LastLogin <= limit).OrderBy(x=>x.AccountID).Select(x => x.AccountID).ToList();

            Console.WriteLine("Deleting: {0}", acid.Count);

            //Console.WriteLine(dbo.Accounts.Where(x=>x.SpringBattlePlayers.Any(y=>y.IsSpectator)  && !x.SpringBattlePlayers.Any(y=>!y.IsSpectator)).Count());
            
            foreach (var id in acid)
            {
                try
                {
                    using (var db = new ZkDataContext())
                    {
                        var acc = db.Accounts.Find(id);
                        db.ForumLastReads.RemoveRange(acc.ForumLastReads);
                        db.ForumThreadLastReads.RemoveRange(acc.ForumThreadLastReads);
                        db.SpringBattlePlayers.RemoveRange(acc.SpringBattlePlayers);
                        db.AbuseReports.RemoveRange(acc.AbuseReportsByAccountID);
                        db.AbuseReports.RemoveRange(acc.AbuseReportsByReporterAccountID);
                        db.AccountRoles.RemoveRange(acc.AccountRolesByAccountID);
                        db.Ratings.RemoveRange(acc.Ratings);
                        db.MapRatings.RemoveRange(acc.MapRatings);
                        db.PollVotes.RemoveRange(acc.PollVotes);
                        db.SaveChanges();
                        db.Accounts.Remove(acc);
                        db.SaveChanges();
                        Console.WriteLine("Deleted: {0}",id);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.InnerException.InnerException.Message);
                }

            }
            //db.Accounts
        }


        public static void MakeActiveClanLeaders()
        {
            var db = new ZkDataContext();
            var year = DateTime.UtcNow.AddYears(-1);
            var day4 = DateTime.UtcNow.AddDays(-4);
            List<Clan> toDelClans = new List<Clan>();
            var leaderRole = db.RoleTypes.FirstOrDefault(x => x.RightKickPeople && x.IsClanOnly);
            foreach (var clan in db.Clans.Where(x=>!x.IsDeleted).ToList())
            {
                List<Tuple<Account,DateTime>> players = new List<Tuple<Account, DateTime>>();
                var acc = clan.Accounts.ToList();
                foreach (var a in acc)
                {
                    var lastTime =
                        db.SpringBattlePlayers.Where(x => x.AccountID == a.AccountID && !x.IsSpectator)
                            .OrderByDescending(x => x.SpringBattle.StartTime).Select(x=>x.SpringBattle.StartTime).FirstOrDefault();
                    players.Add(Tuple.Create(a, lastTime));
                }

                if (!Clan.IsShortcutValid(clan.Shortcut))
                {
                    toDelClans.Add(clan);
                } else if (!players.Any(x => x.Item2 >= year))
                {
                    clan.IsDeleted = true;
                    toDelClans.Add(clan);
                }
                else
                {
                    var leader = players.Where(x => x.Item1.AccountRolesByAccountID.Any(y=>y.RoleTypeID == leaderRole.RoleTypeID)).FirstOrDefault();
                    if (leader == null || leader.Item2 < day4)
                    {
                        var newLeader = players.FirstOrDefault();
                        if (newLeader != null && (leader == null || newLeader.Item1.AccountID != leader.Item1.AccountID))
                        {
                            db.AccountRoles.InsertOnSubmit(new AccountRole()
                            {
                                AccountID = newLeader.Item1.AccountID,
                                ClanID = clan.ClanID,
                                RoleTypeID = leaderRole.RoleTypeID,
                                Inauguration = DateTime.UtcNow
                            });
                        }
                    }
                }
                db.SaveChanges();
            }

            db.Clans.RemoveRange(toDelClans);
            db.SaveChanges();
        }


        private static void TestPwMatchMaker()
        {
            var server = new global::ZkLobbyServer.ZkLobbyServer("", new PlanetwarsEventCreator());
            var mm = server.PlanetWarsMatchMaker;
            mm.ChallengeTime = DateTime.Now;
            mm.AttackerSideCounter = 1;
            //mm.ResetAttackOptions();
            mm.GenerateLobbyCommand();
            mm.Challenge = mm.AttackOptions[3];
            mm.Challenge.OwnerFactionID = 2;
            mm.Challenge.PlanetID = 4375;
            mm.GetDefendingFactions(mm.Challenge);
            mm.GenerateLobbyCommand();

            //mm.Challenge = 
            Global.Server.PlanetWarsMatchMaker.GenerateLobbyCommand();
        }

        public static void SteamMapUnfeaturer()
        {
            var extraNames = RegistratorRes.campaignMaps.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim())
                .ToArray();

            var db = new ZkDataContext();
            DateTime limit = DateTime.Now.AddMonths(-6);


            var top100 = db.SpringBattles.Where(x => x.StartTime >= limit).GroupBy(x => x.ResourceByMapResourceID).Where(x => x.Key.MapSupportLevel >= MapSupportLevel.Supported).OrderByDescending(x => x.Sum(y => y.Duration * (y.SpringBattlePlayers.Count))).Select(x => x.Key.ResourceID).Take(50).ToList();

            //var pwMaps = db.Galaxies.Where(x => x.IsDefault).SelectMany(x => x.Planets).Where(x => x.MapResourceID != null).Select(x => x.MapResourceID.Value).ToList();

            //top100.AddRange(pwMaps);

            top100 = top100.Distinct().ToList();

            var shipping = db.Resources.Where(x => top100.Contains(x.ResourceID) || extraNames.Contains(x.InternalName) || (x.TypeID == ResourceType.Map && x.MapSupportLevel >= MapSupportLevel.MatchMaker)).Select(x => x.InternalName).ToList();

            File.WriteAllText(@"c:\temp\shipped.txt", string.Join("\n", shipping));


            var featured = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.MapSupportLevel >= MapSupportLevel.Featured)
                .Select(x => x.InternalName).ToList();


            File.WriteAllText(@"c:\temp\featured.txt", string.Join("\n", featured));



            var unfeature = featured.Where(x => !shipping.Contains(x)).ToList();


            File.WriteAllText(@"c:\temp\unfeature.txt", string.Join("\n", unfeature));

            //db.Resources.Where(x => unfeature.Contains(x.InternalName)).Update(x => new Resource { MapSupportLevel = MapSupportLevel.Supported });
            //db.SaveChanges();

        }


        static void Main(string[] args)
        {
            if (Console.ReadLine()?.StartsWith("i read the code") != true) return;

            
            return;




            //RemoveUnusedMaps();

            //PlanetwarsFixer.PurgeGalaxy();

            //PlanetwarsFixer.AddWormholes(28);
            //PlanetwarsFixer.StartGalaxy(28, 3);
            //PlanetwarsFixer.OwnPlanets(28);
            //PlanetwarsFixer.SetPlanetTeamSizes(28);

            Console.WriteLine("DONE");
            Console.ReadLine();

            return;
        }

        static IEnumerable<MiniBat> GetMiniBats() {

            foreach (var bid in new ZkDataContext().SpringBattles.Where(x => !x.IsMission && !x.HasBots).Select(x=>x.SpringBattleID))
            {
                using (var db = new ZkDataContext())
                {
                    var b = db.SpringBattles.Find(bid);
                    var bat = new MiniBat() { ID = b.SpringBattleID, Duration = b.Duration, MapID = b.MapResourceID, Players = new List<List<int>>() };

                    foreach (var team in b.SpringBattlePlayers.GroupBy(x => x.AllyNumber).OrderByDescending(x => x.First().IsInVictoryTeam))bat.Players.Add(team.Select(x => x.AccountID).ToList());
                    yield return bat;
                }
            }
        }

        static void CountPlayers()
        {
            var db = new ZkDataContext();
            var accs =
                db.SpringBattles.OrderByDescending(x => x.SpringBattleID)
                    .Take(5000)
                    .SelectMany(x => x.SpringBattlePlayers)
                    .Where(x => !x.IsSpectator)
                    .Select(x => new { x.Account, x.SpringBattle.Duration })
                    .GroupBy(x => x.Account)
                    .Select(x => new { Account = x.Key, Duration = x.Sum(y => y.Duration) })
                    .ToList();

            var durOther = accs.Where(x => x.Account.LobbyVersion != null && !x.Account.LobbyVersion.StartsWith("ZK")).Sum(x => (long)x.Duration);
            var durZk = accs.Where(x => x.Account.LobbyVersion != null && x.Account.LobbyVersion.StartsWith("ZK")).Sum(x => (long)x.Duration);

            var cntOther = accs.Where(x => x.Account.LobbyVersion != null && !x.Account.LobbyVersion.StartsWith("ZK")).Count();
            var cntZk = accs.Where(x => x.Account.LobbyVersion != null && x.Account.LobbyVersion.StartsWith("ZK")).Count();
        }

        static void MigrateDatabase()
        {
            GlobalConst.Mode = ModeType.Test;
            var cloner = new DbCloner("zero-k", "zero-k_test", GlobalConst.ZkDataContextConnectionString);
            cloner.LogEvent += s => { Console.WriteLine(s); };
            cloner.CloneAllTables();
        }


        public static void RecalculateKudos()
        {
            var db = new ZkDataContext();
            foreach (var acc in db.Accounts.Where(x => x.KudosPurchases.Any())) acc.HasKudos = true;
            db.SaveChanges();
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
            foreach (var m in db.Resources.Where(x => x.MapSupportLevel>=MapSupportLevel.Featured && x.TypeID == ResourceType.Map))
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
            db.SaveChanges();
        }



        public static void FixMissionScripts()
        {
            var db = new ZkDataContext();
            var missions = db.Missions.ToList();
            foreach (Mission mission in missions)
            {
                mission.Script = Regex.Replace(mission.Script, "GameType=([^;]+);", (m) => { return string.Format("GameType={0};", mission.NameWithVersion); });
            }
            db.SaveChanges();
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
        /*
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
        }*/
        
        /*
        public static void GetClanStackWinRate(int clanID, int minElo)
        {
            int won = 0, lost = 0;
            ZkDataContext db = new ZkDataContext();
            Clan clan = db.Clans.FirstOrDefault(x => x.ClanID == clanID);
            var battles = db.SpringBattles.Where(x => x.SpringBattlePlayers.Count(p => p.Account.ClanID == clanID && p.Account.Elo >= minElo && !p.IsSpectator) >= 2
                && x.StartTime.CompareTo(DateTime.Now.AddMonths(-1)) > 0 && x.ResourceByMapResourceID.MapIsSpecial != true).ToList();
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
        }*/

        public static void RenameOldAccounts()
        {
            using (var db = new ZkDataContext())
            {
                db.Database.ExecuteSqlCommand(
                    "update accounts set name = name + '#old#' + cast(accountid as nvarchar)  where lastlogin<'20150101' and (select count(*) from springbattleplayers where accountid=accounts.accountid and isspectator=0) < 10 and LastNewsRead < '20150101' and name not like '%#old#%'");
            }
            

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
            db.SaveChanges();
        }

        public class EloEntry
        {
            public double Elo = 1500;
            public int Cnt = 0;

        }

        /*
        public static void Test1v1Elo()
        {
            var db = new ZkDataContext();
            Dictionary<Account, EloEntry> PlayerElo = new Dictionary<Account, EloEntry>();

            int cnt = 0;
            foreach (var sb in db.SpringBattles.Where(x => !x.IsMission && !x.HasBots && x.PlayerCount == 2).OrderBy(x => x.SpringBattleID))
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

        }*/

        /*
        public static void TestPrediction()
        {
            var db = new ZkDataContext();
            var cnt = 0;
            var winPro = 0;
            var winLessNub = 0;
            var winMoreVaried = 0;
            var winPredicted = 0;


            foreach (var sb in db.SpringBattles.Where(x => !x.IsMission && !x.HasBots && x.IsEloProcessed && x.PlayerCount >= 8 && !x.Events.Any()).OrderByDescending(x => x.SpringBattleID))
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
                db.SaveChanges();
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
            db.SaveChanges();
        }*/

        static void FixMaps()
        {
            try
            {
                var db = new ZkDataContext();
                foreach (var r in db.Resources.Where(x => x.LastChange == null)) r.LastChange = DateTime.UtcNow;
                db.SaveChanges();
                return;

                foreach (var resource in db.Resources.Where(x => x.TypeID == ResourceType.Map))//&&x.MapSizeSquared == null))
                {
                    var file = String.Format("{0}/{1}.metadata.xml.gz", GlobalConst.SiteDiskPath + @"\Resources", resource.InternalName.EscapePath());
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

                    var minimap = String.Format("{0}/{1}.minimap.jpg", GlobalConst.SiteDiskPath + @"\Resources", resource.InternalName.EscapePath());

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

                            var target = String.Format("{0}/{1}.thumbnail.jpg", GlobalConst.SiteDiskPath + @"\Resources", resource.InternalName.EscapePath());
                            correctMinimap.Save(target, jgpEncoder, encoderParams);
                        }
                    }
                    Console.WriteLine(string.Format("{0}", resource.InternalName));
                }
                db.SaveChanges();
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
            db.SaveChanges();
        }


        public static void TestPwMatch()
        {
            //Global.Nightwatch = new Nightwatch();
            //Global.Nightwatch.Start();
            // Global.PlanetWarsMatchMaker = new PlanetWarsMatchMaker(Global.Nightwatch.Tas);
            var db = new ZkDataContext();
            var gal = db.Galaxies.First(x => x.IsDefault);
            // Global.PlanetWarsMatchMaker.AddAttackOption(gal.Planets.Skip(1).First());
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
