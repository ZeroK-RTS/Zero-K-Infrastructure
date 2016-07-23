
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    [Auth(Role = AuthRole.ZkAdmin)]
    public class AdminController : Controller
    {
        

        [Auth(Role = AuthRole.ZkAdmin)]
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

        [Auth(Role = AuthRole.ZkAdmin)]
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


    }
}