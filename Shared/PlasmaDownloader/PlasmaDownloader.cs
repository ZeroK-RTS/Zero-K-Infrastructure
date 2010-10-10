#region using

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using PlasmaDownloader.Packages;
using PlasmaDownloader.Torrents;
using PlasmaShared;

#endregion

namespace PlasmaDownloader
{
	public enum DownloadType
	{
		MOD,
		MAP,
		MISSION,
		GAME,
		ENGINE,
		UNKNOWN
	}


	public class PlasmaDownloader: IDisposable
	{
		readonly List<Download> downloads = new List<Download>();

		readonly PackageDownloader packageDownloader;
		readonly SpringScanner scanner;
		readonly TorrentDownloader torrentDownloader;

		public IPlasmaDownloaderConfig Config { get; private set; }

		public IEnumerable<Download> Downloads { get { return downloads.AsReadOnly(); } }

		public PackageDownloader PackageDownloader { get { return packageDownloader; } }

		public SpringPaths SpringPaths { get; private set; }

		public event EventHandler<EventArgs<Download>> DownloadAdded = delegate { };

		public event EventHandler PackagesChanged { add { packageDownloader.PackagesChanged += value; } remove { packageDownloader.PackagesChanged -= value; } }

		public event EventHandler SelectedPackagesChanged { add { packageDownloader.SelectedPackagesChanged += value; } remove { packageDownloader.SelectedPackagesChanged -= value; } }

		public PlasmaDownloader(IPlasmaDownloaderConfig config, SpringScanner scanner, SpringPaths springPaths)
		{
			SpringPaths = springPaths;
			Config = config;
			this.scanner = scanner;
			torrentDownloader = new TorrentDownloader(this);
			packageDownloader = new PackageDownloader(this);
		}

		public void Dispose()
		{
			packageDownloader.Dispose();
		}

		[CanBeNull]
		public Download GetResource(DownloadType type, string name)
		{
			lock (downloads)
			{
				downloads.RemoveAll(x => x.IsAborted || x.IsComplete != null); // remove already completed downloads from list}
				var existing = downloads.SingleOrDefault(x => x.Name == name);
				if (existing != null) return existing;

				if (scanner != null && scanner.HasResource(name)) return null;

				if (type == DownloadType.MOD || type == DownloadType.UNKNOWN)
				{
					var down = packageDownloader.GetPackageDownload(name);
					if (down != null)
					{
						downloads.Add(down);
						DownloadAdded.RaiseAsyncEvent(this, new EventArgs<Download>(down));
						return down;
					}
				}

				if (type == DownloadType.MAP || type == DownloadType.MOD || type == DownloadType.UNKNOWN)
				{
					var down = torrentDownloader.DownloadTorrent(name);
					if (down != null)
					{
						downloads.Add(down);
						DownloadAdded.RaiseAsyncEvent(this, new EventArgs<Download>(down));
						return down;
					}
				}

				if (type == DownloadType.MISSION || type == DownloadType.GAME || type == DownloadType.ENGINE) throw new ApplicationException(string.Format("{0} download not supported in this version", type));

				return null;
			}
		}
	}
}