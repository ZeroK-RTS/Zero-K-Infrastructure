using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using PlasmaShared;

namespace ZeroKLobby.MicroLobby
{
    public class SendBox : TextBox
    {
        readonly List<string> history = new List<string>();
        int historyIndex = 0;
        string ncFirstChunk;
        IEnumerator ncMatchesEn;
        string ncSecondChunk;
        string ncWordToComplete = "";
        bool nickCompleteMode;
        bool pressingEnter;
        public event Func<string, IEnumerable<string>> CompleteWord; //processed by ChatControl.cs
        public event EventHandler<EventArgs<string>> LineEntered = delegate { };
        bool isLinux = Environment.OSVersion.Platform == PlatformID.Unix;
        bool isPreviewingHistory = false;
        String currentText = String.Empty;
        public bool dontSendTextOnEnter = false; //allow multi-line text when pressing enter

        public SendBox()
        {
            Multiline = true;
            WordWrap = false; //long text continue to the right instead of appearing in new line
            this.Font = Program.Conf.ChatFont;
            this.BackColor = Program.Conf.BgColor;
            this.ForeColor = Program.Conf.TextColor;
            this.KeyDown += SendBox_KeyDown;
            this.MouseDown += SendBox_MouseDown;
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (e.KeyChar == '\t') 
            {
                if (CompleteNick()) e.Handled = true; //intercept TAB when cursor is at end of a text (for autocomplete) but ignore other cases
            }
            if (e.KeyChar == '\r' && !dontSendTextOnEnter)
            {
                if (!pressingEnter) SendTextNow(); //send text online
                e.Handled = true; //block ENTER because we already sent the text, no need to add newline character to textbox. We send it now rather than waiting the newline at OnKeyUp of ENTER (because we dont want this delay).
                pressingEnter = true; //remember that we already pressed ENTER to avoid spamming sendtext due to key repeat.
            }
            base.OnKeyPress(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            //if (Lines.Length > 1) //send text now if newline detected
            //{
            //    SendTextNow();
            //}
            if (pressingEnter && e.KeyCode == Keys.Return) pressingEnter = false; 
            base.OnKeyUp(e);
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                if (e.Control)
                {
                    ActionHandler.PrevButton();
                }
                else
                {
                    if (!isPreviewingHistory)
                    {
                        currentText = Text;
                        isPreviewingHistory = true;
                    }
                    historyIndex--;
                    if (historyIndex < 0) historyIndex = 0;
                    if (historyIndex < history.Count) Text = history[historyIndex];
                    if (historyIndex == history.Count) Text = currentText;
                    SelectionStart = Text.Length;
                }
            }
            else if (e.KeyCode == Keys.Down)
            {
                if (e.Control)
                {
                    ActionHandler.NextButton();
                }
                else
                {
                    if (!isPreviewingHistory)
                    {
                        currentText = Text;
                        isPreviewingHistory = true;
                    }
                    historyIndex++;
                    if (historyIndex < 0) historyIndex = 0;
                    if (historyIndex < history.Count) Text = history[historyIndex];
                    if (historyIndex == history.Count) Text = currentText;
                    if (historyIndex > history.Count)
                    {
                        historyIndex = history.Count+1;
                        Text = String.Empty;
                    }
                    SelectionStart = Text.Length;
                }
            }
            else
            {
                isPreviewingHistory = false;
                historyIndex = history.Count;
            }

            //Prevent cutting line in half when sending
            if (e.KeyCode == Keys.Return) SelectionStart = Text.Length;
            if (e.KeyCode != Keys.Tab) nickCompleteMode = false;

            base.OnPreviewKeyDown(e);
        }

        void SendTextNow()
        {
            var line = Text.Replace("\t", "        ").TrimEnd(new[] { '\r', '\n' });

            if (!string.IsNullOrEmpty(line))
            {
                history.Add(line);
                historyIndex = history.Count;
            }

            Text = String.Empty;
            LineEntered(this, new EventArgs<string>(line)); //send text online
        }

        bool CompleteNick()
        {
            if (CompleteWord == null) return false;

            var ss = SelectionStart; //cursor position
            if (isLinux)
            {
                ss = ss - 1; //in Linux Mono (not sure which version), OnKeyPress() is called after text is entered when different than in MS Windows
                if (ss <= 0) return false;
                var test = Text.Substring(ss - 1, 1);
                if (test == " " || test == "\t") return false;
                Text.Remove(ss, 1); //remove the pre-entered TAB
            }
            else
            {
                //don't bother nick complete if caret is at start of box or after a space or after a tab
                if (ss == 0) return false;
                var test = Text.Substring(ss - 1, 1);
                if (test == " " || test == "\t") return false;
            }

            if (!nickCompleteMode)
            {
                //split chatbox text chunks
                var ncFirstChunkTemp = Text.Substring(0, ss).Split(' ');
                ncFirstChunk = "";
                for (var i = 0; i < ncFirstChunkTemp.Length - 1; i++) ncFirstChunk += ncFirstChunkTemp[i] + " "; //text up to cursor
                ncSecondChunk = Text.Substring(ss);

                //word entered by user
                ncWordToComplete = ncFirstChunkTemp[ncFirstChunkTemp.Length - 1];

                //match up entered word and nick list, store in enumerator

                var ncMatches = CompleteWord(ncWordToComplete).ToList();

                if (ncMatches.Any())
                {
                    ncMatchesEn = ncMatches.GetEnumerator();
                    nickCompleteMode = true;
                }
            }

            if (nickCompleteMode)
            {
                //get next matched nickname
                if (!ncMatchesEn.MoveNext())
                {
                    ncMatchesEn.Reset();
                    ncMatchesEn.MoveNext();
                }
                var nick = ncMatchesEn.Current.ToString();

                //remake chatbox text
                Text = ncFirstChunk + nick + ncSecondChunk;
                SelectionStart = ncFirstChunk.Length + nick.Length;
            }
            return true;
        }

        
        //Reference: http://stackoverflow.com/questions/1124639/winforms-textbox-using-ctrl-backspace-to-delete-whole-word (second answer)
        internal void CtrlBackspace()
        {
            int selStart = SelectionStart;
            while (selStart > 0 && Text.Substring(selStart - 1, 1) == " ")
            {
                selStart--;
            }
            int prevSpacePos = -1;
            if (selStart != 0)
            {
                prevSpacePos = Text.LastIndexOf(' ', selStart - 1);
            }
            Select(prevSpacePos + 1, SelectionStart - prevSpacePos - 1);
            SelectedText = "";
        }

        internal void InsertColorCharacter(string textColor,string backColor)
        {
            if (SelectionLength > 1) //color selection
            {
                int curSelectionStart = SelectionStart;
                int selLen = SelectionLength;
                Text = Text.Insert(curSelectionStart + SelectionLength, "\x03");
                Text = Text.Insert(curSelectionStart, "\x03" + textColor + "," + backColor);
                SelectionStart = curSelectionStart + selLen + 6;
            }
            else if (SelectionStart > 0) //color previous word
            {
                int end = SelectionStart;
                int begin = SelectionStart-1;
                while (begin > 0 && (Text.Substring(begin, 1) == " " || Text.Substring(begin, 1) == "\t"))
                {
                    begin = begin - 1;
                }
                while (begin > 0 && Text.Substring(begin, 1) != " " && Text.Substring(begin, 1) != "\t")
                {
                    begin = begin - 1;
                }
                Text = Text.Insert(end, "\x03");
                Text = Text.Insert(begin, "\x03" + textColor + "," + backColor);
                SelectionStart = end + 6;
            }
        }

        //Ctrl+A and Ctrl+Backspace behaviour.
        //Reference: http://stackoverflow.com/questions/14429445/how-can-i-allow-things-such-as-ctrl-a-and-ctrl-backspace-in-a-c-sharp-textbox

        private void SendBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control & e.KeyCode == Keys.A)
            {
                SelectAll();
            }
            else if (e.Control & e.KeyCode == Keys.Back)
            {
                e.SuppressKeyPress = true;
                CtrlBackspace();
            }else if (e.Control & e.KeyCode == Keys.R)
                InsertColorCharacter("04","12"); //red on light cyan
            else if (e.Control & e.KeyCode == Keys.G)
                InsertColorCharacter("03", "12"); //green on light cyan
            else if (e.Control & e.KeyCode == Keys.B)
                InsertColorCharacter("02", "12");//blue on light cyan
        }

        private int clickCount = 0;
        private long lastClick = 0;
        private int systemDoubleClickTime = SystemInformation.DoubleClickTime * 10000;
        private void SendBox_MouseDown(object sender, EventArgs e)
        {   //reference: http://stackoverflow.com/questions/5014825/triple-mouse-click-in-c
            //10,000 ticks is a milisecond, therefore 2,000,000 ticks is 200milisecond . http://msdn.microsoft.com/en-us/library/system.datetime.ticks.aspx
            //double click time: http://msdn.microsoft.com/en-us/library/system.windows.forms.systeminformation.doubleclicktime(v=vs.110).aspx
            if (DateTime.Now.Ticks - lastClick <= systemDoubleClickTime) clickCount = clickCount + 1;
            else clickCount = 1;
            if (clickCount%3 == 0) SelectAll(); //select all text when triple click
            lastClick = DateTime.Now.Ticks;
        }
    }
}