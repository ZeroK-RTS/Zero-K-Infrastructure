using System.Runtime.InteropServices;
using System;

namespace PlanetWarsShared.UnitSyncLib
{
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct StartPos // If the fields in this class are changed, it might not work with unitsync anymore.
    {
        int x;
        int z;

        public int X
        {
            get { return x; }
            set { x = value; }
        }

        public int Z
        {
            get { return z; }
            set { z = value; }
        }
    }
}