using System;
using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class PollController: Controller
    {
        //
        // GET: /Poll/

        public ActionResult Index(int pollID)
        {
            var db = new ZkDataContext();
            var poll = db.Polls.Single(x => x.PollID == pollID);
            return PartialView("PollView", poll);
        }

        [Auth(Role = AuthRole.ZkAdmin)]
        public ActionResult NewPoll(string question, string answers, bool? isAnonymous)
        {
            var p = new Poll() { CreatedAccountID = Global.AccountID, IsHeadline = true, QuestionText = question, IsAnonymous = isAnonymous == true, };
            var db = new ZkDataContext();

            foreach (var a in answers.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) p.PollOptions.Add(new PollOption() { OptionText = a });

            db.Polls.InsertOnSubmit(p);
            db.SubmitChanges();
            return RedirectToAction("UserVotes", new { id = Global.AccountID });
        }

        [Auth()]
        public ActionResult NominateRole(int roleTypeID, bool isRemoval = false)
        {
            var db = new ZkDataContext();
            var rt = db.RoleTypes.Single(x => x.RoleTypeID == roleTypeID);
            if (rt.RestrictFactionID != null && rt.RestrictFactionID!= Global.FactionID) throw new ApplicationException("Invalid faction");
            if (Global.FactionID == 0) throw new ApplicationException("No faction");
            if (rt.IsClanOnly && Global.ClanID == 0) throw new ApplicationException("No clan");

            var p = new Poll()
                    {
                        CreatedAccountID = Global.AccountID,
                        RoleTargetAccountID = Global.AccountID,
                        ExpireBy = DateTime.UtcNow.AddDays(rt.PollDurationDays),
                        IsAnonymous = true,
                        IsHeadline = true,
                        RoleIsRemoval = isRemoval,
                        RoleType = rt,
                        RestrictClanID = rt.IsClanOnly ? Global.Account.ClanID : null,
                        RestrictFactionID = rt.IsClanOnly ? null : Global.Account.FactionID,
                        QuestionText = string.Format("Do you want {0} to become your {1}?", Global.Account.Name, rt.Name)
                    };
            p.PollOptions.Add(new PollOption() { OptionText = "Yes" });
            p.PollOptions.Add(new PollOption() { OptionText = "No" });
            db.Polls.InsertOnSubmit(p);
            db.SubmitChanges();
            return RedirectToAction("Detail", "Users", new { id = Global.AccountID });
        }

        [Auth]
        public ActionResult PollVote(int pollID)
        {
            var key = Request.Form.AllKeys.Where(x => !string.IsNullOrEmpty(x)).First(x => x.StartsWith("option"));
            var optionID = Convert.ToInt32(key.Substring(6));

            var db = new ZkDataContext();
            var poll = Global.Account.ValidPolls(db).Single(x => x.PollID == pollID);

            if (!poll.PollOptions.Any(x => x.OptionID == optionID)) return Content("Invalid option");

            var entry = poll.PollVotes.SingleOrDefault(x => x.AccountID == Global.AccountID);
            if (entry == null)
            {
                entry = new PollVote() { PollID = poll.PollID, AccountID = Global.AccountID };
                poll.PollVotes.Add(entry);
            }
            entry.OptionID = optionID;

            db.SubmitChanges();
            foreach (var opt in poll.PollOptions) opt.Votes = opt.PollVotes.Count(x => x.PollID == poll.PollID);
            db.SubmitChanges();

            return PartialView("PollView", poll);
        }

        [Auth(Role = AuthRole.ZkAdmin)]
        public ActionResult SwapHeadline(int pollid)
        {
            var db = new ZkDataContext();
            var p = db.Polls.Single(x => x.PollID == pollid);
            p.IsHeadline = !p.IsHeadline;
            db.SubmitChanges();
            ;
            return RedirectToAction("UserVotes", new { id = Global.AccountID });
        }

        public ActionResult UserVotes(int id)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Single(x => x.AccountID == id);
            return View("PollUserVotes", acc);
        }
    }
}