using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ZeroKLobby.MicroLobby
{
    public partial class TextColoringPanel : Form
    {
        private string[] history = new string[256]; //undo capacity: 256 minus 2 items
        private int historyPos = -1;
        private bool isUndoingText = false;
        private int progressType = 1;
        private bool waitForBackground = false;
        private int backgroundColor = 99;
        private bool tooltipOnce = false;
        private SendBox currentSendbox_ = null;
        private Timer timedUpdate = new Timer();
        public TextColoringPanel(SendBox currentSendbox)
        {
            timedUpdate.Interval = 50; //timer tick to add micro delay to ChatBox preview update.
            timedUpdate.Tick += timedUpdate_Tick;
            InitializeComponent();
            Icon = ZklResources.ZkIcon;
            button1.BackColor = TextColor.GetColor(0);//white
            button2.BackColor = TextColor.GetColor(1); //black
            button3.BackColor = TextColor.GetColor(2); //blue
            button4.BackColor = TextColor.GetColor(3); //green
            button5.BackColor = TextColor.GetColor(4); //red
            button6.BackColor = TextColor.GetColor(5); //brown
            button7.BackColor = TextColor.GetColor(6); //purple
            button8.BackColor = TextColor.GetColor(7); //orange
            button9.BackColor = TextColor.GetColor(8); //yellow
            button10.BackColor = TextColor.GetColor(9); //light green
            button11.BackColor = TextColor.GetColor(10); //teal
            button12.BackColor = TextColor.GetColor(11);
            button13.BackColor = TextColor.GetColor(12);
            button14.BackColor = TextColor.GetColor(13);
            button15.BackColor = TextColor.GetColor(14);
            button16.BackColor = TextColor.GetColor(15);
            if (sendBox.SelectionLength<=1) sendBox.SelectionStart = 0;
            comboBox1.SelectedItem = "To-line-end";
            ignoreSpaceCheck.Checked = true;

            sendBox.dontSendTextOnEnter = true; //pressing enter wont send text
            sendBox.ScrollBars = ScrollBars.Vertical;
            sendBox.dontUseUpDownHistoryKey = true;
            sendBox.CompleteWord += (word) => //autocomplete of username
            {
                var w = word.ToLower();
                IEnumerable<string> firstResult = new string[1];
                ChatControl zkChatArea = Program.MainWindow.navigationControl.ChatTab.GetChannelControl("zk");
                if (zkChatArea != null)
                {
                    IEnumerable<string> extraResult = zkChatArea.playerBox.GetUserNames()
                        .Where(x => x.ToLower().StartsWith(w))
                        .Union(zkChatArea.playerBox.GetUserNames().Where(x => x.ToLower().Contains(w)));
                    firstResult = firstResult.Concat(extraResult);
                }
                return firstResult;

            };
            sendBox.WordWrap = true;
            sendBox.Text = currentSendbox.Text; //copy paste from chat area to coloring panel
            currentSendbox_ = currentSendbox;
            Program.ToolTip.SetText(sendBox, "Tips: press CTRL+R/G/B on chatbox for instant Red,Green or Blue coloring");
        }

        private void button17_Click(object sender, EventArgs e) //remove color button
        {
            sendBox.SelectionStart = 0;
            sendBox.Text = TextColor.StripAllCodes(sendBox.Text);
            helpLabel.Text = "";
        }

        private void undoButton_Click(object sender, EventArgs e)
        {
            bool reachEnd = false;
            if (historyPos > 0) reachEnd = (history[historyPos-1] == null); //rolling history
            else reachEnd = (history[history.Length-1] == null);
            if (!reachEnd)
            {
                if (historyPos > 0) historyPos = historyPos - 1; //rolling history
                else historyPos = history.Length-1;
                isUndoingText = true;
                sendBox.Text = history[historyPos];
            }
        }

        private void comboBox1_SelectionChangeCommitted(object sender, EventArgs e)
        {
            sendBox.SelectionStart = 0;
            sendBox.SelectionLength = 0;
            if (comboBox1.SelectedItem == "To-line-end") progressType = 1;
            else if (comboBox1.SelectedItem == "Word-by-word") progressType = 2;
            else if (comboBox1.SelectedItem == "Char-by-char") progressType = 3;
            helpLabel.Text = "Do: select color";
        }

        private void ColorButtonClicked(object sender, EventArgs e)
        {
            int buttonNum = int.Parse((sender as Button).Name.Substring(6)) - 1; //convert string to number
            if (waitForBackground)
            {
                helpLabel.Text = "";
                backgroundcolor.Text = "";
                backgroundcolor.BackColor = TextColor.GetColor(buttonNum);
                waitForBackground = false; //background color selected, toggle off background color mode
                backgroundColor = buttonNum;
                return;
            }
            int selectionStart = sendBox.SelectionStart;
            if (sendBox.Text.Length > 0 && selectionStart < sendBox.Text.Length) //is not at string end
            {
                helpLabel.Text = "";
                if (sendBox.SelectionLength >= 1)
                {
                    sendBox.Text = sendBox.Text.Insert(selectionStart + sendBox.SelectionLength, "\x03");
                    sendBox.Text = sendBox.Text.Insert(selectionStart, "\x03" + buttonNum.ToString("00") + ","+ backgroundColor.ToString("00")); //convert number to string
                    sendBox.SelectionStart = selectionStart + 6;
                }
                else if (progressType == 1)
                {
                    if (ignoreSpaceCheck.Checked)
                        ColorWholeLine(buttonNum);
                    else
                        while (ColorWords(buttonNum)) ;
                }
                else if (progressType == 2)
                {
                    ColorWords(buttonNum);
                }
                else if (progressType == 3) //"Char-by-char"
                {
                    int newLineChar = IsAtNewLine(selectionStart);
                    int textLen = sendBox.Text.Length;
                    while (selectionStart < textLen && newLineChar > -1) //skip over current line-end
                    {
                        selectionStart = selectionStart + newLineChar;
                        newLineChar = IsAtNewLine(selectionStart);
                    }
                    if (ignoreSpaceCheck.Checked)
                    {
                        string tempSubstring = sendBox.Text.Substring(selectionStart, 1);
                        while (selectionStart < textLen && (tempSubstring == " " || tempSubstring == "\t")) //skip over space
                        {
                            selectionStart = selectionStart + 1;
                            if (selectionStart < textLen) tempSubstring = sendBox.Text.Substring(selectionStart, 1);
                        }
                    }
                    if (selectionStart < textLen) sendBox.Text = sendBox.Text.Insert(selectionStart + 1, "\x03");
                    else sendBox.Text = sendBox.Text + "\x03"; //end of string
                    sendBox.Text = sendBox.Text.Insert(selectionStart, "\x03" + buttonNum.ToString("00") + "," + backgroundColor.ToString("00"));
                    sendBox.SelectionStart = selectionStart + 8;
                }
            }
            else helpLabel.Text = "Do: check caret position/selection";
        }

        private int IsAtNewLine(int index)
        {
            if (index > sendBox.Text.Length - 1) return 0; //end-of-string
            else if (sendBox.Text.Substring(index, 1) == "\n") return 1; //Linux's newLine
            else if (sendBox.Text.Substring(index, 1) == "\r" && sendBox.Text.Substring(index + 1, 1) == "\n") return 2; //Windows' newLine
            else return -1;
        }

        private void ColorWholeLine(int colorNum)
        {
            int selectionStart = sendBox.SelectionStart;
            int textLen = sendBox.Text.Length;
            int newLineChar = IsAtNewLine(selectionStart);
            while (selectionStart < textLen && newLineChar > -1) //skip over current line-end
            {
                selectionStart = selectionStart + newLineChar;
                newLineChar = IsAtNewLine(selectionStart);
            }
            int begin = selectionStart;
            while (selectionStart < textLen && newLineChar == -1) //fast-forward over text
            {
                selectionStart = selectionStart + 1;
                newLineChar = IsAtNewLine(selectionStart);
            }
            sendBox.Text = sendBox.Text.Insert(selectionStart, "\x03");
            sendBox.Text = sendBox.Text.Insert(begin, "\x03" + colorNum.ToString("00") + "," + backgroundColor.ToString("00"));
            sendBox.SelectionStart = selectionStart + 7;
        }

        private bool ColorWords(int colorNum)
        {
            int selectionStart = sendBox.SelectionStart;
            int textLen = sendBox.Text.Length;
            int newLineChar = IsAtNewLine(selectionStart);
            while (selectionStart < textLen && newLineChar > -1) //skip over current line-end
            {
                selectionStart = selectionStart + newLineChar;
                newLineChar = IsAtNewLine(selectionStart);
            }
            int begin = selectionStart;
            string tempSubstring = sendBox.Text.Substring(selectionStart, 1);
            while (selectionStart < textLen && (tempSubstring == " " || tempSubstring == "\t")) //fast-forward over spaces
            {
                selectionStart = selectionStart + 1;
                if (selectionStart < textLen) tempSubstring = sendBox.Text.Substring(selectionStart, 1);
            }
            if (!ignoreSpaceCheck.Checked) begin = selectionStart;
            newLineChar = IsAtNewLine(selectionStart);
            while (selectionStart < textLen && tempSubstring != " " && tempSubstring != "\t" && newLineChar == -1) //fast-forward over text
            {
                selectionStart = selectionStart + 1;
                if (selectionStart < textLen) tempSubstring = sendBox.Text.Substring(selectionStart, 1); //next char
                newLineChar = IsAtNewLine(selectionStart);
            }
            sendBox.Text = sendBox.Text.Insert(selectionStart, "\x03");
            sendBox.Text = sendBox.Text.Insert(begin, "\x03" + colorNum.ToString("00") + "," + backgroundColor.ToString("00"));
            sendBox.SelectionStart = selectionStart + 7;
            return newLineChar == -1;
        }

        private void backgroundcolor_Click(object sender, EventArgs e)
        {
            waitForBackground = true;
            backgroundcolor.Text = "?";
            helpLabel.Text = "Do: select background color";
        }

        private void sendBox_TextChanged(object sender, EventArgs e)
        {
            timedUpdate.Start(); //accumulate update for 50ms to avoid spam when text is changed using Callout.
        }

        private void timedUpdate_Tick(object sender, EventArgs e)
        {
            timedUpdate.Stop(); //finish update, stop timer.
            chatBox.ClearTextWindow();
            chatBox.AppendText("  "); //+2 newline to make scrollbar appear earlier
            chatBox.AppendText("  ");
            String[] rowOfTexts = sendBox.Text.Split(new char[1] { '\n', });
            for (int i = 0; i < rowOfTexts.Length; i++)
                chatBox.AppendText(" " + rowOfTexts[i]); //extra space for color issue on preview window
            helpLabel.Text = "";
            if (!tooltipOnce)
            {
                Program.ToolTip.Clear(sendBox);
                tooltipOnce = true;
            }
            if (!isUndoingText)
            {
                if (historyPos < history.Length-1) historyPos = historyPos + 1; //rolling history
                else historyPos = 0;
                history[historyPos] = sendBox.Text;
                if (historyPos < history.Length-1) history[historyPos + 1] = null; //marker for end of history
                else history[0] = null;
            }
            isUndoingText = false;
        }

        private void pasteToChat_Click(object sender, EventArgs e)
        {
            currentSendbox_.Text = sendBox.Text;
        }

        private void TextColoringPanel_Resize(object sender, EventArgs e) //reposition un-anchored button during resize. Note: do not include 'raw' numbers or it need to take account of DPI scaling
        {
            int layoutPanelMidY = tableLayoutPanel1.Location.Y + tableLayoutPanel1.Height / 2;
            int layoutPanelRightMidBottomX = tableLayoutPanel1.Location.X + tableLayoutPanel1.Width - removeColorButton.Width;
            int layoutPanelRightMidBottomY = layoutPanelMidY - removeColorButton.Height;
            previewLabel.Location = new Point(tableLayoutPanel1.Location.X, layoutPanelMidY);
            removeColorButton.Location = new Point(layoutPanelRightMidBottomX, layoutPanelRightMidBottomY);
            undoButton.Location = new Point(layoutPanelRightMidBottomX - undoButton.Width, layoutPanelRightMidBottomY);
        }
    }
}
