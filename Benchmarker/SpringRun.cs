using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using PlasmaShared;

namespace Benchmarker
{
    public class SpringRun
    {
        Process process;
        StringBuilder LogLines;
        public string Start(SpringPaths paths, TestRun test, Benchmark benchmark) {
            LogLines = new StringBuilder();

            paths.SetEnginePath(paths.GetEngineFolderByVersion(test.Engine));

            process = new Process();
            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.Arguments += string.Format("--config \"{0}\"", Path.Combine(test.Config.ConfigPath, "springsettings.cfg"));

            process.StartInfo.EnvironmentVariables["SPRING_DATADIR"] = test.Config.ConfigPath + ";" +  paths.WritableDirectory+";"+ Directory.GetParent(benchmark.BenchmarkPath);
            process.StartInfo.EnvironmentVariables["OMP_WAIT_POLICY"] = "ACTIVE";

            process.StartInfo.EnvironmentVariables.Remove("SPRING_ISOLATED");
            process.StartInfo.FileName = test.UseMultithreaded ? paths.MtExecutable : paths.Executable;
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(paths.Executable);
            
            
            var scriptPath = Path.GetTempFileName();
            File.WriteAllText(scriptPath, new ScriptGenerator().Generate(benchmark, test));
            process.StartInfo.Arguments += string.Format(" \"{0}\"", scriptPath);

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            //process.Exited += springProcess_Exited;
            process.ErrorDataReceived += (sender, args) =>{ LogLines.AppendLine(args.Data); };
            process.OutputDataReceived += (sender, args) => { LogLines.AppendLine(args.Data); };
            process.EnableRaisingEvents = true;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            var lines = LogLines.ToString();
            return lines;
        }

    }
}