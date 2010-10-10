#region using

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using System.Xml.Serialization;
using LobbyClient;
using PlasmaShared;
using Timer=System.Timers.Timer;

#endregion

namespace CaTracker
{
    public class Main
    {
        public const string ConfigMain = "CaTrackerConfig.xml";
        DateTime lastStatsSave;

        Timer recon;
        readonly Timer statsTimer = new Timer(60000);
        TasClient tas;

        public static Config config;
        public Thread ListenThread;
        public Dictionary<string, int> MapPlayerMinutes = new Dictionary<string, int>();
        public Dictionary<string, int> ModPlayerMinutes = new Dictionary<string, int>();
        public bool StopListening;

        public TasClient Tas { get { return tas; } }

        public Main()
        {
            LoadConfig();
            SaveConfig();
            statsTimer.Elapsed += statsTimer_Elapsed;
            statsTimer.AutoReset = true;
            statsTimer.Start();
        }


        public void ListenClient()
        {
            while (!StopListening)
            {
                TcpListener listener = null;
                try
                {
                    listener = new TcpListener(IPAddress.Any, config.ClientProtocolPort);
                    listener.Start();
                    while (!StopListening)
                    {
                        var cli = listener.AcceptTcpClient();
                        try
                        {
                            var ca = new CaClient(cli, tas);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error while creating client: " + ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                finally
                {
                    if (listener != null)
                    {
                        try
                        {
                            listener.Stop();
                        }
                        catch {}
                        ;
                    }
                }
                Thread.Sleep(1000);
            }
        }

        public void LoadConfig()
        {
            config = new Config();
            if (File.Exists(ConfigMain))
            {
                var s = new XmlSerializer(config.GetType());
                using (var r = File.OpenText(ConfigMain))
                {
                    config = (Config)s.Deserialize(r);
                    r.Close();
                }
            }
        }


        public void SaveConfig()
        {
            var s = new XmlSerializer(config.GetType());
            var f = File.OpenWrite(ConfigMain);
            f.SetLength(0);
            s.Serialize(f, config);
            f.Close();
        }

        public bool Start()
        {
            if (config.AttemptToRecconnect)
            {
                recon = new Timer(config.AttemptReconnectInterval*1000);
                recon.AutoReset = true;
                recon.Elapsed += recon_Elapsed;
            }

            recon.Enabled = false;

            tas = new TasClient(null, "CaTracker");
            tas.ConnectionLost += tas_ConnectionLost;
            tas.Connected += tas_Connected;
            tas.LoginDenied += tas_LoginDenied;
            tas.LoginAccepted += tas_LoginAccepted;

            try
            {
                tas.Connect(config.ServerHost, config.ServerPort);
            }
            catch
            {
                recon.Start();
            }

            ListenThread = Utils.SafeThread(ListenClient);
            ListenThread.Start();

            return true;
        }


        void TrackPlayerMinutes()
        {
            try
            {
                var now = DateTime.Now;
                if (now.Minute != lastStatsSave.Minute)
                {
                    foreach (var pair in tas.ExistingBattles)
                    {
                        var mod = pair.Value.ModName;
                        var map = pair.Value.MapName;
                        var cnt = pair.Value.Users.Count;
                        if (ModPlayerMinutes.ContainsKey(mod)) ModPlayerMinutes[mod] = ModPlayerMinutes[mod] + cnt;
                        else ModPlayerMinutes.Add(mod, cnt);

                        if (MapPlayerMinutes.ContainsKey(map)) MapPlayerMinutes[map] = MapPlayerMinutes[map] + cnt;
                        else MapPlayerMinutes.Add(map, cnt);
                    }

                    if (!Directory.Exists("stats")) Directory.CreateDirectory("stats");

                    var fname = now.ToString("yyyy-MM-dd-HH") + ".mods";
                    var res = "";
                    foreach (var st in ModPlayerMinutes) res += st.Key + "|" + st.Value + "\n";
                    File.WriteAllText(Directory.GetCurrentDirectory() + "/stats/" + fname, res);

                    fname = now.ToString("yyyy-MM-dd-HH") + ".maps";
                    res = "";
                    foreach (var st in MapPlayerMinutes) res += st.Key + "|" + st.Value + "\n";
                    File.WriteAllText(Directory.GetCurrentDirectory() + "/stats/" + fname, res);

                    if (now.Hour != lastStatsSave.Hour)
                    {
                        ModPlayerMinutes.Clear();
                        MapPlayerMinutes.Clear();
                    }

                    lastStatsSave = now;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while tracking stats " + e.ToString());
            }
        }

        void recon_Elapsed(object sender, ElapsedEventArgs e)
        {
            recon.Stop();
            try
            {
                tas.Connect(config.ServerHost, config.ServerPort);
            }
            catch
            {
                recon.Start();
            }
        }

        void statsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            TrackPlayerMinutes();
        }

        void tas_Connected(object sender, TasEventArgs e)
        {
            tas.Login(config.AccountName, config.AccountPassword);
        }

        void tas_ConnectionLost(object sender, TasEventArgs e)
        {
            recon.Start();
        }

        void tas_LoginAccepted(object sender, TasEventArgs e)
        {
            for (var i = 0; i < config.JoinChannels.Length; ++i) tas.JoinChannel(config.JoinChannels[i]);
        }

        void tas_LoginDenied(object sender, TasEventArgs e)
        {
            recon.Start();
        }
    }
}