using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CMissionLib.UnitSyncLib
{
	static unsafe class Tiles
	{
		public const int TileMipLevel1Size = 32; // Size of tile side in pixels, 1st mip level.
		const int TileMipLevel1Bytes = 512; // Size of 1st mip level.
		const int TileTotalBytes = 680; // Size of tile, including all mip levels.
		const int MipLevels = 4;
		const int UncompressedBytesPerPixel = 4;

		public static event ProgressChangedEventHandler ProgressChanged = delegate { };

		public static BitmapSource LoadTiles(Dictionary<byte[], int> tileFiles, int[] tilePositions, int tilesX, int tilesY,
		                                     int mipLevel)
		{
			if (mipLevel >= MipLevels) throw new ArgumentException("Mip level not supported");

			ProgressChanged(null, new ProgressChangedEventArgs(0, "Decompressing Map Texture"));

			// Make a new array in which index is the tileID and the value is a list of tile positions, 
			// so the tile position can be quickly found from the TileID.
			var tilePosByID = new List<int>[tileFiles.Values.Sum()];
			for (var i = 0; i < tilePosByID.Length; i++) tilePosByID[i] = new List<int>(1);
			for (var i = 0; i < tilePositions.Length; i++) tilePosByID[tilePositions[i]].Add(i);

			// Create the big final map bitmap.
			var tileSize = TileMipLevel1Size >> mipLevel;
			var bitmapWidth = tilesX*tileSize;
			var bitmapHeight = tilesY*tileSize;

			// Keep the same on-screen size as a 1024x1024 image with 96 dpi.
			var dpi = Math.Max(bitmapWidth, bitmapHeight)*96/1024;

			// Create the uncompressed final bitmap.
			var map = new WriteableBitmap(bitmapWidth, bitmapHeight, dpi, dpi, PixelFormats.Bgr32, null);

			// Expose its data.
			map.Lock();
			var decompressedData = (byte*) map.BackBuffer;
			var stride = map.BackBufferStride;

			// Each tile has 4 mipmap levels. Get the offset of the level we need.
			var mipLevelOffset = Enumerable.Range(0, mipLevel).Select(n => TileMipLevel1Bytes >> n*2).Sum();

			// Get the tiles.

			var tilesInPreviousTileFile = 0;

			foreach (var kvp in tileFiles)
			{
				var tileFileBuffer = kvp.Key;
				var tileFileHandle = GCHandle.Alloc(tileFileBuffer, GCHandleType.Pinned);
				var tileFileData = (byte*) tileFileHandle.AddrOfPinnedObject();

				// Get the tile file header.
				var header = (TileFileHeader) Marshal.PtrToStructure((IntPtr) tileFileData, typeof (TileFileHeader));
				header.SelfCheck(kvp.Value);
				tileFileData += Marshal.SizeOf(typeof (TileFileHeader));

				// Function that gets a tile
				Action<int> processTile = tileInTileFileIndex =>
					{
						var tiles = tilePosByID[tileInTileFileIndex + tilesInPreviousTileFile];
						var firstUseOffsetX = 0;
						var firstUseOffsetY = 0;

						for (var tileUseIndex = 0; tileUseIndex < tiles.Count; tileUseIndex++)
						{
							var compressedData = tileFileData + tileInTileFileIndex*TileTotalBytes + mipLevelOffset;
							var tilePosition = tiles[tileUseIndex];
							var tileX = tilePosition%tilesX;
							var tileY = tilePosition/tilesX;
							var offsetX = tileX*tileSize;
							var offsetY = tileY*tileSize;
							if (tileUseIndex == 0)
							{
								// First time the tile is used, decompress data.
								firstUseOffsetX = offsetX;
								firstUseOffsetY = offsetY;
								DecodeDxt1Texture(compressedData, tileSize, decompressedData, stride, offsetX, offsetY);
							}
							else
							{
								// Copy data that was already decompressed.
								CopyBitmap32Section(decompressedData, tileSize, stride, firstUseOffsetX, firstUseOffsetY, offsetX,
								                    offsetY);
							}
						}
					};

				// Process the tiles in parallel.
				RunParallel(processTile, header.numTiles);

				tileFileHandle.Free();
				tilesInPreviousTileFile = kvp.Value;
			}

			map.Unlock();
			map.Freeze();

			return map;
		}

		static void CopyBitmap32Section(byte* bitmap, int tileSize, int stride, int sourceOffsetX, int sourceOffsetY,
		                                int destOffsetX, int destOffsetY)
		{
			for (var x = 0; x < tileSize; x++)
				for (var y = 0; y < tileSize; y++)
				{
					var sourcePixel = (y + sourceOffsetY)*stride + (x + sourceOffsetX)*UncompressedBytesPerPixel;
					var destinationPixel = (y + destOffsetY)*stride + (x + destOffsetX)*UncompressedBytesPerPixel;
					*(int*) (bitmap + destinationPixel) = *(int*) &bitmap[sourcePixel];
				}
		}


		public static void DecodeDxt1Texture(byte* sourceBitmap, int size, byte* destinationBitmap, int stride, int offsetX,
		                                     int offsetY)
		{
			var blockCount = ((size + 3)/4)*((size + 3)/4);
			var progress = sourceBitmap;

			for (var block = 0; block < blockCount; block++)
			{
				var x = 4*(block%((size + 3)/4)) + offsetX;
				var y = 4*(block/((size + 3)/4)) + offsetY;
				DecodeDxt1Block(progress, (int*) destinationBitmap, x, y, stride/UncompressedBytesPerPixel);
				progress += 8;
			}
		}

		static void DecodeDxt1Block(byte* blockData, int* destinationBitmap, int offsetX, int offsetY, int stride)
		{
			int* colors = stackalloc int[4];
			colors[0] = *(ushort*) &blockData[0];
			colors[1] = 0;
			colors[2] = 0;
			colors[3] = *(ushort*) &blockData[2];

			var r0 = UnpackRed(colors[0]);
			var g0 = UnpackGreen(colors[0]);
			var b0 = UnpackBlue(colors[0]);

			var r1 = UnpackRed(colors[3]);
			var g1 = UnpackGreen(colors[3]);
			var b1 = UnpackBlue(colors[3]);

			colors[2] = ((r0*2 + r1)/3) << 16 | ((g0*2 + g1)/3) << 8 | ((b0*2 + b1)/3);
			colors[3] = ((r0 + r1*2)/3) << 16 | ((g0 + g1*2)/3) << 8 | ((b0 + b1*2)/3);
			colors[0] = r0 << 16 | g0 << 8 | b0;
			colors[1] = r1 << 16 | g1 << 8 | b1;

			var bits = *(uint*) &blockData[4];

			for (var y = 0; y < 4; y++)
			{
				for (var x = 0; x < 4; x++)
				{
					destinationBitmap[(y + offsetY)*stride + (x + offsetX)] = colors[bits & 0x3];
					bits >>= 2;
				}
			}
		}

		static int UnpackRed(int x)
		{
			return (x & 0x0000F800) >> 8;
		}

		static int UnpackGreen(int x)
		{
			return (x & 0x000007E0) >> 3;
		}

		static int UnpackBlue(int x)
		{
			return (x & 0x0000001F) << 3;
		}


		public static void RunParallel(this Action<int> task, int runCount)
		{
			// Check the progress only so often to reduce the lock overhead.
			const int batchSize = 10;

			var progress = 0;

			System.Action allRuns = delegate
				{
					while (true)
					{
						// Get the next batch.
						int item;
						lock (task)
						{
							item = progress;
							progress += batchSize;
						}
						// Run the batch.
						for (var i = item; i < item + batchSize; ++i)
						{
							if (i >= runCount) return;
							task(i);
						}
					}
				};


			var threadCount = Environment.ProcessorCount;

			// Start the tasks.
			var results = new IAsyncResult[threadCount];
			for (var i = 0; i < threadCount; ++i)
			{
				results[i] = allRuns.BeginInvoke(null, null);
			}

			// Wait for all tasks to complete
			for (var i = 0; i < threadCount; ++i)
			{
				allRuns.EndInvoke(results[i]);
			}
		}
	}
}