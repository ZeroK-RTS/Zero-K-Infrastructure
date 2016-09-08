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
        static Image[] rankImages = new Image[9];

        public static Dictionary<string, Image> CountryFlags = new Dictionary<string, Image>();


        static Images()
        {
            foreach (var country in CountryNames.Names.Keys) CountryFlags[country] = (Image)Flags.ResourceManager.GetObject(country.ToLower());
            for (var i = 0; i < 9; i++) rankImages[i] = (Image)Ranks.ResourceManager.GetObject(string.Format("_{0}",i+1));
        }



        public static Image GetRank(int level)
        {
            var rankIndex = level / 10;
            return rankImages[Math.Min(rankIndex, rankImages.Length - 1)];
        }
    }
}