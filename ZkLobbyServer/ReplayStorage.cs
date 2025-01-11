using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using ZkData;

namespace ZkLobbyServer
{
    public class ReplayStorage
    {
        public static ReplayStorage Instance { get; } = new ReplayStorage();

        BlobContainerClient azureContainer;

        string basePath = GlobalConst.SpringieDataDir;
        
        internal ReplayStorage()
        {
            try
            {
                if (string.IsNullOrEmpty(MiscVar.ReplaysConnectionString) || string.IsNullOrEmpty(MiscVar.ReplaysContainerName))
                {
                    Trace.TraceWarning("Replay storage not configured, replays won't be stored in blobs");
                    return;
                }
                
                azureContainer = new BlobContainerClient(MiscVar.ReplaysConnectionString, MiscVar.ReplaysContainerName);
            } catch (Exception ex)
            {
                Trace.TraceError("Error initializing replay storage: {0}", ex.Message);
            } 
        }

        public async Task<bool> UploadAndDeleteFileAsync(string path)
        {
            try
            {
                // check whether file exists and is not empty
                var fi = new FileInfo(path);
                if (!fi.Exists || fi.Length == 0) return false;
                var blobName = fi.Name;

                var blobClient = azureContainer.GetBlobClient(blobName);

                // check whether the file already exists
                if (await blobClient.ExistsAsync()) return false;


                // upload to azure storage
                await blobClient.UploadAsync(path);
                
                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error uploading replay {1}: {0}", ex, path);
                return false;
            }
        }
        
        public string GetFileUrl(string replayName)
        {
            var blobClient = azureContainer.GetBlobClient(replayName);
            var sas = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTime.UtcNow.AddHours(24));
            return sas.ToString();
        }
        
        public async Task<byte[]> GetFileContent(string replayName)
        {
            var blobClient = azureContainer.GetBlobClient(replayName);
            var stream = new MemoryStream();
            await blobClient.DownloadToAsync(stream);
            return stream.ToArray();
        }
        
        
        public byte[] GetLocalFileContent(string replayName)
        {
            var path = Path.Combine(basePath, "demos-server", replayName);
            if (!File.Exists(path)) return null;
            return File.ReadAllBytes(path);
        }
        
        public async Task MigrateReplays()
        {
            // replays themselves
            var files = Directory.GetFiles(Path.Combine(basePath,"demos-server"));
            foreach (var fi in files)
            {
                await UploadAndDeleteFileAsync(fi);
            }
            
            // infologs
            files = Directory.GetFiles(basePath,"infolog_*.txt");
            foreach (var fi in files)
            {
                await UploadAndDeleteFileAsync(fi);
            }
        }

    }
}