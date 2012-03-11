using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace ZeroKLobby
{
    public class EngineConfigurator
    {
        const int DefaultLevel = 2; // == best 


        static readonly FileInfo[] FileInfos = new FileInfo[]
                                               {
                                                   new FileInfo() { RelativePath = "cmdcolors.txt", Resource = "/Resources/Conf/cmdcolors.txt" },
                                                   new FileInfo()
                                                   {
                                                       RelativePath = "springsettings.cfg",
                                                       Resource = "/Resources/Conf/springsettings.cfg",
                                                       KeyValueRegex = "([^=]+)=(.*)",
                                                       KeyValueFormat = "{0}={1}",
                                                       MergeRegex = "([^=]+)=",
                                                       SpecialLines = () =>
                                                           {
                                                               var size = SystemInformation.PrimaryMonitorSize;
                                                               return string.Format("XResolution={0}\r\nYResolution={1}\r\n", size.Width, size.Height);
                                                           }
                                                   }, new FileInfo() { RelativePath = "uikeys.txt", Resource = "/Resources/Conf/uikeys.txt" },
                                                   new FileInfo() { RelativePath = "selectkeys.txt", Resource = "/Resources/Conf/selectkeys.txt" },
                                                   new FileInfo() { RelativePath = "lups.cfg", Resource = "/Resources/Conf/lups.cfg" },
                                               };
        readonly string path;


        public EngineConfigurator(string path)
        {
            this.path = path;
            Configure(false, DefaultLevel);
            if (!Program.Conf.ResetUiKeysHack4)
            {
                foreach (var f in FileInfos.Where(x => x.RelativePath == "uikeys.txt" || x.RelativePath == "selectkeys.txt"))
                {
                    var target = Path.Combine(path, f.RelativePath);
                    var data = ReadResourceString(string.Format("{0}{1}", f.Resource, DefaultLevel));
                    if (data == null) data = ReadResourceString(f.Resource);
                    File.WriteAllText(target, data);
                }
                Program.Conf.ResetUiKeysHack4 = true;
                Program.SaveConfig();
            }
        }

        public void Configure(bool overwrite, int level)
        {
            foreach (var f in FileInfos)
            {
                var target = Path.Combine(path, f.RelativePath);
                if (overwrite || !File.Exists(target))
                {
                    var data = ReadResourceString(string.Format("{0}{1}", f.Resource, level));
                    if (data == null) data = ReadResourceString(f.Resource);

                    ApplyFileChanges(target, data, f.MergeRegex);
                    if (f.SpecialLines != null) ApplyFileChanges(target, f.SpecialLines(), f.MergeRegex);
                }
            }
            SetConfigValue("widgetDetailLevel", level.ToString());
        }


        public string GetConfigValue(string key)
        {
            return GetAndSetConfigValue(key, null);
        }

        public void SetConfigValue(string key, string newValue)
        {
            GetAndSetConfigValue(key, newValue ?? "");
        }


        void ApplyFileChanges(string target, string data, string regex)
        {
            if (string.IsNullOrEmpty(data)) return;
            if (string.IsNullOrEmpty(regex) || !File.Exists(target)) File.WriteAllText(target, data);
            else
            {
                var targetDict = File.ReadAllLines(target).ToDictionary(x =>
                    {
                        var m = Regex.Match(x, regex);
                        if (m.Success) return m.Groups[1].Value;
                        else return x;
                    });
                var sourceDict = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToDictionary(x =>
                    {
                        var m = Regex.Match(x, regex);
                        if (m.Success) return m.Groups[1].Value;
                        else return x;
                    });
                ;

                foreach (var kvp in targetDict) if (!sourceDict.ContainsKey(kvp.Key)) sourceDict[kvp.Key] = kvp.Value;
                File.WriteAllLines(target, sourceDict.Values);
            }
        }

        /// <summary>
        /// Sets value if newValue is not null
        /// </summary>
        string GetAndSetConfigValue(string key, string newValue = null)
        {
            string foundValue = null;
            foreach (var f in FileInfos.Where(x => !string.IsNullOrEmpty(x.KeyValueRegex)))
            {
                var targetFile = Path.Combine(path, f.RelativePath);
                var targetLines = new List<string>();
                foreach (var line in File.ReadAllLines(targetFile))
                {
                    var finalLine = line;
                    var match = Regex.Match(line, f.KeyValueRegex);
                    if (match.Success && match.Groups[1].Value == key)
                    {
                        foundValue = match.Groups[2].Value;
                        if (newValue != null) finalLine = string.Format(f.KeyValueFormat, key, newValue);
                    }
                    targetLines.Add(finalLine);
                }

                // didnt find key, add as new line 
                if (foundValue == null) targetLines.Add(string.Format(f.KeyValueFormat, key, newValue));

                if (newValue != null) File.WriteAllLines(targetFile, targetLines);
            }

            return foundValue;
        }

        string ReadResourceString(string uri)
        {
            try
            {
                using (var steam = new StreamReader(Application.GetResourceStream(new Uri(uri, UriKind.Relative)).Stream)) return steam.ReadToEnd();
            }
            catch {}
            return null;
        }


        public class FileInfo
        {
            public string KeyValueFormat;
            public string KeyValueRegex;
            public string MergeRegex;
            public string RelativePath;
            public string Resource;
            public Func<string> SpecialLines;
        }
    }
}