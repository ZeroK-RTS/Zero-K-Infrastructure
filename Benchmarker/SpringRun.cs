using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using PlasmaShared;

namespace Benchmarker
{
    public class SpringRun
    {
        StringBuilder LogLines;
        Process process;

        public void Abort() {
            if (process != null) process.Kill();
        }

        public string Start(SpringPaths paths, TestCase test, Benchmark benchmark) {
            LogLines = new StringBuilder();

            paths.SetEnginePath(paths.GetEngineFolderByVersion(test.Engine));

            process = new Process();
            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.Arguments += string.Format("--config \"{0}\"", Path.Combine(test.Config.ConfigPath, "springsettings.cfg"));

            process.StartInfo.EnvironmentVariables["SPRING_DATADIR"] = test.Config.ConfigPath + ";" + paths.WritableDirectory + ";" +
                                                                       Directory.GetParent(benchmark.BenchmarkPath).Parent.FullName;
            process.StartInfo.EnvironmentVariables["OMP_WAIT_POLICY"] = "ACTIVE";

            process.StartInfo.EnvironmentVariables.Remove("SPRING_ISOLATED");
            process.StartInfo.FileName = test.UseMultithreaded ? paths.MtExecutable : paths.Executable;
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(paths.Executable);

            var scriptPath = Path.GetTempFileName();
            File.WriteAllText(scriptPath, benchmark.GetScriptForTestCase(test));
            process.StartInfo.Arguments += string.Format(" \"{0}\"", scriptPath);

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.ErrorDataReceived += (sender, args) => { LogLines.AppendLine(args.Data); };
            process.OutputDataReceived += (sender, args) => { LogLines.AppendLine(args.Data); };
            process.EnableRaisingEvents = true;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            var lines = LogLines.ToString();

            File.Delete(scriptPath);

            return lines;
        }
    }
}