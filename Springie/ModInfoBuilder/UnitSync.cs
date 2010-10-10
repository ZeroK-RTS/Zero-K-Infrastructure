using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using PlasmaShared.UnitSyncLib;

namespace Springie.SpringNamespace
{
	public class UnitSync : IDisposable
	{
		#region Fields

		private Dictionary<string, Map> mapList = new Dictionary<string, Map>();
		private Dictionary<string, Mod> modList = new Dictionary<string, Mod>();
		private string path;

		#endregion

		#region Properties

		public Dictionary<string, Map> MapList
		{
			get { return mapList; }
		}

		public Dictionary<string, Mod> ModList
		{
			get { return modList; }
		}

		#endregion

		#region Constructors

		public UnitSync() : this(Directory.GetCurrentDirectory()) {}

		public UnitSync(string path)
		{
			this.path = path;
			string opath = Directory.GetCurrentDirectory();
			Directory.SetCurrentDirectory(path);
			if (Init(false, 0) != 1) throw new Exception("unitsync.dll init failed");
			//if (InitArchiveScanner() != 1) throw new Exception("unitsync.dll:InitArchiveScanner() failed");
			LoadModList();
			LoadMapList();
			Directory.SetCurrentDirectory(opath);
		}

		public void Dispose()
		{
			UnInit();
		}

		#endregion

		#region Public methods

		public Map GetMapInfo(string name)
		{
			return mapList[name];
		}

		public Mod GetModInfo(string name)
		{
			if (modList.ContainsKey(name)) return modList[name];
			else {
				foreach (var p in modList) if (p.Value.ArchiveName == name) return p.Value;
				return null;
			}
		}

		public bool HasMap(string name)
		{
			return mapList.ContainsKey(name);
		}


		public bool HasMod(string modName)
		{
			if (!modList.ContainsKey(modName)) {
				foreach (var p in modList) if (p.Value.ArchiveName == modName) return true;
				return false;
			} else return true;
		}

		/// <summary>
		/// Loads new map information
		/// </summary>

		internal  void LoadMapInfo(Map mi, int mapId)
		{
			string opath = Directory.GetCurrentDirectory();
			Directory.SetCurrentDirectory(path);
			ReInit(true);
      
			mi.Checksum = (int)GetMapChecksum(mapId);

			Directory.SetCurrentDirectory(opath);
		}

		internal void LoadModInfo(Mod mi, int ModId)
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			string opath = Directory.GetCurrentDirectory();
			Directory.SetCurrentDirectory(path);
			ReInit(true);

			//GetPrimaryModCount();
			if (mi.Name != GetPrimaryModName(ModId)) throw new Exception("Mod " + mi.Name + " modified without reload");

			//uint result = 0;
			mi.ArchiveName = GetPrimaryModArchive(ModId);

			mi.Checksum = (int)GetPrimaryModChecksum(ModId);

			AddAllArchives(mi.ArchiveName);
			mi.Sides = new String[GetSideCount()];
			for (int x = 0; x < mi.Sides.Length; ++x) mi.Sides[x] = GetSideName(x);

			// weirdest stuff of all...
			while (ProcessUnitsNoChecksum() != 0) {}
			
			mi.Units = new UnitInfo[GetUnitCount()];
			for (int x = 0; x < mi.Units.Length; ++x) mi.Units[x] = new UnitInfo(GetUnitName(x), GetFullUnitName(x));

			int opts = GetModOptionCount();
			for (int x = 0; x < opts; x++) {
				var o = LoadOption(x);
				if (o != null) mi.Options.Add(o);
			}

			Directory.SetCurrentDirectory(opath);
		}

		internal void Reload(bool reloadMods, bool reloadMaps)
		{
			if (reloadMods || reloadMaps) ReInit(true);
			if (reloadMaps) LoadMapList();
			if (reloadMods) LoadModList();
		}

		#endregion

		#region Other methods

		[DllImport("unitsync.dll")]
		private static extern void AddAllArchives(string root);

		[DllImport("unitsync.dll")]
		private static extern uint GetArchiveChecksum(string archive);

		[DllImport("unitsync.dll")]
		private static extern string GetFullUnitName(int index);

		[DllImport("unitsync.dll")]
		private static extern int GetMapArchiveCount(string mapName);

		[DllImport("unitsync.dll")]
		private static extern string GetMapArchiveName(int index);

		[DllImport("unitsync.dll")]
		private static extern int GetMapCount();

		[DllImport("unitsync.dll")]
		private static extern string GetMapName(int index);

		/************************************************************************/
		/*     OPTIONS                                                          */
		/************************************************************************/

		[DllImport("unitsync.dll")]
		private static extern int GetMapOptionCount(string mapName);

		[DllImport("unitsync.dll")]
		private static extern int GetModOptionCount();

		[DllImport("unitsync.dll")]
		private static extern int GetOptionBoolDef(int index);

		[DllImport("unitsync.dll")]
		private static extern string GetOptionDesc(int index);

		[DllImport("unitsync.dll")]
		private static extern string GetOptionKey(int index);

		[DllImport("unitsync.dll")]
		private static extern int GetOptionListCount(int index);

		[DllImport("unitsync.dll")]
		private static extern string GetOptionListDef(int index);

		[DllImport("unitsync.dll")]
		private static extern string GetOptionListItemDesc(int index, int itemIndex);

		[DllImport("unitsync.dll")]
		private static extern string GetOptionListItemKey(int index, int itemIndex);

		[DllImport("unitsync.dll")]
		private static extern string GetOptionListItemName(int index, int itemIndex);

		[DllImport("unitsync.dll")]
		private static extern string GetOptionName(int index);

		[DllImport("unitsync.dll")]
		private static extern float GetOptionNumberDef(int index);

		[DllImport("unitsync.dll")]
		private static extern float GetOptionNumberMax(int index);

		[DllImport("unitsync.dll")]
		private static extern float GetOptionNumberMin(int index);

		[DllImport("unitsync.dll")]
		private static extern float GetOptionNumberStep(int index);

		[DllImport("unitsync.dll")]
		private static extern string GetOptionStringDef(int index);

		[DllImport("unitsync.dll")]
		private static extern int GetOptionStringMaxLen(int index);

		[DllImport("unitsync.dll")]
		private static extern int GetOptionType(int index);

		[DllImport("unitsync.dll")]
		private static extern string GetPrimaryModArchive(int index);

		[DllImport("unitsync.dll")]
		private static extern int GetPrimaryModArchiveCount(int index);

		[DllImport("unitsync.dll")]
		private static extern string GetPrimaryModArchiveList(int index);

		[DllImport("unitsync.dll")]
		private static extern uint GetMapChecksumFromName(string mapName);

		[DllImport("unitsync.dll")]
		private static extern uint GetMapChecksum(int mapIndex);

		[DllImport("unitsync.dll")]
		private static extern uint GetPrimaryModChecksum(int index);

		[DllImport("unitsync.dll")]
		private static extern int GetPrimaryModCount();

		[DllImport("unitsync.dll")]
		private static extern string GetPrimaryModName(int index);

		[DllImport("unitsync.dll")]
		private static extern int GetSideCount();

		[DllImport("unitsync.dll")]
		private static extern string GetSideName(int index);

		[DllImport("unitsync.dll")]
		private static extern int GetUnitCount();

		[DllImport("unitsync.dll")]
		private static extern string GetUnitName(int index);

		/*
    //     INIT
    */

		[DllImport("unitsync.dll")]
		private static extern int Init(bool isServer, int id);

		/// <summary>
		/// Gets map list from unit sync, does not make full reinit by default
		/// </summary>
		/// <returns></returns>
		private void LoadMapList()
		{
			string opath = Directory.GetCurrentDirectory();
			Directory.SetCurrentDirectory(path);

			int mapCount = GetMapCount();
			mapList.Clear();
			for (int i = 0; i < mapCount; ++i) {
				var mi = new Map(GetMapName(i));
				LoadMapInfo(mi, i);
				mapList[mi.Name] = mi;
			}
			Directory.SetCurrentDirectory(opath);
		}


		/// <summary>
		/// Gets mod list - does not make full reinit by default
		/// </summary>
		/// <returns></returns>
		private void LoadModList()
		{
			string opath = Directory.GetCurrentDirectory();
			Directory.SetCurrentDirectory(path);

			int modCount = GetPrimaryModCount();

			modList.Clear();
			for (int i = 0; i < modCount; ++i) {
				var mi = new Mod(GetPrimaryModName(i));
				LoadModInfo(mi, i);
				modList[mi.Name] = mi;
			}
		}

		[DllImport("unitsync.dll")]
		private static extern int ProcessUnitsNoChecksum();

		/// <summary>
		/// ReInits unitsync
		/// </summary>
		/// <param name="full">if true does complete reinit (neccesary to find new map or mod), if false does just archivescannerinit</param>
		private void ReInit(bool full)
		{
			string opath = Directory.GetCurrentDirectory();
			Directory.SetCurrentDirectory(path);

			if (full) {
				UnInit();
				if (Init(false, 0) != 1) throw new Exception("unitsync.dll init failed");
			}
			//if (InitArchiveScanner() != 1) throw new Exception("unitsync.dll:InitArchiveScanner() failed");

			Directory.SetCurrentDirectory(opath);
		}

		private string RequestMapArchive(string mapname)
		{
			int i = GetMapArchiveCount(mapname);
			if (i > 0) {
				string arch = GetMapArchiveName(0);
				int lastslash = arch.LastIndexOfAny(new[] {'/', '\\'});
				return arch.Substring(lastslash + 1);
			} else return "";
		}


		[DllImport("unitsync.dll")]
		private static extern void UnInit();

		#endregion

	

		public Option LoadOption(int idx)
		{
			var o = new Option();

				o.Name = GetOptionName(idx);
				o.Key = GetOptionKey(idx);
				o.Description = GetOptionDesc(idx);
				o.OptionType = (Option.Type)GetOptionType(idx);
				o.strMaxLen = GetOptionStringMaxLen(idx);
				o.min = GetOptionNumberMin(idx);
				o.max = GetOptionNumberMax(idx);
				int listCount = GetOptionListCount(idx);
				for (int i = 0; i < listCount; i++) {
					var optl = LoadListOption(idx, i);
					o.ListOptions.Add(optl);
				}
				switch (o.OptionType)
				{
					case Option.Type.Bool:
						o.Default = GetOptionBoolDef(idx).ToString();
						break;
					case Option.Type.Number:
						o.Default = GetOptionNumberDef(idx).ToString();
						break;
					case Option.Type.String:
						o.Default = GetOptionStringDef(idx);
						break;
					case Option.Type.List:
						o.Default = GetOptionListDef(idx);
						break;
					default:
						return null;
				
				}
			return o;	
		}

		public ListOption LoadListOption(int optionIndex, int itemIndex)
		{
			var lo = new ListOption();
			lo.Name = GetOptionListItemName(optionIndex, itemIndex);
			lo.Description = GetOptionListItemDesc(optionIndex, itemIndex);
			lo.Key = GetOptionListItemKey(optionIndex, itemIndex);
			return lo;
		}

	}
}