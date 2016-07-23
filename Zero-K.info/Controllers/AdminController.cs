
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using SharpCompress.Archive;
using SharpCompress.Common;
using ZkData;

namespace ZeroKWeb.Controllers
{
    [Auth(Role = AuthRole.ZkAdmin)]
    public class AdminController : Controller
    {
        public static string[] EnginePlatforms = new[] { "win32", "linux64", "linux32" };

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

        public ActionResult Engines(EnginesModel model)
        {
            model = model ?? new EnginesModel();

            if (!string.IsNullOrEmpty(model.UploadName) && model.upload != null)
            {
                model.Message = UploadEngine(model.UploadName, model.UploadPlatforms);
            }

            var defaultPlatform = EnginePlatforms[0];

            var winBasePath = Path.Combine(Server.MapPath("~"), "engine", defaultPlatform);
            if (!Directory.Exists(winBasePath)) Directory.CreateDirectory(winBasePath);

            var items = new List<EngineItem>();
            foreach (var name in new DirectoryInfo(winBasePath).GetFiles().Select(x => x.Name).Select(Path.GetFileNameWithoutExtension))
            {
                var item = new EngineItem() { Name = name, Platforms = new List<string>() { defaultPlatform } };

                foreach (var p in EnginePlatforms.Where(x => x != defaultPlatform))
                {
                    if (System.IO.File.Exists(Path.Combine(Server.MapPath("~"), "engine", p, $"{name}.zip"))) item.Platforms.Add(p);
                }
            }

            if (model.SearchName != null) items = items.Where(x => x.Name.Contains(model.SearchName)).ToList();

            model.Data = items.OrderByDescending(x => x.Name).AsQueryable();

            return View("Engines", model);
        }

        private string UploadEngine(string uploadName, List<string> uploadPlatforms)
        {
            for (var i = 0; i < EnginePlatforms.Length; i++)
            {
                var platform = EnginePlatforms[i];
                var link = uploadPlatforms[i];
                if (string.IsNullOrEmpty(link)) continue;

                var dir = Path.Combine(Server.MapPath("~"), "engine", platform);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var temp = Path.Combine(dir, uploadName);
                try
                {
                    Directory.CreateDirectory(temp);

                    var wc = new WebClient();
                    var path7z = Path.Combine(temp, "temp.7z");
                    var finalPath = Path.Combine(dir, $"{uploadName}.zip");
                    wc.DownloadFile(link, path7z);
                    var pi = new ProcessStartInfo();
                    pi.WorkingDirectory = temp;
                    pi.FileName = Path.Combine(Server.MapPath("~"), "7za.exe");
                    pi.CreateNoWindow = true;
                    pi.UseShellExecute = false;
                    pi.Arguments = "x " + path7z;
                    var p = Process.Start(pi);
                    p.WaitForExit();
                    if (p.ExitCode == 0)
                    {
                        // success
                        System.IO.File.Delete(Path.Combine(temp, "temp.7z"));

                        // todo compress and move step above
                        if (System.IO.File.Exists(finalPath)) System.IO.File.Delete(finalPath);
                        var archive = ArchiveFactory.Create(ArchiveType.Zip);
                        archive.AddAllFromDirectory(temp);
                        archive.SaveTo(finalPath, CompressionType.Deflate);
                        archive.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
                finally
                {
                    Directory.Delete(temp, true);
                }
            }
            return "succcess";
        }

        public class TraceLogIndex
        {
            public IQueryable<LogEntry> Data;
            public DateTime? TimeFrom { get; set; }
            public DateTime? TimeTo { get; set; }
            public string Text { get; set; }
            public List<TraceEventType> Types { get; set; } = new List<TraceEventType>();
        }


        public class EnginesModel
        {
            public IQueryable<EngineItem> Data;
            public string Message;
            public string SearchName { get; set; }
            public string UploadName { get; set; }
            public List<string> UploadPlatforms { get; set; } = new List<string>();
            public string upload { get; set; }
        }

        public class EngineItem
        {
            public List<string> Platforms { get; set; } = new List<string>();
            public string Name { get; set; }
        }
    }
}