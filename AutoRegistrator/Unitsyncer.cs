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

        public class ScanResult
        {
            public ResourceInfo ResourceInfo { get; set; }
            public ResourceFileStatus Status { get; set; }
        }

        public enum ResourceFileStatus
        {
            AlreadyExists = 0,
            Registered = 1,
            RegistrationError= 2
        }
        
        
        public UnitSyncer(SpringPaths npaths, string nengine)
        {
            Paths = npaths;
            Engine = nengine;
            if (string.IsNullOrEmpty(Engine) || !Paths.HasEngineVersion(Engine))
            {
                Trace.TraceWarning("UnitSyncer: Engine {0} not found, trying backup", Engine);
                Engine = Paths.GetEngineList().FirstOrDefault();
                if (Engine == null) throw new Exception("UnitSyncer: No engine found for unitsync");
            }
        }


        
        
        public List<ScanResult> Scan()
        {
            var results = new List<ScanResult>();
            using (var unitsync = new UnitSync(Paths, Engine))
            {
                unitsync.ReInit();
                var archiveCache = unitsync.GetArchiveCache();
                using (var db = new ZkDataContext())
                {
                    var registered = db.Resources.Select(x => x.InternalName).ToDictionary(x => x, x => true);
                    foreach (var archive in archiveCache.Archives)
                    {
                        if (!UnitSync.DependencyExceptions.Contains(archive.Name))
                        {
                            if (registered.ContainsKey(archive.Name))
                            {
                                results.Add(new ScanResult() { ResourceInfo = archive, Status = ResourceFileStatus.AlreadyExists, });
                            }
                            else
                            {
                                var fullInfo = Register(unitsync, archive);
                                results.Add(new ScanResult()
                                {
                                    ResourceInfo = fullInfo ?? archive,
                                    Status = fullInfo != null ? ResourceFileStatus.Registered : ResourceFileStatus.RegistrationError
                                });
                            }
                        }

                    }
                    unitsync.Reset();
                }

                return results;
            }
        }


        private static ResourceInfo Register(UnitSync unitsync, ResourceInfo resource)
        {
            Trace.TraceInformation("UnitSyncer: registering {0}", resource.Name);
            ResourceInfo info = null;
            try
            {
                info = unitsync.GetResourceFromFileName(resource.ArchivePath);

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
                        e = service.Query(new RegisterResourceRequest()
                        {
                            ApiVersion = PlasmaServiceVersion,
                            Md5 = hash.ToString(),
                            Length = (int)length,
                            ResourceType = info.ResourceType,
                            ArchiveName = resource.ArchiveName,
                            InternalName = info.Name,
                            SerializedData = serializedData,
                            Dependencies = info.Dependencies,
                            Minimap = minimap,
                            MetalMap = metalMap,
                            HeightMap = heightMap,
                            TorrentData = ms.ToArray()
                        }).ReturnValue;
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("UnitSyncer: Error uploading data to server: {0}", ex);
                        return null;
                    }

                    if (e != ReturnValue.Ok) Trace.TraceWarning("UnitSyncer: Resource registering failed: {0}", e);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error registering resource {0} : {1}", resource.ArchivePath, ex);
                return null;
            }

            return info;
        }
    }
}