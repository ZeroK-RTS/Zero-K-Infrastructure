using System;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.IO;
using System.Web;
using JetBrains.Annotations;
using PlasmaShared.Properties;

namespace ZkData
{
    public class StableMappingSource: MappingSource {
        MetaModel model;
        public StableMappingSource(Type dbType) {
            model = new AttributeMappingSource().GetModel(dbType);

        }

        protected override MetaModel CreateModel(Type dataContextType)
        {
            return model;
        }
    }

    partial class ZkDataContext
	{
        private static string ConnectionStringLocal = @"Data Source=.\SQLEXPRESS;Initial Catalog=zero-k-dev;Integrated Security=True";
        
#if !DEPLOY
        private static string ConnectionStringLive = @"Data Source=omega.licho.eu,100;Initial Catalog=zero-k;Persist Security Info=True;User ID=zero-k;Password=zkdevpass1";
#else 
        private static string ConnectionStringLive = Settings.Default.zero_kConnectionString;
#endif 


        private static bool wasDbChecked = false;
        private static object locker = new object();

#if DEBUG
        //public static bool UseLiveDb = false;
        public static bool UseLiveDb = true;
#else 
        public static bool UseLiveDb = true;
#endif

        private static readonly MappingSource mapping = new StableMappingSource(typeof(ZkDataContext));


        public static Action<ZkDataContext> DataContextCreated = context => { };

        public ZkDataContext(): this(UseLiveDb) {   
        }


        public ZkDataContext(bool? useLiveDb) : base(useLiveDb != null ? (useLiveDb.Value ? ConnectionStringLive : ConnectionStringLocal):(UseLiveDb ? ConnectionStringLive : ConnectionStringLocal), mapping) {
#if DEBUG
            if (!wasDbChecked)
            {
                lock (locker)
                {
                    if (!DatabaseExists()) CreateDatabase();
                    wasDbChecked = true;
                }
            }
#endif
            DataContextCreated(this);
        }


        public void SubmitAndMergeChanges() {
            try
            {
                SubmitChanges(ConflictMode.ContinueOnConflict);
            }

            catch (ChangeConflictException)
            {
                // Automerge database values for members that client has modified
                ChangeConflicts.ResolveAll(RefreshMode.KeepChanges);

                // Submit succeeds on second try.
                SubmitChanges();
            }
        
        }
	}
}
