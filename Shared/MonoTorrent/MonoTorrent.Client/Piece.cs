using System;
using System.Collections;

namespace MonoTorrent.Client
{
	public class Piece: IComparable<Piece>
	{
		internal static readonly int BlockSize = (1 << 14); // 16kB

		Block[] blocks;
		readonly int index;
		int totalReceived;
		int totalRequested;
		int totalWritten;

		public bool AllBlocksReceived { get { return totalReceived == BlockCount; } }
		public bool AllBlocksRequested { get { return totalRequested == BlockCount; } }

		public bool AllBlocksWritten { get { return totalWritten == BlockCount; } }

		public int BlockCount { get { return blocks.Length; } }
		internal Block[] Blocks { get { return blocks; } }

		public int Index { get { return index; } }

		public bool NoBlocksRequested { get { return totalRequested == 0; } }

		public int TotalReceived { get { return totalReceived; } internal set { totalReceived = value; } }

		public int TotalRequested { get { return totalRequested; } internal set { totalRequested = value; } }

		public int TotalWritten { get { return totalWritten; } internal set { totalWritten = value; } }

		internal Piece(int pieceIndex, int pieceLength, long torrentSize)
		{
			index = pieceIndex;

			// Request last piece. Special logic needed
			if ((torrentSize - (long)pieceIndex*pieceLength) < pieceLength) LastPiece(pieceIndex, pieceLength, torrentSize);

			else
			{
				var numberOfPieces = (int)Math.Ceiling(((double)pieceLength/BlockSize));

				blocks = new Block[numberOfPieces];

				for (var i = 0; i < numberOfPieces; i++) blocks[i] = new Block(this, i*BlockSize, BlockSize);

				if ((pieceLength%BlockSize) != 0) // I don't think this would ever happen. But just in case
					blocks[blocks.Length - 1] = new Block(this, blocks[blocks.Length - 1].StartOffset, pieceLength - blocks[blocks.Length - 1].StartOffset);
			}
		}

		public IEnumerator GetEnumerator()
		{
			return blocks.GetEnumerator();
		}

		public override bool Equals(object obj)
		{
			var p = obj as Piece;
			return (p == null) ? false : index.Equals(p.index);
		}

		public override int GetHashCode()
		{
			return index;
		}

		void LastPiece(int pieceIndex, int pieceLength, long torrentSize)
		{
			var bytesRemaining = (int)(torrentSize - ((long)pieceIndex*pieceLength));
			var numberOfBlocks = bytesRemaining/BlockSize;
			if (bytesRemaining%BlockSize != 0) numberOfBlocks++;

			blocks = new Block[numberOfBlocks];

			var i = 0;
			while (bytesRemaining - BlockSize > 0)
			{
				blocks[i] = new Block(this, i*BlockSize, BlockSize);
				bytesRemaining -= BlockSize;
				i++;
			}

			blocks[i] = new Block(this, i*BlockSize, bytesRemaining);
		}

		public Block this[int index] { get { return blocks[index]; } }

		public int CompareTo(Piece other)
		{
			return other == null ? 1 : Index.CompareTo(other.Index);
		}
	}
}