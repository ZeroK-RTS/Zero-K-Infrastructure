using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ZkData
{
    public static class EmbeddedResourceExtractor
    {
        public static bool ExtractFile(string resourceName, string targetFile)
        {
            
            var startAssembly = System.Reflection.Assembly.GetEntryAssembly();
            using (var s = startAssembly.GetManifestResourceStream(resourceName))
                if (s != null)
                {
                    if (File.Exists(targetFile) && new FileInfo(targetFile).Length == s.Length) return true;
                    using (var fs = new FileStream(targetFile, FileMode.Create))
                    {
                        s.CopyTo(fs);
                        return true;
                    }
                }
            return false;
        }

    }
}
