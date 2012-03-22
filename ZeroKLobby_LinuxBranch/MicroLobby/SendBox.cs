using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using PlasmaShared;

namespace ZeroKLobby.MicroLobby
{
	public class SendBox: TextBox
	{
		readonly List<string> history = new List<string>();
		int historyIndex = 0;
		string ncFirstChunk;
		IEnumerator ncMatchesEn;
		string ncSecondChunk;
		string ncWordToComplete = "";
		bool nickCompleteMode;
		public event Func<string, IEnumerable<string>> CompleteWord;
		public event EventHandler<EventArgs<string>> LineEntered = delegate { };

		public SendBox()
		{
			Multiline = true;
            this.Font = Program.Conf.ChatFont;
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar == '\t')
			{
				CompleteNick();
				e.Handled = true;
			}
			base.OnKeyPress(e);
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (Lines.Length > 1)
			{
				var line = Text.Replace("\t", "  ").TrimEnd(new[] { '\r', '\n' });

				if (!string.IsNullOrEmpty(line))
				{
					history.Add(line);
					historyIndex = history.Count;
				}

				Text = String.Empty;
				LineEntered(this, new EventArgs<string>(line));
			}
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
                    historyIndex--;
                    if (historyIndex < 0) historyIndex = 0;

                    if (history.Count > historyIndex) Text = history[historyIndex];
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
                    historyIndex++;
                    if (historyIndex >= history.Count) historyIndex = history.Count - 1;
                    if (historyIndex < 0) historyIndex = 0;
                    if (history.Count > historyIndex) Text = history[historyIndex];
                }
			}
			else historyIndex = history.Count;

			//Prevent cutting line in half when sending
			if (e.KeyCode == Keys.Return) SelectionStart = Text.Length;
			if (e.KeyCode != Keys.Tab) nickCompleteMode = false;

			base.OnPreviewKeyDown(e);
		}


		void CompleteNick()
		{
			if (CompleteWord == null) return;

			var ss = SelectionStart;

			//don't bother nick complete if caret is at start of box or after a space
			if (ss == 0) return;
			var test = Text.Substring(ss - 1, 1);
			if (test == " ") return;

			if (!nickCompleteMode)
			{
				//split chatbox text chunks
				var ncFirstChunkTemp = Text.Substring(0, ss).Split(' ');
				ncFirstChunk = "";
				for (var i = 0; i < ncFirstChunkTemp.Length - 1; i++) ncFirstChunk += ncFirstChunkTemp[i] + " ";
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
		}
	}
}