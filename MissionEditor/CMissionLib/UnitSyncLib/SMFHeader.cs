using System;
using System.Runtime.InteropServices;

namespace CMissionLib.UnitSyncLib
{
	[StructLayout(LayoutKind.Sequential)]
	struct SMFHeader
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)] string magic; // "spring map file\0"
		public int version; // Must be 1 for now
		public int mapid; // Sort of a GUID of the file, just set to a random value when writing a map

		public int mapx; // Must be divisible by 128
		public int mapy; // Must be divisible by 128
		public int squareSize; // Distance between vertices. Must be 8
		public int texelPerSquare; // Number of texels per square, must be 8 for now
		public int tilesize; // Number of texels in a tile, must be 32 for now
		public float minHeight; // Height value that 0 in the heightmap corresponds to	
		public float maxHeight; // Height value that 0xffff in the heightmap corresponds to

		public int heightmapPtr; // File offset to elevation data (short int[(mapy+1)*(mapx+1)])
		public int typeMapPtr; // File offset to typedata (unsigned char[mapy/2 * mapx/2])
		public int tilesPtr; // File offset to tile data (see MapTileHeader)
		public int minimapPtr; // File offset to minimap (always 1024*1024 dxt1 compresed data plus 8 mipmap sublevels)
		public int metalmapPtr; // File offset to metalmap (unsigned char[mapx/2 * mapy/2])
		public int featurePtr; // File offset to feature data (see MapFeatureHeader)

		public int numExtraHeaders; // Numbers of extra headers following main header

		public void SelfCheck()
		{
			if (magic != "spring map file" || version != 1 || mapx%128 != 0 || mapy%128 != 0 || squareSize != 8 ||
			    texelPerSquare != 8 || tilesize != 32)
			{
				throw new Exception("Invalid SMF header");
			}
		}
	} ;
}