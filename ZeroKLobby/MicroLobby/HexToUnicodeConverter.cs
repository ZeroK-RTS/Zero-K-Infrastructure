using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

namespace ZeroKLobby.MicroLobby
{
    public partial class HexToUnicodeConverter : Form
    {
        private int activeSendbox = 1;
        public HexToUnicodeConverter()
        {
            InitializeComponent();
            Icon = ZklResources.ZkIcon;
            sendBox1.dontSendTextOnEnter = true; //pressing enter wont send text
            sendBox1.dontUseUpDownHistoryKey = true;
            sendBox2.dontSendTextOnEnter = true;
            sendBox2.dontUseUpDownHistoryKey = true;
        }

        private void sendBox2_TextChanged(object sender, EventArgs e)
        {
            if (activeSendbox == 2)
            {
                //Reference: http://msdn.microsoft.com/en-us/library/zf50za27(v=vs.100).aspx
                //Reference2: http://social.msdn.microsoft.com/Forums/vstudio/en-US/786480e4-f020-41a7-a545-8f691b646ba1/convert-string-to-hex
                Int32 number;
                if (Int32.TryParse(sendBox2.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out number))
                {
                    sendBox1.Text = ((char)number).ToString();
                }
                else
                {
                    if (sendBox2.Text == String.Empty) sendBox1.Text = String.Empty;
                    else sendBox1.Text = "Ops! Check value.";
                    // TODO: Error processing
                } 
            }
        }

        private void sendBox1_TextChanged(object sender, EventArgs e)
        {
            if (activeSendbox == 1)
            {
                //Char to Hex(1). Reference: http://social.msdn.microsoft.com/Forums/vstudio/en-US/786480e4-f020-41a7-a545-8f691b646ba1/convert-string-to-hex
                //char[] chars = sendBox2.Text.ToCharArray();
                //StringBuilder stringBuilder = new StringBuilder();
                //foreach (char c in chars)
                //{
                //stringBuilder.Append(((Int16)c).ToString("x"));
                //}
                //String textAsHex = stringBuilder.ToString();
                //sendBox1.Text=textAsHex;
                
                //Char to Hex(2). Reference: http://social.msdn.microsoft.com/Forums/vstudio/en-US/9a09cb14-5eb3-4b74-9cf1-ac9e0ae641fc/convert-string-to-unicode
                byte[] stringBytes = Encoding.Unicode.GetBytes(sendBox1.Text);
                char[] stringChars = Encoding.Unicode.GetChars(stringBytes);
                StringBuilder builder = new StringBuilder();
                Array.ForEach<char>(stringChars, c => builder.AppendFormat("\\u{0:X}", (int)c));
                sendBox2.Text = builder.ToString();
            }
        }

        private void sendBox2_Enter(object sender, EventArgs e)
        {
            activeSendbox = 2;
        }

        private void sendBox1_Enter(object sender, EventArgs e)
        {
            activeSendbox = 1;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Program.MainWindow.navigationControl.Path = "http://unicode-table.com/en/sets/";
        }
    }
}
