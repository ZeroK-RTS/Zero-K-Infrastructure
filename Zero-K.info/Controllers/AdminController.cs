
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.Controllers
{
    [Auth(Role = AdminLevel.Moderator)]
    public class AdminController : Controller
    {
        

        [Auth(Role = AdminLevel.SuperAdmin)]
        public ActionResult ResetDb()
        {
            if (GlobalConst.Mode == ModeType.Test)
            {
                var cloner = new DbCloner("zero-k", "zero-k_test", GlobalConst.ZkDataContextConnectionString);
                cloner.LogEvent += s =>
                {
                    Response.Write(s);
                    Response.Flush();
                };
                cloner.CloneAllTables();
                Response.Write("DONE! Database copied");
                Response.Flush();
                return Content("");
            }
            else return Content("Not allowed!");
        }

        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult TraceLogs(TraceLogIndex model)
        {
            model = model ?? new TraceLogIndex();
            var db = new ZkDataContext();
            var ret = db.LogEntries.AsQueryable();

            if (model.TimeFrom != null) ret = ret.Where(x => x.Time >= model.TimeFrom);
            if (model.TimeTo != null) ret = ret.Where(x => x.Time <= model.TimeTo);
            if (!string.IsNullOrEmpty(model.Text)) ret = ret.Where(x => x.Message.Contains(model.Text));
            if (model.Types?.Count > 0) ret = ret.Where(x => model.Types.Contains(x.TraceEventType));

            model.Data = ret.OrderByDescending(x => x.LogEntryID);
            return View("TraceLogs", model);
        }

   

        public class TraceLogIndex
        {
            public IQueryable<LogEntry> Data;
            public DateTime? TimeFrom { get; set; }
            public DateTime? TimeTo { get; set; }
            public string Text { get; set; }
            public List<TraceEventType> Types { get; set; } = new List<TraceEventType>();
        }


        [Auth(Role = AdminLevel.SuperAdmin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetZklsMaxPlayers(int maxPlayers)
        {
            MiscVar.ZklsMaxUsers = maxPlayers;
            return RedirectToAction("Index", "Home");
        }


        // Recomputes every rating in the database, on the lobby server. A link could fire it,
        // which makes it forgeable: a moderator need only load a page that someone else wrote.
        // Nothing is corrupted by it - that is why this is a token and not an incident - but it
        // is a state change that a GET should not be able to ask for, and the work it starts is
        // not small.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult ForceRatingsUpdate()
        {
            Global.LobbyApi?.ForceRatingsUpdate();
            return RedirectToAction("Index", "Home");
        }

        [Auth(Role = AdminLevel.SuperAdmin)]
        public ActionResult EditDynamicConfig()
        {
            return View("DynamicConfigDetail", DynamicConfig.Instance);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Auth(Role =  AdminLevel.SuperAdmin)]
        public ActionResult EditDynamicConfigSubmit(DynamicConfig config)
        {
            DynamicConfig.SaveConfig(config);
            return RedirectToAction("EditDynamicConfig");
        }
    
        /// <summary>
        /// Registers one map again from its archive, regenerating the images stored for it.
        ///
        /// **This is the lossless half of the ToBytes backfill**, for maps registered before that
        /// rule was fixed on 2026-09-25. `ZkData.Core -- minimaps` lists which maps are affected;
        /// `-- backfill-minimaps` corrects their geometry in place but cannot recover the detail
        /// the old rule resized away. This can, because unitsync renders the images again.
        ///
        /// **One map per call, and SuperAdmin.** It downloads an archive, runs unitsync on it and
        /// overwrites stored files - and none of that can be exercised by a test in this
        /// repository, so the unit of work is the one a person can look at the result of before
        /// doing the next. Doing a whole library in a loop is a decision for whoever watches the
        /// first few.
        /// </summary>
        [Auth(Role = AdminLevel.SuperAdmin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReregisterResource(string internalName)
        {
            Trace.TraceInformation("Admin: {0} asked to re-register {1}", Global.Account.Name, internalName);
            return Content(Global.AutoRegistrator.ReregisterResource(internalName));
        }
}
}