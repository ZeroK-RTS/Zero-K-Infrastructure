using System;
using System.Data;
using System.IO;
using JetBrains.Annotations;
using PlasmaShared.Properties;

namespace ZkData
{
	partial class ZkDataContext
	{
#if DEBUG
        private static string ConnectionString = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "zero-k.mdf");
#else 
        private static string ConnectionString= Settings.Default.zero_kConnectionString;

#endif

        private static bool WasDbChecked = false;

        public ZkDataContext() : base(ConnectionString) {
#if DEBUG
            if (!WasDbChecked)
            {
                if (!DatabaseExists()) CreateDatabase();
                WasDbChecked = true;
            }
#endif
        }
	}
}
