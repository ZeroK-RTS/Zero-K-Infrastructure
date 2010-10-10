#region using

using System.Drawing;

#endregion

namespace SpringDownloader.MicroLobby
{
    /// <summary>
    /// Structure for storing lightweight color info and transformation from and to spring format
    /// </summary>
    public struct MyCol
    {
        /************************************************************************/
        /*   PUBLIC ATTRIBS                                                     */
        /************************************************************************/

        public byte B;
        public byte G;
        public byte R;

        /************************************************************************/
        /*   PUBLIC METHODS                                                     */
        /************************************************************************/

        public MyCol(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        /// This method calculates visual apparent "distance" of colors
        /// </summary>
        /// <param name="e1">first color to compare</param>
        /// <param name="e2">second color to compare</param>
        /// <returns>Return number representing distance .. values under 30 000 or so seems similar</returns>
        public static int Distance(MyCol e1, MyCol e2)
        {
            int r, g, b;
            int rmean;
            rmean = (e1.R + e2.R)/2;
            r = e1.R - e2.R;
            g = e1.G - e2.G;
            b = e1.B - e2.B;
            return (((512 + rmean)*r*r) >> 8) + 4*g*g + (((767 - rmean)*b*b) >> 8);
        }

        public int Distance(MyCol e2)
        {
            return Distance(this, e2);
        }

        /// <summary>
        /// Processes input array of colors and if some colors are too similar, it changes them to be more different
        /// </summary>
        /// <param name="input">Array of colors to process</param>
        /// <param name="balanceDistance">Treshold distance, if two colors are more similar than this, it changes one of them</param>
        public static void FixColors(MyCol[] input, int balanceDistance)
        {
            for (var i = 0; i < input.Length - 1; ++i) for (var j = i + 1; j < input.Length; ++j) if (input[i]%input[j] < balanceDistance) GetBestRelocation(input, i, out input[i]);
        }

        /// <summary>
        /// Tries to change one color to not be so similar to others
        /// </summary>
        /// <param name="input">array of colors to process</param>
        /// <param name="index">index of current color to check against others</param>
        /// <param name="bestCol">output color (most far from others)</param>
        /// <returns>minimum distance of best colors to others</returns>
        public static int GetBestRelocation(MyCol[] input, int index, out MyCol bestCol)
        {
            int bestVal;
            bestCol = new MyCol(0, 0, 0);
            bestVal = int.MinValue;

            var temp = new MyCol();
            for (short r = 20; r <= 255; r += 3)
            {
                for (short g = 20; g <= 255; g += 3)
                {
                    for (short b = 50; b <= 255; b += 3)
                    {
                        temp.R = (byte)r;
                        temp.G = (byte)g;
                        temp.B = (byte)b;

                        var minDist = int.MaxValue;
                        for (var i = 0; i < input.Length; ++i)
                        {
                            if (i == index) continue;
                            var cDistance = temp%input[i];
                            if (cDistance <= bestVal)
                            {
                                minDist = int.MinValue;
                                break;
                            }
                            if (cDistance < minDist) minDist = cDistance;
                        }

                        if (minDist > bestVal)
                        {
                            bestVal = minDist;
                            bestCol = temp;
                        }
                    }
                }
            }
            return bestVal;
        }

        /// <summary>
        /// Conversion to color format
        /// </summary>
        public static explicit operator Color(MyCol m)
        {
            return Color.FromArgb(m.R, m.G, m.B);
        }

        /// <summary>
        /// Conversion from color format
        /// </summary>
        public static explicit operator MyCol(Color c)
        {
            return new MyCol(c.R, c.G, c.B);
        }

        /// <summary>
        /// Conversion to spring (tasclient) format
        /// </summary>
        public static explicit operator int(MyCol m)
        {
            int ret = m.B;
            ret = ret << 8;
            ret += m.G;
            ret = ret << 8;
            ret += m.R;
            return ret;
        }

        /// <summary>
        /// Conversion from spring (tasclient) format
        /// </summary>
        public static explicit operator MyCol(int c)
        {
            var r = (byte)(c & 255);
            c = c >> 8;
            var g = (byte)(c & 255);
            c = c >> 8;
            var b = (byte)(c & 255);
            return new MyCol(r, g, b);
        }

        public static int operator %(MyCol e1, MyCol e2)
        {
            return Distance(e1, e2);
        }
    } ;
}