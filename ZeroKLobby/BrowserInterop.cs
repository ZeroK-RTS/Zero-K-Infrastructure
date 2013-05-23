using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZeroKLobby
{
    public class BrowserInterop
    {

        public BrowserInterop() {
        }

        public string AddAuthToUrl(string url) {
            if (url.ToLower().Contains("zero-k") && !url.ToLower().Contains(string.Format("{0}=", GlobalConst.ASmallCakeCookieName)))
            {
                if (url.Contains("?")) url = url + "&";
                else url = url + "?";
                url = url +
                      string.Format("{0}={1}&{2}={3}&{4}=1&zkl=1",
                                    GlobalConst.ASmallCakeCookieName,
                                    AuthTools.GetSiteAuthToken(Program.Conf.LobbyPlayerName, PlasmaShared.Utils.HashLobbyPassword(Program.Conf.LobbyPlayerPassword)),
                                    GlobalConst.ASmallCakeLoginCookieName,
                                    Program.Conf.LobbyPlayerName,
                                    GlobalConst.LobbyAccessCookieName); 
            }
            return url;

        }

        public void OpenUrl(string url) {
            Process.Start(AddAuthToUrl(url));
        }
    }
}