using System;
using System.Linq;

namespace PlasmaShared
{
    public class AuthTools
    {
        public static string GetSiteAuthToken(string login, string passwordHash, DateTime? date = null) {
            if (date == null) date = DateTime.Now;
            return Convert.ToBase64String((byte[])Hash.HashString(string.Format("{0}{1}{2:yyyy-mm-dd}", login,passwordHash, date)));
        }

        public static bool ValidateSiteAuthToken(string login, string passwordHash, string token) {
            var now = DateTime.Now;
            return GetSiteAuthToken(login, passwordHash, now) == token || GetSiteAuthToken(login, passwordHash, now.AddDays(-1)) == token ||
                   GetSiteAuthToken(login, passwordHash, now.AddDays(1)) == token;
        }
    }
}