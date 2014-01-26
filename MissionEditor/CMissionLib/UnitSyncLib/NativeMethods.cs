using System;
using System.Runtime.InteropServices;

namespace CMissionLib.UnitSyncLib
{
    partial class UnitSync
    {
		#region Nested type: NativeMethods

        public static class NativeMethods
        {
            const string UnitSyncName = "unitsync";


            [DllImport(UnitSyncName)]
            public static extern void AddAllArchives([In] [MarshalAs(UnmanagedType.LPStr)] string root);

            [DllImport(UnitSyncName)]
            public static extern void CloseArchive(int archive);

            [DllImport(UnitSyncName)]
            public static extern void CloseArchiveFile(int archive, int handle);

            [DllImport(UnitSyncName)]
            public static extern void CloseFileVFS(int handle);

            [DllImport(UnitSyncName)]
            public static extern int FileSizeVFS(int handle);

			[DllImport(UnitSyncName)]
			public static extern int FindFilesVFS(int handle, [In] [Out] byte[] buffer,
			                                      int size);


            [DllImport(UnitSyncName)]
            public static extern uint GetArchiveChecksum([In] [MarshalAs(UnmanagedType.LPStr)] string arname);

            public static string GetArchivePath(string arname)
            {
                return Marshal.PtrToStringAnsi(RawGetArchivePath(arname));
            }

            public static string GetFullUnitName(int unit)
            {
                return Marshal.PtrToStringAnsi(RawGetFullUnitName(unit));
            }

            [DllImport(UnitSyncName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool GetInfoMap([In] [MarshalAs(UnmanagedType.LPStr)] string filename,
                                                 [In] [MarshalAs(UnmanagedType.LPStr)] string name,
                                                 IntPtr data,
                                                 int typeHint);

            [DllImport(UnitSyncName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool GetInfoMapSize([In] [MarshalAs(UnmanagedType.LPStr)] string filename,
                                                     [In] [MarshalAs(UnmanagedType.LPStr)] string name,
                                                     ref int width,
                                                     ref int height);

            [DllImport(UnitSyncName)]
            public static extern int GetMapArchiveCount([In] [MarshalAs(UnmanagedType.LPStr)] string mapName);

            public static string GetMapArchiveName(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetMapArchiveName(index));
            }

            [DllImport(UnitSyncName)]
            public static extern uint GetMapChecksum(int index);

            [DllImport(UnitSyncName)]
            public static extern int GetMapCount();

            [DllImport(UnitSyncName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool GetMapInfoEx([MarshalAs(UnmanagedType.LPStr)] string name, [In] [Out] ref MapInfo outInfo, int version);

            public static string GetMapName(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetMapName(index));
            }

            [DllImport(UnitSyncName)]
            public static extern int GetMapOptionCount([In] [MarshalAs(UnmanagedType.LPStr)] string mapName);

            [DllImport(UnitSyncName)]
            public static extern IntPtr GetMinimap([In] [MarshalAs(UnmanagedType.LPStr)] string mapName, int mipLevel);


            [DllImport(UnitSyncName)]
            public static extern int GetModOptionCount();

            public static string GetModValidMap(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetModValidMap(index));
            }

            [DllImport(UnitSyncName)]
            public static extern int GetModValidMapCount();

            public static string GetNextError()
            {
                return Marshal.PtrToStringAnsi(RawGetNextError());
            }

            [DllImport(UnitSyncName)]
            public static extern int GetOptionBoolDef(int index);

            public static string GetOptionDesc(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetOptionDesc(index));
            }

            public static string GetOptionKey(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetOptionKey(index));
            }

            [DllImport(UnitSyncName)]
            public static extern int GetOptionListCount(int index);

            public static string GetOptionListDef(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetOptionListDef(index));
            }

            public static string GetOptionListItemDesc(int index, int itemIndex)
            {
                return Marshal.PtrToStringAnsi(RawGetOptionListItemDesc(index, itemIndex));
            }

            public static string GetOptionListItemKey(int index, int itemIndex)
            {
                return Marshal.PtrToStringAnsi(RawGetOptionListItemKey(index, itemIndex));
            }

            public static string GetOptionListItemName(int index, int itemIndex)
            {
                return Marshal.PtrToStringAnsi(RawGetOptionListItemName(index, itemIndex));
            }

            public static string GetOptionName(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetOptionName(index));
            }

            [DllImport(UnitSyncName)]
            public static extern float GetOptionNumberDef(int index);


            [DllImport(UnitSyncName)]
            public static extern float GetOptionNumberMax(int index);


            [DllImport(UnitSyncName)]
            public static extern float GetOptionNumberMin(int index);

            [DllImport(UnitSyncName)]
            public static extern float GetOptionNumberStep(int index);

            public static string GetOptionScope(int optIndex)
            {
                return Marshal.PtrToStringAnsi(RawGetOptionScope(optIndex));
            }

            public static string GetOptionSection(int optIndex)
            {
                return Marshal.PtrToStringAnsi(RawGetOptionSection(optIndex));
            }

            public static string GetOptionStringDef(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetOptionStringDef(index));
            }

            [DllImport(UnitSyncName)]
            public static extern int GetOptionStringMaxLen(int index);

            public static string GetOptionStyle(int optIndex)
            {
                return Marshal.PtrToStringAnsi(RawGetOptionStyle(optIndex));
            }

            [DllImport(UnitSyncName)]
            public static extern int GetOptionType(int index);

            public static string GetPrimaryModArchive(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetPrimaryModArchive(index));
            }

            public static string GetInfoType(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetInfoType(index));
            }

            [DllImport(UnitSyncName)]
            public static extern int GetPrimaryModArchiveCount(int index);

            public static string GetPrimaryModArchiveList(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetPrimaryModArchiveList(index));
            }

            [DllImport(UnitSyncName)]
            public static extern uint GetPrimaryModChecksum(int index);

            [DllImport(UnitSyncName)]
            public static extern uint GetPrimaryModChecksumFromName([MarshalAs(UnmanagedType.LPStr)] string name);

            [DllImport(UnitSyncName)]
            public static extern int GetPrimaryModCount();

            public static string GetPrimaryModDescription(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetPrimaryModDescription(index));
            }

            public static string GetPrimaryModGame(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetPrimaryModGame(index));
            }

            [DllImport(UnitSyncName)]
            public static extern int GetPrimaryModIndex([MarshalAs(UnmanagedType.LPStr)] string name);

            public static string GetPrimaryModMutator(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetPrimaryModMutator(index));
            }

            [DllImport(UnitSyncName)]
            public static extern int GetPrimaryModInfoCount(int index);

            public static string GetPrimaryModName(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetPrimaryModName(index));  // deprecated

                /*  this is the not-deprecated way to do it, but if it ain't broke don't fix it
                int infoKeyCount = GetPrimaryModInfoCount(index);
                for (int infoKeyIndex=0; infoKeyIndex<infoKeyCount; ++infoKeyIndex)
                {
                    string infoKeyType = GetInfoType(infoKeyIndex);
                    string infoKeyName = GetInfoKey(infoKeyIndex);
                    if (infoKeyType == "string" && infoKeyName == "name")
                    {
                        return GetInfoValueString(infoKeyIndex);
                    }
                }
                return null;
                */
            }

            public static string GetPrimaryModShortGame(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetPrimaryModShortGame(index));
            }

            public static string GetPrimaryModShortName(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetPrimaryModShortName(index));
            }

            public static string GetPrimaryModVersion(int index)
            {
                return Marshal.PtrToStringAnsi(RawGetPrimaryModVersion(index));
            }

            [DllImport(UnitSyncName)]
            public static extern int GetSideCount();

            public static string GetSideName(int side)
            {
                return Marshal.PtrToStringAnsi(RawGetSideName(side));
            }

            public static string GetSideStartUnit(int side)
            {
                return Marshal.PtrToStringAnsi(RawGetSideStartUnit(side));
            }

            [DllImport(UnitSyncName)]
            public static extern IntPtr GetSpringConfigFile();

            [DllImport(UnitSyncName)]
            public static extern float GetSpringConfigFloat([In] [MarshalAs(UnmanagedType.LPStr)] string name, float defValue);

            [DllImport(UnitSyncName)]
            public static extern int GetSpringConfigInt([In] [MarshalAs(UnmanagedType.LPStr)] string name, int defValue);

            [DllImport(UnitSyncName)]
            [return: MarshalAs(UnmanagedType.LPStr)]
            public static extern string GetSpringConfigString([In] [MarshalAs(UnmanagedType.LPStr)] string name,
                                                              [In] [MarshalAs(UnmanagedType.LPStr)] string defValue);

            public static string GetSpringVersion()
            {
                return Marshal.PtrToStringAnsi(RawGetSpringVersion());
            }

            [DllImport(UnitSyncName)]
            public static extern int GetUnitCount();

            public static string GetUnitName(int unit)
            {
                return Marshal.PtrToStringAnsi(RawGetUnitName(unit));
            }

            public static string GetWritableDataDirectory()
            {
                return Marshal.PtrToStringAnsi(RawGetWritableDataDirectory());
            }

            [DllImport(UnitSyncName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool Init([MarshalAs(UnmanagedType.I1)] bool isServer, int id);

            [DllImport(UnitSyncName)]
            public static extern int InitDirListVFS([In] [MarshalAs(UnmanagedType.LPStr)] string path,
                                                    [In] [MarshalAs(UnmanagedType.LPStr)] string pattern,
                                                    [In] [MarshalAs(UnmanagedType.LPStr)] string modes);

            [DllImport(UnitSyncName)]
            public static extern int InitFindVFS([In] [MarshalAs(UnmanagedType.LPStr)] string pattern);

            [DllImport(UnitSyncName)]
            public static extern int InitSubDirsVFS([In] [MarshalAs(UnmanagedType.LPStr)] string path,
                                                    [In] [MarshalAs(UnmanagedType.LPStr)] string pattern,
                                                    [In] [MarshalAs(UnmanagedType.LPStr)] string modes);

            [DllImport(UnitSyncName)]
            public static extern void lpAddIntKeyBoolVal(int key, int val);


            [DllImport(UnitSyncName)]
            public static extern void lpAddIntKeyFloatVal(int key, float val);

            [DllImport(UnitSyncName)]
            public static extern void lpAddIntKeyIntVal(int key, int val);

            [DllImport(UnitSyncName)]
            public static extern void lpAddIntKeyStrVal(int key, [In] [MarshalAs(UnmanagedType.LPStr)] string val);

            [DllImport(UnitSyncName)]
            public static extern void lpAddStrKeyBoolVal([In] [MarshalAs(UnmanagedType.LPStr)] string key, int val);

            [DllImport(UnitSyncName)]
            public static extern void lpAddStrKeyFloatVal([In] [MarshalAs(UnmanagedType.LPStr)] string key, float val);

            [DllImport(UnitSyncName)]
            public static extern void lpAddStrKeyIntVal([In] [MarshalAs(UnmanagedType.LPStr)] string key, int val);


            [DllImport(UnitSyncName)]
            public static extern void lpAddStrKeyStrVal([In] [MarshalAs(UnmanagedType.LPStr)] string key,
                                                        [In] [MarshalAs(UnmanagedType.LPStr)] string val);
            [DllImport(UnitSyncName)]
            public static extern void lpAddTableInt(int key, int @override);


            [DllImport(UnitSyncName)]
            public static extern void lpAddTableStr([In] [MarshalAs(UnmanagedType.LPStr)] string key, int @override);

            [DllImport(UnitSyncName)]
            public static extern void lpClose();

            [DllImport(UnitSyncName)]
            public static extern void lpEndTable();

            public static string lpErrorLog()
            {
                return Marshal.PtrToStringAnsi(RawlpErrorLog());
            }

            [DllImport(UnitSyncName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool lpExecute();

            [DllImport(UnitSyncName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool lpGetIntKeyBoolVal(int key, int defVal);

            [DllImport(UnitSyncName)]
            public static extern float lpGetIntKeyFloatVal(int key, float defVal);

            [DllImport(UnitSyncName)]
            public static extern int lpGetIntKeyIntVal(int key, int defVal);


            [DllImport(UnitSyncName)]
            public static extern int lpGetIntKeyListCount();

            [DllImport(UnitSyncName)]
            public static extern int lpGetIntKeyListEntry(int index);

            public static string lpGetIntKeyStrVal(int key, [In] [MarshalAs(UnmanagedType.LPStr)] string defVal)
            {
                return Marshal.PtrToStringAnsi(RawlpGetIntKeyStrVal(key, defVal));
            }

            [DllImport(UnitSyncName)]
            public static extern int lpGetIntKeyType(int key);

            [DllImport(UnitSyncName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool lpGetKeyExistsInt(int key);

            [DllImport(UnitSyncName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool lpGetKeyExistsStr([In] [MarshalAs(UnmanagedType.LPStr)] string key);

            [DllImport(UnitSyncName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool lpGetStrKeyBoolVal([In] [MarshalAs(UnmanagedType.LPStr)] string key, int defVal);

            [DllImport(UnitSyncName)]
            public static extern float lpGetStrKeyFloatVal([In] [MarshalAs(UnmanagedType.LPStr)] string key, float defVal);

            [DllImport(UnitSyncName)]
            public static extern int lpGetStrKeyIntVal([In] [MarshalAs(UnmanagedType.LPStr)] string key, int defVal);

            [DllImport(UnitSyncName)]
            public static extern int lpGetStrKeyListCount();

            public static string lpGetStrKeyListEntry(int index)
            {
                return Marshal.PtrToStringAnsi(RawlpGetStrKeyListEntry(index));
            }


            public static string lpGetStrKeyStrVal(string key, string defVal)
            {
                return Marshal.PtrToStringAnsi(RawlpGetStrKeyStrVal(key, defVal));
            }


            [DllImport(UnitSyncName)]
            public static extern int lpGetStrKeyType([In] [MarshalAs(UnmanagedType.LPStr)] string key);

            [DllImport(UnitSyncName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool lpOpenFile([In] [MarshalAs(UnmanagedType.LPStr)] string filename,
                                                 [In] [MarshalAs(UnmanagedType.LPStr)] string fileModes,
                                                 [In] [MarshalAs(UnmanagedType.LPStr)] string accessModes);

            [DllImport(UnitSyncName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool lpOpenSource([In] [MarshalAs(UnmanagedType.LPStr)] string source,
                                                   [In] [MarshalAs(UnmanagedType.LPStr)] string accessModes);

            [DllImport(UnitSyncName)]
            public static extern void lpPopTable();

            [DllImport(UnitSyncName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool lpRootTable();

            [DllImport(UnitSyncName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool lpRootTableExpr([In] [MarshalAs(UnmanagedType.LPStr)] string expr);

            [DllImport(UnitSyncName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool lpSubTableExpr([In] [MarshalAs(UnmanagedType.LPStr)] string expr);

            [DllImport(UnitSyncName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool lpSubTableInt(int key);

            [DllImport(UnitSyncName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool lpSubTableStr([In] [MarshalAs(UnmanagedType.LPStr)] string key);

            [DllImport(UnitSyncName)]
            public static extern int OpenArchive([In] [MarshalAs(UnmanagedType.LPStr)] string name);

            [DllImport(UnitSyncName)]
            public static extern int OpenArchiveFile(int archive, [In] [MarshalAs(UnmanagedType.LPStr)] string name);

            [DllImport(UnitSyncName)]
            public static extern int OpenFileVFS([In] [MarshalAs(UnmanagedType.LPStr)] string name);

            [DllImport(UnitSyncName)]
            public static extern int ProcessUnits();

            [DllImport(UnitSyncName)]
            public static extern int ProcessUnitsNoChecksum();

            [DllImport(UnitSyncName, EntryPoint = "lpGetStrKeyStrVal")]
            static extern IntPtr RawlpGetStrKeyStrVal([In] [MarshalAs(UnmanagedType.LPStr)] string key,
                                                             [In] [MarshalAs(UnmanagedType.LPStr)] string defVal);

            [DllImport(UnitSyncName)]
            public static extern int ReadArchiveFile(int archive, int handle, IntPtr buffer, int numBytes);

            [DllImport(UnitSyncName)]
            public static extern int ReadFileVFS(int handle, [In] [Out] [MarshalAs(UnmanagedType.LPArray)] byte[] buffer, int length);

            [DllImport(UnitSyncName)]
            public static extern void RemoveAllArchives();

            [DllImport(UnitSyncName)]
            public static extern void SetSpringConfigFile([In] [MarshalAs(UnmanagedType.LPStr)] string filenameAsAbsolutePath);

            [DllImport(UnitSyncName)]
            public static extern void SetSpringConfigFloat([In] [MarshalAs(UnmanagedType.LPStr)] string name, float value);

            [DllImport(UnitSyncName)]
            public static extern void SetSpringConfigInt([In] [MarshalAs(UnmanagedType.LPStr)] string name, int value);

            [DllImport(UnitSyncName)]
            public static extern void SetSpringConfigString([In] [MarshalAs(UnmanagedType.LPStr)] string name,
                                                            [In] [MarshalAs(UnmanagedType.LPStr)] string value);

            [DllImport(UnitSyncName)]
            public static extern int SizeArchiveFile(int archive, int handle);

            [DllImport(UnitSyncName)]
            public static extern void UnInit();

            [DllImport(UnitSyncName, EntryPoint = "GetArchivePath")]
            static extern IntPtr RawGetArchivePath([In] [MarshalAs(UnmanagedType.LPStr)] string arname);

            [DllImport(UnitSyncName, EntryPoint = "GetFullUnitName")]
            static extern IntPtr RawGetFullUnitName(int unit);

            [DllImport(UnitSyncName, EntryPoint = "GetMapArchiveName")]
            static extern IntPtr RawGetMapArchiveName(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetMapName")]
            static extern IntPtr RawGetMapName(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetModValidMap")]
            static extern IntPtr RawGetModValidMap(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetNextError")]
            static extern IntPtr RawGetNextError();

            [DllImport(UnitSyncName, EntryPoint = "GetOptionDesc")]
            static extern IntPtr RawGetOptionDesc(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetOptionKey")]
            static extern IntPtr RawGetOptionKey(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetOptionListDef")]
            static extern IntPtr RawGetOptionListDef(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetOptionListItemDesc")]
            static extern IntPtr RawGetOptionListItemDesc(int index, int itemIndex);

            [DllImport(UnitSyncName, EntryPoint = "GetOptionListItemKey")]
            static extern IntPtr RawGetOptionListItemKey(int index, int itemIndex);

            [DllImport(UnitSyncName, EntryPoint = "GetOptionListItemName")]
            static extern IntPtr RawGetOptionListItemName(int index, int itemIndex);

            [DllImport(UnitSyncName, EntryPoint = "GetOptionName")]
            static extern IntPtr RawGetOptionName(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetOptionScope")]
            static extern IntPtr RawGetOptionScope(int optIndex);

            [DllImport(UnitSyncName, EntryPoint = "GetOptionSection")]
            static extern IntPtr RawGetOptionSection(int optIndex);

            [DllImport(UnitSyncName, EntryPoint = "GetOptionStringDef")]
            static extern IntPtr RawGetOptionStringDef(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetOptionStyle")]
            static extern IntPtr RawGetOptionStyle(int optIndex);

            [DllImport(UnitSyncName, EntryPoint = "GetInfoType")]
            static extern IntPtr RawGetInfoType(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetPrimaryModArchive")]
            static extern IntPtr RawGetPrimaryModArchive(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetPrimaryModArchiveList")]
            static extern IntPtr RawGetPrimaryModArchiveList(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetPrimaryModDescription")]
            static extern IntPtr RawGetPrimaryModDescription(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetPrimaryModGame")]
            static extern IntPtr RawGetPrimaryModGame(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetPrimaryModMutator")]
            static extern IntPtr RawGetPrimaryModMutator(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetPrimaryModName")]
            static extern IntPtr RawGetPrimaryModName(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetPrimaryModShortGame")]
            static extern IntPtr RawGetPrimaryModShortGame(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetPrimaryModShortName")]
            static extern IntPtr RawGetPrimaryModShortName(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetPrimaryModVersion")]
            static extern IntPtr RawGetPrimaryModVersion(int index);

            [DllImport(UnitSyncName, EntryPoint = "GetSideName")]
            static extern IntPtr RawGetSideName(int side);

            [DllImport(UnitSyncName, EntryPoint = "GetSideStartUnit")]
            static extern IntPtr RawGetSideStartUnit(int side);

            [DllImport(UnitSyncName, EntryPoint = "GetSpringVersion")]
            static extern IntPtr RawGetSpringVersion();

            [DllImport(UnitSyncName, EntryPoint = "GetUnitName")]
            static extern IntPtr RawGetUnitName(int unit);

            [DllImport(UnitSyncName, EntryPoint = "GetWritableDataDirectory")]
            static extern IntPtr RawGetWritableDataDirectory();

            [DllImport(UnitSyncName, EntryPoint = "lpErrorLog")]
            static extern IntPtr RawlpErrorLog();

            [DllImport(UnitSyncName, EntryPoint = "lpGetIntKeyStrVal")]
            static extern IntPtr RawlpGetIntKeyStrVal(int key, [In] [MarshalAs(UnmanagedType.LPStr)] string defVal);

            [DllImport(UnitSyncName, EntryPoint = "lpGetStrKeyListEntry")]
            static extern IntPtr RawlpGetStrKeyListEntry(int index);

            [DllImport(UnitSyncName)]
            public static extern int GetSkirmishAICount();

            [DllImport(UnitSyncName)]
            public static extern int GetSkirmishAIInfoCount(int aiIndex);

            [DllImport(UnitSyncName)]
            public static extern int GetSkirmishAIOptionCount(int aiIndex);


            [DllImport(UnitSyncName, EntryPoint = "GetInfoKey")]
            static extern IntPtr RawGetInfoKey(int infoIndex);

            public static string GetInfoKey(int infoIndex)
            {
                return Marshal.PtrToStringAnsi(RawGetInfoKey(infoIndex));
            }

            [DllImport(UnitSyncName, EntryPoint = "GetInfoValueString")]
            static extern IntPtr RawGetInfoValueString(int infoIndex);

            public static string GetInfoValueString(int infoIndex)
            {
                return Marshal.PtrToStringAnsi(RawGetInfoValueString(infoIndex));
            }

			// FIXME: deprecated
			[DllImport(UnitSyncName, EntryPoint = "GetInfoValue")]
			static extern IntPtr RawGetInfoValue(int infoIndex);

			public static string GetInfoValue(int infoIndex)
			{
				return Marshal.PtrToStringAnsi(RawGetInfoValue(infoIndex));
			}
            
            [DllImport(UnitSyncName, EntryPoint = "GetInfoDescription")]
            static extern IntPtr RawGetInfoDescription(int infoIndex);

            public static string GetInfoDescription(int infoIndex)
            {
                return Marshal.PtrToStringAnsi(RawGetInfoDescription(infoIndex));
            }
        }

		#endregion
    }
}