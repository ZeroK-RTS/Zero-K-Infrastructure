using System;
using System.Collections.Generic;
using ZeroKLobby;
using ZeroKLobby.Lines;

namespace ZeroKLobby.MicroLobby
{
    public class ChatBox: TextWindow
    {
        string filter;
        string highlight;
        List<IChatLine> lines = new List<IChatLine>();

        bool highlightActive;
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
        
        public string LineHighlight
        {
            get { return highlight; }
            set
            {
                highlight = value;
                highlightActive = !string.IsNullOrWhiteSpace(value);
                RefreshText();
            }
        }

        public void AddLine(IChatLine line)
        {
            lines.Add(line);
            WriteLine(line);
        }

        public void RefreshText()
        {
            ClearTextWindow();

            foreach (var line in lines)
                WriteLine(line);
        }

        public void Reset()
        {
            Text = "";
            lines.Clear(); // clear() seems to be more efficient than new List because it just reset index. Reference: http://stackoverflow.com/questions/5358129/how-is-list-clear-implemented-in-c ;
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

        bool DeHighlighted(string text)
        {
            return (text.Contains(highlight));
        }

        void WriteLine(IChatLine line)
        {
            if (PassesFilter(line)) 
            {
                if (highlightActive && !DeHighlighted(line.Text))
                    line = new HistoryLine(line.Text);

                var splitText = line.Text.Replace("\r\n", "\n").Replace("\\n", "\n").Split('\n');
                AppendText(splitText[0]);
                string padding = (line is TopicLine) ? "" : "        "; //padding to avoid player spoofing different player using multi-line & color & formating
                for (int i = 1; i < splitText.Length;i++ )
                    AppendText(padding + splitText[i]);
            }
        }

        private void InitializeComponent()  //minimum size >0 avoid chat window crash
        {
            Font = Config.ChatFont;
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
