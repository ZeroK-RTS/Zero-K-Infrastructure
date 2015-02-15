using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LobbyClient;

namespace ZeroKLobby
{
    static class Extensions
    {
        public static bool IsOfficial(this Battle b) {
            var gameInfo = KnownGames.GetGame(b.ModName);
            if (gameInfo != null && gameInfo.IsPrimary) return true;
            else return false;
        }
    }
}
