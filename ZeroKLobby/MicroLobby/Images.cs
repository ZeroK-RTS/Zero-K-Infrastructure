using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using LobbyClient;

namespace ZeroKLobby.MicroLobby
{
    static class Images
    {
        static Image[,] rankImages = new Image[8, 8];

        public static Dictionary<string, Image> CountryFlags = new Dictionary<string, Image>();


        static Images()
        {
            foreach (var country in CountryNames.Names.Keys) CountryFlags[country] = (Image)Flags.ResourceManager.GetObject(country.ToLower());
            for (var i = 0; i < rankImages.GetLength(0); i++)
            {
                for (var j = 0; j < rankImages.GetLength(1); j++)
                rankImages[i, j] = (Image)Ranks.ResourceManager.GetObject(string.Format("_{0}_{1}", i, j));
            }
        }



        public static Image GetRank(int level, int elo)
        {
            var clampedLevel = System.Math.Max(0, System.Math.Min(rankImages.GetLength(0) - 1, (int)System.Math.Floor(
                (-0.12 / Math.Cosh((level - 61.9) / 7.08) + 1) * 2.93 * Math.Log(Math.Exp(-2.31) * level + 1) - 0.89 / Math.Cosh((level - 28.55) / 3.4))));
            //0, 5, 10, 20, 35, 50, 75, 100 -> 0, 1, 2, 3, 4, 5, 6, 7
            var clampedSkill = System.Math.Max(0, System.Math.Min(rankImages.GetLength(1) - 1, (int)System.Math.Floor((elo - 1000.0) / 200)));
            return rankImages[clampedLevel, clampedSkill];
        }
    }
}
