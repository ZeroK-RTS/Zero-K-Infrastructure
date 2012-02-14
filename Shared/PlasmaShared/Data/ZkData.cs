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
        private static string ConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=zero-k-dev;Integrated Security=True";
#else 
        private static string ConnectionString= Settings.Default.zero_kConnectionString;

#endif

        private static bool WasDbChecked = false;
        private static object locker = new object();


        public ZkDataContext() : base(ConnectionString) {
#if DEBUG
            if (!WasDbChecked)
            {
                lock (locker)
                {
                    if (!DatabaseExists()) CreateDatabase();
                    WasDbChecked = true;
                }
            }
#endif
        }
	}
}
