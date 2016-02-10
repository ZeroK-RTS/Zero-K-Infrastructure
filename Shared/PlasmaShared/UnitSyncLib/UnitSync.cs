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
using PlasmaShared.UnitSyncLib;

namespace ZkData.UnitSyncLib
{
    public partial class UnitSync: IDisposable
    {
        private const int AuthorBufferSize = 200;
        private const int DefaultMapInfoVersion = 1;
        private const int DescriptionBufferSize = 256;
        private const int MaxMipLevel = 10;
        private const int MaxUnits = 2000;

        public static string[] DependencyExceptions =
        {
            "Spring Bitmaps",
            "Spring Cursors",
            "Map Helper v1",
            "Spring content v1",
            "TA Content version 2",
            "tatextures.sdz",
            "TA Textures v0.62",
            "tacontent.sdz",
            "springcontent.sdz",
            "cursors.sdz"
        };
        private static readonly object unitsyncInitLocker = new object();
        private readonly string originalDirectory;
        private bool disposed;
        private int? loadedArchiveIndex;
        private readonly SpringPaths paths;
        private string unitsyncWritableFolder;

        public UnitSync(SpringPaths springPaths) {
            lock (unitsyncInitLocker)
            {
                paths = springPaths;
                //Getting the directory of this application instead of the non-constant currentDirectory. Reference: http://stackoverflow.com/questions/52797/how-do-i-get-the-path-of-the-assembly-the-code-is-in
                originalDirectory = AppDomain.CurrentDomain.BaseDirectory;
                Trace.TraceInformation("UnitSync: Directory: {0}", paths.UnitSyncDirectory);
                Trace.TraceInformation("UnitSync: ZKL: {0}", originalDirectory);
                Directory.SetCurrentDirectory(paths.UnitSyncDirectory);
                Environment.CurrentDirectory = paths.UnitSyncDirectory;
                var settingsPath = Path.Combine(paths.UnitSyncDirectory, "springsettings.cfg");
                File.WriteAllText(settingsPath, $"SpringData={paths.DataDirectoriesJoined}\n");
                if (!NativeMethods.Init(false, 666)) throw new UnitSyncException("Unitsync initialization failed. " + NativeMethods.GetNextError());

                Version = NativeMethods.GetSpringVersion();
                unitsyncWritableFolder = NativeMethods.GetWritableDataDirectory();
                var read = NativeMethods.GetDataDirectories();
                Trace.TraceInformation("UnitSync version: {0}", Version);
                Trace.TraceInformation("UnitSync READ: {0}", string.Join(",", read));
                Trace.TraceInformation("UnitSync WRITE: {0}", unitsyncWritableFolder);

                TraceErrors();
                Trace.TraceInformation("UnitSync Initialized");

            }
        }

        public string Version { get; set; }

        public void Dispose() {
            if (!disposed)
            {
                try
                {
                    NativeMethods.UnInit();
                }
                catch (DllNotFoundException)
                {
                    // do nothing, already thrown on init
                }
                Directory.SetCurrentDirectory(originalDirectory);
                Environment.CurrentDirectory = originalDirectory;
                disposed = true;
                Trace.TraceInformation("UnitSync Disposed");
            }
            GC.SuppressFinalize(this);
        }


        ~UnitSync() {
            Dispose();
        }

        public Bitmap GetHeightMap(string mapName) {
            return GetInfoMap(mapName, "height", 1);
        }


        private Map GetMap(ResourceInfo ae) {
            var map = GetMapNoBitmaps(ae);
            if (map == null) return map;
            map.Minimap = GetMinimap(map);
            map.Heightmap = GetHeightMap(map.Name);
            map.Metalmap = GetMetalMap(map.Name);
            return map;
        }

        public ResourceInfo GetResourceFromFileName(string filePath) {
            var archiveCache = new ArchiveCache(unitsyncWritableFolder);
            var ae = archiveCache.Archives.FirstOrDefault(x => x.ArchiveName == Path.GetFileName(filePath));
            if (ae == null) return null;
            try
            {
                return GetMap(ae);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Not a map: {0}" ,ex);
            }
            try
            {
                return GetMod(ae);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Not a mod: {0}", ex);
            }
            return ae;
        }


        private Map GetMapNoBitmaps(ResourceInfo ae) {
            NativeMethods.RemoveAllArchives();
            var mapInfo = GetMapInfo(ae, DefaultMapInfoVersion);
            var map = new Map(ae)
            {
                TidalStrength = mapInfo.tidalStrength,
                Gravity = mapInfo.gravity,
                MaxMetal = mapInfo.maxMetal,
                ExtractorRadius = mapInfo.extractorRadius,
                MinWind = mapInfo.minWind,
                MaxWind = mapInfo.maxWind,
                Size = new Size(mapInfo.width, mapInfo.height),
                Positions = mapInfo.positions,
            };
            map.Options = GetMapOptions(map.Name, map.ArchiveName).ToArray();
            NativeMethods.RemoveAllArchives();
            TraceErrors();
            return map;
        }

        public Bitmap GetMetalMap(string mapName) {
            return GetInfoMap(mapName, "metal", 1);
        }

        public Bitmap GetMinimap(Map map) {
            return FixAspectRatio(map, GetSquareMinimap(map.Name, 0));
        }


        public Mod GetMod(ResourceInfo ae) {
            if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
            NativeMethods.RemoveAllArchives();
            NativeMethods.GetPrimaryModCount(); // pre-requisite for the following calls
            NativeMethods.AddAllArchives(ae.Name);
            var modIndex = NativeMethods.GetPrimaryModIndex(ae.Name);
            string[] sides;

            var mod = new Mod(ae)
            {
                UnitDefs = GetUnitList(ae.Name).Select(ui => new UnitInfo(ui.Name, ui.FullName)).ToArray(),
                StartUnits = new SerializableDictionary<string, string>(GetStartUnits(ae.Name, out sides)),
                Sides = sides,
                Options = GetModOptions(ae.ArchiveName).ToArray(),
                SideIcons = GetSideIcons(sides).ToArray(),
                ModAis = GetAis().Where(ai => ai.IsLuaAi).ToArray()
            };

            Trace.TraceInformation(
                "Mod Information: Description {0}, Game {1}, Mutator {2}, ShortGame {3}, PrimaryModVersion {4}",
                mod.Description,
                mod.Game,
                mod.Mutator,
                mod.ShortGame,
                mod.PrimaryModVersion);

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


        public ResourceInfo GetArchiveEntryByInternalName(string name) {
            var archiveCache = new ArchiveCache(unitsyncWritableFolder);
            return archiveCache.Archives.FirstOrDefault(x => x.Name == name);
        }

        /// <summary>
        ///     Use when processing a new archive
        /// </summary>
        public void Reset() {
            NativeMethods.RemoveAllArchives();
        }

        /// <summary>
        ///     Obtain the search handle with FindFilesInVfs or GetFilesInVfsDirectory
        /// </summary>
        private IEnumerable<string> CompleteFindFilesInVfs(int searchHandle) {
            const int MaxNamebuffer = 255;
            var nameBuffer = new string(' ', MaxNamebuffer);
            while (searchHandle != 0)
            {
                searchHandle = NativeMethods.FindFilesVFS(searchHandle, nameBuffer, MaxNamebuffer);
                yield return nameBuffer.Trim();
                TraceErrors();
            }
        }

        /// <summary>
        ///     Call AddAllArchives before this
        /// </summary>
        private IEnumerable<string> FindFilesInVfs(string pattern) {
            var searchHandle = NativeMethods.InitFindVFS(pattern);
            return CompleteFindFilesInVfs(searchHandle);
        }

        private static Bitmap FixAspectRatio(Map map, Image squareMinimap) {
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

        private IEnumerable<AiInfoPair> GetAiInfo(int aiIndex) {
            for (var i = 0; i < NativeMethods.GetSkirmishAIInfoCount(aiIndex); i++)
            {
                yield return
                    new AiInfoPair
                    {
                        Key = NativeMethods.GetInfoKey(i),
                        Value = NativeMethods.GetInfoValueString(i),
                        Description = NativeMethods.GetInfoDescription(i)
                    };
                TraceErrors();
            }
        }

        private IEnumerable<Option> GetAiOptions(int aiIndex) {
            for (var i = 0; i < NativeMethods.GetSkirmishAIOptionCount(aiIndex); i++)
            {
                yield return LoadOption(i);
                TraceErrors();
            }
        }

        public IEnumerable<Ai> GetAis() {
            for (var i = 0; i < NativeMethods.GetSkirmishAICount(); i++) yield return new Ai { Info = GetAiInfo(i).ToArray(), Options = GetAiOptions(i).ToArray() };
        }


        /// <summary>
        ///     Call AddAllArchives before this
        /// </summary>
        private IEnumerable<string> GetFilesInVfsDirectory(string folder, string pattern) {
            var searchHandle = NativeMethods.InitDirListVFS(folder, pattern, null);
            return CompleteFindFilesInVfs(searchHandle);
        }

        private unsafe Bitmap GetInfoMap(string mapName, string name, int bytesPerPixel) {
            var width = 0;
            var height = 0;
            if (!NativeMethods.GetInfoMapSize(mapName, name, ref width, ref height)) Trace.TraceInformation("GetInfoMapSize failed"); //ignore negative return
            var infoMapData = new byte[width*height];
            var infoMapHandle = GCHandle.Alloc(infoMapData, GCHandleType.Pinned);
            try
            {
                var infoMapPointer = Marshal.UnsafeAddrOfPinnedArrayElement(infoMapData, 0);
                var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                if (!NativeMethods.GetInfoMap(mapName, name, infoMapPointer, bytesPerPixel)) throw new UnitSyncException("GetInfoMap " + name + " failed");
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

        private string GetMapArchive(string mapName) {
            var i = NativeMethods.GetMapArchiveCount(mapName);
            if (i <= 0) return null;
            var archivePath = NativeMethods.GetMapArchiveName(0);
            if (archivePath == null) throw new UnitSyncException(NativeMethods.GetNextError());
            return Path.GetFileName(archivePath);
        }


        private MapInfo GetMapInfo(ResourceInfo ae, int mapInfoVersion) {
            if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
            if (!new[] { 0, 1 }.Contains(mapInfoVersion)) throw new ArgumentOutOfRangeException("mapInfoVersion", "must be 0 or 1.");
            var mapInfo = new MapInfo { author = new string(' ', AuthorBufferSize), description = new string(' ', DescriptionBufferSize) };
            if (!NativeMethods.GetMapInfoEx(ae.Name, ref mapInfo, mapInfoVersion)) throw new UnitSyncException("Error getting map information.");
            TestMapInfo(mapInfo);
            return mapInfo;
        }


        private IEnumerable<Option> GetMapOptions(string mapName, string archiveName) {
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


        private IEnumerable<Option> GetModOptions(string archiveName) {
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
        ///     Call AddAllArchives before this
        ///     Icons not found are null
        /// </summary>
        private byte[][] GetSideIcons(IEnumerable<string> sides) {
            return sides.Select(side => ReadVfsFile("SidePics\\" + side + ".bmp")).ToArray();
        }

        private Bitmap GetSquareMinimap(string mapName, int mipLevel) {
            if (mipLevel < 0 || mipLevel > MaxMipLevel) throw new ArgumentOutOfRangeException("mipLevel", string.Format("Mip level must range from 0 to {0}.", MaxMipLevel));

            var size = 1024 >> mipLevel;
            var pointer = NativeMethods.GetMinimap(mapName, mipLevel);
            const PixelFormat format = PixelFormat.Format16bppRgb565;
            var pixelFormatSize = Image.GetPixelFormatSize(format)/8;
            var stride = size*pixelFormatSize;
            return new Bitmap(size, size, stride, format, pointer);
        }


        private Dictionary<string, string> GetStartUnits(string modName, out string[] sides) {
            if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
            var modIndex = NativeMethods.GetPrimaryModIndex(modName);
            if (modIndex < 0) throw new UnitSyncException("Mod not found (" + modName + ").");
            return GetStartUnits(modIndex, out sides);
        }

        private Dictionary<string, string> GetStartUnits(int modIndex, out string[] sides) {
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

        private IEnumerable<UnitInfo> GetUnitList(string modName) {
            var modIndex = NativeMethods.GetPrimaryModIndex(modName);
            if (modIndex < 0) throw new UnitSyncException(string.Format("Mod not found ({0}).", modName));
            return GetUnitList(modIndex);
        }

        private IEnumerable<UnitInfo> GetUnitList(int modIndex) {
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


        private ListOption LoadListOption(int optionIndex, int itemIndex) {
            var listOption = new ListOption
            {
                Name = NativeMethods.GetOptionListItemName(optionIndex, itemIndex),
                Description = NativeMethods.GetOptionListItemDesc(optionIndex, itemIndex),
                Key = NativeMethods.GetOptionListItemKey(optionIndex, itemIndex)
            };
            TraceErrors();
            return listOption;
        }

        private void LoadModArchive(int modIndex) {
            if (disposed) throw new ObjectDisposedException("Unitsync has already been released.");
            if (modIndex == loadedArchiveIndex) return;
            NativeMethods.AddAllArchives(NativeMethods.GetPrimaryModArchive(modIndex));
            loadedArchiveIndex = modIndex;
        }

        private Option LoadOption(int index) {
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
        ///     Call AddAllArchives before this
        /// </summary>
        private byte[] ReadVfsFile(string filePath) {
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

        private static void TestMapInfo(MapInfo mapInfo) {
            if (mapInfo.description == null) throw new UnitSyncException("Null description.");
            if (mapInfo.description.StartsWith("Parse error")) throw new UnitSyncException("Parse error. This usually means the map is broken.");
            if (mapInfo.description.EndsWith("not found")) throw new UnitSyncException("Map file not found. This usually means the map is broken.");
            if (mapInfo.width <= 1 || mapInfo.height <= 1) throw new UnitSyncException(string.Format("Invalid map size. ({0}, {1})", mapInfo.width, mapInfo.height));
        }

        private void ThrowError() {
            var error = NativeMethods.GetNextError();
            if (error != null) throw new UnitSyncException(error);
        }

        private void TraceErrors() {
            var error = NativeMethods.GetNextError();
            while (error != null)
            {
                Trace.TraceWarning("Unitsync error: " + error.TrimEnd());
                error = NativeMethods.GetNextError();
            }
        }
    }
}