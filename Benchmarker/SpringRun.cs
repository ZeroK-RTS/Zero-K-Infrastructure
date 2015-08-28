using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ZkData;

namespace Benchmarker
{
    public class SpringRun
    {
        StringBuilder LogLines;
        Process process;

        public Action<string> LineAdded = s => { };

        public void Abort() {
            if (process != null) process.Kill();
        }

        public string Start(SpringPaths paths, TestCase test, Benchmark benchmark) {
            LogLines = new StringBuilder();

            paths.SetEnginePath(paths.GetEngineFolderByVersion(test.Engine));

            var optirun = Environment.GetEnvironmentVariable("OPTIRUN");
            
            process = new Process();
            process.StartInfo.CreateNoWindow = true;
            List<string> arg = new List<string>();

            if (string.IsNullOrEmpty(optirun)) {
                process.StartInfo.FileName = paths.Executable;
            }
            else {
                Trace.TraceInformation("Using optirun {0} to start the game (OPTIRUN env var defined)", optirun);
                process.StartInfo.FileName = optirun;
                arg.Add(string.Format("\"{0}\"", ( paths.Executable)));
            }



            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(paths.Executable);

            arg.Add(string.Format("--config \"{0}\"", Path.Combine(test.Config.ConfigPath, "springsettings.cfg")));
            if (test.BenchmarkArg > 0) arg.Add("--benchmark " + test.BenchmarkArg);


            var dataDirList = new List<string>()
            {
                test.Config.ConfigPath,
                Directory.GetParent(benchmark.BenchmarkPath).Parent.FullName,
                paths.WritableDirectory,
            };
            dataDirList.AddRange(paths.DataDirectories);
            dataDirList.Add(Path.GetDirectoryName(paths.Executable));

            if (Environment.OSVersion.Platform == PlatformID.Unix) dataDirList = dataDirList.Distinct().Select(x => x.Replace(" ", "\\ ")).ToList();
            else dataDirList = dataDirList.Distinct().ToList();

            var datadirs = string.Join(Environment.OSVersion.Platform == PlatformID.Unix ? ":" : ";", dataDirList);

            process.StartInfo.EnvironmentVariables["SPRING_DATADIR"] = datadirs;
            process.StartInfo.EnvironmentVariables["SPRING_ISOLATED"] = test.Config.ConfigPath;
            process.StartInfo.EnvironmentVariables["SPRING_WRITEDIR"] = test.Config.ConfigPath;

            var scriptPath = Path.GetTempFileName();
            File.WriteAllText(scriptPath, test.StartScript.GetScriptForTestCase(test, benchmark));
            arg.Add(string.Format("\"{0}\"", scriptPath));

            
            process.StartInfo.Arguments = string.Join(" ", arg);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.ErrorDataReceived += (sender, args) =>
                {
                    LogLines.AppendLine(args.Data);
                    LineAdded(args.Data);
                };
            process.OutputDataReceived += (sender, args) =>
                {
                    LogLines.AppendLine(args.Data);
                    LineAdded(args.Data);
                };
            process.EnableRaisingEvents = true;

            try {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
            } catch (Exception ex) {
                Trace.TraceError("Error waiting for process: {0}", ex);
            }
            var lines = LogLines.ToString();

            File.Delete(scriptPath);

            return lines;
        }
    }
}