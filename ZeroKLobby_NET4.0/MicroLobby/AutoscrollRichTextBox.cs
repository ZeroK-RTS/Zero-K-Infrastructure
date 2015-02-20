using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SpringDownloader.Lines;

namespace SpringDownloader.MicroLobby
{
    class AutoscrollRichTextBox: RichTextBox
    {
        List<IChatLine> lines = new List<IChatLine>();


        bool showHistory = true;
        bool showJoinLeave;
        public bool ShowHistory
        {
            get { return showHistory; }
            set
            {
                Refresh();
                showHistory = value;
            }
        }
        public bool ShowJoinLeave
        {
            get { return showJoinLeave; }
            set
            {
                Refresh();
                showJoinLeave = value;
            }
        }

        public AutoscrollRichTextBox()
        {
            Font = Program.Conf.Chat.ChatFont;
        }

        public void AddLine(IChatLine line)
        {
            lines.Add(line);
            WriteLine(line);
        }

        public void Refresh()
        {
            Text = String.Empty;
            lines.ForEach(WriteLine);
        }

        void WriteLine(IChatLine line)
        {
            if (!showJoinLeave && line is JoinLine || line is LeaveLine) return;
            if (!showHistory && line is HistoryLine) return;

            // var scrollToEnd = !Focused; // why?
            var scrollToEnd = true;
            var selectionStart = SelectionStart;
            var selectionLength = SelectionLength;

            foreach (var text in line.FormattedText)
            {
                SelectionColor = text.Color;
                using (var font = new Font(Font, text.Style))
                {
                    SelectionFont = font;
                    AppendText(text.Text);
                }
            }
            SelectionStart = selectionStart;
            SelectionLength = selectionLength;
            if (scrollToEnd)
            {
                SelectionStart = Text.Length;
                ScrollToCaret();
            }
        }
    }
}