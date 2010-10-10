using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LuaManagerLib.Tools
{
    public class Md5Cached
    {
        Dictionary<String, CacheInfo> cacheTable = new Dictionary<String, CacheInfo>();

        public void clearCache()
        {
            cacheTable.Clear();
        }

        public String file2Md5(string fullFilePath)
        {
            var md5 = "";
            var lastWrite = File.GetLastWriteTime(fullFilePath);
            if ((cacheTable.ContainsKey(fullFilePath)) && (lastWrite == cacheTable[fullFilePath].lastAccess)) md5 = cacheTable[fullFilePath].md5;
            else
            {
                md5 = file2Md5Uncached(fullFilePath);

                var cinfo = new CacheInfo();
                cinfo.fullFilePath = fullFilePath;
                cinfo.lastAccess = lastWrite;
                cinfo.md5 = md5;

                if (cacheTable.ContainsKey(fullFilePath)) cacheTable.Remove(fullFilePath);
                cacheTable.Add(fullFilePath, cinfo);
            }
            return md5;
        }

        public static string file2Md5Uncached(string fullFilePath) //, string Checksumme)
        {
            var FileCheck = File.OpenRead(fullFilePath);
            // MD5-Hash aus dem Byte-Array berechnen
            MD5 md5 = new MD5CryptoServiceProvider();
            var md5Hash = md5.ComputeHash(FileCheck);
            FileCheck.Close();

            return BitConverter.ToString(md5Hash).Replace("-", "").ToLower();
        }

        public static byte[] string2Md5Binary(string input)
        {
            var x = new MD5CryptoServiceProvider();
            var bs = Encoding.UTF8.GetBytes(input);
            return x.ComputeHash(bs);
        }

        public static string string2Md5Uncached(string input)
        {
            var x = new MD5CryptoServiceProvider();
            var bs = Encoding.UTF8.GetBytes(input);
            bs = x.ComputeHash(bs);
            var s = new StringBuilder();
            foreach (var b in bs) s.Append(b.ToString("x2").ToLower());
            var password = s.ToString();
            return password;
        }

        class CacheInfo
        {
            public String fullFilePath;
            public DateTime lastAccess;
            public String md5;
        }
    }
}