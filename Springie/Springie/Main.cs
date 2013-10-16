using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Xml.Serialization;
using LobbyClient;
using PlasmaShared;
using PlasmaShared.SpringieInterfaceReference;
using Springie.autohost;

namespace Springie
{
    /// <summary>
    /// Holds and handles autohost instances
    /// </summary>
    /// 
    public class Main
    {
        public const string ConfigMain = "main.xml";
        const int ConfigUpdatePeriod = 60;
        const int JugglePeriod = 61;
        const int MinJuggleDelay = 5;

        readonly List<AutoHost> autoHosts = new List<AutoHost>();
        List<AutoHost> deletionCandidate = new List<AutoHost>();
        DateTime lastConfigUpdate = DateTime.Now;
        DateTime lastJuggle = DateTime.Now;
        readonly Timer timer;

        public MainConfig Config;

        public PlasmaDownloader.PlasmaDownloader Downloader;
        public MetaDataCache MetaCache;
        public string RootWorkPath { get; private set; }
        public readonly SpringPaths paths;
        private bool forceJuggleNext;

        public Main(string path)
        {
            RootWorkPath = path;
            LoadConfig();
            Config.RestartCounter++;
            if (Config.RestartCounter > 3) Config.RestartCounter = 0;
            SaveConfig();
            paths = new SpringPaths(Path.GetDirectoryName(Config.ExecutableName), writableFolderOverride: Config.DataDir);
            if (!string.IsNullOrEmpty(Config.ExecutableName)) paths.OverrideDedicatedServer(Config.ExecutableName);
            paths.MakeFolders();

            MetaCache = new MetaDataCache(paths, null);

            timer = new Timer(30000);
            timer.Elapsed += timer_Elapsed;
            timer.AutoReset = true;
            timer.Start();

            Downloader = new PlasmaDownloader.PlasmaDownloader(Config, null, paths);
        }

        public int GetFreeHostingPort()
        {
            lock (autoHosts)
            {
                var usedPorts = autoHosts.ToDictionary(x => x.hostingPort);
                var freePort =
                    Enumerable.Range(Config.HostingPortStart + Config.RestartCounter * 100, Config.MaxInstances).FirstOrDefault(x => !usedPorts.ContainsKey(x) && VerifyUdpSocket(x));
                return freePort;
            }
        }


        public string JuggleNow()
        {
            try
            {
                lastJuggle = DateTime.Now;
                forceJuggleNext = false;
                using (var serv = new SpringieService())
                {
                    serv.Timeout = 8000;
                    JugglerAutohost[] data;
                    lock (autoHosts)
                    {
                        data =
                            autoHosts.Where(x => x.tas.MyBattle != null && x.SpawnConfig == null && x.config.Mode != AutohostMode.None).Select(
                                x =>
                                new JugglerAutohost()
                                {
                                    LobbyContext = x.tas.MyBattle.GetContext(),
                                    RunningGameStartContext = (x.spring.IsRunning && !x.spring.IsBattleOver) ? x.spring.StartContext : null
                                }).ToArray();
                    }
                    var ret = serv.JugglePlayers(data);
                    if (ret != null)
                    {
                        if (ret.PlayerMoves != null)
                        {
                            /*
                            foreach (var playermove in ret.PlayerMoves)
                            {
                                var ah =
                                    autoHosts.FirstOrDefault(x => x.tas.MyBattle != null && x.tas.MyBattle.Users.Any(y => y.Name == playermove.Name));
                                if (ah != null) ah.ComMove(TasSayEventArgs.Default, new[] { playermove.Name, playermove.TargetAutohost });
                            }
                             */
                        }
                        if (ret.AutohostsToClose != null)
                        {
                            foreach (var ahToKill in ret.AutohostsToClose)
                            {
                                var ah = autoHosts.FirstOrDefault(x => x.tas.UserName == ahToKill);
                                if (ah != null) StopAutohost(ah);
                            }
                        }
                        return ret.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error juggling: {0}", ex);
                return ex.ToString();
            }
            return null;
        }


        public void LoadConfig()
        {
            Config = new MainConfig();
            if (File.Exists(RootWorkPath + '/' + ConfigMain))
            {
                var s = new XmlSerializer(Config.GetType());
                var r = File.OpenText(RootWorkPath + '/' + ConfigMain);
                Config = (MainConfig)s.Deserialize(r);
                r.Close();
            }
        }

        public void PeriodicCheck()
        {
            if (DateTime.Now.Subtract(lastConfigUpdate).TotalSeconds > ConfigUpdatePeriod)
            {
                UpdateAll();
                lastConfigUpdate = DateTime.Now;
            }
            if (DateTime.Now.Subtract(lastJuggle).TotalSeconds > JugglePeriod || forceJuggleNext)
            {
                JuggleNow();
            }
        }

        public void RequestJuggle()
        {
            lastJuggle = DateTime.MinValue;
            forceJuggleNext = true;
        }


        public void SaveConfig()
        {
            var s = new XmlSerializer(Config.GetType());
            var f = File.OpenWrite(RootWorkPath + '/' + ConfigMain);
            f.SetLength(0);
            s.Serialize(f, Config);
            f.Close();
        }

        public void SpawnAutoHost(AhConfig config, SpawnConfig spawnData)
        {
            lock (autoHosts)
            {
                var ah = new AutoHost(MetaCache, config, GetFreeHostingPort(), spawnData);
                autoHosts.Add(ah);
                ah.ServerVerifyMap(true);
            }
        }


        public void StopAll()
        {
            lock (autoHosts)
            {
                foreach (var ah in autoHosts) ah.Dispose();
                autoHosts.Clear();
            }
        }

        public void StopAutohost(AutoHost ah)
        {
            ah.Dispose();
            lock (autoHosts)
            {
                autoHosts.Remove(ah);
            }
        }

        public void UpdateAll()
        {
            try
            {
                using (var serv = new SpringieService())
                {
                    serv.Timeout = 5000;
                    var configs = serv.GetClusterConfigs(Config.ClusterNode);

                    lock (autoHosts)
                    {
                        foreach (var conf in configs)
                        {
                            if (!autoHosts.Any(x => x.config.Login == conf.Login)) SpawnAutoHost(conf, null);
                            else foreach (var ah in autoHosts.Where(x => x.config.Login == conf.Login && x.SpawnConfig == null)) ah.config = conf;
                        }
                        var todel = autoHosts.Where(x => !configs.Any(y => y.Login == x.config.Login)).ToList();
                        foreach (var ah in todel) StopAutohost(ah);
                    }
                }
            }
            catch (Exception ex) {
                Trace.TraceError("Error in periodic updates: {0}",ex);
            }
        }

        public static bool VerifyUdpSocket(int port)
        {
            try
            {
                using (var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    var endpoint = new IPEndPoint(IPAddress.Loopback, port);
                    sock.ExclusiveAddressUse = true;
                    sock.Bind(endpoint);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                timer.Stop(); ;
                lock (autoHosts)
                {
                    // spawned autohosts
                    var spawnedToDel =
                        autoHosts.Where(
                            x => x.SpawnConfig != null && !x.spring.IsRunning && (x.tas.MyBattle == null || x.tas.MyBattle.Users.Count <= 1)).ToList();
                    foreach (var ah in spawnedToDel.Where(x => deletionCandidate.Contains(x))) StopAutohost(ah); // delete those who are empty during 2 checks
                    deletionCandidate = spawnedToDel;

                    // autohosts which have clones
                    var keys = autoHosts.Where(x => x.config.AutoSpawnClones).Select(x => x.config.Login).Distinct().ToList();
                    foreach (var key in keys)
                    {
                        // 0-1 players = empty
                        var empty =
                            autoHosts.Where(
                                x =>
                                x.SpawnConfig == null && x.config.Login == key && !x.spring.IsRunning &&
                                (x.tas.MyBattle == null || (x.tas.MyBattle.Users.Count <= 1 && !x.tas.MyUser.IsInGame))).ToList();

                        if (empty.Count == 1) continue;

                        else if (empty.Count == 0)
                        {
                            var existing = autoHosts.Where(x => x.config.Login == key).First();
                            SpawnAutoHost(existing.config, null);
                        }
                        else // more than 1 empty running, stop all but 1
                        {
                            var minNumber = empty.Min(y => y.CloneNumber);
                            foreach (var ah in empty.Where(x => x.CloneNumber != minNumber && x.SpawnConfig == null)) StopAutohost(ah);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleException(ex, "While checking autohosts");
            }
            finally {
                timer.Start(); 
            }
        }
    }

    public class SpawnConfig
    {
        public string Mod;
        public string Owner;
        public string Password;
        public string Title;

        public SpawnConfig(string owner, Dictionary<string, string> config)
        {
            Owner = owner;
            config.TryGetValue("password", out Password);
            config.TryGetValue("mod", out Mod);
            config.TryGetValue("title", out Title);
        }
    }
}