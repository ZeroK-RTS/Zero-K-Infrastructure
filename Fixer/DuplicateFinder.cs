using FileHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZkData;

namespace Fixer
{
    public class DuplicateFinder
    {
        [DelimitedRecord(",")]
        class ZKAccount
        {
            public int AccountID;
            public string Name;
        }

        public static void PrintDuplicates(Dictionary<string, List<int>> duplicatesByName)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"duplicateaccounts.htm"))
            {
                file.WriteLine("DUPLICATES<br/>");
                foreach (var duplicate in duplicatesByName)
                {
                    string duplies = "";
                    foreach (int accountID in duplicate.Value)
                    {
                        duplies = duplies + String.Format("<a href=http://zero-k.info/Users/Detail/{0}>{0}</a>, ", accountID);
                    }
                    file.WriteLine("{0}: {1}<br/>", duplicate.Key, duplies);
                }
            }
        }

        public static void FixDuplicates(Dictionary<string, List<int>> duplicatesByName)
        {
            var db = new ZkDataContext();

            foreach (var duplicate in duplicatesByName)
            {
                Account primaryAccount = null;
                int highestXP = -1;
                foreach (int accountID in duplicate.Value)
                {
                    Account acc = db.Accounts.FirstOrDefault(x => x.AccountID == accountID);
                    Console.WriteLine("Account {0} has {1} XP", acc.Name, acc.Xp);
                    if (acc.Xp > 0 || primaryAccount == null)
                    {
                        if (acc.Xp > highestXP)
                        {
                            primaryAccount = acc;
                            highestXP = acc.Xp;
                            Console.WriteLine("Primary account is " + acc.Name);
                        }
                    }
                }

                foreach (int accountID in duplicate.Value)
                {
                    Account acc = db.Accounts.FirstOrDefault(x => x.AccountID == accountID);
                    if (acc == primaryAccount) continue;

                    Console.WriteLine("Terminatoring account: " + acc.Name);

                    if (acc.AccountCampaignProgress != null && acc.AccountCampaignProgress.Count > 0)
                    {
                        Console.WriteLine("\tAccount has campaign progress, aborting");
                        continue;
                        
                        foreach (AccountCampaignProgress progress in acc.AccountCampaignProgress)
                        {
                            // FIXME
                            //db.AccountCampaignProgress.DeleteOnSubmit(progress);
                        }
                    }
                    if (acc.AccountRolesByAccountID != null)
                        db.AccountRoles.DeleteAllOnSubmit(acc.AccountRolesByAccountID);
                    acc.FactionID = null;
                    acc.ClanID = null;
                    //if (acc.Commanders != null)
                        //db.Commanders.DeleteAllOnSubmit(acc.Commanders);
                    if (acc.ForumPosts != null && acc.ForumPosts.Count > 0)
                    {
                        foreach (ForumPost post in acc.ForumPosts)
                        {
                            post.AuthorAccountID = primaryAccount.AccountID;
                            Console.WriteLine("\tTransferring post " + post.ForumPostID);
                        }
                    }
                    Console.WriteLine("\tChecking votes");
                    if (acc.AccountForumVotes != null && acc.AccountForumVotes.Count > 0)
                    {
                        foreach (AccountForumVote vote in acc.AccountForumVotes)
                        {
                            if (vote.Vote > 0)
                            {
                                vote.ForumPost.Upvotes = vote.ForumPost.Upvotes - 1;
                                vote.ForumPost.Account.ForumTotalUpvotes = vote.ForumPost.Account.ForumTotalUpvotes - 1;
                            }
                            else if (vote.Vote < 0)
                            {
                                vote.ForumPost.Downvotes = vote.ForumPost.Downvotes - 1;
                                vote.ForumPost.Account.ForumTotalDownvotes = vote.ForumPost.Account.ForumTotalDownvotes - 1;
                            }
                            db.AccountForumVotes.DeleteOnSubmit(vote);
                            
                        }
                    }
                    Console.WriteLine("\tChecking battles");
                    if (acc.SpringBattlePlayers != null && acc.SpringBattlePlayers.Count > 0)
                    {
                        foreach (var players in acc.SpringBattlePlayers)
                        {
                            players.AccountID = primaryAccount.AccountID;
                            Console.WriteLine("\tTransferring battle B" + players.SpringBattleID);
                        }
                    }
                    if (acc.PunishmentsByAccountID != null && acc.PunishmentsByAccountID.Count > 0)
                    {
                        foreach (Punishment pun in acc.PunishmentsByAccountID)
                        {
                            db.Punishments.DeleteOnSubmit(pun);
                            Console.WriteLine("\tDeleting punishment");
                        }
                    }

                    // "AccountID is part of the object's key information and cannot be modified"
                    //db.AccountUserIDs.DeleteAllOnSubmit(acc.AccountUserIDs);
                    //db.AccountIPs.DeleteAllOnSubmit(acc.AccountIPs);
                    //db.ForumLastReads.DeleteAllOnSubmit(acc.ForumLastReads);
                    //db.ForumThreadLastReads.DeleteAllOnSubmit(acc.ForumThreadLastReads);  

                    //db.Accounts.DeleteOnSubmit(acc);
                    bool success = false;
                    for (int i = 0; i < 9; i++)
                    {
                        string newName = acc.Name + "_000" + i;
                        if (db.Accounts.FirstOrDefault(x => x.Name == newName) == null)
                        {
                            acc.Name = newName;
                            acc.IsDeleted = true;
                            Console.WriteLine("\tRenamed account to " + newName);
                            success = true;
                            break;
                        }
                    }
                    if (!success)
                    {
                        Console.WriteLine("ERROR changing name for user " + acc.Name);
                        return;
                    }
                }
            }
            //db.SubmitChanges();
        }

        public static void GetDuplicates()
        {
            FileHelperEngine engine = new FileHelperEngine(typeof(ZKAccount));
            ZKAccount[] accounts = engine.ReadFile(@"ZKAccounts.csv") as ZKAccount[];

            Dictionary<string, List<int>> accountsByName = new Dictionary<string, List<int>>();
            Dictionary<string, List<int>> duplicatesByName = new Dictionary<string, List<int>>();
            Console.WriteLine("Processing " + accounts.Length + " accounts");
            Console.WriteLine("---------------");
            foreach (ZKAccount account in accounts)
            {
                string lowerName = account.Name.ToLower();
                if (accountsByName.ContainsKey(lowerName))
                {
                    var entry = accountsByName[lowerName];
                    entry.Add(account.AccountID);
                    duplicatesByName[lowerName] = entry;
                    Console.WriteLine("Duplicate found: {0}, {1}", lowerName, account.AccountID);
                }
                else
                {
                    accountsByName.Add(lowerName, new List<int>( new int[] {account.AccountID} ));
                }
            }
            Console.WriteLine("---------------");
            Console.WriteLine("Number of duplicates: " + duplicatesByName.Count);

            //PrintDuplicates(duplicatesByName);
            FixDuplicates(duplicatesByName);
        }

        public static void PrintUserData(List<Account> users)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"ZKAccounts.csv"))
            {
                file.WriteLine("AccountID,Name");
                foreach (Account user in users)
                {
                    file.WriteLine(user.AccountID + "," + user.Name);
                }
            }
        }

        public static void GetUserCsv()
        {
            ZkDataContext db = new ZkDataContext();
            var users = db.Accounts.ToList();
            Console.WriteLine(users.Count);
            PrintUserData(users);
        }
    }
}
