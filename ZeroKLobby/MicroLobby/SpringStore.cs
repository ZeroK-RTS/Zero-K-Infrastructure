using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using PlasmaDownloader;
using PlasmaShared;
using PlasmaShared.UnitSyncLib;
using LobbyClient;

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
//using PlasmaShared.ContentService;

namespace ZeroKLobby.MicroLobby
{
	/// <summary>
	/// pre-computes Spring data that might be required in the gui
	/// </summary>
	class SpringStore
	{
		readonly object locker = new object();
		List<Ai> Ais = new List<Ai>(10);
        Dictionary<string,EngineConfigEntry> settingsOptions = null;
        CacheFile cache = new CacheFile();
        string cachePath = null;
        bool loadedFile = false;

		public string ChangedOptions { get; private set; }

		public SpringStore()
		{
            Program.SpringPaths.SpringVersionChanged += (s, e) => SpringVersionChanged(s, e);
		}

        private void LoadCache()
        {
            SpringPaths springPaths = Program.SpringPaths;
            cachePath = Utils.MakePath(springPaths.Cache, "SpringInfoCache.dat");
            CacheFile loadedCache = null;
            var serializer = new BinaryFormatter();
            if (File.Exists(cachePath))
            {
                try
                {
                    using (var fs = File.OpenRead(cachePath)) loadedCache = (CacheFile)serializer.Deserialize(fs);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Warning: problem reading SpringInfo cache: {0}", ex);
                    loadedCache = null;
                }
            }

            if (loadedCache != null)
            {
                cache = loadedCache;
                string springVersion = springPaths.SpringVersion;
                if (springVersion != null)
                {
                    if (cache.springAiInfo.ContainsKey(springVersion))
                        Ais = cache.springAiInfo[springVersion];
                    if (cache.springSettingList.ContainsKey(springVersion))
                        settingsOptions = cache.springSettingList[springVersion];
                }
            }
        }

        void SaveCache()
        {
            lock (cache)
            {
                try
                {
                    var saver = new BinaryFormatter();
                    Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                    using (var fs = File.OpenWrite(cachePath)) saver.Serialize(fs, cache);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error saving SpringInfo cache: {0}", ex);
                }
            }
        }

        public List<Ai> GetSpringAis()
        {
            if (!loadedFile) //only load cache when being used
            {
                LoadCache();
                loadedFile = true;
            }

            string springVersion = Program.SpringPaths.SpringVersion;
            if (springVersion == null)
                return Ais;

            if (Ais.Count == 0 && cache.springAiInfo.ContainsKey(springVersion))
                Ais = cache.springAiInfo[springVersion];

            if (Ais.Count == 0)
            {
                if (Program.SpringScanner.UseUnitSync)
                {
                    Program.SpringScanner.VerifyUnitSync();
                    if (Program.SpringScanner.unitSync != null)
                    {
                        Ais.Clear(); //reset count to 0
                        foreach (var bot in Program.SpringScanner.unitSync.GetAis()) //IEnumberable can't be serialized, so convert to List. Ref: http://stackoverflow.com/questions/9102234/xmlserializer-in-c-sharp-wont-serialize-ienumerable
                            Ais.Add(bot);
                        cache.springAiInfo.Add(springVersion,Ais);
                        Program.SpringScanner.UnInitUnitsync();
                        SaveCache();
                    }
                }
            }
            return Ais;
        }

        public Dictionary<string, EngineConfigEntry> GetSpringSettings()
        {
            if (!loadedFile) //only load cache when being used
            {
                LoadCache();
                loadedFile = true;
            }

            string springVersion = Program.SpringPaths.SpringVersion;
            if (springVersion == null)
                return settingsOptions;

            if (settingsOptions == null && cache.springSettingList.ContainsKey(springVersion))
                settingsOptions = cache.springSettingList[springVersion];

            if (settingsOptions == null)
            {
                settingsOptions = new Spring(Program.SpringPaths).GetEngineConfigOptions();
                cache.springSettingList.Add(springVersion, settingsOptions);
                SaveCache();
            }
            return settingsOptions;
        }

        public void SpringVersionChanged(object sender, EventArgs e)
        {
            Ais.Clear();//reset count to 0
            settingsOptions = null;
        }


        [Serializable]
        class CacheFile
        {
            public readonly Dictionary<string, List<Ai>> springAiInfo = new Dictionary<string, List<Ai>>();
            public readonly Dictionary<string, Dictionary<string, EngineConfigEntry>> springSettingList = new Dictionary<string, Dictionary<string, EngineConfigEntry>>();
        }
	}
}