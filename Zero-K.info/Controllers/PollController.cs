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

        public ActionResult UserVotes(int id) {
            var db = new ZkDataContext();
            var acc = db.Accounts.Single(x => x.AccountID == id);
            return View("PollUserVotes", acc);
        }

        [Auth(Role = AuthRole.ZkAdmin)]
        public ActionResult NewPoll(string question, string answers, bool? isAnonymous)
        {
            var p = new Poll()
            {
                CreatedAccountID = Global.AccountID,
                IsHeadline = true,
                QuestionText = question,
                IsAnonymous = isAnonymous == true,
            };
            var db = new ZkDataContext();

            foreach (var a in answers.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
                p.PollOptions.Add(new PollOption() { OptionText = a});
            }

            db.Polls.InsertOnSubmit(p);
            db.SubmitChanges();
            return RedirectToAction("UserVotes", new { id = Global.AccountID });

        }

        [Auth(Role = AuthRole.ZkAdmin)]
        public ActionResult SwapHeadline(int pollid)
        {
            
            var db = new ZkDataContext();
            var p = db.Polls.Single(x => x.PollID == pollid);
            p.IsHeadline = !p.IsHeadline;
            db.SubmitChanges(); ;
            return RedirectToAction("UserVotes", new { id = Global.AccountID });
        }
    }
}