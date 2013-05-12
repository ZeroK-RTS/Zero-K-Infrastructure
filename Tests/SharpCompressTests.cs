using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SharpCompress.Archive;
using SharpCompress.Archive.Zip;
using SharpCompress.Common;
using SharpCompress.Compressor.Deflate;

namespace Tests
{
    [TestFixture]
    public class SharpCompressTests
    {
        [Test]
        public void TestModifyZip() {

            var zf = ZipArchive.Open(new MemoryStream(File.ReadAllBytes(@"c:\temp\bench.zip")));
            var entry = zf.Entries.First(x => x.FilePath == "poznamka.txt");
            var stream = entry.OpenEntryStream();
            var text = new StreamReader(stream).ReadToEnd();
            text = text + " haf haf";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(text));
            zf.RemoveEntry(entry);
            zf.AddEntry("poznamka.txt", ms);
            zf.SaveTo(File.OpenWrite(@"c:\temp\bench.zip"), new CompressionInfo(){DeflateCompressionLevel = CompressionLevel.BestCompression, Type = CompressionType.Deflate});
        }
    }
}
