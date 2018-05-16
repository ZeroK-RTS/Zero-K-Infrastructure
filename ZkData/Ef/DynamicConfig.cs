using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace ZkData
{
    public class DynamicConfig
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Key { get; set; } = 1;


        public int MmBanSecondsIncrease { get; set; } = 1;
        public int MmBanSecondsMax { get; set; } = 15;
        public int MmBanReset { get; set; } = 60;
        public double MmStartingWidth { get; set; } = 80.0;
        public double MmWidthGrowth { get; set; } = 240.0;
        public double MmWidthGrowthTime { get; set; } = 120.0;
        public double MmWidthReductionForParties { get; set; } = 0.7;

        public static DynamicConfig Instance;

        static DynamicConfig()
        {
            using (var db = new ZkDataContext())
            {
                Instance = db.DynamicConfigs.FirstOrDefault();
                if (Instance == null)
                {
                    Instance = new DynamicConfig();
                    db.DynamicConfigs.Add(Instance);
                    db.SaveChanges();
                }
            }
        }

        public static void SaveConfig(DynamicConfig config)
        {
            Instance = config;
            using (var db = new ZkDataContext())
            {
                config.Key = 1;
                db.Entry(config).State = EntityState.Modified;
                db.SaveChanges();
            }
        }
    }
}