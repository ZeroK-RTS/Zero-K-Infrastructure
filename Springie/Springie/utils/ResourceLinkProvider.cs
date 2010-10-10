#region using

using System.Collections.Generic;
using LobbyClient;
using Springie.autohost;

#endregion

namespace Springie
{
    /// <summary>
    /// Class responsible for providing map links
    /// </summary>
    public class ResourceLinkProvider
    {
        public enum FileType
        {
            Map,
            Mod
        }

        /*public void Downloader_LinksRecieved(object sender, object r);
		{
			lock (locker) {
				var todel = new List<LinkRequest>();
				foreach (var request in requests) {
					if (DateTime.Now.Subtract(request.Created).TotalSeconds > RequestTimeout) todel.Add(request);
					else if (request.Hash == e.Checksum) foreach (var s in e.Mirrors) Program.main.AutoHost.Respond(request.SayArgs, s);
				}
				requests.RemoveAll(todel.Contains);
			}
		}*/

        readonly PlasmaService plasmaService;

        AutoHost ah;
        public ResourceLinkProvider(AutoHost ah)
        {
            this.ah = ah;
            plasmaService = new PlasmaService();
            plasmaService.DownloadFileCompleted += ps_DownloadFileCompleted;
        }

        public void FindLinks(string[] words, FileType type, TasClient tas, TasSayEventArgs e)
        {
            if (words.Length == 0)
            {
                Battle b = tas.MyBattle;
                if (b == null) return;
                ah.Respond(e, string.Format("Getting Zero-K mirrors for currently hosted {0}", type));
                if (type == FileType.Map) plasmaService.DownloadFileAsync(b.MapName, e);
                else plasmaService.DownloadFileAsync(b.ModName, e);
            }
            else
            {
                List<string> items;
                if (type == FileType.Map) items = new List<string>(Program.main.UnitSyncWrapper.MapList.Keys);
                else items = new List<string>(Program.main.UnitSyncWrapper.ModList.Keys);

                int[] resultIndexes;
                string[] resultVals;
                int cnt = AutoHost.Filter(items.ToArray(), words, out resultVals, out resultIndexes);
                if (cnt == 0) ah.Respond(e, string.Format("No such {0} found", type));
                ah.Respond(e, string.Format("Getting Zero-K mirrors for {0}, please wait", resultVals[0]));

                plasmaService.DownloadFileAsync(resultVals[0], e);
            }
        }

        void ps_DownloadFileCompleted(object sender, DownloadFileCompletedEventArgs e)
        {
            var ev = (TasSayEventArgs)e.UserState;
            foreach (string s in e.links) ah.Respond(ev, s);
        }
    }
}