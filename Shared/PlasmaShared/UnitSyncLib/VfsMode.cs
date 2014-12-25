namespace ZkData.UnitSyncLib
{
    public static class VfsMode
    {
        public const string All = RawFirst;
        public const string Base = "b"; // currently equivalent to VfsMode.Zip
        public const string Map = "m"; // currently equivalent to VfsMode.Zip
        public const string MapBase = Map + Base;
        public const string Mod = "M"; // currently equivalent to VfsMode.Zip
        public const string ModBase = Mod + Base;
        public const string None = " ";
        public const string Raw = "r";
        public const string RawFirst = Raw + Zip;
        public const string Zip = Mod + Map + Base;
        public const string ZipFirst = Zip + Raw;
    }
}