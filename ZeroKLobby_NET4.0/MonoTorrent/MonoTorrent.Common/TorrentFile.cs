using System;
using System.Text;

namespace MonoTorrent.Common
{
	/// <summary>
	/// This is the base class for the files available to download from within a .torrent.
	/// This should be inherited by both Client and Tracker "TorrentFile" classes
	/// </summary>
	public class TorrentFile: IEquatable<TorrentFile>
	{
		readonly BitField bitfield;
		readonly byte[] ed2k;
		readonly int endPiece;
		readonly long length;
		readonly byte[] md5;
		readonly string path;
		BitField selector;
		readonly byte[] sha1;
		readonly int startPiece;

		/// <summary>
		/// The number of pieces which have been successfully downloaded which are from this file
		/// </summary>
		public BitField BitField { get { return bitfield; } }

		public long BytesDownloaded { get { return (long)(BitField.PercentComplete*Length/100.0); } }

		/// <summary>
		/// The ED2K hash of the file
		/// </summary>
		public byte[] ED2K { get { return ed2k; } }

		/// <summary>
		/// The index of the last piece of this file
		/// </summary>
		public int EndPieceIndex { get { return endPiece; } }

		/// <summary>
		/// The length of the file in bytes
		/// </summary>
		public long Length { get { return length; } }

		/// <summary>
		/// The MD5 hash of the file
		/// </summary>
		public byte[] MD5 { get { return md5; } }

		/// <summary>
		/// In the case of a single torrent file, this is the name of the file.
		/// In the case of a multi-file torrent this is the relative path of the file
		/// (including the filename) from the base directory
		/// </summary>
		public string Path { get { return path; } }

		/// <summary>
		/// The priority of this torrent file
		/// </summary>
		public Priority Priority { get; set; }

		/// <summary>
		/// The SHA1 hash of the file
		/// </summary>
		public byte[] SHA1 { get { return sha1; } }

		/// <summary>
		/// The index of the first piece of this file
		/// </summary>
		public int StartPieceIndex { get { return startPiece; } }

		public TorrentFile(string path, long length): this(path, length, 0, 0, null, null, null) {}

		public TorrentFile(string path, long length, int startIndex, int endIndex): this(path, length, startIndex, endIndex, null, null, null) {}

		public TorrentFile(string path, long length, int startIndex, int endIndex, byte[] md5, byte[] ed2k, byte[] sha1)
		{
			bitfield = new BitField(endIndex - startIndex + 1);
			this.ed2k = ed2k;
			endPiece = endIndex;
			this.length = length;
			this.md5 = md5;
			this.path = path;
			Priority = Priority.Normal;
			this.sha1 = sha1;
			startPiece = startIndex;
		}

		internal BitField GetSelector(int totalPieces)
		{
			if (selector != null) return selector;

			selector = new BitField(totalPieces);
			for (var i = StartPieceIndex; i <= EndPieceIndex; i++) selector[i] = true;
			return selector;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as TorrentFile);
		}

		public override int GetHashCode()
		{
			return path.GetHashCode();
		}

		public override string ToString()
		{
			var sb = new StringBuilder(32);
			sb.Append("File: ");
			sb.Append(path);
			sb.Append(" StartIndex: ");
			sb.Append(StartPieceIndex);
			sb.Append(" EndIndex: ");
			sb.Append(EndPieceIndex);
			return sb.ToString();
		}

		public bool Equals(TorrentFile other)
		{
			return other == null ? false : path == other.path && length == other.length;
			;
		}
	}
}