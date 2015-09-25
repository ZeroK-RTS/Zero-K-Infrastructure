using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
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

            var mapDir = UnitSync.NativeMethods.GetMapFileName(UnitSync.NativeMethods.GetMapIndex(map.Name));
            var smfFileData = unitSync.ReadVfsFile(mapDir);
            BinaryReader reader;
            try {
                reader = new BinaryReader(new MemoryStream(smfFileData));
            } catch (System.ArgumentNullException nullEx)
            {
                throw new System.ArgumentNullException("smfFileData", "Unable to read SMF: " + mapDir);
            }
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

            BitmapSource ret;
            try {
                ret = Tiles.LoadTiles(tileFiles, tileIndices, tilesX, tilesY, detail);
            } catch (System.Exception ex)
            {
                //if (map.Texture == null) throw new System.Exception("Unable to load map texture");
                //var newBitmap = new Bitmap(GetBitmap(map.Texture), new Size(map.Texture.PixelWidth / 8, map.Texture.PixelHeight / 8));

                map.Minimap = unitSync.GetMinimap(map);

                ret = ConvertBitmap(map.Minimap);
            }

            return ret;
		}

        // http://stackoverflow.com/questions/2284353/is-there-a-good-way-to-convert-between-bitmapsource-and-bitmap
        Bitmap GetBitmap(BitmapSource source)
        {
            Bitmap bmp = new Bitmap(
              source.PixelWidth,
              source.PixelHeight,
              PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(
              new Rectangle(Point.Empty, bmp.Size),
              ImageLockMode.WriteOnly,
              PixelFormat.Format32bppPArgb);
            source.CopyPixels(
              System.Windows.Int32Rect.Empty,
              data.Scan0,
              data.Height * data.Stride,
              data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        BitmapSource ConvertBitmap(Bitmap source)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                          source.GetHbitmap(),
                          System.IntPtr.Zero,
                          System.Windows.Int32Rect.Empty,
                          BitmapSizeOptions.FromEmptyOptions());
        }

    }
}