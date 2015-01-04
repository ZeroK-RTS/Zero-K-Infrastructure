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
        string login;
        string password;

        public BrowserInterop(TasClient tas, Config conf) {
            login = conf.LobbyPlayerName;
            password = conf.LobbyPlayerPassword;
            var baseUrl = GlobalConst.BaseSiteUrl;
            WindowsApi.InternetSetCookiePub(baseUrl, GlobalConst.LoginCookieName, login);
            WindowsApi.InternetSetCookiePub(baseUrl, GlobalConst.PasswordHashCookieName, ZkData.Utils.HashLobbyPassword(password));
            WindowsApi.InternetSetCookiePub(baseUrl, GlobalConst.LobbyAccessCookieName, "1");


            tas.LoginAccepted += delegate
                {
                    login = tas.UserName;
                    password = tas.UserPassword;
                    var wc = new WebClient();
                    var uri =
                        new Uri(string.Format("{2}Home/Logon?login={0}&password={1}",
                                              Uri.EscapeDataString(login),
                                              Uri.EscapeDataString(password), 
                                              GlobalConst.BaseSiteUrl));

                    WindowsApi.InternetSetCookiePub(baseUrl, GlobalConst.LoginCookieName, login);
                    WindowsApi.InternetSetCookiePub(baseUrl, GlobalConst.PasswordHashCookieName, ZkData.Utils.HashLobbyPassword(password));

                    if (conf.IsFirstRun) wc.DownloadString(uri);
                    else wc.DownloadStringAsync(uri);
                };
        }

        public string AddAuthToUrl(string url) {
            try {
                if (string.IsNullOrEmpty(url)) return "";
                if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password)) return url;
                if (url.ToLower().Contains("zero-k") &&  url.EndsWith("/") && !url.ToLower().Contains(string.Format("{0}=", GlobalConst.ASmallCakeCookieName))) {
                    if (url.Contains("?")) url = url + "&";
                    else url = url + "?";
                    url = url +
                          string.Format("{0}={1}&{2}={3}&{4}=1&zkl=1",
                                        GlobalConst.ASmallCakeCookieName,
                                        Uri.EscapeDataString(AuthTools.GetSiteAuthToken(login, ZkData.Utils.HashLobbyPassword(password))),
                                        GlobalConst.ASmallCakeLoginCookieName,
                                        Uri.EscapeDataString(login),
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