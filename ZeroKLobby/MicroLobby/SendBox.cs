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

        public SendBox()
        {
            Multiline = true;
            this.Font = Program.Conf.ChatFont;
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (e.KeyChar == '\t') 
            {
                if (CompleteNick()) e.Handled = true; //intercept TAB when cursor is at end of a text (for autocomplete) but ignore other cases
            }
            if (e.KeyChar == '\r')
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
    }
}