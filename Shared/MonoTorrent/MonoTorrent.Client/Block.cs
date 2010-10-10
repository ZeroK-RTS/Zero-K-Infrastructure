namespace MonoTorrent.Client
{
	/// <summary>
	/// 
	/// </summary>
	public struct Block
	{
		readonly Piece piece;
		bool received;
		readonly int requestLength;
		bool requested;
		readonly int startOffset;
		bool written;

		public int PieceIndex { get { return piece.Index; } }

		public bool Received
		{
			get { return received; }
			internal set
			{
				if (value && !received) piece.TotalReceived++;

				else if (!value && received) piece.TotalReceived--;

				received = value;
			}
		}

		public int RequestLength { get { return requestLength; } }
		public bool Requested
		{
			get { return requested; }
			internal set
			{
				if (value && !requested) piece.TotalRequested++;

				else if (!value && requested) piece.TotalRequested--;

				requested = value;
			}
		}

		public int StartOffset { get { return startOffset; } }

		public bool Written
		{
			get { return written; }
			internal set
			{
				if (value && !written) piece.TotalWritten++;

				else if (!value && written) piece.TotalWritten--;

				written = value;
			}
		}

		internal Block(Piece piece, int startOffset, int requestLength)
		{
			this.piece = piece;
			received = false;
			requested = false;
			this.requestLength = requestLength;
			this.startOffset = startOffset;
			written = false;
		}

		internal static int IndexOf(Block[] blocks, int startOffset, int blockLength)
		{
			var index = startOffset/Piece.BlockSize;
			if (blocks[index].startOffset != startOffset || blocks[index].RequestLength != blockLength) return -1;
			return index;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Block)) return false;

			var other = (Block)obj;
			return PieceIndex == other.PieceIndex && startOffset == other.startOffset && requestLength == other.requestLength;
		}

		public override int GetHashCode()
		{
			return PieceIndex ^ requestLength ^ startOffset;
		}
	}
}