using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Springie
{
    static class TeamColorMaker
    {
        // generate the entire spectrum of hues in the hsv color space (this includes magenta, which is not in the wavelength spectrum)
        // the key in the dictionary is the cumulative percieved color difference from the previous color
        // we're using the color difference because it's really easy to tell apart some colors (like blues) while it's hard to tell apart others (like greens)
        /// <summary>
        /// Gets colors for players, allies have similar colors
        /// </summary>
        /// <param name="playerCounts">Array of player counts in different teams</param>
        /// <returns>Array of colors in a team</returns>
        public static Color[][] GetTeamColors(int[] playerCounts)
        {
            Dictionary<int, Color> allColors = GetColorSpectrum();
            double maxColorIndex = allColors.Max(kvp => kvp.Key);
            int teamCount = playerCounts.Length;
            var teamColors = new Color[teamCount][];

            // part of the spectrum that is used to separate teams
            const double colorSeparation = 0.33;

            // width of the section of the spectrum that gets assigned to one team
            double teamSpectrumSliceWidth = maxColorIndex/teamCount*(1 - colorSeparation);

            for (int teamID = 0; teamID < teamCount; teamID++)
            {
                // color at which the team spectrum slice starts
                double startColor = teamID*maxColorIndex/teamCount;

                // all colors inside the spectrum slice assigned to the team
                KeyValuePair<int, Color>[] teamSpectrum =
                    allColors.Where(kvp => kvp.Key > startColor && kvp.Key < startColor + teamSpectrumSliceWidth).OrderBy(kvp => kvp.Key).ToArray();

                int playerCount = playerCounts[teamID];
                var playerColors = new Color[playerCount];
                for (int playerID = 0; playerID < playerCount; playerID++)
                {
                    // get equally spaced colors for players inside a team
                    int playerColorPercentile = playerID*teamSpectrum.Length/playerCount;
                    Color playerColor = teamSpectrum[playerColorPercentile].Value;

                    // consecutive colors can be hard to tell apart, so make the brightness vary
                    playerColor = RGBHSL.SetBrightness(playerColor, playerID%2 < 0.1 ? 0.4 : 0.6);

                    playerColors[playerID] = playerColor;
                }
                teamColors[teamID] = playerColors;
            }

            return teamColors;
        }

        static Dictionary<int, Color> GetColorSpectrum()
        {
            var spectrum = new Dictionary<int, Color>();
            int colorIndex = 0;
            for (int i = 1; i < 360.0 - 1; i++)
            {
                Color nextColor = RGBHSL.HSL_to_RGB(new RGBHSL.HSL { H = (i + 1)/360.0, S = 1, L = 0.5 });
                Color previousColor = RGBHSL.HSL_to_RGB(new RGBHSL.HSL { H = i/360.0, S = 1, L = 0.5 });
                colorIndex += PercievedColorDistance(nextColor, previousColor);
                if (!spectrum.ContainsKey(colorIndex)) spectrum.Add(colorIndex, nextColor);
            }
            return spectrum;
        }

        static int PercievedColorDistance(Color color1, Color color2)
        {
            int deltaR = color1.R - color2.R;
            int deltaG = color1.G - color2.G;
            int deltaB = color1.B - color2.B;
            int radicand = 2*deltaR*deltaR + 4*deltaG*deltaG + deltaB*deltaB;
            return (int)Math.Sqrt(radicand);
        }
    }
}