﻿using System;
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

         public static ResourceData ToResourceData(Resource r)
         {
             var ret = new ResourceData() {
                 ResourceID = r.ResourceID,
                 InternalName = r.InternalName,
                 ResourceType = r.TypeID,
                 Dependencies = r.ResourceDependencies.Select(x => x.NeedsInternalName).ToList(),
                 MapIs1v1 = r.MapIs1v1,
                 MapIsTeams = r.MapIsTeams,
                 MapIsFfa = r.MapIsFfa,
                 MapIsSpecial = r.MapIsSpecial,
                 MapSupportLevel = r.MapSupportLevel
             };
             return ret;
         }

   
        public static ReturnValue DeleteResource(string internalName)
        {
            var db = new ZkDataContext();
            var todel = db.Resources.SingleOrDefault(x => x.InternalName == internalName);
            if (todel == null) return ReturnValue.ResourceNotFound;
            RemoveResourceFiles(todel);

            db.Resources.Remove(todel);
            db.SaveChanges();
            return ReturnValue.Ok;
        }


        public static DownloadFileResponse DownloadFile(string internalName)
        {
            List<string> links;
            byte[] torrent;
            List<string> dependencies;
            ResourceType resourceType;
            string torrentFileName;
            var ok =  ResourceLinkProvider.GetLinksAndTorrent(internalName,
                                                           out links,
                                                           out torrent,
                                                           out dependencies,
                                                           out resourceType,
                                                           out torrentFileName);
            if (ok) {
                return new DownloadFileResponse() {
                    links = links,
                    torrent = torrent,
                    dependencies = dependencies,
                    resourceType = resourceType,
                    torrentFileName = torrentFileName,
                };
            }
            return null;
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
            return ToResourceData(ret);
        }

        public static List<ResourceData> GetResourceList(DateTime? lastChange, out DateTime currentTime)
        {
            var ct = DateTime.UtcNow;
            currentTime = ct;
            var db = new ZkDataContext();
            return db.Resources.Where(x => lastChange == null || x.LastChange > lastChange).AsEnumerable().Select(ToResourceData).ToList();
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
            return Global.MapPath(String.Format("~/Resources/{0}", (object)GetTorrentFileName(name, md5)));
        }

        public static string GetTorrentPath(ResourceContentFile cf)
        {
            return GetTorrentPath(cf.Resource.InternalName, cf.Md5);
        }

        public static ReturnValue RegisterResource(RegisterResourceRequest req)
        {
            if (req.Md5 == null) throw new ArgumentNullException("md5");
            if (req.ArchiveName == null) throw new ArgumentNullException("archiveName");
            if (req.InternalName == null) throw new ArgumentNullException("internalName");
            if (req.SerializedData == null) throw new ArgumentNullException("serializedData");
            if (req.TorrentData == null) throw new ArgumentNullException("torrentData");
            if (PlasmaServerApiVersion > req.ApiVersion) throw new Exception("Obsolete PlasmaServer Client");
            if (req.Dependencies == null) req.Dependencies = new List<string>();

            var db = new ZkDataContext();


            var contentFile = db.ResourceContentFiles.FirstOrDefault(x => x.Md5 == req.Md5);
            if (contentFile != null)
            {
                // content file already stored
                if (contentFile.Resource.InternalName != req.InternalName) return ReturnValue.Md5AlreadyExistsWithDifferentName;

                // new spring version we add its hash
                StoreMetadata(req.Md5, contentFile.Resource, req.SerializedData, req.TorrentData, req.Minimap, req.MetalMap, req.HeightMap);
                db.SaveChanges();
                return ReturnValue.Ok;
            }

            var resource = db.Resources.Where(x => x.InternalName == req.InternalName).SingleOrDefault();

            if (resource == null)
            {
                resource = new Resource { InternalName = req.InternalName, TypeID = req.ResourceType };
                db.Resources.Add(resource);
                StoreMetadata(req.Md5, resource, req.SerializedData, req.TorrentData, req.Minimap, req.MetalMap, req.HeightMap);
            }

            if (!resource.ResourceDependencies.Select(x => x.NeedsInternalName).Except(req.Dependencies).Any())
            {
                // new dependencies are superset
                foreach (var depend in req.Dependencies)
                {
                    // add missing dependencies
                    var s = depend;
                    if (!resource.ResourceDependencies.Any(x => x.NeedsInternalName == s)) resource.ResourceDependencies.Add(new ResourceDependency { NeedsInternalName = depend });
                }
            }

            if (resource.ResourceContentFiles.Any(x => x.Length == req.Length && x.Md5 != req.Md5))
            {
                return ReturnValue.Md5AlreadyExistsWithDifferentName;
                // add proper message - file exists with different md5 and same size - cant register cant detect mirrors 
            }

            var newContentFile = new ResourceContentFile { FileName = req.ArchiveName, Length = req.Length, Md5 = req.Md5, Resource = resource};
            resource.ResourceContentFiles.Add(newContentFile);
            ResourceLinkProvider.UpdateLinks(newContentFile);
            File.WriteAllBytes(GetTorrentPath(req.InternalName, req.Md5), req.TorrentData); // add new torrent file

            db.SaveChanges();



            return ReturnValue.Ok;
        }

        public static void RemoveResourceFiles(Resource resource)
        {
            var file = String.Format("{0}/{1}", Global.MapPath("~/Resources"), resource.InternalName.EscapePath());
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
            var resPath = Global.MapPath("~/Resources");
            if (!Directory.Exists(resPath)) Directory.CreateDirectory(resPath);
            var file = String.Format("{0}/{1}", resPath, resource.InternalName.EscapePath());

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
                            if (!string.IsNullOrEmpty(map.Description))
                            {
                                var m = Regex.Match(map.Description, "by ([\\w]+)", RegexOptions.IgnoreCase);
                                if (m.Success) resource.AuthorName = m.Groups[1].Value;
                            }
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
                                                       Global.MapPath("~/Resources"),
                                                       resource.InternalName.EscapePath());
                            correctMinimap.Save(target, jgpEncoder, encoderParams);
                        }
                    }
                }
            }
        }

     
    }
}
