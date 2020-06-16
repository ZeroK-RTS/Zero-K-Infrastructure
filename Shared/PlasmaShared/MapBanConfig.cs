using System.Collections.Generic;

namespace ZkData
{
    public class MapBanConfig
    {
        private static Dictionary<int, int> BansPerPlayerByGameSize = new Dictionary<int, int>() {
            // 6 bans per player in a 2 player game, 3 bans per player in a 4 or 6 player game
            { 2, 6 },
            { 4, 3 },
            { 6, 3 },
            { 8, 2 },
            { 10, 1 },
            { 12, 1 }
        };

        public static int GetPlayerBanCount(int gameSize)
        {
            int value;
            return BansPerPlayerByGameSize.TryGetValue(gameSize, out value) ? value : 1;
        }

        public static int GetMaxBanCount()
        {
            return GetPlayerBanCount(2);
        }
    }
}
