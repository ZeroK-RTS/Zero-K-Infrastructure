using System.ComponentModel;
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
        public double MmSizeGrowthTime { get; set; } = 40.0;
        public double MmTeamsMinimumWinChance { get; set; } = 0.0; //every team needs to have a chance of at least x = [0, 0.5) to win for a game to be made. 0 to disable
        public double Mm1v1MinimumWinChance { get; set; } = 0.0; //every team needs to have a chance of at least x = [0, 0.5) to win for a game to be made. 0 to disable
        public double MmMinimumMinutesBetweenGames { get; set; } = 5.0; //you can't join MM if you started a game less than X minutes ago and it's still ongoing
        public double MmMinimumMinutesBetweenSuggestions { get; set; } = 600.0; //if somebody declined a MM suggestion, don't annoy them for at least X minutes

        public int MaximumBattlePlayers { get; set; } = 32; //maximum amount of players allowed in rooms that are not autohosts
        
        public bool TimeQueueEnabled { get; set; }

        public int MinimumPlayersForStdevBalance { get; set; } = 32; // minimum number of players to enable balance that optimizes for stdev
        public double StdevBalanceWeight { get; set; } = 0.01; // weight of stdev difference between teams during balance, elo difference has weight 1
        public double MmEloBonusMultiplier { get; set; } = 0; // elo bonus multiplier to even out matches

        public int MaximumStatLimitedBattlePlayers { get; set; } // if a battle has more than this number of players, maxelo/minelo, maxrank/minrank and maxleve/minlevel are disabled

        [Description("Map vote always tries to include some of the most popular maps (precentile <0.2), this value controls how big fraction of offers is most popular maps.")]
        public double MapVoteFractionOfPopularMaps { get; set; } = 0.5;
        

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