#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using Springie.autohost;
using ZkData;

#endregion

namespace Springie
{
    /// <summary>
    /// Class responsible for providing map links
    /// </summary>
    public class ResourceLinkSpringieClient
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

        readonly IContentService plasmaService;

        AutoHost ah;
        public ResourceLinkSpringieClient(AutoHost ah)
        {
            this.ah = ah;
            plasmaService = GlobalConst.GetContentService();
        }

        private void GetLinksAsync(string name, TasSayEventArgs e)
        {
            Task.Factory.StartNew(() => {
                DownloadFileResult ret;
                try {
                    ret = plasmaService.DownloadFile(name);
                } catch (Exception ex) {
                    Trace.TraceError(ex.ToString());
                    return;
                }
            
                if (ret.links != null && ret.links.Count > 0) {
                    foreach (string s in ret.links) ah.Respond(e, s.Replace(" ", "%20"));
                } else {
                    ah.Respond(e,"No links found");
                }
            });
        }


        public void FindLinks(string[] words, FileType type, TasClient tas, TasSayEventArgs e)
        {
            if (words.Length == 0)
            {
                Battle b = tas.MyBattle;
                if (b == null) return;
                ah.Respond(e, string.Format("Getting Zero-K mirrors for currently hosted {0}", type));
                if (type == FileType.Map) GetLinksAsync(b.MapName, e);
                else GetLinksAsync(b.ModName, e);
            }
            else
            {
                int[] resultIndexes;
                string[] resultVals;
                int cnt;
                if (type == FileType.Map) cnt = ah.FilterMaps(words, out resultVals, out resultIndexes);
                else cnt = ah.FilterMods(words, out resultVals, out resultIndexes);

                if (cnt == 0) ah.Respond(e, string.Format("No such {0} found", type));
                else
                {
                    ah.Respond(e, string.Format("Getting Zero-K mirrors for {0}, please wait", resultVals[0]));
                    GetLinksAsync(resultVals[0], e);
                }
            }
        }

    }
}