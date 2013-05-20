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
        readonly HttpListener listener;
        readonly NavigationControl nav;
        int port = 16501;
        readonly TasClient tas;
        Task task;


        public BrowserInterop(TasClient tas, NavigationControl nav) {
            this.nav = nav;
            this.tas = tas;
            listener = new HttpListener();
            listener.Prefixes.Add(string.Format("http://127.0.0.1:{0}/", port));
        }

        public void OpenUrl(string url) {
            if (url.ToLower().Contains("zero-k")) {
                if (url.Contains("?")) url = url + "&";
                else url = url + "?";
                url = url +
                      string.Format("{0}={1}&{2}={3}&zkl={4}",
                                    GlobalConst.ASmallCakeCookieName,
                                    AuthTools.GetSiteAuthToken(tas.UserName, PlasmaShared.Utils.HashLobbyPassword(tas.UserPassword)),
                                    GlobalConst.ASmallCakeLoginCookieName,
                                    tas.UserName,
                                    port);
                Process.Start(url);
            }
        }

        public void Start() {
            while (!VerifyTcpSocket(port)) port++;
            listener.Start();
            listener.BeginGetContext(HandleRequest, this);
        }

        public void Stop() {
            listener.Stop();
        }

        void HandleRequest(IAsyncResult ar) {
            var context = listener.EndGetContext(ar);
            if (context.Request.QueryString["link"] != null) {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(context.Request.QueryString["link"]));
                Program.MainWindow.InvokeFunc(() => { nav.Path = decoded; });
                
            }
            context.Response.OutputStream.Close();
            listener.BeginGetContext(HandleRequest, this);
        }

        static bool VerifyTcpSocket(int port) {
            try {
                using (var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)) {
                    var endpoint = new IPEndPoint(IPAddress.Loopback, port);
                    sock.ExclusiveAddressUse = true;
                    sock.Bind(endpoint);
                }
            } catch {
                return false;
            }
            return true;
        }
    }
}