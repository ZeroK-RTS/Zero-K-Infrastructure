using System;
using System.Diagnostics;
using System.IO;
using ZeroKLobby;
using ZeroKLobby.Lines;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;

namespace ZeroKLobby.MicroLobby
{
    static class HistoryManager
    {
        public const int HistoryLines = 30;
        
        static private Dictionary<string,List<string>> newlineBuffer = new Dictionary<string,List<string>>();
        static private Timer timedFlush = new Timer();

        static readonly object locker = new object();


        private static string GetHistoryFolder()
        {
            if (Program.SpringPaths != null) return Path.Combine(Program.SpringPaths.Cache, "ChatHistory");
            else return "ChatHistory";
        }



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
                if (!File.Exists(Path.Combine(GetHistoryFolder(), historyFileName))) return;
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
        /// but also called when ZKL exit (at Program.cs).
        /// </summary>
        public static void FlushBuffer()
        {
            try
            {
                foreach (var channelName in newlineBuffer)
                    if (channelName.Value.Count > 0)
                    {
                        var fileName = channelName.Key + ".txt";
                        Directory.CreateDirectory(GetHistoryFolder());
                        String[] lineArray = channelName.Value.ToArray();
                        lock (locker) File.AppendAllLines(Path.Combine(GetHistoryFolder(), fileName), channelName.Value.ToArray());
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
            var path = Path.Combine(GetHistoryFolder(), channel + ".txt");
            try
            {
                Process.Start(path);
            }
            catch {}
        }

        static string[] GetLines(string fileName)
        {
            //lock (locker) return File.ReadAllLines(Path.Combine(historyFolder, fileName));
            lock (locker)
            {
                //Open the stream and read it from back. 
                using (FileStream fileStream = File.OpenRead(Path.Combine(GetHistoryFolder(), fileName)))
                {
                    var fileLen = fileStream.Length;
                    var lenToRead = (int)Math.Min(fileLen, 3990);
                    fileStream.Seek(-lenToRead, SeekOrigin.End);
                    List<string> lines = new List<string>();
                    using (var stream = new StreamReader(fileStream))
                        while (!stream.EndOfStream)
                            lines.Add(stream.ReadLine());
                    return lines.ToArray();
                }
            }
        }
    }
}