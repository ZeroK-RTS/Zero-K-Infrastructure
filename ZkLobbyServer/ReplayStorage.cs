using System;
using System.Diagnostics;
using System.IO;
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

        internal ReplayStorage()
        {
            azureContainer = new BlobContainerClient(MiscVar.ReplaysConnectionString, MiscVar.ReplaysContainerName);
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


                // upload to azure storage and set access tier to archive
                var options = new BlobUploadOptions() { AccessTier = AccessTier.Archive };
                await blobClient.UploadAsync(path, options);
                
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
        
    }
}