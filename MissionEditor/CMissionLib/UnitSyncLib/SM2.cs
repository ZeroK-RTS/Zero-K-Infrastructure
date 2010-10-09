using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace CMissionLib.UnitSyncLib
{
	public class SM2
	{
		public event ProgressChangedEventHandler ProgressChanged = delegate { };


		/// <summary>
		/// Returns an SM2 map texuture
		/// </summary>
		public BitmapSource GetTexture(Map map, int detail, UnitSync unitSync)
		{
			UnitSync.NativeMethods.RemoveAllArchives();
			UnitSync.NativeMethods.AddAllArchives(map.ArchiveName);
			ProgressChanged(this, new ProgressChangedEventArgs(0, "Extracting map"));
			var mapName = map.Name + ".smf";
			var smfFileData = unitSync.ReadVfsFile("maps\\" + mapName);
			var reader = new BinaryReader(new MemoryStream(smfFileData));
			var smfHeader = reader.ReadStruct<SMFHeader>();
			smfHeader.SelfCheck();
			var mapWidth = smfHeader.mapx;
			var mapHeight = smfHeader.mapy;

			reader.BaseStream.Position = smfHeader.tilesPtr;
			var mapTileHeader = reader.ReadStruct<MapTileHeader>();

			// get the tile files and the number of tiles they contain
			var tileFiles = new Dictionary<byte[], int>();
			for (var i = 0; i < mapTileHeader.numTileFiles; i++)
			{
				var numTiles = reader.ReadInt32();
				var tileFileData = unitSync.ReadVfsFile("maps\\" + reader.ReadCString());
				tileFiles.Add(tileFileData, numTiles);
			}

			// get the position of the tiles
			var mapUnitInTiles = Tiles.TileMipLevel1Size/smfHeader.texelPerSquare;
			var tilesX = smfHeader.mapx/mapUnitInTiles;
			var tilesY = smfHeader.mapy/mapUnitInTiles;
			var tileIndices = new int[tilesX*tilesY];
			for (var i = 0; i < tileIndices.Length; i++)
			{
				tileIndices[i] = reader.ReadInt32();
			}

			Tiles.ProgressChanged += (s, e) => ProgressChanged(this, e);

			UnitSync.NativeMethods.RemoveAllArchives();
			// load the tiles
			return Tiles.LoadTiles(tileFiles, tileIndices, tilesX, tilesY, detail);
		}
	}
}