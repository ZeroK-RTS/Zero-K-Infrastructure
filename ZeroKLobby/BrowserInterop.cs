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
        readonly TasClient tas;


        public BrowserInterop(TasClient tas) {
            this.tas = tas;
        }

        public string AddAuthToUrl(string url) {
            if (url.ToLower().Contains("zero-k") && !url.ToLower().Contains(string.Format("{0}=", GlobalConst.ASmallCakeCookieName)))
            {
                if (url.Contains("?")) url = url + "&";
                else url = url + "?";
                url = url +
                      string.Format("{0}={1}&{2}={3}&{4}=1&zkl=1",
                                    GlobalConst.ASmallCakeCookieName,
                                    AuthTools.GetSiteAuthToken(tas.UserName, PlasmaShared.Utils.HashLobbyPassword(tas.UserPassword)),
                                    GlobalConst.ASmallCakeLoginCookieName,
                                    tas.UserName,
                                    GlobalConst.LobbyAccessCookieName); 
            }
            return url;

        }

        public void OpenUrl(string url) {
            Process.Start(AddAuthToUrl(url));
        }
    }
}