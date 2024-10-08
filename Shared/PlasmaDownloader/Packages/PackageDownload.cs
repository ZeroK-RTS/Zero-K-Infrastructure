#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading;
using Ionic.Zip;
using PlasmaShared;
using SharpCompress.Common;
using ZkData;
using CompressionLevel = Ionic.Zlib.CompressionLevel;

#endregion

namespace PlasmaDownloader.Packages
{
	/// <summary>
	/// This class is not thread safe!
	/// </summary>
	public class PackageDownload: Download
	{
		long doneAll;
		readonly WebDownload fileListWebGet = new WebDownload();
		string tempFilelist = "";
	    private PlasmaDownloader downloader;
	    private SpringPaths paths;
	    private string urlRoot;

	    private RapidHandling rapidHandling;

	    public override double IndividualProgress
		{
			get
			{
				if (IsComplete == true) return 100;
				if (Length == 0)
				{
					// still not getting files
					return (fileListWebGet.IndividualProgress)/20;
				}
				else
				{
					// getting files - can get accurate progress 
				    return rapidHandling != RapidHandling.DefaultSdp 
				        ? (80.0 * (doneAll + fileListWebGet.Length) / Length) + 20.0 * zipProgress
				        : (100.0 * (doneAll + fileListWebGet.Length) / Length);

				}
			}
		}
		//public Hash PackageHash { get; protected set; }

	    public PackageDownload(string name, PlasmaDownloader downloader) {
	        this.Name = name;
	        this.downloader = downloader;
	        this.rapidHandling = downloader.RapidHandling;
	    }
        
		public void Start()
		{
			Utils.SafeThread(MasterDownloadThread).Start();
		}


		SdpArchive GetFileList()
		{
			SdpArchive fileList;
			tempFilelist = GetTempFileName();
			fileListWebGet.Start(urlRoot + "/packages/" + packageHash + ".sdp");
			fileListWebGet.WaitHandle.WaitOne();

			if (fileListWebGet.Result != null)
			{
				File.WriteAllBytes(tempFilelist, fileListWebGet.Result);
				using (var fs = new FileStream(tempFilelist, FileMode.Open)) fileList = new SdpArchive(new GZipStream(fs, CompressionMode.Decompress));
			}
			else throw new ApplicationException("FileList has download failed");
			return fileList;
		}

		string GetTempFileName()
		{
			return Utils.MakePath(paths.WritableDirectory, "temp", Path.GetRandomFileName());
		}

		bool LoadFiles(SdpArchive fileList)
		{
			try
			{
				doneAll = 0;

				var hashesToDownload = new List<Hash>();
				var bitArray = new BitArray(fileList.Files.Count);
				for (var i = 0; i < fileList.Files.Count; i++)
				{
					if (!pool.Exists(fileList.Files[i].Hash))
					{
						bitArray.PushBit(true);
						hashesToDownload.Add(fileList.Files[i].Hash);
					}
					else bitArray.PushBit(false);
				}

				var wr = WebRequest.Create(string.Format("{0}/streamer.cgi?{1}", urlRoot, packageHash));
				wr.Method = "POST";
				wr.Proxy = null;
				var zippedArray = bitArray.GetByteArray().Compress();
			    using (var requestStream = wr.GetRequestStream())
			    {
			        requestStream.Write(zippedArray, 0, zippedArray.Length);
			        requestStream.Close();

			        using (var response = wr.GetResponse())
			        {
			            Length = (int)(response.ContentLength + fileListWebGet.Length);
			            var responseStream = response.GetResponseStream();

			            var numberBuffer = new byte[4];
			            var cnt = 0;
			            while (responseStream.ReadExactly(numberBuffer, 0, 4))
			            {
			                if (IsAborted) break;
			                var sizeLength = (int)SdpArchive.ParseUint32(numberBuffer);
			                var buf = new byte[sizeLength];

			                if (!responseStream.ReadExactly(buf, 0, sizeLength, ref doneAll))
			                {
			                    Trace.TraceError("{0} download failed - unexpected end of stream", Name);
			                    return false;
			                }
			                pool.PutToStorage(buf, hashesToDownload[cnt]);
			                cnt++;
			            }

			            if (cnt != hashesToDownload.Count)
			            {
			                Trace.TraceError("{0} download failed - unexpected end of stream", Name);
			                return false;
			            }
			            Trace.TraceInformation("{0} download complete - {1}", Name, Utils.PrintByteLength(Length));
			        }
			    }

			    return true;
			}
			catch (Exception ex)
			{
				Trace.TraceError("Error downloading {0}: {1}", Name, ex);
				return false;
			}
		}

	    


        void MasterDownloadThread()
		{
			try
			{
                this.paths = downloader.SpringPaths;

                downloader.PackageDownloader.LoadMasterAndVersions()?.Wait();
			    var entry = downloader.PackageDownloader.FindEntry(Name);
			    if (entry == null)
			    {
			        Finish(false);
			        return;
			    }

                this.urlRoot = entry.Item1.BaseUrl;
                this.packageHash = entry.Item2.Hash;

			    string sdzName = string.Empty;
			    if (rapidHandling == RapidHandling.SdzNameHash) sdzName = packageHash.ToString();
                else if (rapidHandling == RapidHandling.SdzNameTagForceDownload) sdzName = entry.Item2.Name.Replace(":", "-");

                var targetSdz = Path.Combine(paths.WritableDirectory, "games", sdzName + ".sdz");
                var targetSdp = Path.Combine(paths.WritableDirectory, "packages", packageHash + ".sdp");
                
                if (rapidHandling == RapidHandling.SdzNameHash && File.Exists(targetSdz)) // SDZ exists, abort
			    {
			        Finish(true);
			        return;
			    }

			    if (rapidHandling == RapidHandling.DefaultSdp && File.Exists(targetSdp)) // SDP exists, abort
			    {
			        Finish(true);
			        return;
                }

                AddDependencies(entry);

                this.pool = new Pool(paths);
                
                var fileList = GetFileList();
				var ok = LoadFiles(fileList);

				var i = 0;
				while (!IsAborted && !ok && i++ < 3) ok = LoadFiles(fileList);
				if (ok)
				{
					if (File.Exists(targetSdp)) File.Delete(targetSdp);
					File.Move(tempFilelist, targetSdp);

				    if (rapidHandling != RapidHandling.DefaultSdp)
				    {
                        try
                        {
                            var convertor = new Sdp2Sdz();
                            convertor.ConvertSdp2Sdz(paths, packageHash, targetSdz, (p) => { zipProgress = p; });

                            if (rapidHandling == RapidHandling.SdzNameHash) RemoveOtherSdzVersions(entry);
                        }catch(Exception ex)
                        {
                            Trace.TraceError("Error converting {0} to sdz {1}", Name, ex);
                        }
				    }

                    
				    Finish(true);
				}
				else Finish(false);
			}
			catch (Exception ex)
			{
				Abort();
				Trace.TraceError("Error downloading {0} {1}", Name, ex);
				Finish(false);
			}
		}

	    private void RemoveOtherSdzVersions(Tuple<PackageDownloader.Repository, PackageDownloader.Version> entry)
	    {
            var fileNames = Directory.GetFiles(Path.Combine(paths.WritableDirectory, "games"), "*.sdz").Select(Path.GetFileNameWithoutExtension).ToList();

	        foreach (var other in entry.Item1.VersionsByTag)
	        {
	            if (other.Value.Hash != packageHash && !other.Key.EndsWith(":stable") && !other.Key.EndsWith(":test") &&
	                fileNames.Contains(other.Value.Hash.ToString())) File.Delete(Path.Combine(paths.WritableDirectory, "games", other.Value.Hash + ".sdz"));
	        }
	    }




	    private double zipProgress;



	    private Hash packageHash;
	    private Pool pool;

	    private void AddDependencies(Tuple<PackageDownloader.Repository, PackageDownloader.Version> entry) {
	        if (entry.Item2.Dependencies != null)
	        {
	            foreach (var dept in entry.Item2.Dependencies)
	            {
	                if (!string.IsNullOrEmpty(dept))
	                {
	                    var dd = downloader.GetResource(DownloadType, dept);
	                    if (dd != null) AddNeededDownload(dd);
	                }
	            }
	        }
	    }
	}
}