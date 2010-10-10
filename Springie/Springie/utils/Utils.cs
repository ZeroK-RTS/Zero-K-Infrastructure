#region using

using System;

#endregion

namespace Springie
{
    /// <summary>
    /// General purpose static functions here
    /// </summary>
    public class Utils
    {
        /// <summary>
        /// Glues remaining arguments together
        /// </summary>
        /// <param name="args">argument array</param>
        /// <param name="startindex">index to start gluing</param>
        /// <returns>glued string</returns>
        public static string Glue(string[] args, int startindex)
        {
            if (args.Length <= startindex) return "";
            string ret = args[startindex];
            for (int i = startindex + 1; i < args.Length; ++i) ret += ' ' + args[i];
            return ret;
        }

        public static string Glue(string[] args)
        {
            return Glue(args, 0);
        }

        /// <summary>
        /// shifts array by given number of indexes
        /// </summary>
        /// <param name="input">input array</param>
        /// <param name="bynum">if bynum is negative, creates shorter array starting at abs(bynum) element of original arraym, if bynum is positive creates new array and makes bynum empty pieces in the beginning</param>
        /// <returns>returned new array</returns>
        public static T[] ShiftArray<T>(T[] input, int bynum)
        {
            var ret = new T[input.Length + bynum];
            if (bynum == 0) input.CopyTo(ret, 0);
            else if (bynum < 0) for (int i = 0; i < ret.Length; ++i) ret[i] = input[i - bynum];
            else if (bynum > 0) for (int i = 0; i < input.Length; ++i) ret[i + bynum] = input[i];
            return ret;
        }

        public static long ToUnix(DateTime t)
        {
            if (t == DateTime.MinValue) return 0;
            return (long)(t.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }

        public static long ToUnix(TimeSpan t)
        {
            if (t == TimeSpan.MinValue) return 0;
            return (long)t.TotalSeconds;
        }
    }
}