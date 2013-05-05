using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                                                       KeyValueRegex = "([^=]+)=(.*)", //return "<key>=<value>", "<key>", and "<value>"
                                                       KeyValueFormat = "{0}={1}",
                                                       MergeRegex = "([^=]+)=", //return "<key>=", "<key>"
                                                       SpecialLines = "addResolution",
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
                try
                {
                    foreach (var f in FileInfos.Where(x => x.RelativePath == "uikeys.txt" || x.RelativePath == "selectkeys.txt"))
                    {
                        var target = Path.Combine(path, f.RelativePath);
                        var data = ReadResourceString(string.Format("{0}{1}", f.Resource, DefaultLevel));
                        if (data == null) data = ReadResourceString(f.Resource);
                        File.WriteAllText(target, data);
                    }
                }
                catch (Exception ex) {
                    Trace.TraceError(string.Format("Error replacing config files: {0}", ex));
                }
                Program.Conf.ResetUiKeysHack4 = true;
                Program.SaveConfig();
            }
        }

        public void Reset() {
            foreach (var f in FileInfos) {
                var target = Path.Combine(path, f.RelativePath);
                if (File.Exists(target)) File.Delete(target);
            }
            Configure(true,DefaultLevel);
        }


        public void Configure(bool overwrite, int level)
        {
            foreach (var f in FileInfos)
            {
                var target = Path.Combine(path, f.RelativePath);
                if (overwrite || !File.Exists(target))
                {
                    var data = ReadResourceString(string.Format("{0}{1}", f.Resource, level));
                    bool usinglocal = false;
                    if (data == null) {
                        data = ReadResourceString(f.Resource);
                        usinglocal = true;
                    }

                    ApplyFileChanges(target, data, f.KeyValueRegex);

                    //add resolution data if not yet added:
                    string SpecialLinesToAdd = null;
                    if (f.SpecialLines == "addResolution")
                    {
                        bool useCustomRes = false;
                        if (!usinglocal) //extract options from "data"
                        {
                            //find out if data file already contain resolution data
                            var m = Regex.Match(data, "XResolution");
                            if (m.Success) useCustomRes = true;
                        }
                        if (!useCustomRes)
                        {   //if data file do not contain resolution data, create new one
                            var size = SystemInformation.PrimaryMonitorSize;
                            SpecialLinesToAdd = string.Format("XResolution={0}\r\nYResolution={1}\r\n", size.Width, size.Height);
                            Trace.TraceInformation("addResolution:");
                            Trace.TraceInformation(SpecialLinesToAdd);
                        }
                    }
                    if (SpecialLinesToAdd != null) ApplyFileChanges(target, SpecialLinesToAdd, f.KeyValueRegex);
                    //end add resolution data
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
                Dictionary<string, string> targetDict = new Dictionary<string, string>();
                foreach (var line in File.ReadAllLines(target))
                {
                    var m = Regex.Match(line, regex);
                    if (m.Success)
                    {
                        string capturedKeyString = m.Groups[1].Value.Trim(); //remove whitespace
                        if (!targetDict.ContainsKey(capturedKeyString)) targetDict.Add(capturedKeyString, m.Groups[0].Value);
                        else Trace.TraceInformation("Duplicate key in user's configuration file detected: " + capturedKeyString);
                    }
                }
                //    var targetDict = File.ReadAllLines(target).ToDictionary(x =>
                //        {
                //            var m = Regex.Match(x, regex);
                //            if (m.Success) return m.Groups[1].Value;
                //            else return x;
                //        });
                Dictionary<string, string> sourceDict = new Dictionary<string, string>();
                string[] linesOfOption = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in linesOfOption)
                {
                    var m = Regex.Match(line, regex);
                    if (m.Success)
                    {
                        string capturedKeyString = m.Groups[1].Value.Trim(); //remove whitespace
                        if (!sourceDict.ContainsKey(capturedKeyString)) sourceDict.Add(capturedKeyString, m.Groups[0].Value);
                        else Trace.TraceInformation("Duplicate key in source's configuration file detected: " + capturedKeyString);
                    }
                }
                //var sourceDict = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToDictionary(x =>
                //    {
                //        var m = Regex.Match(x, regex);
                //        if (m.Success) return m.Groups[1].Value;
                //        else return x;
                //    });
                //;
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
                    var match = Regex.Match(line, f.KeyValueRegex, RegexOptions.IgnoreCase);
                    if (match.Success && string.Equals(match.Groups[1].Value.Trim(),key, StringComparison.InvariantCultureIgnoreCase))
                    {
                        foundValue = match.Groups[2].Value.Trim();
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
            public string SpecialLines;
        }
    }
}