using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ZkData
{
    public static class EmbeddedResourceExtractor
    {
        public static bool ExtractFile(string resourceName, string targetFile)
        {
            if (File.Exists(targetFile)) return true;
            try
            {
                var startAssembly = System.Reflection.Assembly.GetEntryAssembly();
                using (var s = startAssembly.GetManifestResourceStream(resourceName))
                    if (s != null)
                    {
                        using (var fs = new FileStream(targetFile, FileMode.Create))
                        {
                            s.CopyTo(fs);
                            return true;
                        }
                    }
                return false;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error extracting resource {0} : {1}",resourceName,ex.Message);
                return false;
            }
        }

    }
}
