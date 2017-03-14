﻿using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class ClansController : Controller
    {
        public class ClansModel
        {
            public string Search { get; set; }
            public IQueryable<Clan> Data;
        }

        /// <summary>
        /// Clan list
        /// </summary>
        public ActionResult Index(ClansModel model) {
            model = model ?? new ClansModel();
            var db = new ZkDataContext();
            var ret = db.Clans.Where(x => !x.IsDeleted && (x.Faction == null || !x.Faction.IsDeleted));
            if (!string.IsNullOrEmpty(model.Search)) ret = ret.Where(x => x.ClanName.Contains(model.Search) || x.Shortcut.Contains(model.Search));
            model.Data = ret.OrderBy(x => x.ClanName);
            return View("ClansIndex", model);
        }


        [Auth]
        public ActionResult Create()
        {
            if (Global.Account.Clan == null || Global.Account.HasClanRight(x => x.RightEditTexts)) return View(Global.Clan ?? new Clan() { FactionID = Global.FactionID });
            else return Content("You already have clan and you dont have rights to it");
        }

        /// <summary>
        /// Shows clan page
        /// </summary>
        /// <returns></returns>
        public ActionResult Detail(int id)
        {
            var db = new ZkDataContext();
            var clan = db.Clans.First(x => x.ClanID == id);
            if (Global.ClanID == clan.ClanID)
            {
                if (clan.ForumThread != null)
                {
                    clan.ForumThread.UpdateLastRead(Global.AccountID, false);
                    db.SaveChanges();
                }
            }
            return View(clan);
        }

        public void AddClanLeader(int accountID, int clanID, ZkDataContext db = null)
        {
            if (db == null) db = new ZkDataContext();
            var leader = db.RoleTypes.FirstOrDefault(x => x.RightKickPeople && x.IsClanOnly);
            if (leader != null)
                db.AccountRoles.InsertOnSubmit(new AccountRole()
                {
                    AccountID = accountID,
                    ClanID = clanID,
                    RoleType = leader,
                    Inauguration = DateTime.UtcNow
                });
        }

        /// <summary>
        /// Clan leaving logic (including after kick)
        /// </summary>
        public static Clan PerformLeaveClan(int accountID, ZkDataContext db = null)
        {
            if (db == null) db = new ZkDataContext();
            var acc = db.Accounts.Single(x => x.AccountID == accountID);
            var clan = acc.Clan;
            if (clan.Accounts.Count() > GlobalConst.ClanLeaveLimit) return null; // "This clan is too big to leave";

            RoleType leader = db.RoleTypes.FirstOrDefault(x => x.RightKickPeople && x.IsClanOnly);
            bool isLeader = acc.AccountRolesByAccountID.Any(x => x.RoleType == leader);
            // remove role
            db.AccountRoles.DeleteAllOnSubmit(acc.AccountRolesByAccountID.Where(x => x.RoleType.IsClanOnly).ToList());

            // delete active polls
            db.Polls.DeleteAllOnSubmit(acc.PollsByRoleTargetAccountID);

            // remove planets
            acc.Planets.Clear();

            acc.ResetQuotas();

            // delete channel subscription
            //if (!acc.IsZeroKAdmin || acc.IsZeroKAdmin)
            //{
            //    var channelSub = db.LobbyChannelSubscriptions.FirstOrDefault(x => x.Account == acc && x.Channel == acc.Clan.GetClanChannel());
            //    db.LobbyChannelSubscriptions.DeleteOnSubmit(channelSub);
            //}

            acc.Clan = null;
            db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} leaves clan {1}", acc, clan));
            db.SaveChanges();
            if (!clan.Accounts.Any())
            {
                clan.IsDeleted = true;
                db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} is disbanded", clan));
            }
            else if (isLeader)
            {
                var otherClanMember = clan.Accounts.FirstOrDefault(x => x.AccountID != accountID);
                if (otherClanMember != null)
                {
                    db.AccountRoles.InsertOnSubmit(new AccountRole() { AccountID = otherClanMember.AccountID, Clan = clan, RoleType = leader, Inauguration = DateTime.UtcNow });
                }
            }

            db.SaveChanges();
            Global.Server.PublishAccountUpdate(acc);
            return clan;
        }

        /// <summary>
        /// Clan leaving (<see cref="PerformLeaveClan"/>) + redirect
        /// </summary>
        [Auth]
        public ActionResult LeaveClan()
        {
            var clan = PerformLeaveClan(Global.AccountID);
            if (clan == null) return Content("This clan is too big to leave");
            PlanetWarsTurnHandler.SetPlanetOwners(new PlanetwarsEventCreator());

            return RedirectToAction("Index", new { id = clan.ClanID });
        }

        [Auth]
        public ActionResult JoinClan(int id, string password)
        {
            var db = new ZkDataContext();
            var clan = db.Clans.Single(x => x.ClanID == id);
            

            if (clan.CanJoin(Global.Account))
            {
                if (!string.IsNullOrEmpty(clan.Password) && clan.Password != password) return View(clan.ClanID);
                else
                {
                    var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
                    acc.ClanID = clan.ClanID;
                    acc.FactionID = clan.FactionID;
                    db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} joins clan {1}", acc, clan));

                    if (clan.IsDeleted) // recreate clan
                    {
                        AddClanLeader(acc.AccountID, clan.ClanID, db);
                        clan.IsDeleted = false;
                        db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("Clan {0} reformed by {1}", clan, acc));
                    }

                    db.SaveChanges();
                    Global.Server.PublishAccountUpdate(acc);
                    return RedirectToAction("Detail", new { id = clan.ClanID });
                }
            }
            else return Content("You cannot join this clan - its full, or has password, or is different faction");
        }

        [Auth]
        public ActionResult KickPlayerFromClan(int accountID)
        {
            var db = new ZkDataContext();

            var kickee_acc = db.Accounts.SingleOrDefault(x => x.AccountID == accountID);
            if (kickee_acc == null) return Content("No such person");

            if (kickee_acc.ClanID != Global.Account.ClanID) return Content("Target not in your clan");
            if (!Global.Account.HasClanRight(x => x.RightKickPeople)) return Content("You have no kicking rights"); // unclanned people get handled here

            PerformLeaveClan(accountID);
            db.SaveChanges();
            PlanetWarsTurnHandler.SetPlanetOwners(new PlanetwarsEventCreator());
            Global.Server.PublishAccountUpdate(kickee_acc);
            return RedirectToAction("Detail", new { id = Global.Account.ClanID });
        }

        /// <summary>
        /// Creates a clan and redirects to the new clan page
        /// </summary>
        [Auth]
        public ActionResult SubmitCreate(Clan clan, HttpPostedFileBase image, HttpPostedFileBase bgimage, bool noFaction = false)
        {
            //using (var scope = new TransactionScope())
            //{
            ZkDataContext db = new ZkDataContext();
            bool created = clan.ClanID == 0; // existing clan vs creation
            var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            //return Content(noFaction ? "true":"false");
            if (noFaction) clan.FactionID = null;

            Faction new_faction = db.Factions.SingleOrDefault(x => x.FactionID == clan.FactionID);
            if ((new_faction != null) && new_faction.IsDeleted) return Content("Cannot create clans in deleted factions");

            if (string.IsNullOrEmpty(clan.ClanName) || string.IsNullOrEmpty(clan.Shortcut)) return Content("Name and shortcut cannot be empty!");
            if (!ZkData.Clan.IsShortcutValid(clan.Shortcut)) return Content("Shortcut must have at least 1 characters and contain only numbers and letters");

            if (created && (image == null || image.ContentLength == 0)) return Content("A clan image is required");

            Clan orgClan = null;

            if (!created)
            {
                if (!Global.Account.HasClanRight(x => x.RightEditTexts) || clan.ClanID != Global.Account.ClanID) return Content("Unauthorized");

                // check if our name or shortcut conflicts with existing clans
                var existingClans = db.Clans.Where(x => ((SqlFunctions.PatIndex(clan.Shortcut, x.Shortcut) > 0 || SqlFunctions.PatIndex(clan.ClanName, x.ClanName) > 0) && x.ClanID != clan.ClanID));
                if (existingClans.Count() > 0)
                {
                    if (existingClans.Any(x => !x.IsDeleted)) 
                        return Content("Clan with this shortcut or name already exists");
                }

                orgClan = db.Clans.Single(x => x.ClanID == clan.ClanID);
                string orgImageUrl = Server.MapPath(orgClan.GetImageUrl());
                string orgBGImageUrl = Server.MapPath(orgClan.GetBGImageUrl());
                string orgShortcut = orgClan.Shortcut;
                string newImageUrl = Server.MapPath(clan.GetImageUrl());
                string newBGImageUrl = Server.MapPath(clan.GetBGImageUrl());
                orgClan.ClanName = clan.ClanName;
                orgClan.Shortcut = clan.Shortcut;
                orgClan.Description = clan.Description;
                orgClan.SecretTopic = clan.SecretTopic;
                orgClan.Password = clan.Password;
                bool shortcutChanged = orgShortcut != clan.Shortcut;

                if (image != null && image.ContentLength > 0)
                {
                    var im = Image.FromStream(image.InputStream);
                    if (im.Width != 64 || im.Height != 64) im = im.GetResized(64, 64, InterpolationMode.HighQualityBicubic);
                    im.Save(newImageUrl);
                }
                else if (shortcutChanged)
                {
                    //if (System.IO.File.Exists(newImageUrl)) System.IO.File.Delete(newImageUrl);
                    //System.IO.File.Move(orgImageUrl, newImageUrl);
                    try {
                        //var im = Image.FromFile(orgImageUrl);
                        //im.Save(newImageUrl);
                        System.IO.File.Copy(orgImageUrl, newImageUrl, true);
                    } catch (System.IO.FileNotFoundException fnfex) // shouldn't happen but hey
                    {
                        return Content("A clan image is required");
                    }
                }

                if (bgimage != null && bgimage.ContentLength > 0)
                {
                    var im = Image.FromStream(bgimage.InputStream);
                    im.Save(newBGImageUrl);
                }
                else if (shortcutChanged)
                {
                    //if (System.IO.File.Exists(newBGImageUrl)) System.IO.File.Delete(newBGImageUrl);
                    //System.IO.File.Move(orgBGImageUrl, newBGImageUrl);
                    try
                    {
                        //var im = Image.FromFile(orgBGImageUrl);
                        //im.Save(newBGImageUrl);
                        System.IO.File.Copy(orgBGImageUrl, newBGImageUrl, true);
                    }
                    catch (System.IO.FileNotFoundException fnfex)
                    {
                        // there wasn't an original background image, do nothing
                    }
                }

                if (clan.FactionID != orgClan.FactionID)   
                {
                    // set factions of members
                    Faction oldFaction = orgClan.Faction;
                    orgClan.FactionID = clan.FactionID; 
                    foreach (Account member in orgClan.Accounts)
                    {
                        if (member.FactionID != clan.FactionID && member.FactionID != null)
                        {
                            FactionsController.PerformLeaveFaction(member.AccountID, true, db);
                        }
                        member.FactionID = clan.FactionID;
                    }
                    db.SaveChanges();
                    if (clan.FactionID != null) 
                        db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("Clan {0} moved to faction {1}", orgClan, orgClan.Faction));
                    else
                        db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("Clan {0} left faction {1}", orgClan, oldFaction));
                }
                db.SaveChanges();
            }
            else
            {
                if (Global.Clan != null) return Content("You already have a clan");
                // should just change their faction for them?
                if (Global.FactionID != 0 && Global.FactionID != clan.FactionID) return Content("Clan must belong to same faction you are in");

                // check if our name or shortcut conflicts with existing clans
                // if so, allow us to create a new clan over it if it's a deleted clan, else block action
                var existingClans = db.Clans.Where(x => ((SqlFunctions.PatIndex(clan.Shortcut,x.Shortcut) > 0 || SqlFunctions.PatIndex(clan.ClanName, x.ClanName) > 0) && x.ClanID != clan.ClanID));
                if (existingClans.Count() > 0)
                {
                    if (existingClans.Any(x => !x.IsDeleted)) return Content("Clan with this shortcut or name already exists");
                    Clan deadClan = existingClans.First();
                    Clan inputClan = clan;
                    clan = deadClan;
                    if (noFaction) clan.FactionID = null;
                    clan.IsDeleted = false;
                    clan.ClanName = inputClan.ClanName;
                    clan.Password = inputClan.Password;
                    clan.Description = inputClan.Description;
                    clan.SecretTopic = inputClan.SecretTopic;
                }
                else 
                    db.Clans.InsertOnSubmit(clan);

                db.SaveChanges();

                acc.ClanID = clan.ClanID;

                // we created a new clan, set self as founder and rights
                AddClanLeader(acc.AccountID, clan.ClanID, db);

                db.SaveChanges();

                if (image != null && image.ContentLength > 0)
                {
                    var im = Image.FromStream(image.InputStream);
                    if (im.Width != 64 || im.Height != 64) im = im.GetResized(64, 64, InterpolationMode.HighQualityBicubic);
                    im.Save(Server.MapPath(clan.GetImageUrl()));
                }
                if (bgimage != null && bgimage.ContentLength > 0)
                {
                    var im = Image.FromStream(bgimage.InputStream);
                    im.Save(Server.MapPath(clan.GetBGImageUrl()));
                }

                db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("New clan {0} formed by {1}", clan, acc));
                db.SaveChanges();
            }

            Global.Server.PublishAccountUpdate(acc);
            //scope.Complete();
            Global.Server.ChannelManager.AddClanChannel(clan);;
            Global.Server.SetTopic(clan.GetClanChannel(), clan.SecretTopic, Global.Account.Name);
            //}
            return RedirectToAction("Detail", new { id = clan.ClanID });
        }

        /*
        public ActionResult JsonJoinClan(string login, string password, int clanID)
        {
            var db = new ZkDataContext();
            var clan = db.Clans.First(x => x.ClanID == clanID && x.Password == null);
            var acc = db.Accounts.First(x=>x.AccountID == AuthServiceClient.VerifyAccountPlain(login, password).AccountID);
            PerformLeaveClan(acc.AccountID, db);
            return Json(db.Clans.Where(x => !x.IsDeleted).ToList(), JsonRequestBehavior.AllowGet);
        }*/

    }
}
