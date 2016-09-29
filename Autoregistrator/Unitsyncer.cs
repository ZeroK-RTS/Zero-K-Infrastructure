using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MonoTorrent.Common;
using PlasmaShared;
using ZkData;
using ZkData.UnitSyncLib;

namespace AutoRegistrator
{
    /// <summary>
    ///     Checks data folders and unitsyncs/registers all new resources
    /// </summary>
    public class UnitSyncer
    {
        private const int ImageSize = 256; // max size of minimap to be sent to server
        private const int PlasmaServiceVersion = 3;
        public string Engine { get; private set; }
        public SpringPaths Paths { get; private set; }

        public UnitSyncer(SpringPaths paths, string engine)
        {
            if (string.IsNullOrEmpty(engine) || !paths.HasEngineVersion(engine))
            {
                Trace.TraceWarning("UnitSyncer: Engine {0} not found, trying backup", engine);
                engine = paths.GetEngineList().FirstOrDefault();
                if (engine == null) throw new Exception("UnitSyncer: No engine found for unitsync");
            }
        }


        public void Scan()
        {
            using (var unitsync = new UnitSync(Paths, Engine))
            {
                unitsync.ReInit();
                var archiveCache = unitsync.GetArchiveCache();
                using (var db = new ZkDataContext())
                {
                    var registered = db.Resources.Select(x => x.InternalName).ToDictionary(x => x, x => true);
                    foreach (var archive in archiveCache.Archives) if (!registered.ContainsKey(archive.Name)) Register(unitsync, archive);
                }
            }
        }


        private static void Register(UnitSync unitsync, ResourceInfo resource)
        {
            Trace.TraceInformation("UnitSyncer: registering {0}", resource.Name);
            try
            {
                var info = unitsync.GetResourceFromFileName(resource.ArchivePath);

                if (info != null)
                {
                    var serializedData = MetaDataCache.SerializeAndCompressMetaData(info);

                    var map = info as Map;
                    var creator = new TorrentCreator();
                    creator.Path = resource.ArchivePath;
                    var ms = new MemoryStream();
                    creator.Create(ms);

                    byte[] minimap = null;
                    byte[] metalMap = null;
                    byte[] heightMap = null;
                    if (map != null)
                    {
                        minimap = map.Minimap.ToBytes(ImageSize);
                        metalMap = map.Metalmap.ToBytes(ImageSize);
                        heightMap = map.Heightmap.ToBytes(ImageSize);
                    }

                    var hash = Hash.HashBytes(File.ReadAllBytes(resource.ArchivePath));
                    var length = new FileInfo(resource.ArchivePath).Length;

                    Trace.TraceInformation("UnitSyncer: uploading {0} to server", info.Name);

                    ReturnValue e;
                    try
                    {
                        var service = GlobalConst.GetContentService();
                        e = service.RegisterResource(PlasmaServiceVersion,
                            null,
                            hash.ToString(),
                            (int)length,
                            info.ResourceType,
                            resource.ArchivePath,
                            info.Name,
                            serializedData,
                            info.Dependencies,
                            minimap,
                            metalMap,
                            heightMap,
                            ms.ToArray());
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("UnitSyncer: Error uploading data to server: {0}", ex);
                        return;
                    }

                    if (e != ReturnValue.Ok) Trace.TraceWarning("UnitSyncer: Resource registering failed: {0}", e);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error registering resource {0} : {1}", resource.ArchivePath, ex);
            }
        }
    }
}