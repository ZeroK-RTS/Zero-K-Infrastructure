using System;

namespace PlasmaDownloader.Packages
{
	public class BitArray
	{
		private byte[] buffer;
		private int position;
		public int SizeInBytes { get; private set; }


		public BitArray(int sizeInBits) {
			Reset(sizeInBits);
		}

		public byte[] GetByteArray() {
			return buffer;
		}

		public void PushBit(bool value) {
			if (value) {
				var bytePos = position/8;
				buffer[bytePos] |= (byte)(1 << (position%8));
			}
			position++;
		}

		public void Reset(int sizeInBits) {
			SizeInBytes = (int)Math.Ceiling(sizeInBits/8.0);
			buffer = new byte[SizeInBytes];
		}
	}
}