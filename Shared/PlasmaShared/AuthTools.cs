using System;
using System.Linq;

namespace ZkData
{
    public class AuthTools
    {
        public static string GetSiteAuthToken(string passwordHash) {
            var str = string.Format("{0}", passwordHash);
            return str;
        }

    }
}