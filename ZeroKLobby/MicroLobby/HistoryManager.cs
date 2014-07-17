using System;
using System.Diagnostics;
using System.IO;
using ZeroKLobby;
using ZeroKLobby.Lines;

namespace ZeroKLobby.MicroLobby
{
    static class HistoryManager
    {
        static readonly string historyFolder = Path.Combine(Program.SpringPaths.Cache, "ChatHistory");
        public const int HistoryLines = 30;

        static readonly object locker = new object();

        public static void InsertLastLines(string channelName, ChatBox control)
        {
            try
            {
                var historyFileName = channelName + ".txt";
                if (!File.Exists(Path.Combine(historyFolder, historyFileName))) return;
                var lines = GetLines(historyFileName);
                for (var i = Math.Max(0, lines.Length - HistoryLines); i < lines.Length; i++)
                {
                    control.AddLine(new HistoryLine(lines[i].StripAllCodes()));
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("History manager: " + e);
            }
        }

        public static void LogLine(string channelName, IChatLine line)
        {
            try
            {
                var fileName = channelName + ".txt";
                if (line is JoinLine || line is LeaveLine || line is HistoryLine || line is TopicLine) return;
                Directory.CreateDirectory(historyFolder);
                var lineStr = line is ChimeLine ? "*** " +
                    ((ChimeLine)line).Date.ToString(System.Globalization.CultureInfo.CurrentCulture) :
                    line.Text.StripAllCodes();
                lock (locker) File.AppendAllText(Path.Combine(historyFolder, fileName), lineStr + Environment.NewLine);
            }
            catch (Exception e)
            {
                Trace.WriteLine("History manager: " + e);
            }
        }

        public static void OpenHistory(string channel)
        {
            var path = Path.Combine(historyFolder, channel + ".txt");
            try
            {
                Process.Start(path);
            }
            catch {}
        }

        static string[] GetLines(string fileName)
        {
            lock (locker) return File.ReadAllLines(Path.Combine(historyFolder, fileName));
        }
    }
}