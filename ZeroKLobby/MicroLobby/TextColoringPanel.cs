using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ZeroKLobby.MicroLobby
{
    public partial class TextColoringPanel : Form
    {
        private int progressType = 1;
        private bool waitForBackground = false;
        private int backgroundColor = 99;
        private bool tooltipOnce = false;
        public TextColoringPanel()
        {
            InitializeComponent();
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
            Program.ToolTip.SetText(sendBox, "Tips: press CTRL+R,G or B on chatbox for a quick Red,Green and Blue coloring respectively");
        }

        private void button17_Click(object sender, EventArgs e) //remove color button
        {
            sendBox.SelectionStart = 0;
            sendBox.Text = TextColor.StripAllCodes(sendBox.Text);
            helpLabel.Text = "";
        }

        private void comboBox1_SelectionChangeCommitted(object sender, EventArgs e)
        {
            sendBox.SelectionStart = 0;
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
                waitForBackground = false;
                backgroundColor = buttonNum;
                return;
            }
            int selectionStart = sendBox.SelectionStart;
            if (sendBox.Text.Length > 0 && selectionStart < sendBox.Text.Length)
            {
                helpLabel.Text = "";
                if (sendBox.SelectionLength > 1)
                {
                    sendBox.Text = sendBox.Text.Insert(selectionStart + sendBox.SelectionLength, "\x03");
                    sendBox.Text = sendBox.Text.Insert(selectionStart, "\x03" + buttonNum.ToString("00") + ","+ backgroundColor.ToString("00")); //convert number to string
                    sendBox.SelectionStart = selectionStart + 6;
                }
                else if (progressType == 1)
                {
                    sendBox.Text = sendBox.Text.Insert(sendBox.Text.Length, "\x03");
                    sendBox.Text = sendBox.Text.Insert(selectionStart, "\x03" + buttonNum.ToString("00") + "," + backgroundColor.ToString("00"));
                    sendBox.SelectionStart = sendBox.Text.Length;
                }
                else if (progressType == 2)
                {
                    int textLen = sendBox.Text.Length;
                    int begin = 1;
                    if (ignoreSpaceCheck.Checked) begin = selectionStart;
                    while (selectionStart <= textLen && (sendBox.Text.Substring(selectionStart, 1) == " " || sendBox.Text.Substring(selectionStart, 1) == "\t"))
                    {
                        selectionStart = selectionStart + 1;
                    }
                    if (!ignoreSpaceCheck.Checked) begin = selectionStart;
                    while (selectionStart < textLen && sendBox.Text.Substring(selectionStart, 1) != " " && sendBox.Text.Substring(selectionStart, 1) != "\t")
                    {
                        selectionStart = selectionStart + 1;
                    }
                    sendBox.Text = sendBox.Text.Insert(selectionStart, "\x03");
                    sendBox.Text = sendBox.Text.Insert(begin, "\x03" + buttonNum.ToString("00") + "," + backgroundColor.ToString("00"));
                    sendBox.SelectionStart = selectionStart + 7;
                }
                else if (progressType == 3) //"Char-by-char"
                {
                    if (ignoreSpaceCheck.Checked)
                    {
                        int textLen = sendBox.Text.Length;
                        while (selectionStart <= textLen && (sendBox.Text.Substring(selectionStart, 1) == " " || sendBox.Text.Substring(selectionStart, 1) == "\t"))
                        {
                            selectionStart = selectionStart + 1;
                        }
                    }
                    sendBox.Text = sendBox.Text.Insert(selectionStart + 1, "\x03");
                    sendBox.Text = sendBox.Text.Insert(selectionStart, "\x03" + buttonNum.ToString("00") + "," + backgroundColor.ToString("00"));
                    sendBox.SelectionStart = selectionStart + 8;
                }
            }
            else helpLabel.Text = "Do: check caret position/selection";
        }

        private void backgroundcolor_Click(object sender, EventArgs e)
        {
            waitForBackground = true;
            backgroundcolor.Text = "?";
            helpLabel.Text = "Do: select background color";
        }

        private void sendBox_TextChanged(object sender, EventArgs e)
        {
            chatBox.ClearTextWindow();
            chatBox.AppendText("  "); //+1 newline to make scrollbar appear earlier
            chatBox.AppendText(" " + sendBox.Text); //extra space for color issue on preview window
            helpLabel.Text = "";
            if (!tooltipOnce)
            {
                Program.ToolTip.Clear(sendBox);
                tooltipOnce = true;
            }
        }
    }
}
