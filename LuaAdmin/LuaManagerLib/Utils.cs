using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Collections;



namespace LuaManagerLib
{
    public class Utils
    {
        [Obsolete]static public string encodeUrl(string inp)
        { 
            return MyHttpUtility.UrlEncode(inp);
        }

        static public string commaToDot(string inp)
        {
            return inp.Replace(",", ".");
        }

        static private byte[] stringToByteArray(string str)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            return enc.GetBytes(str);
        }

        private string byteArrayToString(byte[] arr)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            return enc.GetString(arr);
        }

        static public string normalizePathname(string path)
        {
            string normPath = path.Replace("\\", "/");
            normPath = normPath.Replace("///", "/");
            normPath = normPath.Replace("//", "/");
           // normPath = normPath.ToLower();
            return normPath;
        }

        static public ArrayList normalizePathnames(ArrayList pathes)
        {
            ArrayList newList = new ArrayList();
            IEnumerator ienum = pathes.GetEnumerator();
            while (ienum.MoveNext())
            {
                string path = (string)ienum.Current;
                newList.Add( Utils.normalizePathname( path) );
            }

            return newList;
        }

        static public void convertWhitespacesToBlanks(ref String str)
        {
            str = str.Replace('\r', ' ');
            str = str.Replace('\n', ' ');
            str = str.Replace('\t', ' ');
            //are there more?
        }

    }
}
