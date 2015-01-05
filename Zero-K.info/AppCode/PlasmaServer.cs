using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Serialization;
using PlasmaShared;
using ZkData.UnitSyncLib;
using ZkData;

namespace ZeroKWeb
{
    public class PlasmaServer
    {
        public const int PlasmaServerApiVersion = 3;
        const int ThumbnailSize = 96;

   
        public static ReturnValue DeleteResource(string internalName)
        {
            var db = new ZkDataContext();
            var todel = db.Resources.SingleOrDefault(x => x.InternalName == internalName);
            if (todel == null) return ReturnValue.ResourceNotFound;
            RemoveResourceFiles(todel);

            db.Resources.Remove(todel);
            db.SubmitChanges();
            return ReturnValue.Ok;
        }


        public static bool DownloadFile(string internalName,
                                        out List<string> links,
                                        out byte[] torrent,
                                        out List<string> dependencies,
                                        out ResourceType resourceType,
                                        out string torrentFileName)
        {
            return ResourceLinkProvider.GetLinksAndTorrent(internalName,
                                                           out links,
                                                           out torrent,
                                                           out dependencies,
                                                           out resourceType,
                                                           out torrentFileName);
        }

        public static List<string> GetLinkArray(ResourceContentFile cf)
        {
            if (cf.LinkCount == 0 || cf.Links == null) return new List<string>();
            else return new List<string>(cf.Links.Split('\n'));
        }

        public static ResourceData GetResourceData(string md5, string internalName)
        {
            var ret = FindResource(md5, internalName);
            if (ret == null) return null;
            return new ResourceData(ret);
        }

        public static List<ResourceData> GetResourceList(DateTime? lastChange, out DateTime currentTime)
        {
            var ct = DateTime.UtcNow;
            currentTime = ct;
            var db = new ZkDataContext();
            return db.Resources.Where(x => lastChange == null || x.LastChange > lastChange).Select(r => new ResourceData(r)).ToList();
        }


        public static byte[] GetTorrentData(ResourceContentFile cf)
        {
            return File.ReadAllBytes(GetTorrentPath(cf));
        }

        public static string GetTorrentFileName(string name, string md5)
        {
            return String.Format("{0}_{1}.torrent", name.EscapePath(), md5);
        }

        public static string GetTorrentFileName(ResourceContentFile cf)
        {
            return GetTorrentFileName(cf.Resource.InternalName, cf.Md5);
        }

        public static string GetTorrentPath(string name, string md5)
        {
            return HttpContext.Current.Server.MapPath(String.Format("~/Resources/{0}", (object)GetTorrentFileName(name, md5)));
        }

        public static string GetTorrentPath(ResourceContentFile cf)
        {
            return GetTorrentPath(cf.Resource.InternalName, cf.Md5);
        }

        public static ReturnValue RegisterResource(int apiVersion,
                                                   string springVersion,
                                                   string md5,
                                                   int length,
                                                   ResourceType resourceType,
                                                   string archiveName,
                                                   string internalName,
                                                   int springHash,
                                                   byte[] serializedData,
                                                   List<string> dependencies,
                                                   byte[] minimap,
                                                   byte[] metalMap,
                                                   byte[] heightMap,
                                                   byte[] torrentData)
        {
            if (md5 == null) throw new ArgumentNullException("md5");
            if (archiveName == null) throw new ArgumentNullException("archiveName");
            if (internalName == null) throw new ArgumentNullException("internalName");
            if (serializedData == null) throw new ArgumentNullException("serializedData");
            if (torrentData == null) throw new ArgumentNullException("torrentData");
            if (PlasmaServerApiVersion > apiVersion) throw new Exception("Obsolete PlasmaServer Client");
            if (dependencies == null) dependencies = new List<string>();

            var db = new ZkDataContext();

            if (
                db.Resources.Any(
                    x =>
                    x.InternalName == internalName && x.ResourceSpringHashes.Any(y => y.SpringVersion == springVersion && y.SpringHash != springHash))) return ReturnValue.InternalNameAlreadyExistsWithDifferentSpringHash;

            var contentFile = db.ResourceContentFiles.FirstOrDefault(x => x.Md5 == md5);
            if (contentFile != null)
            {
                // content file already stored
                if (contentFile.Resource.InternalName != internalName) return ReturnValue.Md5AlreadyExistsWithDifferentName;
                if (contentFile.Resource.ResourceSpringHashes.Any(x => x.SpringVersion == springVersion)) return ReturnValue.Md5AlreadyExists;

                // new spring version we add its hash
                contentFile.Resource.ResourceSpringHashes.Add(new ResourceSpringHash { SpringVersion = springVersion, SpringHash = springHash });
                StoreMetadata(md5, contentFile.Resource, serializedData, torrentData, minimap, metalMap, heightMap);
                db.SubmitChanges();
                return ReturnValue.Ok;
            }

            var resource = db.Resources.Where(x => x.InternalName == internalName).SingleOrDefault();

            if (resource == null)
            {
                resource = new Resource { InternalName = internalName, TypeID = resourceType };
                db.Resources.Add(resource);
                StoreMetadata(md5, resource, serializedData, torrentData, minimap, metalMap, heightMap);
            }

            if (!resource.ResourceDependencies.Select(x => x.NeedsInternalName).Except(dependencies).Any())
            {
                // new dependencies are superset
                foreach (var depend in dependencies)
                {
                    // add missing dependencies
                    var s = depend;
                    if (!resource.ResourceDependencies.Any(x => x.NeedsInternalName == s)) resource.ResourceDependencies.Add(new ResourceDependency { NeedsInternalName = depend });
                }
            }

            if (resource.ResourceContentFiles.Any(x => x.Length == length && x.Md5 != md5))
            {
                return ReturnValue.Md5AlreadyExistsWithDifferentName;
                // add proper message - file exists with different md5 and same size - cant register cant detect mirrors 
            }

            resource.ResourceContentFiles.Add(new ResourceContentFile { FileName = archiveName, Length = length, Md5 = md5 });
            File.WriteAllBytes(GetTorrentPath(internalName, md5), torrentData); // add new torrent file
            if (!resource.ResourceSpringHashes.Any(x => x.SpringVersion == springVersion))
            {
                resource.ResourceSpringHashes.Add(new ResourceSpringHash { SpringVersion = springVersion, SpringHash = springHash });
                StoreMetadata(md5, resource, serializedData, torrentData, minimap, metalMap, heightMap);
            }

            db.SubmitChanges();

            return ReturnValue.Ok;
        }

        public static void RemoveResourceFiles(Resource resource)
        {
            var file = String.Format("{0}/{1}", HttpContext.Current.Server.MapPath("~/Resources"), resource.InternalName.EscapePath());
            Utils.SafeDelete(String.Format("{0}.minimap.jpg", file));
            Utils.SafeDelete(String.Format("{0}.thumbnail.jpg", file));
            Utils.SafeDelete(String.Format("{0}.heightmap.jpg", file));
            Utils.SafeDelete(String.Format("{0}.metalmap.jpg", file));
            Utils.SafeDelete(String.Format("{0}.metadata.xml.gz", file));
            foreach (var content in resource.ResourceContentFiles) Utils.SafeDelete(GetTorrentPath(content));
        }

        static Resource FindResource(string md5, string internalName)
        {
            var db = new ZkDataContext();
            Resource ret = null;
            if (!String.IsNullOrEmpty(md5))
            {
                var r = db.ResourceContentFiles.SingleOrDefault(x => x.Md5 == md5);
                if (r != null) ret = r.Resource;
            }
            else if (!String.IsNullOrEmpty(internalName)) ret = db.Resources.SingleOrDefault(x => x.InternalName == internalName);
            return ret;
        }

        static void StoreMetadata(string md5,
                                  Resource resource,
                                  byte[] serializedData,
                                  byte[] torrentData,
                                  byte[] minimap,
                                  byte[] metalMap,
                                  byte[] heightMap)
        {
            var file = String.Format("{0}/{1}", HttpContext.Current.Server.MapPath("~/Resources"), resource.InternalName.EscapePath());

            resource.LastChange = DateTime.UtcNow;

            if (minimap != null) File.WriteAllBytes(String.Format("{0}.minimap.jpg", file), minimap);
            if (metalMap != null) File.WriteAllBytes(String.Format("{0}.metalmap.jpg", file), metalMap);
            if (heightMap != null) File.WriteAllBytes(String.Format("{0}.heightmap.jpg", file), heightMap);
            if (torrentData != null) File.WriteAllBytes(GetTorrentPath(resource.InternalName, md5), torrentData);
            if (serializedData != null)
            {
                File.WriteAllBytes(String.Format("{0}.metadata.xml.gz", file), serializedData);
                if (minimap != null)
                {
                    var map = (Map)new XmlSerializer(typeof(Map)).Deserialize(new MemoryStream(serializedData.Decompress()));

                    if (string.IsNullOrEmpty(resource.AuthorName))
                    {
                        if (!string.IsNullOrEmpty(map.Author)) resource.AuthorName = map.Author;
                        else
                        {
                            var m = Regex.Match(map.Description, "by ([\\w]+)", RegexOptions.IgnoreCase);
                            if (m.Success) resource.AuthorName = m.Groups[1].Value;
                        }
                    }

                    if (resource.MapIsSpecial == null) resource.MapIsSpecial = map.ExtractorRadius > 120 || map.MaxWind > 40;
                    resource.MapSizeSquared = (map.Size.Width/512)*(map.Size.Height/512);
                    resource.MapSizeRatio = (float)map.Size.Width/map.Size.Height;
                    resource.MapWidth = map.Size.Width/512;
                    resource.MapHeight = map.Size.Height/512;

                    using (var im = Image.FromStream(new MemoryStream(minimap)))
                    {
                        int w, h;
                        if (resource.MapSizeRatio > 1)
                        {
                            w = ThumbnailSize;
                            h = (int)(w/resource.MapSizeRatio);
                        }
                        else
                        {
                            h = ThumbnailSize;
                            w = (int)(h*resource.MapSizeRatio);
                        }

                        using (var correctMinimap = new Bitmap(w, h, PixelFormat.Format24bppRgb))
                        {
                            using (var graphics = Graphics.FromImage(correctMinimap))
                            {
                                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                graphics.DrawImage(im, 0, 0, w, h);
                            }

                            var jgpEncoder = ImageCodecInfo.GetImageEncoders().First(x => x.FormatID == ImageFormat.Jpeg.Guid);
                            var encoderParams = new EncoderParameters(1);
                            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 100L);

                            var target = String.Format("{0}/{1}.thumbnail.jpg",
                                                       HttpContext.Current.Server.MapPath("~/Resources"),
                                                       resource.InternalName.EscapePath());
                            correctMinimap.Save(target, jgpEncoder, encoderParams);
                        }
                    }
                }
            }
        }

     
    }
}