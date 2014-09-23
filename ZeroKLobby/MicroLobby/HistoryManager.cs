using System;
using System.Diagnostics;
using System.IO;
using ZeroKLobby;
using ZeroKLobby.Lines;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ZeroKLobby.MicroLobby
{
    static class HistoryManager
    {
        static readonly string historyFolder = Path.Combine(Program.SpringPaths.Cache, "ChatHistory");
        public const int HistoryLines = 30;
        
        static private Dictionary<string,List<string>> newlineBuffer = new Dictionary<string,List<string>>();
        static private Timer timedFlush = new Timer();

        static readonly object locker = new object();

        static HistoryManager()
        {
            timedFlush.Interval = 30000;
            timedFlush.Tick += timedFlush_Tick;
            timedFlush.Start();
        }

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
            if (line is JoinLine || line is LeaveLine || line is HistoryLine || line is TopicLine) return;
            var lineStr = line is ChimeLine ? "*** " +
                ((ChimeLine)line).Date.ToString(System.Globalization.CultureInfo.CurrentCulture) :
                line.Text.StripAllCodes();

            if (!newlineBuffer.ContainsKey(channelName)) //initialize content if haven't
                newlineBuffer.Add(channelName, new List<string>());

            newlineBuffer[channelName].Add(lineStr); //put string to buffer for write later

            if (newlineBuffer[channelName].Count == 30) //write every 30th line
                FlushBuffer();
        }

        /// <summary>
        /// Write all chat-lines to text file in ChatHistory folder. 
        /// Is automatically done every 30th line and every 30 second, 
        /// but should also be called when ZKL exit.
        /// </summary>
        public static void FlushBuffer()
        {
            try
            {
                foreach (var channelName in newlineBuffer)
                    if (channelName.Value.Count > 0)
                    {
                        var fileName = channelName.Key + ".txt";
                        Directory.CreateDirectory(historyFolder);
                        String[] lineArray = channelName.Value.ToArray();
                        lock (locker) File.AppendAllLines(Path.Combine(historyFolder, fileName), channelName.Value.ToArray());
                        //lock (locker) File.AppendAllText(Path.Combine(historyFolder, fileName), lineStr + Environment.NewLine);
                        channelName.Value.Clear();
                    }
            }
            catch (Exception e)
            {
                Trace.WriteLine("History manager: " + e);
            }
        }

        static private void timedFlush_Tick(object sender, EventArgs e)
        {
            FlushBuffer(); //every 30 second
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