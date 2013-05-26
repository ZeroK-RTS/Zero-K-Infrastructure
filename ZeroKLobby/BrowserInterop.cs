using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZeroKLobby
{
    public class BrowserInterop
    {
        string login;
        string password;

        public BrowserInterop(TasClient tas, Config conf) {
            login = conf.LobbyPlayerName;
            password = conf.LobbyPlayerPassword;
            WindowsApi.InternetSetCookiePub(Config.BaseUrl, GlobalConst.LoginCookieName, login);
            WindowsApi.InternetSetCookiePub(Config.BaseUrl, GlobalConst.PasswordHashCookieName, PlasmaShared.Utils.HashLobbyPassword(password));
            WindowsApi.InternetSetCookiePub(Config.BaseUrl, GlobalConst.LobbyAccessCookieName, "1");


            tas.LoginAccepted += delegate
                {
                    login = tas.UserName;
                    password = tas.UserPassword;
                    var wc = new WebClient();
                    var uri =
                        new Uri(string.Format("http://zero-k.info/Home/Logon?login={0}&password={1}",
                                              Uri.EscapeDataString(login),
                                              Uri.EscapeDataString(password)));

                    WindowsApi.InternetSetCookiePub(Config.BaseUrl, GlobalConst.LoginCookieName, login);
                    WindowsApi.InternetSetCookiePub(Config.BaseUrl, GlobalConst.PasswordHashCookieName, PlasmaShared.Utils.HashLobbyPassword(password));

                    if (conf.IsFirstRun) wc.DownloadString(uri);
                    else wc.DownloadStringAsync(uri);
                };
        }

        public string AddAuthToUrl(string url) {
            if (url.ToLower().Contains("zero-k") && !url.ToLower().Contains(string.Format("{0}=", GlobalConst.ASmallCakeCookieName))) {
                if (url.Contains("?")) url = url + "&";
                else url = url + "?";
                url = url +
                      string.Format("{0}={1}&{2}={3}&{4}=1&zkl=1",
                                    GlobalConst.ASmallCakeCookieName,
                                    Uri.EscapeDataString(AuthTools.GetSiteAuthToken(login,PlasmaShared.Utils.HashLobbyPassword(password))),
                                    GlobalConst.ASmallCakeLoginCookieName,
                                    Uri.EscapeDataString(login),
                                    GlobalConst.LobbyAccessCookieName);
            }
            return url;
        }

        public void OpenUrl(string url) {
            Process.Start(AddAuthToUrl(url));
        }
    }
}