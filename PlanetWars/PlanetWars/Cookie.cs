using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Browser;

/* www.datawinconsulting.com */

namespace PlanetWars
{

    /* 
    Thanks to Nikola  -  http://blogs.msdn.com/nikola
    http://blogs.msdn.com/nikola/archive/2008/04/14/setting-cookies-through-    silverlight.aspx
    */

    public static class Cookie
    {


        /// 
        /// sets a persistent cookie with huge expiration date
        /// 
        /// the cookie key
        /// the cookie value
        public static void SetCookie(string key, string value)
        {

            string oldCookie = HtmlPage.Document.GetProperty("cookie") as String;
            DateTime expiration = DateTime.UtcNow + TimeSpan.FromDays(2000);
            string cookie = String.Format("{0}={1};expires={2}", key, value, expiration.ToString("R"));
            HtmlPage.Document.SetProperty("cookie", cookie);
        }

        /// 
        /// Retrieves an existing cookie
        /// 
        /// cookie key
        /// null if the cookie does not exist, otherwise the cookie value
        public static string GetCookie(string key)
        {
            string[] cookies = HtmlPage.Document.Cookies.Split(';');
            key += '=';
            foreach (string cookie in cookies) {
                string cookieStr = cookie.Trim();
                if (cookieStr.StartsWith(key, StringComparison.OrdinalIgnoreCase)) {
                    string[] vals = cookieStr.Split('=');

                    if (vals.Length >= 2) {
                        return vals[1];
                    }

                    return string.Empty;
                }
            }

            return null;
        }

        /// 
        /// Deletes a specified cookie by setting its value to empty and expiration to -1 days
        /// 
        /// the cookie key to delete
        public static void DeleteCookie(string key)
        {
            string oldCookie = HtmlPage.Document.GetProperty("cookie") as String;
            DateTime expiration = DateTime.UtcNow - TimeSpan.FromDays(1);
            string cookie = String.Format("{0}=;expires={1}", key, expiration.ToString("R"));
            HtmlPage.Document.SetProperty("cookie", cookie);
        }


    }
}