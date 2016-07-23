using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using SharpCompress.Archive;
using SharpCompress.Common;

namespace ZeroKWeb.Controllers
{
    [Auth(Role = AuthRole.ZkAdmin)]
    public class EnginesController : Controller
    {
        public static string[] EnginePlatforms = new[] { "win32", "linux64", "linux32" };


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

        public ActionResult Index(EnginesModel model)
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
                items.Add(item);
            }

            if (model.SearchName != null) items = items.Where(x => x.Name.Contains(model.SearchName)).ToList();

            model.Data = items.OrderByDescending(x => x.Name).AsQueryable();

            return View("EnginesIndex", model);
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
    }
}