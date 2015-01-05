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
using ZkData;
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

        readonly List<AutoHost> autoHosts = new List<AutoHost>();
        List<AutoHost> deletionCandidate = new List<AutoHost>();
        DateTime lastConfigUpdate = DateTime.Now;
        readonly Timer timer;

        public MainConfig Config;

        public PlasmaDownloader.PlasmaDownloader Downloader;
        public MetaDataCache MetaCache;
        public string RootWorkPath { get; private set; }
        public readonly SpringPaths paths;

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
        }

        public void SaveConfig()
        {
            var s = new XmlSerializer(Config.GetType());
            var f = File.OpenWrite(RootWorkPath + '/' + ConfigMain);
            f.SetLength(0);
            s.Serialize(f, Config);
            f.Close();
        }

        public AutoHost SpawnAutoHost(AhConfig config, SpawnConfig spawnData)
        {
            AutoHost ah;
            lock (autoHosts)
            {
                ah = new AutoHost(MetaCache, config, GetFreeHostingPort(), spawnData);
                autoHosts.Add(ah);
            }
            return ah;
        }



        public void StopAutohost(AutoHost ah)
        {
            lock (autoHosts)
            {
                autoHosts.Remove(ah);
            }
            ah.Dispose();
        }

        public void UpdateAll()
        {
            try
            {
                var serv = GlobalConst.GetSpringieService();
                var configs = serv.GetClusterConfigs(Config.ClusterNode);

                var copy = new List<AutoHost>();
                lock (autoHosts)
                {
                    copy = autoHosts.ToList();
                }
                foreach (var conf in configs)
                {
                    if (!copy.Any(x => x.config.Login == conf.Login)) SpawnAutoHost(conf, null).Start();
                    else foreach (var ah in copy.Where(x => x.config.Login == conf.Login && x.SpawnConfig == null)) ah.config = conf;
                }
                var todel = copy.Where(x => !configs.Any(y => y.Login == x.config.Login)).ToList();
                foreach (var ah in todel) StopAutohost(ah);


            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in periodic updates: {0}", ex);
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
                timer.Stop();
                lock (autoHosts)
                {
                    // spawned autohosts
                    var spawnedToDel =
                        autoHosts.Where(
                            x => x.SpawnConfig != null && !x.spring.IsRunning && (x.tas.MyBattle == null || x.tas.MyBattle.Users.Count <= 1)).ToList();
                    if (spawnedToDel.Count > 0)
                    {
                        foreach (var ah in spawnedToDel.Where(x => deletionCandidate.Contains(x))) StopAutohost(ah); // delete those who are empty during 2 checks
                        deletionCandidate = spawnedToDel;
                    }
                    else deletionCandidate = new List<AutoHost>();

                    // autohosts which have clones
                    var keys = autoHosts.Where(x => x.config.AutoSpawnClones).Select(x => x.config.Login).Distinct().ToList();
                    if (keys.Count > 0)
                    {
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
                                var existing = autoHosts.FirstOrDefault(x => x.config.Login == key);
                                if (existing != null) SpawnAutoHost(existing.config, null).Start();
                            }
                            else // more than 1 empty running, stop all but 1
                            {
                                var minNumber = empty.Min(y => y.CloneNumber);
                                foreach (var ah in empty.Where(x => x.CloneNumber != minNumber && x.SpawnConfig == null)) StopAutohost(ah);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while checking autohosts: {0}", ex);
            }
            finally
            {
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
        public string Engine;
        public string Handle;
        public string Map;
        public int MaxPlayers;

        public SpawnConfig(string owner, Dictionary<string, string> config = null)
        {
            Owner = owner;
            if (config != null)
            {
                config.TryGetValue("password", out Password);
                config.TryGetValue("mod", out Mod);
                config.TryGetValue("title", out Title);
                config.TryGetValue("engine", out Engine);
                config.TryGetValue("handle", out Handle);
                config.TryGetValue("map", out Map);
                string mp;
                if (config.TryGetValue("maxplayers", out mp))
                {
                    int.TryParse(mp, out MaxPlayers);
                }
            }
        }


    }
}