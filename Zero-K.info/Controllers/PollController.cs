﻿using System;
using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class PollController : Controller
    {
        //
        // GET: /Poll/

        /// <summary>
        /// View a specific poll
        /// </summary>
        public ActionResult Index(int pollID)
        {
            var db = new ZkDataContext();
            var poll = db.Polls.FirstOrDefault(x => x.PollID == pollID);
            if (poll != null) return PartialView("PollView", poll);
            return null;
        }

        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult NewPoll(string question, string answers, bool? isAnonymous)
        {
            var p = new Poll() { CreatedAccountID = Global.AccountID, IsHeadline = true, QuestionText = question, IsAnonymous = isAnonymous == true, };
            var db = new ZkDataContext();

            foreach (var a in answers.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) p.PollOptions.Add(new PollOption() { OptionText = a });

            db.Polls.InsertOnSubmit(p);
            db.SaveChanges();
            return RedirectToAction("UserVotes", new { id = Global.AccountID });
        }

        /// <summary>
        /// Automatically close old polls
        /// </summary>
        public static void AutoClosePolls()
        {
            var db = new ZkDataContext();
            foreach (var p in db.Polls.Where(x => x.IsHeadline && x.ExpireBy != null && x.ExpireBy < DateTime.UtcNow && x.RoleTypeID != null).ToList())
            {
                var yes = p.PollVotes.Count(x => x.PollOption.OptionText == "Yes");
                var no = p.PollVotes.Count(x => x.PollOption.OptionText == "No");
                var acc = p.AccountByRoleTargetAccountID;
                if (yes > no)
                {
                    if (p.RoleIsRemoval)
                    {
                        var toDelete = db.AccountRoles.Where(x => x.AccountID == acc.AccountID && x.RoleTypeID == p.RoleTypeID);
                        db.AccountRoles.DeleteAllOnSubmit(toDelete);
                        db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} was removed from the {1} role of {2} by a vote - {3} for, {4} against", acc, (object)p.Clan ?? p.Faction, p.RoleType, yes, no));

                        db.SaveChanges();

                        Global.Server.GhostPm(acc.Name, string.Format("You were recalled from the function of {0} by a vote", p.RoleType.Name));
                    }
                    else
                    {
                        if (!acc.AccountRolesByAccountID.Any(x => x.RoleTypeID == p.RoleTypeID))
                        {
                            Account previous = null;
                            if (p.RoleType.IsOnePersonOnly)
                            {
                                var entries = db.AccountRoles.Where(x => x.RoleTypeID == p.RoleTypeID && (p.RoleType.IsClanOnly ? x.ClanID == p.RestrictClanID : x.FactionID == p.RestrictFactionID)).ToList();

                                if (entries.Any())
                                {
                                    previous = entries.First().Account;
                                    db.AccountRoles.DeleteAllOnSubmit(entries);
                                    db.SaveChanges();
                                }
                            }

                            var entry = new AccountRole()
                                        {
                                            Account = acc,
                                            Inauguration = DateTime.UtcNow,
                                            Clan = p.Clan,
                                            Faction = p.Faction,
                                            RoleType = p.RoleType
                                        };
                            acc.AccountRolesByAccountID.Add(entry);
                            if (previous == null)
                                db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} was elected for the {1} role of {2} by a vote - {3} for, {4} against",
                                                   acc,
                                                   (object)p.Clan ?? p.Faction,
                                                   p.RoleType,
                                                   yes,
                                                   no));

                            else db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} was elected for the {1} role of {2} by a vote, replacing {3} - {4} for, {5} against",
                                                   acc,
                                                   (object)p.Clan ?? p.Faction,
                                                   p.RoleType,
                                                   previous,
                                                   yes,
                                                   no));

                            Global.Server.GhostPm(acc.Name, string.Format("Congratulations!! You were elected into a function of {0} by a vote", p.RoleType.Name));
                        }
                    }
                }

                p.IsHeadline = false;
                db.Polls.DeleteOnSubmit(p);
            }
            db.SaveChanges();
        }

        /// <summary>
        /// Starts a poll to elect someone to a <see cref="RoleType"/>, or remove an existing holder
        /// </summary>
        [Auth()]
        public ActionResult NominateRole(int roleTypeID, string text, bool isRemoval = false, int? removalAccountID = null)
        {
            var db = new ZkDataContext();
            var pollActive = Global.Account.PollsByRoleTargetAccountID.Any(x => x.ExpireBy > DateTime.UtcNow);
            if (pollActive) return Content("Poll already active, wait until it ends");

            var rt = db.RoleTypes.Single(x => x.RoleTypeID == roleTypeID);

            if (Global.Account.Level < GlobalConst.MinLevelForForumVote) return Content($"You need to be level {GlobalConst.MinLevelForForumVote} to vote");
            if (!rt.IsClanOnly && GlobalConst.PlanetWarsMode == PlanetWarsModes.AllOffline) return Content("Round over, no nominations can be made");
            if (rt.RestrictFactionID != null && rt.RestrictFactionID != Global.FactionID) throw new ApplicationException("Invalid faction");
            if (!rt.IsClanOnly && Global.FactionID == 0) throw new ApplicationException("No faction");
            if (!rt.IsClanOnly && rt.Faction.IsDeleted) throw new ApplicationException("Disabled faction");
            if (rt.IsClanOnly && Global.ClanID == 0) throw new ApplicationException("No clan");
            if (!rt.IsVoteable) throw new ApplicationException("Cannot be voted");

            int targetID = Global.AccountID;
            if (isRemoval)
            {
                var target = db.Accounts.Single(x => x.AccountID == removalAccountID);
                if (Global.Account.CanVoteRecall(target, rt))
                {
                    targetID = removalAccountID.Value;
                }
                else
                {
                    return Content("Cannot recall him/her");
                }
            }

            var p = new Poll()
                    {
                        CreatedAccountID = Global.AccountID,
                        RoleTargetAccountID = targetID,
                        ExpireBy = DateTime.UtcNow.AddDays(rt.PollDurationDays),
                        IsAnonymous = true,
                        IsHeadline = true,
                        RoleIsRemoval = isRemoval,
                        RoleType = rt,
                        RestrictClanID = rt.IsClanOnly ? Global.Account.ClanID : null,
                        RestrictFactionID = rt.IsClanOnly ? null : Global.Account.FactionID,
                        QuestionText = text
                    };
            p.PollOptions.Add(new PollOption() { OptionText = "Yes" });
            p.PollOptions.Add(new PollOption() { OptionText = "No" });
            db.Polls.InsertOnSubmit(p);
            db.SaveChanges();
            return RedirectToAction("Detail", "Users", new { id = Global.AccountID });
        }

        [Auth]
        public ActionResult PollVote(int pollID)
        {
            var db = new ZkDataContext();
            var poll = Account.ValidPolls(Global.Account, db).Single(x => x.PollID == pollID);

            if (Global.Account.Level >= GlobalConst.MinLevelForForumVote)
            {
                var key = Request.Form.AllKeys.Where(x => !string.IsNullOrEmpty(x)).First(x => x.StartsWith("option"));
                var optionID = Convert.ToInt32(key.Substring(6));

                if (!poll.PollOptions.Any(x => x.OptionID == optionID)) return Content("Invalid option");

                var entry = poll.PollVotes.SingleOrDefault(x => x.AccountID == Global.AccountID);
                if (entry == null)
                {
                    entry = new PollVote() { PollID = poll.PollID, AccountID = Global.AccountID };
                    poll.PollVotes.Add(entry);
                }
                entry.OptionID = optionID;

                db.SaveChanges();
                foreach (var opt in poll.PollOptions) opt.Votes = opt.PollVotes.Count(x => x.PollID == poll.PollID);
                db.SaveChanges();
            }

            return PartialView("PollView", poll);
        }

        //[ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult SwapHeadline(int pollid)
        {
            var db = new ZkDataContext();
            var p = db.Polls.Single(x => x.PollID == pollid);
            p.IsHeadline = !p.IsHeadline;
            db.SaveChanges();
            ;
            return RedirectToAction("UserVotes", new { id = Global.AccountID });
        }

        public ActionResult UserVotes(int id)
        {
            var db = new ZkDataContext();
            var acc = id > 0 ? db.Accounts.Find(id) : null;
            return View("PollUserVotes", acc);
        }

        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult SwapVisible(int pollid)
        {
            var db = new ZkDataContext();
            var p = db.Polls.Single(x => x.PollID == pollid);
            p.IsVisible = !p.IsVisible;
            db.SaveChanges();
            return RedirectToAction("UserVotes", new { id = Global.AccountID });
        }
    }
}
