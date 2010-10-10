#region using

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#endregion

namespace PlasmaShared
{
    public class SdpArchive
    {
        readonly List<PoolFileInfo> files = new List<PoolFileInfo>();


        public IList<PoolFileInfo> Files { get { return files; } }

        public SdpArchive(Stream input)
        {
            files.Clear();
            var r = new BinaryReader(input, Encoding.ASCII);

            var temp = new Byte[4];
            while (r.Read(temp, 0, 1) > 0)
            {
                int nameLen = temp[0];
                var pi = new PoolFileInfo { Name = new string(r.ReadChars(nameLen)), Hash = new Hash(r.ReadBytes(Hash.Size)) };
                temp = r.ReadBytes(4);
                pi.Crc = ParseUint32(temp);
                temp = r.ReadBytes(4);
                pi.UncompressedSize = ParseUint32(temp);
                files.Add(pi);
            }
        }

        public static uint ParseUint32(byte[] c)
        {
            uint i = 0;
            i = (uint)c[0] << 24 | i;
            i = (uint)c[1] << 16 | i;
            i = (uint)c[2] << 8 | i;
            i = (uint)c[3] << 0 | i;
            return i;
        }
    }
}