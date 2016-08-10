using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using Amazon;
using Amazon.Glacier;
using Amazon.Glacier.Model;
using Amazon.Runtime;
using ZkData;

namespace DemoCleaner
{
    public class DemoCleaner
    {
        private const string GlacierAccessKey = "AKIAI2OMUISCMSCIR7LQ";
        private const string GlacierOwnerAccountID = "847019226085";
        private const string GlacierVaultName = "zero-k_replays";
        private const int DemoKeepDays = 60;
        private static RegionEndpoint GlacierRegion = RegionEndpoint.USWest2;


        private void ArchiveBattle(SpringBattle battle, string path)
        {
            if (!string.IsNullOrEmpty(battle.GlacierArchiveID))
            {
                Trace.TraceWarning("Unable to archive battle {0} : already archived", battle.SpringBattleID);
                return;
            }

            if (string.IsNullOrEmpty(battle.ReplayFileName))
            {
                Trace.TraceWarning("Unable to archive battle {0} : no file name in DB", battle.SpringBattleID);
                return;
            }

            using (var fs = File.OpenRead(path))
            {
                var result = StoreArchive(fs, $"Spring battle {battle.SpringBattleID}");
                if (!string.IsNullOrEmpty(result.ArchiveId) && (result.HttpStatusCode == HttpStatusCode.OK || result.HttpStatusCode == HttpStatusCode.Accepted || result.HttpStatusCode == HttpStatusCode.Created))
                {
                    battle.GlacierArchiveID = result.ArchiveId;
                    Trace.TraceInformation("Spring battle {0} archived as {1}", battle.SpringBattleID, result.ArchiveId);
                }
                else
                {

                    throw new Exception("Invalid glacier upload: " + result.HttpStatusCode);
                }
            }
        }


        public void CleanAllFiles()
        {
            GlobalConst.Mode = ModeType.Live;
            if (GlobalConst.Mode != ModeType.Live)
            {
                Trace.TraceError("This must be run only against live DB");
                return;
            }

            foreach (var dir in GlobalConst.ReplaysPossiblePaths)
            {
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir);
                    files.AsParallel().ForAll(path =>
                    {
                        try
                        {
                            var name = Path.GetFileName(path);
                            using (var db = new ZkDataContext())
                            {
                                var sb = db.SpringBattles.FirstOrDefault(x => x.ReplayFileName == name);
                                if (sb == null)
                                {
                                    Trace.TraceWarning("deleting unknown: {0}", name);
                                    File.Delete(path);
                                }
                                else
                                {

                                    if (DateTime.Now.Subtract(sb.StartTime).TotalDays > DemoKeepDays && sb.GlacierArchiveID == null &&
                                        (sb.ForumThread == null || sb.ForumThread.PostCount == 0))
                                    {
                                        Trace.TraceInformation("archiving: {0}", name);
                                        ArchiveBattle(sb, path);
                                        db.SaveChanges();
                                        if (!string.IsNullOrEmpty(sb.GlacierArchiveID)) File.Delete(path);
                                    }
                                    else
                                    {
                                        Trace.TraceInformation("keeping valid: {0}", name);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Error processing demo file {0} : {1}", path, ex);
                        }
                    });
                }
            }
        }


        private UploadArchiveResponse StoreArchive(Stream uncompressedContent, string archiveDescription)
        {
            using (
                var glacierClient = new AmazonGlacierClient(new BasicAWSCredentials(GlacierAccessKey, new Secrets().GetGlacierSecretKey()),
                    GlacierRegion))
            {
                // zip to memory stream
                var ms = new MemoryStream();
                var zipper = new GZipStream(ms, CompressionLevel.Optimal);
                uncompressedContent.CopyTo(zipper);
                ms.Seek(0, SeekOrigin.Begin);


                //calculate sha256 hash
                var shaTree = TreeHashGenerator.CalculateTreeHash(ms);
                ms.Seek(0, SeekOrigin.Begin);

                var result =
                    glacierClient.UploadArchive(new UploadArchiveRequest()
                    {
                        AccountId = GlacierOwnerAccountID,
                        VaultName = GlacierVaultName,
                        ArchiveDescription = archiveDescription,
                        Body = ms,
                        Checksum = shaTree,
                    });

                return result;
            }
        }
    }
}