using System.IO;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
	class TorrentFileStream: FileStream
	{
		readonly TorrentFile file;
		readonly string path;

		public TorrentFile File { get { return file; } }

		public string Path { get { return path; } }


		public TorrentFileStream(string filePath, TorrentFile file, FileMode mode, FileAccess access, FileShare share): base(filePath, mode, access, share)
		{
			this.file = file;
			path = filePath;
		}
	}
}