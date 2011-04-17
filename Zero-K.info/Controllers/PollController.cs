using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class PollController : Controller
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
					int optionID = Convert.ToInt32(key.Substring(6));

					var db = new ZkDataContext();
					var poll = db.Polls.Single(x => x.PollID == pollID);

					if (!poll.PollOptions.Any(x => x.OptionID == optionID)) return Content("Invalid option");

					var entry = poll.PollVotes.SingleOrDefault(x => x.AccountID == Global.AccountID);
					if (entry == null)
					{
						entry = new PollVote() { PollID = poll.PollID, AccountID = Global.AccountID};
						poll.PollVotes.Add(entry);
					}
					entry.OptionID = optionID;
					
					db.SubmitChanges();
					foreach (var opt in poll.PollOptions)
					{
						opt.Votes = opt.PollVotes.Count(x=>x.PollID == poll.PollID);
					}
					db.SubmitChanges();

					return PartialView("PollView", poll);
				}

    }
}
