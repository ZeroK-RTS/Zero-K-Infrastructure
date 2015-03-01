using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using LobbyClient;
using ZkData;

namespace ZeroKLobby
{
    public class BrowserInterop
    {
        public BrowserInterop() {
        }

        public string AddAuthToUrl(string url) {
            try {
                if (string.IsNullOrEmpty(url)) return "";
                if (string.IsNullOrEmpty(Program.Conf.LobbyPlayerName) || string.IsNullOrEmpty(Program.Conf.LobbyPlayerPassword)) return url;
                if (url.ToLower().Contains(GlobalConst.BaseSiteUrl) && url.EndsWith("/") && !url.ToLower().Contains(string.Format("{0}=", GlobalConst.ASmallCakeCookieName))) {
                    if (url.Contains("?")) url = url + "&";
                    else url = url + "?";
                    url = url +
                          string.Format("{0}={1}&{2}={3}&{4}=1&zkl=1",
                                        GlobalConst.ASmallCakeCookieName,
                                        Uri.EscapeDataString(AuthTools.GetSiteAuthToken(ZkData.Utils.HashLobbyPassword(Program.Conf.LobbyPlayerPassword))),
                                        GlobalConst.ASmallCakeLoginCookieName,
                                        Uri.EscapeDataString(Program.Conf.LobbyPlayerName),
                                        GlobalConst.LobbyAccessCookieName);
                }
                return url;
            } catch (Exception ex) {
                Trace.TraceError("Error adding auth info to url: {0}", ex);
                return url;
            }
        }

        public void OpenUrl(string url) {
            try {
                Process.Start(AddAuthToUrl(url));
            } catch (Exception ex) {
                Trace.TraceError(ex.ToString());
            }
        }
    }
}