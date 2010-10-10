using System;

namespace MonoTorrent.Common
{
	public class TorrentCreatorEventArgs: EventArgs
	{
		readonly long fileHashed;
		readonly long fileTotal;
		readonly long overallHashed;
		readonly long overallTotal;

		/// <summary>
		/// The number of bytes hashed from the current file
		/// </summary>
		public long FileBytesHashed { get { return fileHashed; } }

		/// <summary>
		/// The percentage of the current file which has been hashed (range 0-100)
		/// </summary>
		public double FileCompletion { get { return fileHashed/(double)fileTotal*100.0; } }
		/// <summary>
		/// The size of the current file
		/// </summary>
		public long FileSize { get { return fileTotal; } }

		/// <summary>
		/// The number of bytes hashed so far
		/// </summary>
		public long OverallBytesHashed { get { return overallHashed; } }

		/// <summary>
		/// The percentage of the data which has been hashed (range 0-100)
		/// </summary>
		public double OverallCompletion { get { return overallHashed/(double)overallTotal*100.0; } }
		/// <summary>
		/// The total number of bytes to hash
		/// </summary>
		public long OverallSize { get { return overallTotal; } }

		internal TorrentCreatorEventArgs(long fileHashed, long fileTotal, long overallHashed, long overallTotal)
		{
			this.fileHashed = fileHashed;
			this.fileTotal = fileTotal;
			this.overallHashed = overallHashed;
			this.overallTotal = overallTotal;
		}
	}
}