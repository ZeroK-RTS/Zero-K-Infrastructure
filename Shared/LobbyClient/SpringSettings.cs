using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ZkData;

namespace LobbyClient
{
    public class SpringSettings
    {
        public Dictionary<string, EngineConfigEntry> GetEngineConfigOptions(SpringPaths paths, string engine = null)
        {
            Trace.TraceInformation("Extracting configuration from Spring located in {0}", paths.Executable);
            var sb = new StringBuilder();
            var p = new Process();
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.Arguments += string.Format("--list-config-vars");
            p.StartInfo.EnvironmentVariables["SPRING_DATADIR"] = paths.WritableDirectory;
            p.StartInfo.EnvironmentVariables.Remove("SPRING_ISOLATED");
            p.StartInfo.FileName = paths.Executable;
            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(paths.Executable);
            p.StartInfo.RedirectStandardOutput = true;
            p.OutputDataReceived += (sender, args) => sb.AppendLine(args.Data);
            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit(3000);
            sb.AppendLine(); //append terminator

            var text = sb.ToString();
            int whereIsTable = text.IndexOf('{');
            text = text.Substring(whereIsTable); // skip empty line or other info (if exist). Compatibility with Spring 94+
            var data = JsonConvert.DeserializeObject<Dictionary<string, EngineConfigEntry>>(text);
            return data;
        }
    }

    public class EngineConfigEntry
    {
        public string description { get; set; }
        public string declarationFile { get; set; }
        public int declarationLine { get; set; }
        public string defaultValue { get; set; }
        public double? maximumValue { get; set; }
        public double? minimumValue { get; set; }
        public string safemodeValue { get; set; }
        public string type { get; set; }
        public int readOnly { get; set; }
    }

}
