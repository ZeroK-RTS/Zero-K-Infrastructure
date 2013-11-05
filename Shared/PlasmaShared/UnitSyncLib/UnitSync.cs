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
using System.Xml.Serialization;
using ZkData;

namespace PlasmaShared.UnitSyncLib
{
	public partial class UnitSync: IDisposable
	{
		const int AuthorBufferSize = 200;
		const int DefaultMapInfoVersion = 1;
		const int DescriptionBufferSize = 256;
		const int MaxMipLevel = 10;
		const int MaxUnits = 2000;
		bool disposed;
		int? loadedArchiveIndex;
		Dictionary<uint, string> maps;
		readonly string originalDirectory;
        //readonly string originalEnvironmentVariable;

		public static string[] DependencyExceptions = new[]
		                                              {
		                                              	"Spring Bitmaps", "Spring Cursors", "Map Helper v1", "Spring content v1", "TA Content version 2",
		                                              	"tatextures.sdz", "TA Textures v0.62", "tacontent.sdz", "springcontent.sdz", "cursors.sdz"
		                                              };

        public string Version { get; set; }

        SpringPaths paths;

		public UnitSync(SpringPaths springPaths)
		{
            paths = springPaths;
			//originalDirectory = Directory.GetCurrentDirectory();
            //Getting the directory of this application instead of the non-constant currentDirectory. Reference: http://stackoverflow.com/questions/52797/how-do-i-get-the-path-of-the-assembly-the-code-is-in
            originalDirectory = AppDomain.CurrentDomain.BaseDirectory;
            System.Diagnostics.Trace.TraceInformation("UnitSync: Directory: {0}", paths.UnitSyncDirectory);
            System.Diagnostics.Trace.TraceInformation("UnitSync: ZKL: {0}", originalDirectory);           
            Directory.SetCurrentDirectory(paths.UnitSyncDirectory);
            //originalEnvironmentVariable = Environment.GetEnvironmentVariable("SPRING_DATADIR", EnvironmentVariableTarget.Process);
            //Environment.SetEnvironmentVariable("SPRING_DATADIR", paths.WritableDirectory, EnvironmentVariableTarget.Process);//no longer needed since SpringPath already set SPRING_DATADIR
		    if (!NativeMethods.Init(false, 666)) throw new UnitSyncException("Unitsync initialization failed.");
			Version = NativeMethods.GetSpringVersion();
            var writ = NativeMethods.GetWritableDataDirectory();
            System.Diagnostics.Trace.TraceInformation("UnitSync Version: {0}", Version);
            //System.Diagnostics.Trace.TraceInformation("UnitSync new SPRING_DATADIR: {0}", paths.WritableDirectory);
            //System.Diagnostics.Trace.TraceInformation("UnitSync original SPRING_DATADIR: {0}", originalEnvironmentVariable);
			TraceErrors();
            System.Diagnostics.Trace.TraceInformation("UnitSync Initialized");
		}

		~UnitSync()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (!disposed)
			{
				Directory.SetCurrentDirectory(originalDirectory);
                //Environment.SetEnvironmentVariable("SPRING_DATADIR", originalEnvironmentVariable, EnvironmentVariableTarget.Process); //restore original path??
				try
				{
					NativeMethods.UnInit();
				}
				catch (DllNotFoundException)
				{
					// do nothing, already thrown on init
				}
				disposed = true;
                System.Diagnostics.Trace.TraceInformation("UnitSync Disposed");
			}
			GC.SuppressFinalize(this);
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

		public Map GetMapNoBitmaps(string mapName, string archiveName)
		{
			if (mapName == null || archiveName == null) return null;
			NativeMethods.RemoveAllArchives();
			var checksum = maps.First(kvp => kvp.Value == mapName).Key;
			var mapInfo = GetMapInfo(checksum);
			var map = new Map(mapName)
			          {
			          	ArchiveName = Path.GetFileName(archiveName),
			          	Checksum = (int)checksum,
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
			return GetInfoMap(mapName, "metal", 1);
		}

		public Bitmap GetMinimap(Map map)
		{
			return FixAspectRatio(map, GetSquareMinimap(map.Name, 0));
		}


		public Mod GetMod(string modName)
		{
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			if (modName == null) throw new ArgumentNullException("modName");
			NativeMethods.RemoveAllArchives();
			NativeMethods.GetPrimaryModCount(); // pre-requisite for the following calls
			var archiveName = GetModArchiveName(modName);
			NativeMethods.AddAllArchives(archiveName);
			var modIndex = NativeMethods.GetPrimaryModIndex(modName);
			string[] sides;
      
			var mod = new Mod
			          {
			          	Name = modName,
			          	ArchiveName = archiveName,
			          	UnitDefs = GetUnitList(modName).Select(ui => new UnitInfo(ui.Name, ui.FullName)).ToArray(),
			          	Description = NativeMethods.GetPrimaryModDescription(modIndex),
			          	Game = NativeMethods.GetPrimaryModGame(modIndex),
			          	Mutator = NativeMethods.GetPrimaryModMutator(modIndex),
			          	ShortGame = NativeMethods.GetPrimaryModShortGame(modIndex),
			          	ShortName = NativeMethods.GetPrimaryModShortName(modIndex),
			          	PrimaryModVersion = NativeMethods.GetPrimaryModVersion(modIndex),
			          	StartUnits = new SerializableDictionary<string, string>(GetStartUnits(modName, out sides)),
			          	Sides = sides,
			          	Checksum = (int)NativeMethods.GetPrimaryModChecksumFromName(modName),
			          	Options = GetModOptions(archiveName).ToArray(),
			          	SideIcons = GetSideIcons(sides).ToArray(),
			          	Dependencies = GetModDependencies(modIndex).Where(x => x != modName && !string.IsNullOrEmpty(x)).ToArray(),
			          	ModAis = GetAis().Where(ai => ai.IsLuaAi).ToArray()
			          };

            System.Diagnostics.Trace.TraceInformation("Mod Information: Description {0}, Game {1}, Mutator {2}, ShortGame {3}, PrimaryModVersion {4}", mod.Description, mod.Game, mod.Mutator, mod.ShortGame,mod.PrimaryModVersion);

			var buf = ReadVfsFile(GlobalConst.MissionScriptFileName);
			if (buf != null && buf.Length > 0) mod.MissionScript = Encoding.UTF8.GetString(buf, 0, buf.Length);

			if (!string.IsNullOrEmpty(mod.MissionScript))
			{
				try
				{
					buf = ReadVfsFile(GlobalConst.MissionSlotsFileName);
					var slotString = Encoding.UTF8.GetString(buf, 0, buf.Length);
					var ser = new XmlSerializer(typeof(List<MissionSlot>));
					mod.MissionSlots = (List<MissionSlot>)ser.Deserialize(new StringReader(slotString));
				}
				catch (Exception ex)
				{
					Trace.TraceError("Error reading mission slots from mod {0}: {1}", mod.Name, ex);
				}
			}

			if (mod.Sides.Length == 0) Trace.WriteLine("Mod has no faction");
			if (mod.UnitDefs.Length == 0) Trace.WriteLine("No unit found.");

			NativeMethods.RemoveAllArchives();
			TraceErrors();
			return mod;
		}

		public Mod GetModFromArchive(string archiveName)
		{
			var modName = GetModNameFromArchive(archiveName);
			return modName != null ? GetMod(modName) : null;
		}

		public IEnumerable<string> GetModNames()
		{
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			for (var i = 0; i < NativeMethods.GetPrimaryModCount(); i++) yield return NativeMethods.GetPrimaryModName(i);
		}

		/// <summary>
		/// Use when processing a new archive
		/// </summary>
		public void Reset()
		{
			NativeMethods.RemoveAllArchives();
		}

		/// <summary>
		/// Obtain the search handle with FindFilesInVfs or GetFilesInVfsDirectory
		/// </summary>
		IEnumerable<string> CompleteFindFilesInVfs(int searchHandle)
		{
			const int MaxNamebuffer = 255;
			var nameBuffer = new String(' ', MaxNamebuffer);
			while (searchHandle != 0)
			{
				searchHandle = NativeMethods.FindFilesVFS(searchHandle, nameBuffer, MaxNamebuffer);
				yield return nameBuffer.Trim();
				TraceErrors();
			}
		}

		/// <summary>
		/// Call AddAllArchives before this
		/// </summary>
		IEnumerable<string> FindFilesInVfs(string pattern)
		{
			var searchHandle = NativeMethods.InitFindVFS(pattern);
			return CompleteFindFilesInVfs(searchHandle);
		}

		static Bitmap FixAspectRatio(Map map, Image squareMinimap)
		{
			var newSize = map.Size.Width > map.Size.Height
			              	? new Size(squareMinimap.Width, (int)(squareMinimap.Height/((float)map.Size.Width/map.Size.Height)))
			              	: new Size((int)(squareMinimap.Width*((float)map.Size.Width/map.Size.Height)), squareMinimap.Height);

			var correctMinimap = new Bitmap(newSize.Width, newSize.Height, PixelFormat.Format24bppRgb);
			using (var graphics = Graphics.FromImage(correctMinimap))
			{
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.DrawImage(squareMinimap, new Rectangle(Point.Empty, newSize));
			}
			squareMinimap.Dispose();
			return correctMinimap;
		}

		IEnumerable<AiInfoPair> GetAiInfo(int aiIndex)
		{
			for (var i = 0; i < NativeMethods.GetSkirmishAIInfoCount(aiIndex); i++)
			{
				yield return
					new AiInfoPair { Key = NativeMethods.GetInfoKey(i), Value = NativeMethods.GetInfoValueString(i), Description = NativeMethods.GetInfoDescription(i) };
				TraceErrors();
			}
		}

		IEnumerable<Option> GetAiOptions(int aiIndex)
		{
			for (var i = 0; i < NativeMethods.GetSkirmishAIOptionCount(aiIndex); i++)
			{
				yield return LoadOption(i);
				TraceErrors();
			}
		}

	  public IEnumerable<Ai> GetAis()
		{
			for (var i = 0; i < NativeMethods.GetSkirmishAICount(); i++) yield return new Ai { Info = GetAiInfo(i).ToArray(), Options = GetAiOptions(i).ToArray() };
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
		IEnumerable<string> GetFilesInVfsDirectory(string folder, string pattern)
		{
			var searchHandle = NativeMethods.InitDirListVFS(folder, pattern, null);
			return CompleteFindFilesInVfs(searchHandle);
		}

		unsafe Bitmap GetInfoMap(string mapName, string name, int bytesPerPixel)
		{
			var width = 0;
			var height = 0;
			if (!NativeMethods.GetInfoMapSize(mapName, name, ref width, ref height)) System.Diagnostics.Trace.TraceInformation("GetInfoMapSize failed");//ignore negative return
			var infoMapData = new byte[width*height];
			var infoMapHandle = GCHandle.Alloc(infoMapData, GCHandleType.Pinned);
			try
			{
				var infoMapPointer = Marshal.UnsafeAddrOfPinnedArrayElement(infoMapData, 0);
				var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
				if (!NativeMethods.GetInfoMap(mapName, name, infoMapPointer, bytesPerPixel)) throw new UnitSyncException("GetInfoMap failed");
				var bitmapData = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, bitmap.PixelFormat);
				const int PixelSize = 3;
				var p = (byte*)bitmapData.Scan0;
				for (var i = 0; i < infoMapData.Length; i++)
				{
					var v = infoMapData[i];
					var d = i/width*bitmapData.Stride + i%width*PixelSize;
					p[d] = p[d + 1] = p[d + 2] = v;
				}
				bitmap.UnlockBits(bitmapData);
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
			if (!new[] { 0, 1 }.Contains(mapInfoVersion)) throw new ArgumentOutOfRangeException("mapInfoVersion", "must be 0 or 1.");
			if (!GetMapHashes().ContainsValue(mapName)) throw new UnitSyncException(String.Format("Map not found ({0}).", mapName));
			var mapInfo = new MapInfo { author = new String(' ', AuthorBufferSize), description = new String(' ', DescriptionBufferSize) };
			if (!NativeMethods.GetMapInfoEx(mapName, ref mapInfo, mapInfoVersion)) throw new UnitSyncException("Error getting map information.");
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
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			if (archiveName == null) throw new ArgumentNullException("archiveName");
			NativeMethods.AddAllArchives(archiveName);
			NativeMethods.AddAllArchives("maphelper.sdz");
			var optionCount = NativeMethods.GetMapOptionCount(mapName);
			for (var optionIndex = 0; optionIndex < optionCount; optionIndex++)
			{
				var option = LoadOption(optionIndex);
				if (option != null) yield return option;
				TraceErrors();
			}
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
			for (var i = 0; i < count; i++) ret.Add(GetModNameFromArchive(Path.GetFileName(NativeMethods.GetPrimaryModArchiveList(i))));
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
			NativeMethods.AddAllArchives(archiveName);
			var optionCount = NativeMethods.GetModOptionCount();
			for (var optionIndex = 0; optionIndex < optionCount; optionIndex++)
			{
				var option = LoadOption(optionIndex);
				if (option != null) yield return option;
				TraceErrors();
			}
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
			if (!GetMapHashes().ContainsValue(mapName)) throw new UnitSyncException(string.Format("Map not found ({0}).", mapName));
			if (mipLevel < 0 || mipLevel > MaxMipLevel) throw new ArgumentOutOfRangeException("mipLevel", string.Format("Mip level must range from 0 to {0}.", MaxMipLevel));

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
					Trace.TraceWarning("UnitSync Error: " + error);
					break;
				}
			}
			for (var i = 0; i < NativeMethods.GetUnitCount(); i++) yield return new UnitInfo(NativeMethods.GetUnitName(i), NativeMethods.GetFullUnitName(i));
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
			             	Type = (OptionType)NativeMethods.GetOptionType(index),
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
		byte[] ReadVfsFile(string filePath)
		{
			if (filePath == null) throw new ArgumentNullException("filePath");
			if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
			var handle = NativeMethods.OpenFileVFS(filePath);
			try
			{
				if (handle == 0)
				{
					Trace.TraceWarning("File " + filePath + " not found in VFS.");
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
				if (bytesRead != fileSize) Trace.TraceWarning("File size and bytes read mismatch for " + filePath);
				return fileData;
			}
			finally
			{
				if (handle != 0) NativeMethods.CloseFileVFS(handle);
			}
		}

		static void TestMapInfo(MapInfo mapInfo)
		{
			if (mapInfo.description == null) throw new UnitSyncException("Null description.");
			if (mapInfo.description.StartsWith("Parse error")) throw new UnitSyncException("Parse error. This usually means the map is broken.");
			if (mapInfo.description.EndsWith("not found")) throw new UnitSyncException("Map file not found. This usually means the map is broken.");
			if (mapInfo.width <= 1 || mapInfo.height <= 1) throw new UnitSyncException(String.Format("Invalid map size. ({0}, {1})", mapInfo.width, mapInfo.height));
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
				Trace.TraceWarning("Unitsync error: " + error.TrimEnd());
				error = NativeMethods.GetNextError();
			}
		}

		class ArchiveInfo
		{
			public readonly List<string> Dependencies = new List<string>();
			public string FileName;
			public string InternalName;
			public string Replaced;
			public readonly List<string> Replaces = new List<string>();

			public bool MatchesName(string name)
			{
				return InternalName == name || FileName == name || Replaces.Contains(name);
			}

			public override string ToString()
			{
				return InternalName ?? FileName ?? "No Name";
			}
		}
	}
}