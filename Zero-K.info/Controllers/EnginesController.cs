using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using SharpCompress.Archives;
using SharpCompress.Common;
using ZkData;

namespace ZeroKWeb.Controllers
{
    [Auth(Role = AdminLevel.Moderator)]
    public class EnginesController : Controller
    {
        public static string[] EnginePlatforms = new[] { "win32", "linux64", "linux32", "win64" };


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
            public bool IsDefault { get; set; }
        }

        public ActionResult Index(EnginesModel model)
        {
            model = model ?? new EnginesModel();

            var defaultPlatform = EnginePlatforms[0];

            var winBasePath = Path.Combine(this.MapPath("~"), "engine", defaultPlatform);
            if (!Directory.Exists(winBasePath)) Directory.CreateDirectory(winBasePath);

            var items = new List<EngineItem>();
            foreach (var name in new DirectoryInfo(winBasePath).GetFiles().Select(x => x.Name).Select(Path.GetFileNameWithoutExtension))
            {
                var item = new EngineItem() { Name = name, Platforms = new List<string>() { defaultPlatform }, IsDefault = name == MiscVar.DefaultEngine};

                foreach (var p in EnginePlatforms.Where(x => x != defaultPlatform))
                {
                    if (System.IO.File.Exists(Path.Combine(this.MapPath("~"), "engine", p, $"{name}.zip"))) item.Platforms.Add(p);
                }
                items.Add(item);
            }

            if (model.SearchName != null) items = items.Where(x => x.Name.Contains(model.SearchName)).ToList();

            model.Data = items.OrderByDescending(x => x.Name).AsQueryable();

            return View("EnginesIndex", model);
        }


        /// <summary>
        /// Uploading an engine, as its own action - which is what makes the attributes below
        /// apply at all.
        ///
        /// They used to sit on a PRIVATE method that Index called, and MVC only runs filters for
        /// actions it invokes: [Auth(SuperAdmin)], [HttpPost] and [ValidateAntiForgeryToken] were
        /// all inert. The effective policy was the class-level [Auth(Moderator)], any verb, no
        /// token - and the form had no method="post", so it submitted as GET with the antiforgery
        /// token sitting unread in the query string.
        ///
        /// The role is deliberately left at the class's Moderator rather than raised to the
        /// SuperAdmin the old attribute named: whoever uploads engines today keeps doing so. What
        /// changes is that it now takes a POST with a valid token, so a link cannot make a
        /// moderator's browser do it.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadEngine(EnginesModel model)
        {
            model = model ?? new EnginesModel();
            if (!string.IsNullOrEmpty(model.UploadName)) model.Message = PerformUpload(model.UploadName, model.UploadPlatforms);
            return Index(model);
        }

        private string PerformUpload(string uploadName, List<string> uploadPlatforms)
        {
            // uploadName becomes a directory and a file name below. Rejected rather than stripped:
            // an engine name is a version like 104.0.1-287-gf7b0fcc, so anything with a separator
            // or a parent reference in it is a mistake or an attempt, and silently rewriting it
            // would put the engine somewhere nobody asked for.
            if (uploadName.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 || uploadName.Contains(".."))
                return "Invalid engine name: " + uploadName;

            for (var i = 0; i < EnginePlatforms.Length; i++)
            {
                var platform = EnginePlatforms[i];
                var link = uploadPlatforms[i];
                if (string.IsNullOrEmpty(link)) continue;

                var dir = Path.Combine(this.MapPath("~"), "engine", platform);
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
                    pi.FileName = Path.Combine(this.MapPath("~"), "7za.exe");
                    pi.CreateNoWindow = true;
                    pi.UseShellExecute = false;
                    pi.Arguments = "x " + path7z;
                    var p = Process.Start(pi);
                    p.WaitForExit();
                    if (p.ExitCode == 0)
                    {
                        // success
                        System.IO.File.Delete(Path.Combine(temp, "temp.7z"));

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

        // Changes the default engine for everyone, starts a Steam depot rebuild and tells the
        // lobby server - from a link, with no token. The class is [Auth(Moderator)], so the
        // forgery needs a moderator to load a page somebody else wrote; that is the whole cost
        // of it, and the same cost ForceRatingsUpdate had.
        //
        // The database write is MiscVar.DefaultEngine, a property whose SETTER writes. That is
        // why tools/check-get-writes.py did not see this one: it matches calls, and an
        // assignment is not a call. It does now.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MakeDefault(string engine)
        {
            Trace.TraceInformation("Trying to change engine to {0}", engine);
            if (new ContentService().GetEngineList(null).Contains(engine))
            {
                MiscVar.DefaultEngine = engine;
                Global.SteamDepotGenerator.RunAll();
                Global.LobbyApi.SetEngine(engine);
            } else Trace.TraceWarning("Engine {0} not found in the list", engine);
            return RedirectToAction("Index");
        }
    }
}