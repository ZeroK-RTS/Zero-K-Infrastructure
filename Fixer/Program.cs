using System;
using System.Collections.Generic;
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
using LobbyClient;
using NightWatch;
using PlasmaShared;
using PlasmaShared.UnitSyncLib;
using ZkData;
using Encoder = System.Drawing.Imaging.Encoder;

namespace Fixer
{
    public static class Program
    {



        public static void FixHashes() {
            var db = new ZkDataContext();
            var lo = new DataLoadOptions();
            lo.LoadWith<Resource>(x => x.ResourceSpringHashes);
            db.LoadOptions = lo;
            foreach (var r in db.Resources) {
                var h84 = r.ResourceSpringHashes.Where(x => x.SpringVersion == "84").Select(x => x.SpringHash).SingleOrDefault();
                var h840 = r.ResourceSpringHashes.Where(x => x.SpringVersion == "84.0").Select(x => x.SpringHash).SingleOrDefault();

                if (h84 != h840) {
                    var entry = r.ResourceSpringHashes.SingleOrDefault(x => x.SpringVersion == "84.0");
                    if (h84 != 0) {
                        if (entry == null) {
                            entry = new ResourceSpringHash() { SpringVersion = "84.0" };
                            r.ResourceSpringHashes.Add(entry);
                        }
                        entry.SpringHash = h84;
                    }
                    else {
                        if (entry != null) db.ResourceSpringHashes.DeleteOnSubmit(entry);
                    }
                }
            }
            db.SubmitChanges();
        }


        public static void SetFFATeams() {
            var db = new ZkDataContext();
            foreach (var m in db.Resources.Where(x => x.FeaturedOrder != null && x.TypeID == ResourceType.Map)) {
                var lg = m.SpringBattlesByMapResourceID.Take(100).ToList();
                double cnt = lg.Count;
                if (cnt == 0) continue;
                ;
                if (lg.Count(x => x.HasBots)/(double)cnt > 0.5) {
                    m.MapIsChickens = true;
                }

                if (lg.Count(x => x.PlayerCount == 2)/cnt > 0.4) {
                    m.MapIs1v1 = true;
                }

                var teams =
                    m.SpringBattlesByMapResourceID.Take(100).GroupBy(
                        x => x.SpringBattlePlayers.Where(y => !y.IsSpectator).Select(y => y.AllyNumber).Distinct().Count()).OrderByDescending(
                            x => x.Count()).Select(x => x.Key).FirstOrDefault();
                if (teams > 2) {
                    m.MapIsFfa = true;
                    m.MapFFAMaxTeams = teams;
                }
                else {}



            }
            db.SubmitChanges();

        }


        public static void FixDemoFiles() {
            var db = new ZkDataContext();
            foreach (var sb in db.SpringBattles) {
                //sb.ReplayFileName = sb.ReplayFileName.Replace("http://springdemos.licho.eu/","http://zero-k.info/replays/");
            }
            //db.SubmitChanges();

        }


        public static void GenerateTechs() {
            var db = new ZkDataContext();
            db.StructureTypes.DeleteAllOnSubmit(db.StructureTypes.Where(x => x.Unlock != null));
            db.SubmitAndMergeChanges();
            
            foreach (var u in db.Unlocks.Where(x=>x.UnlockType== UnlockTypes.Unit)) {
                var s = new StructureType()
                        {
                            BattleDeletesThis = false,
                            Cost = u.XpCost/2,
                            MapIcon = "techlab.png",
                            DisabledMapIcon = "techlab_dead.png",
                            Name = u.Name,
                            Description = string.Format("Access to {0} and increases influence gains", u.Name),
                            TurnsToActivate = u.XpCost/100,
                            IsBuildable = true,
                            IsIngameDestructible = true,
                            IsBomberDestructible = true,
                            Unlock = u,
                            UpkeepEnergy = u.XpCost/5,
                            IngameUnitName = "pw_" + u.Code,
                        };
                db.StructureTypes.InsertOnSubmit(s);
            }
            db.SubmitAndMergeChanges();

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
            System.Console.WriteLine("Total cost: " + numModules*moduleCost);
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

        public static void ProgressCampaign(ZkDataContext db, Account acc, Mission mission)
        {
            CampaignPlanet planet = db.CampaignPlanets.FirstOrDefault(p => p.MissionID == mission.MissionID);
            if (planet != null)
            {
                // first mark this planet as completed - but only if it's already unlocked
                AccountCampaignProgress progress = db.AccountCampaignProgress.FirstOrDefault(x => x.AccountID == acc.AccountID && x.PlanetID == planet.PlanetID && x.CampaignID == planet.CampaignID);
                bool alreadyCompleted = false;
                int accountID = acc.AccountID;
                int campID = planet.CampaignID;

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

                    // unlock this planet's journals if appropriate
                    var journals = db.CampaignJournals.Where(x => x.CampaignID == campID && x.PlanetID == planet.PlanetID);
                    foreach (CampaignJournal journal in journals)
                    {
                        bool proceed = true;

                        var requiredVars = db.CampaignJournalVars.Where(x => x.CampaignID == campID && x.JournalID == journal.JournalID).ToList();
                        if (requiredVars.Count() == 0)
                        {
                            proceed = false; // no need to add to unlock table; it'll be marked as unlocked at runtime
                            if (journal.UnlockOnPlanetCompletion && !alreadyCompleted)
                            {
                            }
                        }
                        else
                        {
                            foreach (CampaignJournalVar variable in requiredVars)
                            {
                                var accountVar = db.AccountCampaignVars.FirstOrDefault(x => x.CampaignID == campID && x.VarID == variable.RequiredVarID);
                                if (accountVar.Value != variable.RequiredValue)
                                {
                                    proceed = false;
                                    break;  // failed to meet var requirement, stop here
                                }
                            }
                        }
                        if (proceed)    // met requirements for unlocking journal
                        {
                            AccountCampaignJournalProgress jp = db.AccountCampaignJournalProgress.FirstOrDefault(x => x.AccountID == accountID
                                && x.CampaignID == campID
                                && x.JournalID == journal.JournalID);
                            if (jp == null)
                            {
                                jp = new AccountCampaignJournalProgress() { AccountID = accountID, CampaignID = campID, JournalID = journal.JournalID, IsUnlocked = true };
                                db.AccountCampaignJournalProgress.InsertOnSubmit(jp);
                            }
                            else if (!jp.IsUnlocked)
                            {
                                jp.IsUnlocked = true;
                            }
                        }
                    }


                    // unlock planets made available by completing this one
                    var links = db.CampaignLinks.Where(x => x.UnlockingPlanetID == planet.PlanetID && x.CampaignID == campID);
                    foreach (CampaignLink link in links)
                    {
                        CampaignPlanet toUnlock = link.PlanetToUnlock;
                        bool proceed = true;
                        var requiredVars = db.CampaignPlanetVars.Where(x => x.CampaignID == campID && x.PlanetID == toUnlock.PlanetID);
                        if (requiredVars.Count() == 0) proceed = true;
                        else
                        {
                            foreach (CampaignPlanetVar variable in requiredVars)
                            {
                                var accountVar = db.AccountCampaignVars.FirstOrDefault(x => x.CampaignID == campID && x.VarID == variable.RequiredVarID);
                                if (accountVar.Value != variable.RequiredValue)
                                {
                                    proceed = false;
                                    break;  // failed to meet var requirement, stop here
                                }
                            }
                            proceed = true;
                        }

                        if (proceed)    // met requirements for unlocking planet
                        {
                            System.Console.WriteLine("\t"+toUnlock);
                            AccountCampaignProgress progress2 = db.AccountCampaignProgress.FirstOrDefault(x => x.AccountID == accountID
                                && x.CampaignID == campID
                                && x.PlanetID == toUnlock.PlanetID);
                            bool alreadyUnlocked = false;
                            if (progress2 == null)
                            {
                                progress2 = new AccountCampaignProgress() { AccountID = accountID, CampaignID = campID, PlanetID = toUnlock.PlanetID, IsCompleted = false, IsUnlocked = true };
                                db.AccountCampaignProgress.InsertOnSubmit(progress2);
                            }
                            else if (!progress2.IsUnlocked)
                            {
                                progress2.IsUnlocked = true;
                            }
                            else alreadyUnlocked = true;

                            // unlock their journals too if appropriate
                            var journals2 = db.CampaignJournals.Where(x => x.CampaignID == campID && x.PlanetID == toUnlock.PlanetID);
                            foreach (CampaignJournal journal in journals2)
                            {
                                bool proceedJ = true;
                                var requiredVarsJ = db.CampaignJournalVars.Where(x => x.CampaignID == campID && x.JournalID == journal.JournalID).ToList();
                                if (requiredVarsJ.Count() == 0)
                                {
                                    proceedJ = false;   // no need to add to unlock table; it'll be marked as unlocked at runtime
                                    if (journal.UnlockOnPlanetUnlock && !alreadyUnlocked)
                                    {
                                    }
                                }
                                else
                                {
                                    foreach (CampaignJournalVar variableJ in requiredVarsJ)
                                    {
                                        var accountVar = db.AccountCampaignVars.FirstOrDefault(x => x.CampaignID == campID && x.VarID == variableJ.RequiredVarID);
                                        if (accountVar.Value != variableJ.RequiredValue)
                                        {
                                            proceedJ = false;
                                            break;  // failed to meet var requirement, stop here
                                        }
                                    }
                                }
                                if (proceedJ)    // met requirements for unlocking journal
                                {
                                    AccountCampaignJournalProgress jp = db.AccountCampaignJournalProgress.FirstOrDefault(x => x.AccountID == accountID
                                        && x.CampaignID == campID
                                        && x.JournalID == journal.JournalID);
                                    if (jp == null)
                                    {
                                        jp = new AccountCampaignJournalProgress() { AccountID = accountID, CampaignID = campID, JournalID = journal.JournalID, IsUnlocked = true };
                                        db.AccountCampaignJournalProgress.InsertOnSubmit(jp);
                                    }
                                    else if (!jp.IsUnlocked)
                                    {
                                        jp.IsUnlocked = true;
                                    }
                                }
                            }
                        }
                    }
                }
                db.SubmitChanges();
            }
        }

        public static void UpdateMissionProgression(int planetID)
        {
            ZkDataContext db = new ZkDataContext();
            var accp = db.AccountCampaignProgress.Where(x => x.PlanetID == planetID && x.IsCompleted && x.IsUnlocked).ToList();
            foreach (var prog in accp)
            {
                Account acc = prog.Account;
                System.Console.WriteLine(acc);
                ProgressCampaign(db, acc, prog.CampaignPlanet.Mission);
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

        public static void GetForumKarmaLadder()
        {
            ZkDataContext db = new ZkDataContext();
            var accounts = db.Accounts.Where(x=> x.ForumTotalUpvotes > 0 || x.ForumTotalDownvotes > 0).OrderByDescending(x => x.ForumTotalUpvotes - x.ForumTotalDownvotes).ToList();

            foreach (Account ac in accounts)
            {
                System.Console.WriteLine(ac.Name + "\t+" + ac.ForumTotalUpvotes + " / -" + ac.ForumTotalDownvotes);
            }
        }

        public static void GetForumKarmaVotes()
        {
            ZkDataContext db = new ZkDataContext();
            var posts = db.ForumPosts.Where(x => x.Upvotes > 0 || x.Downvotes > 0).OrderByDescending(x => x.Upvotes - x.Downvotes).ToList();

            foreach (var p in posts)
            {
                System.Console.WriteLine(p.ForumThreadID + "\t+" + p.ForumPostID + "\t+" + p.Upvotes + " / -" + p.Downvotes);
            }
        }

        public static void GetForumVotesByVoter(int? accountID, int? threadID)
        {
            ZkDataContext db = new ZkDataContext();
            var votes = db.AccountForumVotes.Where(x => (accountID == null || x.AccountID == accountID) && (threadID == null || x.ForumPost.ForumThreadID == threadID)).ToList();

            foreach (var v in votes)
            {
                System.Console.WriteLine(v.Account.Name + "\t+" + v.ForumPostID + "\t" + v.Vote);
            }
        }

        public static void DeleteUserVote(int accountID, int forumPostID, ZkDataContext db)
        {
            AccountForumVote existingVote = db.AccountForumVotes.SingleOrDefault(x => x.ForumPostID == forumPostID && x.AccountID == accountID);
            if (existingVote == null) return;

            ForumPost post = db.ForumPosts.First(x => x.ForumPostID == forumPostID);
            Account author = post.Account;

            int delta = existingVote.Vote;
            // reverse vote effects
            if (delta > 0)
            {
                author.ForumTotalUpvotes = author.ForumTotalUpvotes - delta;
                post.Upvotes = post.Upvotes - delta;
            }
            else if (delta < 0)
            {
                author.ForumTotalDownvotes = author.ForumTotalDownvotes + delta;
                post.Downvotes = post.Downvotes + delta;
            }
            db.AccountForumVotes.DeleteOnSubmit(existingVote);
        }

        public static void DeleteUserVotes(int accountID, int? threadID)
        {
            ZkDataContext db = new ZkDataContext();
            var votes = db.AccountForumVotes.Where(x=> x.AccountID == accountID && (threadID == null || x.ForumPost.ForumThreadID == threadID)).ToList();

            foreach (var v in votes)
            {
                DeleteUserVote(accountID, v.ForumPostID, db);
                System.Console.WriteLine(v.Account.Name + "\t" + v.ForumPostID + "\t" + v.Vote);
            }
            db.SubmitChanges();
        }

        public static void RecountForumVotes()
        {
            ZkDataContext db = new ZkDataContext();
            var accounts = db.Accounts.Where(x => x.ForumTotalUpvotes > 0 || x.ForumTotalDownvotes > 0).ToList();
            foreach (var acc in accounts)
            {
                acc.ForumTotalUpvotes = 0;
                acc.ForumTotalDownvotes = 0;
            }

            var votes = db.AccountForumVotes.ToList();
            foreach (var vote in votes)
            {
                Account poster = vote.ForumPost.Account;
                int delta = vote.Vote;
                if (delta > 0) poster.ForumTotalUpvotes = poster.ForumTotalUpvotes + delta;
                else if (delta < 0) poster.ForumTotalDownvotes = poster.ForumTotalDownvotes - delta;
            }
            db.SubmitChanges();
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

        public static void GetForumVotesByUser(int voterID, int voteeID)
        {
            int up = 0, down = 0;
            ZkDataContext db = new ZkDataContext();
            Account voter = db.Accounts.FirstOrDefault(x => x.AccountID == voterID), votee = db.Accounts.FirstOrDefault(x => x.AccountID == voteeID);
            var votes = db.AccountForumVotes.Where(x => x.AccountID == voterID && x.ForumPost.AuthorAccountID == voteeID).ToList();
            System.Console.WriteLine(voter.Name + ", " + votee.Name);
            foreach (var vote in votes)
            {
                int delta = vote.Vote;
                if (delta > 0) up++;
                else if (delta < 0) down++;
                System.Console.WriteLine(vote.ForumPost.ForumThreadID + ", " + vote.ForumPostID);

            }
            System.Console.WriteLine(string.Format("+{0} / -{1}", up, down));
        }

        public static void GetForumVotesByUserVoterAgnostic(int voteeID)
        {
            int up = 0, down = 0;
            ZkDataContext db = new ZkDataContext();
            Account votee = db.Accounts.FirstOrDefault(x => x.AccountID == voteeID);
            var votes = db.AccountForumVotes.Where(x => x.ForumPost.AuthorAccountID == voteeID && x.Vote > 0).OrderBy(x => x.Account.Name).ToList();
            System.Console.WriteLine(votee.Name);
            foreach (var vote in votes)
            {
                int delta = vote.Vote;
                if (delta > 0) up++;
                else if (delta < 0) down++;
                System.Console.WriteLine(vote.ForumPost.ForumThreadID + ", " + vote.ForumPostID + ", " + vote.Account.Name);

            }
            System.Console.WriteLine(string.Format("+{0} / -{1}", up, down));
        }

        public static void GetForumVotesByPost(int voteeID)
        {
            int up = 0, down = 0;
            ZkDataContext db = new ZkDataContext();
            Account votee = db.Accounts.FirstOrDefault(x => x.AccountID == voteeID);
            var posts = db.ForumPosts.Where(x => x.AuthorAccountID == voteeID && (x.Upvotes > 0 || x.Downvotes > 0)).ToList();
            System.Console.WriteLine(votee.Name);
            foreach (var post in posts)
            {
                up = up + post.Upvotes;
                down = down + post.Downvotes;
                System.Console.WriteLine(post.ForumThreadID + ", " + post.ForumPostID + String.Format(" ({0}/{1})",post.Upvotes, post.Downvotes) );
                //System.Console.WriteLine(vote.ForumPost.Text);
            }
            System.Console.WriteLine(string.Format("+{0} / -{1}", up, down));
        }

        [STAThread]
        static void Main(string[] args) {
            //var bench = new Benchmarker.MainForm();
            //bench.ShowDialog();

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

            //PurgeGalaxy(9, false);
            //RandomizeMaps(9);
            //GenerateStructures(9);

            //AddWormholes();
            //TestPrediction();
            //FixMissionScripts();

            //AnalyzeModuleUsagePatterns();
            //AnalyzeCommUsagePatterns();

            //UpdateMissionProgression(9);
            //PrintNightwatchSubscriptions("zkadmin");
            //RecountForumVotes();
            //GetForumKarmaLadder();
            //GetForumKarmaVotes();
            //GetForumVotesByVoter(161294, 5340);
            //DeleteUserVotes(189201, null);  //neon    
            //GetClanStackWinRate(465, 2000); //Mean
            //GetForumVotesByUserVoterAgnostic(161294);
        }


      static void FixDemoEngineVersion()
      {
          var db = new ZkDataContext();
          foreach (var b in db.SpringBattles.Where(x=>x.Title.Contains("[engine"))) {
              var match = Regex.Match(b.Title, @"\[engine([^\]]+)\]");
              if (match.Success) {
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

        public static void Test1v1Elo() {
          var db = new ZkDataContext();
          Dictionary<Account, EloEntry> PlayerElo = new Dictionary<Account, EloEntry>();

          int cnt = 0;
          foreach (var sb in db.SpringBattles.Where(x => !x.IsMission && !x.HasBots && !x.IsFfa && x.PlayerCount == 2).OrderBy(x => x.SpringBattleID)) {
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
                  if (!PlayerElo.TryGetValue(r.Account, out elo)) {
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
          foreach (var entry in PlayerElo.Where(x=>x.Value.Cnt > 40).OrderByDescending(x=>x.Value.Elo)) {
              Console.WriteLine("{0};{1:f0};{2:f0};{3};{4}", entry.Key.Name,entry.Value.Elo, entry.Key.EffectiveElo, entry.Value.Cnt, entry.Key.SpringBattlePlayers.Count(x=>!x.IsSpectator && x.SpringBattle.PlayerCount > 2));
          }

      }


        public static void TestPrediction() {
        var db = new ZkDataContext();
        var cnt = 0;
            var winPro = 0;
            var winLessNub = 0;
            var winMoreVaried = 0;
            var winPredicted = 0;


        foreach (var sb in db.SpringBattles.Where(x=>!x.IsMission && !x.HasBots && !x.IsFfa && x.IsEloProcessed && x.PlayerCount >=8 && !x.EventSpringBattles.Any()).OrderByDescending(x => x.SpringBattleID)) {

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

            if (eWin > eLose) winPredicted ++;

                
            cnt++;
            if (cnt == 200) break;
        }

        Console.WriteLine("prwin: {0},  lessnubwin: {1},  morevaried: {2},  count: {3}, predicted:{4}",winPro, winLessNub, winMoreVaried, cnt, winPredicted );

    }

      public static void PurgeGalaxy(int galaxyID, bool resetclans = false) {
			using (var db = new ZkDataContext())
			{
				db.CommandTimeout = 300;

			    var gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);
                foreach (var p in gal.Planets)
                {
                    p.ForumThread = null;
                    p.OwnerAccountID = null;
                }
                db.SubmitChanges();

                db.ExecuteCommand("update account set pwbombersproduced=0, pwbombersused=0, pwdropshipsproduced=0, pwdropshipsused=0, pwmetalproduced=0, pwmetalused=0");
                if (resetclans) db.ExecuteCommand("update account set clanid=null");
				db.ExecuteCommand("delete from event");
				db.ExecuteCommand("delete from planetownerhistory");
                db.ExecuteCommand("delete from planetstructure");
                db.ExecuteCommand("delete from planetfaction");
				db.ExecuteCommand("delete from accountplanet");
				
				db.ExecuteCommand("delete from forumthread where forumcategoryid={0}", db.ForumCategories.Single(x => x.IsPlanets).ForumCategoryID);

                if (resetclans)
                {
                    db.ExecuteCommand("delete from clan");
                    db.ExecuteCommand("delete from forumthread where forumcategoryid={0}", db.ForumCategories.Single(x => x.IsClans).ForumCategoryID);
                }
			}
		}

  	static void RandomizeMaps(int galaxyID)
		{
			using (var db = new ZkDataContext())
			{
				var gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);

				var maps = db.Resources.Where(x => x.FeaturedOrder > 0 && x.MapPlanetWarsIcon!=null).ToList().Shuffle();
				int cnt = 0;
				foreach (var p in gal.Planets)
				{
					p.MapResourceID = maps[cnt++].ResourceID;
				}
				gal.Turn = 0;
				gal.Started = DateTime.UtcNow;
				gal.IsDirty = true;
				db.SubmitChanges();
			}
		}

		public static void AddStruct(this Planet p, int structID)
		{
			p.PlanetStructures.Add(new PlanetStructure() { StructureTypeID = structID });
		}

  	static void GenerateStructures(int galaxyID)
  	{
			var rand = new Random();
			var db = new ZkDataContext();
  		var gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);
			var names = Resources.names.Lines().ToList();

  		var wormhole = 16;
  		var wormhole2 = 19;


  		var mine = 1;
  		var mine2 = 3;
  		var mine3 = 4;
  		var warp = 10;
  		var chicken = 20;
  		var dfac = 6;
  		var ddepot = 7;
  		int artefact = 9;
			int militia = 594;
		
			/*
			567	Jumpjet/Specialist Plant
568	Screamer
569	Athena
570	Heavy Tank Factory
571	Airplane Plant
572	Krow
573	Bantha
574	Jugglenaut
575	Detriment
576	Singularity Reactor
577	Annihilator
578	Doomsday Machine
579	Behemoth
580	Starlight
581	Big Bertha
582	Goliath
583	Licho
584	Reef
585	Scythe
586	Panther
587	Black Dawn
588	Dominatrix
589	Newton
590	Shield Bot Factory
591	Silencer
592	Disco Rave Party*/

	
  		List<int> bannedStructures = new List<int>(){};// { 568, 577, 578, 584, 585, 586, 588, 589, 571, 590 };

  		var structs = db.StructureTypes.Where(x => x.Unlock != null && !bannedStructures.Contains(x.StructureTypeID));
  		List<Tuple<int, int>> costs = new List<Tuple<int, int>>();
			foreach (var s in structs)
			{
				costs.Add(Tuple.Create((int)(5000 / s.Cost), s.StructureTypeID)); // probabality is relative to 1200-cost
			}
  		var sumCosts = costs.Sum(x => x.Item1);

  		foreach (var p in gal.Planets) {
				p.PlanetStructures.Clear();
				p.Name = names[rand.Next(names.Count)];
				names.Remove(p.Name);
  			//if (rand.Next(50) == 0 ) p.AddStruct(wormhole2);
				//else 
				//if (rand.Next(10)<8) 
					p.AddStruct(wormhole);
					//p.AddStruct(militia);

				//if (rand.Next(30) ==0) p.AddStruct(mine3);
				//else if (rand.Next(20)==0) p.AddStruct(mine2);
				//else 
				//if (rand.Next(20) ==0) p.AddStruct(mine);

				//if (rand.Next(20) == 0) p.AddStruct(dfac);
				//if (rand.Next(20) == 0) p.AddStruct(ddepot);
				//if (rand.Next(20) == 0) p.AddStruct(warp);

				if (p.Resource.MapIsChickens == true) p.AddStruct(chicken);

				// tech structures
				/*if (rand.Next(8) ==0)
				{

					var probe = rand.Next(sumCosts);
					foreach (var s in costs)
					{
						probe -= s.Item1;
						if (probe <= 0)
						{
							p.AddStruct(s.Item2);
							break;
						}
					}
				}*/
			}
			
			// artefacts
			foreach (var p in gal.Planets.Where(x => x.Resource.MapIsChickens!=true && !x.Resource.MapIsFfa != true && x.Resource.MapIs1v1 != true).Shuffle().Take(5)) p.AddStruct(artefact);

			// jump gates
			//foreach (var p in gal.Planets.Shuffle().Take(6)) p.AddStruct(warp);

			db.SubmitChanges();
			db.SubmitChanges();
  	}

  	public static void AddWormholes()
		{
			var db = new ZkDataContext();
			var wormhole = db.StructureTypes.Where(x => x.EffectInfluenceSpread > 0).OrderBy(x => x.EffectInfluenceSpread).First();
			foreach (var p in db.Planets.Where(x => !x.PlanetStructures.Any(y => y.StructureType.EffectInfluenceSpread > 0)))
			{
				p.PlanetStructures.Add(new PlanetStructure() { StructureTypeID = wormhole.StructureTypeID});
			}
			db.SubmitChanges();
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
          double elo = double.Parse(m.Groups[2].Value,CultureInfo.InvariantCulture);
          double w = double.Parse(m.Groups[3].Value,CultureInfo.InvariantCulture);
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
        foreach (var r in db.Resources.Where(x=>x.LastChange == null)) r.LastChange = DateTime.UtcNow;
        db.SubmitChanges();
        return;

        foreach (var resource in db.Resources.Where(x => x.TypeID == ResourceType.Map ))//&&x.MapSizeSquared == null))
        {
          var file = String.Format("{0}/{1}.metadata.xml.gz", @"d:\zero-k.info\www\Resources", resource.InternalName.EscapePath());
          var map = (Map)new XmlSerializer(typeof(Map)).Deserialize(new MemoryStream(File.ReadAllBytes(file).Decompress()));

          resource.MapWidth = map.Size.Width/512;
          resource.MapHeight = map.Size.Height/512;

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
          resource.MapSizeSquared = (map.Size.Width/512)*(map.Size.Height/512);
          resource.MapSizeRatio = (float)map.Size.Width/map.Size.Height;

          var minimap = String.Format("{0}/{1}.minimap.jpg", @"d:\zero-k.info\www\Resources", resource.InternalName.EscapePath());

          using (var im = Image.FromFile(minimap))
          {

            int w, h;

            if (resource.MapSizeRatio > 1)
            {
              w = 96;
              h = (int)(w/resource.MapSizeRatio);
            }
            else
            {
              h = 96;
              w = (int)(h*resource.MapSizeRatio);
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
      } catch (Exception ex)
      {
        Console.WriteLine(ex);
      }
    }
  }
}
