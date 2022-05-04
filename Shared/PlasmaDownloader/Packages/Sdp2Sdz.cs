using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;
using Ionic.Zlib;
using PlasmaShared;
using ZkData;

namespace PlasmaDownloader.Packages
{
    public class Sdp2Sdz
    {
        private const int paralellZipForFilesAboveSize = 300000;

        public void ConvertSdp2Sdz(SpringPaths paths, Hash sdpHash, string targetSdz, Action<double> progressIndicator = null)
        {
            var sdpPath = Path.Combine(paths.WritableDirectory, "packages", sdpHash + ".sdp");
            var tempSdz = Path.Combine(paths.WritableDirectory, "temp", sdpHash + ".sdz");

            SdpArchive fileList;
            using (var fs = new FileStream(sdpPath, FileMode.Open)) fileList = new SdpArchive(new System.IO.Compression.GZipStream(fs, System.IO.Compression.CompressionMode.Decompress));

            GenerateSdz(paths, fileList, tempSdz, progressIndicator);

            tempSdz.RenameWithOverwrite(targetSdz);
            sdpPath.RenameWithOverwrite(Path.ChangeExtension(sdpPath, "sdpzk")); // rename sdp -> sdpzk
        }

        /// <summary>
        /// Generates sdz archive from pool and file list
        /// </summary>
        private void GenerateSdz(SpringPaths paths, SdpArchive fileList, string tempSdz, Action<double> progressIndicator)
        {
            var pool = new Pool(paths);

            var dir = Path.GetDirectoryName(tempSdz);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(tempSdz)) File.Delete(tempSdz);

            long uncompressedTotalSize = fileList.Files.Sum(x => x.UncompressedSize);
            long uncompressedProgress = 0;


            using (var fs = new FileStream(tempSdz, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var zip = new ZipOutputStream(fs, false))
            {
                zip.CompressionLevel = CompressionLevel.BestSpeed;
                zip.ParallelDeflateThreshold = 0;
                foreach (var fl in fileList.Files)
                {
                    zip.PutNextEntry(fl.Name);

                    // 0 means paralell deflate is used, -1 means it is disabled
                    if (fl.UncompressedSize > paralellZipForFilesAboveSize) zip.ParallelDeflateThreshold = 0;
                    else zip.ParallelDeflateThreshold = -1;

                    using (var itemStream = pool.ReadFromStorageDecompressed(fl.Hash)) itemStream.CopyTo(zip);

                    uncompressedProgress += fl.UncompressedSize;

                    progressIndicator?.Invoke((double)uncompressedProgress / uncompressedTotalSize);
                }
            }
        }

    }
}
