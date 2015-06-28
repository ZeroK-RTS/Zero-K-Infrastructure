using System.IO;

namespace MonoTorrent.BEncoding
{
	public class RawReader: BinaryReader
	{
		readonly bool strictDecoding;

		public bool StrictDecoding { get { return strictDecoding; } }

		public RawReader(Stream input): this(input, true) {}

		public RawReader(Stream input, bool strictDecoding): base(input)
		{
			this.strictDecoding = strictDecoding;
		}
	}
}