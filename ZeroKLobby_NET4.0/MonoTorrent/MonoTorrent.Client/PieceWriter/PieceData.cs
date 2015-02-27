using System;
using System.Threading;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
	public class BufferedIO
	{
		int count;
		readonly TorrentFile[] files;
		long offset;
		readonly int pieceLength;

		public int ActualCount { get; set; }
		public int BlockIndex { get { return PieceOffset/Piece.BlockSize; } }
		public ArraySegment<byte> Buffer { get { return buffer; } }


		public int Count { get { return count; } set { count = value; } }
		public TorrentFile[] Files { get { return files; } }
		public long Offset { get { return offset; } set { offset = value; } }
		public string Path { get; set; }
		internal Piece Piece;
		public int PieceIndex { get { return (int)(offset/pieceLength); } }
		public int PieceOffset
		{
			get
			{
				return (int)(offset%pieceLength);
				;
			}
		}


		public ManualResetEvent WaitHandle { get; set; }
		internal ArraySegment<byte> buffer;

		internal BufferedIO(object manager, ArraySegment<byte> buffer, long offset, int count, int pieceLength, TorrentFile[] files, string path)
		{
			this.Path = path;
			this.files = files;
			this.pieceLength = pieceLength;
			Initialise(buffer, offset, count);
		}

		public BufferedIO(object manager,
		                  ArraySegment<byte> buffer,
		                  int pieceIndex,
		                  int blockIndex,
		                  int count,
		                  int pieceLength,
		                  TorrentFile[] files,
		                  string path)
		{
			this.Path = path;
			this.files = files;
			this.pieceLength = pieceLength;
			Initialise(buffer, (long)pieceIndex*pieceLength + blockIndex*Piece.BlockSize, count);
		}

		public override string ToString()
		{
			return string.Format("Piece: {0} Block: {1} Count: {2}", PieceIndex, BlockIndex, count);
		}

		void Initialise(ArraySegment<byte> buffer, long offset, int count)
		{
			this.buffer = buffer;
			this.count = count;
			this.offset = offset;
		}
	}
}