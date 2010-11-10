using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PlasmaShared
{
    [Serializable]
    public struct Hash: ICloneable
    {
        public const int Size = 16;
        readonly byte[] data;

        public Hash(byte[] source)
        {
            data = new byte[Size];
            Buffer.BlockCopy(source, 0, data, 0, Size);
        }

        public Hash(Hash h): this(h.data) {}

        public Hash(byte[] buf, int offset)
        {
            data = new byte[Size];
            Buffer.BlockCopy(buf, offset, data, 0, Size);
        }

        public Hash(string s)
        {
            data = StringToBytes(s);
        }

        public static bool ByteArrayEquals(byte[] b1, byte[] b2)
        {
            if (b1 == b2) return true;
            if (b1 == null || b2 == null) return false;
            if (b1.Length != b2.Length) return false;
            for (var i = 0; i < b1.Length; ++i) if (b1[i] != b2[i]) return false;
            return true;
        }

        public static Hash HashBytes(byte[] data)
        {
            return (Hash)new MD5CryptoServiceProvider().ComputeHash(data);
        }

        public static Hash HashString(string data)
        {
          return (Hash)new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        public static Hash HashStream(Stream fs)
        {
            return (Hash)new MD5CryptoServiceProvider().ComputeHash(fs);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Hash)) return false;
            var h = (Hash)obj;
            return ByteArrayEquals(data, h.data);
        }

        public override int GetHashCode()
        {
            var hash = 0;
            for (var i = 0; i < data.Length; ++i) hash += data[i];
            return hash;
        }

        public override string ToString()
        {
            return BytesToString(data);
        }

        static string BytesToString(byte[] bytes)
        {
            var str = new StringBuilder();

            for (var i = 0; i < bytes.Length; i++) str.AppendFormat("{0:x2}", bytes[i]);

            return str.ToString();
        }

        static int CharToHexByte(char x)
        {
            if (x >= '0' && x <= '9') return x - '0';
            else
            {
                if (x >= 'a' && x <= 'f') return x - 'a' + 10;
                else if (x >= 'A' && x <= 'F') return x - 'A' + 10;
                else throw new ArgumentException("character is not convertible to hex byte");
            }
        }

        static void HashFolderRecursive(DirectoryInfo folder, string path, SortedList<string, FileInfo> entries)
        {
            foreach (var di in folder.GetDirectories()) HashFolderRecursive(di, Utils.MakePath(path, di.Name), entries);
            foreach (var fi in folder.GetFiles()) entries.Add(Utils.MakePath(path, fi.Name), fi);
        }

        static byte[] StringToBytes(string s)
        {
            Debug.Assert(s.Length%2 == 0);
            s.ToUpper();
            var destLen = s.Length/2;
            var res = new byte[destLen];
            var i = 0;
            var si = 0;

            while (i < destLen) res[i++] = (byte)((CharToHexByte(s[si++]) << 4) + CharToHexByte(s[si++]));
            return res;
        }

        public static explicit operator byte[](Hash h)
        {
            return h.data;
        }

        public static explicit operator string(Hash h)
        {
            return h.ToString();
        }

        public static explicit operator Hash(byte[] v)
        {
            return new Hash(v);
        }

        public static explicit operator Hash(string v)
        {
            return new Hash(v);
        }

        public static bool operator ==(Hash a, Hash b)
        {
            return ByteArrayEquals(a.data, b.data);
        }

        public static bool operator !=(Hash a, Hash b)
        {
            return !(a == b);
        }

        public object Clone()
        {
            return new Hash(this);
        }


        class MyStringCompare: Comparer<string>
        {
            public override int Compare(string x, string y)
            {
                return string.CompareOrdinal(x, y);
            }
        }
    }
}