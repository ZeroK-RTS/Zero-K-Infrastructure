using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using FreeImageAPI;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace CMissionLib.UnitSyncLib
{
	public partial class UnitSync : IDisposable
	{
		const int AuthorBufferSize = 200;
		const int DefaultMapInfoVersion = 1;
		const int DescriptionBufferSize = 256;
		const int MaxMipLevel = 10;
		const int MaxUnits = 2000;

		readonly string originalDirectory;

		bool disposed;
		int? loadedArchiveIndex;
		Dictionary<uint, string> maps;

		string writableDataDirectory;

		public UnitSync(string path)
		{
			if (!Directory.Exists(path)) throw new UnitSyncException("Directory not found");
			UnitSyncPath = path;
			originalDirectory = Directory.GetCurrentDirectory();
			Directory.SetCurrentDirectory(path);
			if (!NativeMethods.Init(false, 666)) throw new UnitSyncException("Unitsync initialization failed.");
			Version = NativeMethods.GetSpringVersion();
			TraceErrors();
		}

		public string UnitSyncPath { get; private set; }

		public string Version { get; private set; }


		public string WritableDataDirectory
		{
			get
			{
				if (writableDataDirectory == null) writableDataDirectory = GetWritableDataDirectory();
				return writableDataDirectory;
			}
			set { writableDataDirectory = value; }
		}

		
		#region IDisposable Members

		public void Dispose()
		{
			if (!disposed)
			{
				Directory.SetCurrentDirectory(originalDirectory);
				try
				{
					NativeMethods.UnInit();
				}
				catch (DllNotFoundException)
				{
					// do nothing, already thrown on init
				}
				disposed = true;
			}
			GC.SuppressFinalize(this);
		}

		#endregion

		~UnitSync()
		{
			Dispose();
		}

		public event EventHandler<EventArgs<string>> LoadingStatusChanged = delegate { };

		void SetLoadingStatus(string status)
		{
			LoadingStatusChanged(this, new EventArgs<string>(status));
		}


		public Bitmap GetHeightMap(string mapName)
		{
			return GetInfoMap(mapName, "height", 1);
		}

		public Map GetMap(string mapName)
		{
			return GetMap(mapName, GetMapArchive(mapName));
		}

		public Map GetMap(string mapName, string archiveName)
		{
			var map = GetMapNoBitmaps(mapName, archiveName);
			if (map == null) return map;
			map.Minimap = GetMinimap(map);
			map.Heightmap = GetHeightMap(map.Name);
			map.Metalmap = GetMetalMap(map.Name);
			return map;
		}

		public Map GetMapFromArchive(string archiveName)
		{
			var mapName = GetMapNameFromArchive(archiveName);
			return mapName == null ? null : GetMap(mapName, archiveName);
		}

		public Map GetMapFromArchiveNoBitmaps(string archiveName)
		{
			var mapName = GetMapNameFromArchive(archiveName);
			return mapName == null ? null : GetMapNoBitmaps(mapName, archiveName);
		}

		public Dictionary<uint, string> GetMapHashes()
		{
			
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			if (maps != null) return maps;
			SetLoadingStatus("Loading Map (Map Hashes)");
			var mapCount = NativeMethods.GetMapCount();
			if (mapCount < 0) throw new UnitSyncException(NativeMethods.GetNextError());
			maps = (from mapIndex in Enumerable.Range(0, mapCount)
			        let name = NativeMethods.GetMapName(mapIndex)
			        let checksum = NativeMethods.GetMapChecksum(mapIndex)
			        group name by checksum).ToDictionary(group => group.Key, group => group.First());
			return maps;
		}

		public string[] GetMapNames()
		{
			return GetMapHashes().Values.Distinct().ToArray();
		}

		public Map GetMapNoBitmaps(string mapName)
		{
			return GetMapNoBitmaps(mapName, GetMapArchive(mapName));
		}

		public BitmapSource GetMapTexture(Map map, int detail)
		{
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			SetLoadingStatus("Loading Map (Map Texture)");
			var sm2 = new SM2();
			return sm2.GetTexture(map, detail, this);
		}

		public Map GetMapNoBitmaps(string mapName, string archiveName)
		{
			if (mapName == null || archiveName == null) return null;
			if (maps == null) GetMapNames();
			NativeMethods.RemoveAllArchives();
			SetLoadingStatus("Loading Map (Map Information)");
			var checksum = maps.First(kvp => kvp.Value == mapName).Key;
			var mapInfo = GetMapInfo(checksum);
			var map = new Map(mapName)
				{
					ArchiveName = Path.GetFileName(archiveName),
					Checksum = (int) checksum,
					Name = mapName,
					Description = mapInfo.description,
					TidalStrength = mapInfo.tidalStrength,
					Gravity = mapInfo.gravity,
					MaxMetal = mapInfo.maxMetal,
					ExtractorRadius = mapInfo.extractorRadius,
					MinWind = mapInfo.minWind,
					MaxWind = mapInfo.maxWind,
					Author = mapInfo.author,
					Size = new Size(mapInfo.width, mapInfo.height),
					Positions = mapInfo.positions,
				};
			map.Options = GetMapOptions(map.Name, map.ArchiveName).ToArray();
			NativeMethods.RemoveAllArchives();
			TraceErrors();
			return map;
		}

		public Bitmap GetMetalMap(string mapName)
		{
			SetLoadingStatus("Loading Map (Metal Map)");
			return GetInfoMap(mapName, "metal", 1);
		}

		public Bitmap GetMinimap(Map map)
		{
			SetLoadingStatus("Loading Map (Minimap)");
			return FixAspectRatio(map, GetSquareMinimap(map.Name, 0));
		}

		public Mod GetMod(string modName)
		{
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			if (modName == null) throw new ArgumentNullException("modName");
			NativeMethods.RemoveAllArchives();
			NativeMethods.GetPrimaryModCount(); // pre-requisite for the following calls
			var archiveName = GetModArchiveName(modName);
			SetLoadingStatus("Loading Mod (Reading Mod Files)");
			NativeMethods.AddAllArchives(archiveName);
			var modIndex = NativeMethods.GetPrimaryModIndex(modName);
			string[] sides;
			SetLoadingStatus("Loading Mod (Mod Information)");
			var mod = new Mod
				{
					Name = modName,
					ArchiveName = archiveName,
					UnitDefs = ReadUnits(),
					Desctiption = NativeMethods.GetPrimaryModDescription(modIndex),
					Game = NativeMethods.GetPrimaryModGame(modIndex),
					Mutator = NativeMethods.GetPrimaryModMutator(modIndex),
					ShortGame = NativeMethods.GetPrimaryModShortGame(modIndex),
					ShortName = NativeMethods.GetPrimaryModShortName(modIndex),
					PrimaryModVersion = NativeMethods.GetPrimaryModVersion(modIndex),
					StartUnits = new Dictionary<string, string>(GetStartUnits(modName, out sides)),
					Sides = sides,
					Checksum = (int) NativeMethods.GetPrimaryModChecksumFromName(modName),
					Options = GetModOptions(archiveName).ToArray(),
					SideIcons = GetSideIcons(sides).ToArray(),
					Dependencies = GetModDependencies(modIndex).Where(x => x != modName && !string.IsNullOrEmpty(x)).ToArray(),
					AllAis = GetAis().ToArray(),
					ModAis = GetAis().Where(ai => ai.IsLuaAi).ToArray(),

				};
			SetLoadingStatus("Loading Mod (Widgets)");
			mod.Widgets = GetFilesInVfsDirectory("LuaUI/Widgets", "*.lua", VfsMode.Mod).Select(Path.GetFileName).ToArray();
			SetLoadingStatus("Loading Mod (Gadgets)");
			mod.Gadgets = GetFilesInVfsDirectory("LuaRules/Gadgets", "*.lua", VfsMode.Mod).Select(Path.GetFileName).ToArray();
			ReadUnits();

			if (mod.Sides.Length == 0) Debug.WriteLine("Mod has no faction");
			if (mod.UnitDefs.Length == 0) Debug.WriteLine("No unit found.");

			NativeMethods.RemoveAllArchives();
			TraceErrors();
			return mod;
		}

		/// <summary>
		/// Obtain the search handle with FindFilesInVfs or GetFilesInVfsDirectory
		/// </summary>
		IEnumerable<string> CompleteFindFilesInVfs(int searchHandle)
		{
			const int MaxNamebuffer = 256;
			var nameBuffer = new byte[MaxNamebuffer];
			var results = new List<string>();
			var encoding = new ASCIIEncoding();
			while (true)
			{
				searchHandle = NativeMethods.FindFilesVFS(searchHandle, nameBuffer, MaxNamebuffer);
				var text = encoding.GetString(nameBuffer).TrimEnd('\0');
				if (!String.IsNullOrWhiteSpace(text)) results.Add(text);
				Array.Clear(nameBuffer, 0, nameBuffer.Length);
				TraceErrors();
				if (searchHandle == 0) break;
			}
			return results;
		}

		/// <summary>
		/// Call AddAllArchives before this
		/// </summary>
		IEnumerable<string> FindFilesInVfs(string pattern)
		{
			var searchHandle = NativeMethods.InitFindVFS(pattern);
			if (searchHandle < 0) throw new UnitSyncException(NativeMethods.GetNextError());
			TraceErrors();
			return CompleteFindFilesInVfs(searchHandle).ToArray();
		}

		public Mod GetModFromArchive(string archiveName)
		{
			var modName = GetModNameFromArchive(archiveName);
			return modName != null ? GetMod(modName) : null;
		}

		public IEnumerable<string> GetModNames()
		{
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			SetLoadingStatus("Loading Mod (Mod Names)");
			var modNames = new List<string>();
			for (var i = 0; i < NativeMethods.GetPrimaryModCount(); i++) modNames.Add(NativeMethods.GetPrimaryModName(i));
			return modNames;
		}

		/// <summary>
		/// Use when processing a new archive
		/// </summary>
		public void Reset()
		{
			NativeMethods.RemoveAllArchives();
		}

		static Bitmap FixAspectRatio(Map map, Image squareMinimap)
		{
			var newSize = map.Size.Width > map.Size.Height
			              	? new Size(squareMinimap.Width, (int) (squareMinimap.Height/((float) map.Size.Width/map.Size.Height)))
			              	: new Size((int) (squareMinimap.Width*((float) map.Size.Width/map.Size.Height)), squareMinimap.Height);

			var correctMinimap = new Bitmap(newSize.Width, newSize.Height, PixelFormat.Format24bppRgb);
			try
			{
				using (var graphics = Graphics.FromImage(correctMinimap))
				{
					graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
					graphics.DrawImage(squareMinimap, new Rectangle(Point.Empty, newSize));
				}
				squareMinimap.Dispose();
			} 
			catch
			{
				correctMinimap.Dispose();
				throw;
			}
			return correctMinimap;
		}

		IEnumerable<AiInfoPair> GetAiInfo(int aiIndex)
		{
			var aiInfoPairs = new List<AiInfoPair>();
			for (var i = 0; i < NativeMethods.GetSkirmishAIInfoCount(aiIndex); i++)
			{
				aiInfoPairs.Add(
					new AiInfoPair
						{
							Key = NativeMethods.GetInfoKey(i),
							Value = NativeMethods.GetInfoValue(i),
							Description = NativeMethods.GetInfoDescription(i)
						});
				TraceErrors();
			}
			return aiInfoPairs;
		}

		IEnumerable<Option> GetAiOptions(int aiIndex)
		{
			var aiOptions = new List<Option>();
			for (var i = 0; i < NativeMethods.GetSkirmishAIOptionCount(aiIndex); i++)
			{
				aiOptions.Add(LoadOption(i));
				TraceErrors();
			}
			return aiOptions;
		}

		IEnumerable<Ai> GetAis()
		{
			SetLoadingStatus("Loading Mod (AIs)");
			var ais = new List<Ai>();
			for (var i = 0; i < NativeMethods.GetSkirmishAICount(); i++)
				ais.Add(new Ai {Info = GetAiInfo(i).ToArray(), Options = GetAiOptions(i).ToArray()});
			return ais;
		}

		string GetArchivePath(string archiveName)
		{
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			var result = NativeMethods.GetArchivePath(archiveName);
			if (result == null) throw new UnitSyncException(NativeMethods.GetNextError());
			return result;
		}

		/// <summary>
		/// Call AddAllArchives before this
		/// </summary>
		IEnumerable<string> GetFilesInVfsDirectory(string folder, string pattern, string modes)
		{
			var searchHandle = NativeMethods.InitDirListVFS(folder, pattern, modes);
			return CompleteFindFilesInVfs(searchHandle);
		}

		unsafe Bitmap GetInfoMap(string mapName, string name, int bytesPerPixel)
		{
			var width = 0;
			var height = 0;
			if (!NativeMethods.GetInfoMapSize(mapName, name, ref width, ref height))
				throw new UnitSyncException("GetInfoMapSize failed");
			var infoMapData = new byte[width*height];
			var infoMapHandle = GCHandle.Alloc(infoMapData, GCHandleType.Pinned);
			try
			{
				var infoMapPointer = Marshal.UnsafeAddrOfPinnedArrayElement(infoMapData, 0);
				var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
				try
				{
					if (!NativeMethods.GetInfoMap(mapName, name, infoMapPointer, bytesPerPixel))
						throw new UnitSyncException("GetInfoMap failed");
					var bitmapData = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly,
					                                 bitmap.PixelFormat);
					const int PixelSize = 3;
					var p = (byte*) bitmapData.Scan0;
					for (var i = 0; i < infoMapData.Length; i++)
					{
						var v = infoMapData[i];
						var d = i/width*bitmapData.Stride + i%width*PixelSize;
						p[d] = p[d + 1] = p[d + 2] = v;
					}
					bitmap.UnlockBits(bitmapData);
				} catch
				{
					bitmap.Dispose();
					throw;
				}
				return bitmap;
			}
			finally
			{
				infoMapHandle.Free();
			}
		}

		string GetMapArchive(string mapName)
		{
			var i = NativeMethods.GetMapArchiveCount(mapName);
			if (i <= 0) return null;
			var archivePath = NativeMethods.GetMapArchiveName(0);
			if (archivePath == null) throw new UnitSyncException(NativeMethods.GetNextError());
			return Path.GetFileName(archivePath);
		}

		uint GetMapChecksum(string mapName)
		{
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			try
			{
				return GetMapHashes().Single(kvp => kvp.Value == mapName).Key;
			}
			catch (InvalidOperationException)
			{
				throw new UnitSyncException("Map not found");
			}
		}

		MapInfo GetMapInfo(string mapName, int mapInfoVersion)
		{
			return GetMapInfo(GetMapChecksum(mapName), mapInfoVersion);
		}

		MapInfo GetMapInfo(uint checksum)
		{
			return GetMapInfo(checksum, DefaultMapInfoVersion);
		}

		MapInfo GetMapInfo(uint checksum, int mapInfoVersion)
		{
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			if (maps == null) GetMapHashes();
			var mapName = maps[checksum];
			if (!new[] {0, 1}.Contains(mapInfoVersion))
				throw new ArgumentOutOfRangeException("mapInfoVersion", "must be 0 or 1.");
			if (!GetMapHashes().ContainsValue(mapName))
				throw new UnitSyncException(String.Format("Map not found ({0}).", mapName));
			var mapInfo = new MapInfo
				{author = new String(' ', AuthorBufferSize), description = new String(' ', DescriptionBufferSize)};
			if (!NativeMethods.GetMapInfoEx(mapName, ref mapInfo, mapInfoVersion))
				throw new UnitSyncException("Error getting map information.");
			TestMapInfo(mapInfo);
			return mapInfo;
		}

		MapInfo GetMapInfo(string mapName)
		{
			return GetMapInfo(mapName, DefaultMapInfoVersion);
		}

		MapInfo GetMapInfoFromArchive(string mapArchive)
		{
			return GetMapInfo(GetMapArchive(Path.GetFileName(mapArchive)));
		}

		string GetMapNameFromArchive(string archiveName)
		{
			var name = GetMapHashes().Values.FirstOrDefault(mapName => GetMapArchive(mapName) == archiveName);
			TraceErrors();
			return name;
		}

		IEnumerable<Option> GetMapOptions(string mapName, string archiveName)
		{
			SetLoadingStatus("Loading Map (Map Options)");
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			if (archiveName == null) throw new ArgumentNullException("archiveName");
			NativeMethods.AddAllArchives(archiveName);
			NativeMethods.AddAllArchives("maphelper.sdz");
			var optionCount = NativeMethods.GetMapOptionCount(mapName);
			var options = new List<Option>();
			for (var optionIndex = 0; optionIndex < optionCount; optionIndex++)
			{
				var option = LoadOption(optionIndex);
				if (option != null) options.Add(option);
				TraceErrors();
			}
			return options;
		}


		string GetModArchiveName(string name)
		{
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			var modIndex = NativeMethods.GetPrimaryModIndex(name);
			if (modIndex < 0) throw new UnitSyncException(string.Format("Mod not found ({0}).", name));
			var modArchive = NativeMethods.GetPrimaryModArchive(modIndex);
			return modArchive;
		}

		Dictionary<string, string> GetModArchives()
		{
			var archives = new Dictionary<string, string>();
			foreach (var modName in GetModNames()) archives[GetModArchiveName(modName)] = modName;
			return archives;
		}

		IEnumerable<string> GetModDependencies(int modIndex)
		{
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");

			var ret = new List<string>();
			var count = NativeMethods.GetPrimaryModArchiveCount(modIndex);
			for (var i = 0; i < count; i++)
				ret.Add(GetModNameFromArchive(Path.GetFileName(NativeMethods.GetPrimaryModArchiveList(i))));
			return ret;
		}

		string GetModNameFromArchive(string archiveName)
		{
			var archives = GetModArchives();
			string modName;
			return archives.TryGetValue(archiveName, out modName) ? modName : null;
		}

		IEnumerable<Option> GetModOptions(string archiveName)
		{
			SetLoadingStatus("Loading Mod (Mod Options)");
			NativeMethods.AddAllArchives(archiveName);
			var optionCount = NativeMethods.GetModOptionCount();
			var options = new List<Option>();
			for (var optionIndex = 0; optionIndex < optionCount; optionIndex++)
			{
				var option = LoadOption(optionIndex);
				if (option != null) options.Add(option);
				TraceErrors();
			}
			return options;
		}

		/// <summary>
		/// Call AddAllArchives before this
		/// Icons not found are null
		/// </summary>
		byte[][] GetSideIcons(IEnumerable<string> sides)
		{
			return sides.Select(side => ReadVfsFile("SidePics\\" + side + ".bmp")).ToArray();
		}

		Bitmap GetSquareMinimap(string mapName, int mipLevel)
		{
			if (!GetMapHashes().ContainsValue(mapName))
				throw new UnitSyncException(string.Format("Map not found ({0}).", mapName));
			if (mipLevel < 0 || mipLevel > MaxMipLevel)
				throw new ArgumentOutOfRangeException("mipLevel", string.Format("Mip level must range from 0 to {0}.", MaxMipLevel));

			var size = 1024 >> mipLevel;
			var pointer = NativeMethods.GetMinimap(mapName, mipLevel);
			const PixelFormat format = PixelFormat.Format16bppRgb565;
			var pixelFormatSize = Image.GetPixelFormatSize(format)/8;
			var stride = size*pixelFormatSize;
			return new Bitmap(size, size, stride, format, pointer);
		}


		Dictionary<string, string> GetStartUnits(string modName, out string[] sides)
		{
			
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			var modIndex = Array.LastIndexOf(GetModNames().ToArray(), modName);
			if (modIndex < 0) throw new UnitSyncException("Mod not found (" + modName + ").");
			return GetStartUnits(modIndex, out sides);
		}

		Dictionary<string, string> GetStartUnits(int modIndex, out string[] sides)
		{
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			LoadModArchive(modIndex);
			SetLoadingStatus("Loading Mod (Start Units)");
			var sideCount = NativeMethods.GetSideCount();
			var startUnits = new Dictionary<string, string>();
			sides = new string[sideCount];
			for (var sideIndex = 0; sideIndex < sideCount; sideIndex++)
			{
				var sideName = NativeMethods.GetSideName(sideIndex);
				sides[sideIndex] = sideName;
				startUnits[sideName] = NativeMethods.GetSideStartUnit(sideIndex);
			}
			return startUnits;
		}

		IEnumerable<UnitInfo> GetUnitList(string modName)
		{
			var modIndex = Array.LastIndexOf(GetModNames().ToArray(), modName);
			if (modIndex < 0) throw new UnitSyncException(string.Format("Mod not found ({0}).", modName));
			return GetUnitList(modIndex);
		}

		IEnumerable<UnitInfo> GetUnitList(int modIndex)
		{
			SetLoadingStatus("Loading Mod (Unit List)");
			if (modIndex != loadedArchiveIndex)
			{
				NativeMethods.AddAllArchives(NativeMethods.GetPrimaryModArchive(modIndex));
				loadedArchiveIndex = modIndex;
			}
			for (var i = 0; i <= MaxUnits && NativeMethods.ProcessUnitsNoChecksum() > 0; i++)
			{
				var error = NativeMethods.GetNextError();
				if (error != null)
				{
					Debug.WriteLine("UnitSync Error: " + error);
					break;
				}
			}
			var units = new List<UnitInfo>();
			for (var i = 0; i < NativeMethods.GetUnitCount(); i++)
			{
				var unitName = NativeMethods.GetUnitName(i);
				var fullUnitName = NativeMethods.GetFullUnitName(i);
				var unitInfo = new UnitInfo{Name = unitName, FullName = fullUnitName};

				units.Add(unitInfo);
			}

			foreach (var unit in units) GetBuildPic(unit);
			return units;
		}

		BitmapSource LoadBuildpic(byte[] data)
		{
			FIBITMAP dib;
			using (var memoryStream = new MemoryStream(data)) 
			{
				dib = FreeImage.LoadFromStream(memoryStream);
			}
			try
			{
				using (var bitmap = FreeImage.GetBitmap(dib))
				{
					return bitmap.ToBitmapSource();
				}
			}
			finally
			{
				FreeImage.UnloadEx(ref dib);
			}
		}

		void GetBuildPic(UnitInfo unit) 
		{
			if (!String.IsNullOrEmpty(unit.BuildPicField))
			{
				var data = ReadVfsFile("unitpics/" + unit.BuildPicField);
				if (data != null)
				{
					try
					{
						unit.BuildPic = LoadBuildpic(data);
						return;
					} catch {}

				}
			}

			// buildpic not specified, try guessing the buildpic
			var files = GetFilesInVfsDirectory("unitpics", unit.Name + "*", VfsMode.Mod);
			if (files.Any())
			{
				foreach (var fileName in files)
				{
					var fileData = ReadVfsFile(fileName);
					if (fileData != null)
					{
						try
						{
							unit.BuildPic = LoadBuildpic(fileData);
							return;
						}
						catch { }
					}
				}
			}
		}

		string GetWritableDataDirectory()
		{
			return NativeMethods.GetWritableDataDirectory();
		}


		ListOption LoadListOption(int optionIndex, int itemIndex)
		{
			var listOption = new ListOption
				{
					Name = NativeMethods.GetOptionListItemName(optionIndex, itemIndex),
					Description = NativeMethods.GetOptionListItemDesc(optionIndex, itemIndex),
					Key = NativeMethods.GetOptionListItemKey(optionIndex, itemIndex)
				};
			TraceErrors();
			return listOption;
		}

		void LoadModArchive(int modIndex)
		{
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			if (modIndex == loadedArchiveIndex) return;
			NativeMethods.AddAllArchives(NativeMethods.GetPrimaryModArchive(modIndex));
			loadedArchiveIndex = modIndex;
		}

		Option LoadOption(int index)
		{
			TraceErrors();
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			var option = new Option
				{
					Name = NativeMethods.GetOptionName(index),
					Key = NativeMethods.GetOptionKey(index),
					Description = NativeMethods.GetOptionDesc(index),
					Type = (OptionType) NativeMethods.GetOptionType(index),
					Scope = NativeMethods.GetOptionScope(index),
					Section = NativeMethods.GetOptionSection(index),
					Style = NativeMethods.GetOptionStyle(index)
				};
			switch (option.Type)
			{
				case OptionType.Bool:
					option.Default = NativeMethods.GetOptionBoolDef(index).ToString();
					break;
				case OptionType.Number:
					option.Default = NativeMethods.GetOptionNumberDef(index).ToString(CultureInfo.InvariantCulture);
					option.Min = NativeMethods.GetOptionNumberMin(index);
					option.Max = NativeMethods.GetOptionNumberMax(index);
					option.Step = NativeMethods.GetOptionNumberStep(index);
					break;
				case OptionType.String:
					option.Default = NativeMethods.GetOptionStringDef(index);
					option.StrMaxLen = NativeMethods.GetOptionStringMaxLen(index);
					break;
				case OptionType.List:
					option.Default = NativeMethods.GetOptionListDef(index);
					var listCount = NativeMethods.GetOptionListCount(index);
					for (var i = 0; i < listCount; i++)
					{
						var optl = LoadListOption(index, i);
						option.ListOptions.Add(optl);
					}
					break;
				case OptionType.Section:
					break;
				default:
					return null;
			}
			TraceErrors();
			return option;
		}

		/// <summary>
		/// Call AddAllArchives before this
		/// </summary>
		public byte[] ReadVfsFile(string filePath)
		{
			if (filePath == null) throw new ArgumentNullException("filePath");
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			var handle = NativeMethods.OpenFileVFS(filePath);
			try
			{
				if (handle == 0)
				{
					Debug.WriteLine("File " + filePath + " not found in VFS.");
					return null;
				}
				var fileSize = NativeMethods.FileSizeVFS(handle);
				if (fileSize == 0) return new byte[0];
				if (fileSize < 0) return null;
				var fileData = new byte[fileSize];
				var bytesRead = NativeMethods.ReadFileVFS(handle, fileData, fileSize);
				if (bytesRead < 0)
				{
					TraceErrors();
					return null;
				}
				if (bytesRead != fileSize) Debug.WriteLine("File size and bytes read mismatch for " + filePath);
				return fileData;
			}
			finally
			{
				if (handle != 0) NativeMethods.CloseFileVFS(handle);
			}
		}

		void ListStrKeys()
		{
			for (var i = 1; i < NativeMethods.lpGetStrKeyListCount(); i++)
			{
				var key = NativeMethods.lpGetStrKeyListEntry(i);
				var type = (LuaType) NativeMethods.lpGetStrKeyType(key);
				Debug.WriteLine(key + " " + type);
			}
		}

		void ListIntKeys()
		{
			for (var i = 1; i < NativeMethods.lpGetIntKeyListCount(); i++)
			{
				var key = NativeMethods.lpGetIntKeyListEntry(i);
				var type = (LuaType)NativeMethods.lpGetIntKeyType(key);
				Debug.WriteLine(key + " " + type);
			}
		}

		enum LuaType
		{
			None = -1,
			Nil = 0,
			Boolean = 1,
			LightUserData = 2,
			Number = 3,
			String = 4,
			Table = 5,
			Function = 6,
			Userdata = 7,
			Thread = 9,
		}

		UnitInfo[] ReadUnits()
		{
			var unitInfos = new List<UnitInfo>();

			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			if (!NativeMethods.lpOpenFile("gamedata/defs.lua", VfsMode.Mod, VfsMode.Mod)) throw new UnitSyncException("Error parsing defs.lua: " + NativeMethods.lpErrorLog());
			SetLoadingStatus("Loading Mod (UnitDefs)");
			if (!NativeMethods.lpExecute()) throw new UnitSyncException("Unable to read  defs.lua: " + NativeMethods.lpErrorLog());
			if (!NativeMethods.lpSubTableStr("unitdefs")) throw new UnitSyncException(); // push unitdefs

			for (var unitKeyIndex = 0; unitKeyIndex < NativeMethods.lpGetStrKeyListCount(); unitKeyIndex++)
			{
				var unitName = NativeMethods.lpGetStrKeyListEntry(unitKeyIndex);
				if (!NativeMethods.lpSubTableStr(unitName)) throw new UnitSyncException(); // push unitdef
				var unitInfo = new UnitInfo();
				unitInfo.Name = unitName;
				SetLoadingStatus(string.Format("Loading Mod (Unit: {0})", unitName));
				if (NativeMethods.lpGetKeyExistsStr("name"))
				{
					unitInfo.FullName = NativeMethods.lpGetStrKeyStrVal("name", String.Empty);
				}
				if (NativeMethods.lpGetKeyExistsStr("buildpic"))
				{
					unitInfo.BuildPicField = NativeMethods.lpGetStrKeyStrVal("buildpic", String.Empty);
				}
				unitInfo.FootprintX = NativeMethods.lpGetStrKeyIntVal("footprintx", 1);
				unitInfo.FootprintY = NativeMethods.lpGetStrKeyIntVal("footprintz", 1);

				if (NativeMethods.lpGetKeyExistsStr("customparams"))
				{
					if (!NativeMethods.lpSubTableStr("customparams")) throw new UnitSyncException(); // push customparams
					if (NativeMethods.lpGetKeyExistsStr("hideinmissioneditor"))
					{
						var type = (LuaType) NativeMethods.lpGetStrKeyType("hideinmissioneditor");
						if (type == LuaType.Boolean)
						{
							unitInfo.Hide = NativeMethods.lpGetStrKeyBoolVal("hideinmissioneditor", 0);
						} 
						else if (type == LuaType.String)
						{
							unitInfo.Hide = NativeMethods.lpGetStrKeyStrVal("hideinmissioneditor", String.Empty) != "false";
						} 
						else
						{
							unitInfo.Hide = true;
						}
					}
					NativeMethods.lpPopTable(); // pop customparams
				}

				if (NativeMethods.lpGetKeyExistsStr("buildoptions"))
				{
					if (NativeMethods.lpGetKeyExistsStr("yardmap")) unitInfo.IsFactory = true;
					if (!NativeMethods.lpSubTableStr("buildoptions")) throw new UnitSyncException(); // push buildoptions
					var unitList = new List<string>();
					for (var buildOptionIndex = 1; buildOptionIndex < NativeMethods.lpGetIntKeyListCount(); buildOptionIndex++)
					{
						var key = NativeMethods.lpGetIntKeyListEntry(buildOptionIndex);
						var buildOption = NativeMethods.lpGetIntKeyStrVal(key, String.Empty);
						unitList.Add(buildOption);
					}
					NativeMethods.lpPopTable(); // pop buildoptions
					unitInfo.BuildOptions = unitList;
				}
				// lazy hide: pretend the unit does not exist
				if (!unitInfo.Hide) unitInfos.Add(unitInfo);
				NativeMethods.lpPopTable(); // pop unitdef
			}
			NativeMethods.lpPopTable(); // pop unitdefs
			NativeMethods.lpClose();
			foreach (var unitInfo in unitInfos) GetBuildPic(unitInfo);
			return unitInfos.ToArray();
		}

		static void TestMapInfo(MapInfo mapInfo)
		{
			if (mapInfo.description == null) throw new UnitSyncException("Null description.");
			if (mapInfo.description.StartsWith("Parse error"))
				throw new UnitSyncException("Parse error. This usually means the map is broken.");
			if (mapInfo.description.EndsWith("not found"))
				throw new UnitSyncException("Map file not found. This usually means the map is broken.");
			if (mapInfo.width <= 1 || mapInfo.height <= 1)
				throw new UnitSyncException(String.Format("Invalid map size. ({0}, {1})", mapInfo.width, mapInfo.height));
		}

		void ThrowError()
		{
			var error = NativeMethods.GetNextError();
			if (error != null) throw new UnitSyncException(error);
		}

		void TraceErrors()
		{
			var error = NativeMethods.GetNextError();
			while (error != null)
			{
				Debug.WriteLine("Unitsync error: " + error.TrimEnd());
				error = NativeMethods.GetNextError();
			}
		}

	}
}