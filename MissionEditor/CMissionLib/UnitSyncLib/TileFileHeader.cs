using System;
using System.Runtime.InteropServices;

namespace CMissionLib.UnitSyncLib
{
	[StructLayout(LayoutKind.Sequential)]
	struct TileFileHeader
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)] string magic; // "spring tilefile\0"
		public int version; // Must be 1
		public int numTiles; // Total number of tiles in this file
		public int tileSize; // Must be 32
		public int compressionType; // Must be 1 (= dxt1)

		public void SelfCheck(int tileNumber)
		{
			if (version != 1 ||
			    tileSize != 32 ||
			    compressionType != 1 ||
			    magic != "spring tilefile" ||
			    tileNumber != numTiles) throw new Exception("Invalid SMT");
		}
	}
}