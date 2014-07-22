using System;
using System.Collections.Generic;
using ZeroKLobby;
using ZeroKLobby.Lines;

namespace ZeroKLobby.MicroLobby
{
    public class ChatBox: TextWindow
    {
        string filter;
        List<IChatLine> lines = new List<IChatLine>();

        bool showHistory = true;
        bool showJoinLeave;
        public bool ShowHistory
        {
            get { return showHistory; }
            set
            {
                showHistory = value;
                RefreshText();
            }
        }
        public bool ShowJoinLeave
        {
            get { return showJoinLeave; }
            set
            {
                showJoinLeave = value;
                RefreshText();
            }
        }
        public string TextFilter
        {
            get { return filter; }
            set
            {
                filter = value;
                RefreshText();
            }
        }

        public void AddLine(IChatLine line)
        {
            lines.Add(line);
            if (PassesFilter(line)) WriteLine(line);
        }

        public void RefreshText()
        {
            ClearTextWindow();

            foreach (var line in lines) if (PassesFilter(line)) WriteLine(line);
        }

        public void Reset()
        {
            Text = "";
            lines = new List<IChatLine>();
            RefreshText();
        }

        public bool PassesFilter(IChatLine line)
        {
            if (!showJoinLeave && (line is JoinLine || line is LeaveLine)) return false;
            if (!showHistory && line is HistoryLine) return false;
            if (!Program.Conf.ShowHourlyChimes && line is ChimeLine) return false;
            if (!String.IsNullOrEmpty(TextFilter)) return line.Text.Contains(TextFilter);
            return true;
        }

        void WriteLine(IChatLine line)
        {
            var splitText = line.Text.Replace("\r\n", "\n").Replace("\\n", "\n").Split('\n');
            foreach (var t in splitText) AppendText(t);
        }

        private void InitializeComponent()  //minimum size >0 avoid chat window crash
        {
            this.SuspendLayout();
            // 
            // ChatBox
            // 
            this.MinimumSize = new System.Drawing.Size(10, 10);
            this.Name = "ChatBox";
            this.ResumeLayout(false);

        }
    }
}