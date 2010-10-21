using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using PlasmaShared;
using ZeroKLobby;
using ZeroKLobby.Lines;

namespace ZeroKLobby.MicroLobby
{
    public partial class ServerTab: UserControl, INavigatable
    {
        SendBox filterBox;
        SendBox sendBox;
        ChatBox textBox;

        public ServerTab()
        {
            InitializeComponent();
            if (Process.GetCurrentProcess().ProcessName == "devenv" && !Debugger.IsAttached) return;
            textBox = new ChatBox { Dock = DockStyle.Fill };
            Controls.Add(textBox);
            filterBox = new SendBox { Dock = DockStyle.Bottom, Text = "Filter (press enter)" };
            Controls.Add(filterBox);
            sendBox = new SendBox { Dock = DockStyle.Bottom, Text = "Raw Send" };
            Controls.Add(sendBox);
            Program.TasClient.Input += TasClient_Input;
            Program.TasClient.Output += TasClient_Output;
            sendBox.LineEntered += (s, e) => Program.TasClient.SendRaw(e.Data);
            filterBox.LineEntered += (s, e) => textBox.TextFilter = e.Data;
        }

        void TasClient_Input(object sender, TasInputArgs e)
        {
            textBox.AddLine(new FromServerLine(e.Command, e.Args));
            // File.AppendAllText("C:\\serverlog.txt", new FromServerLine(e.Command, e.Args).Text.StripAllCodes() + Environment.NewLine);
        }

        void TasClient_Output(object sender, EventArgs<KeyValuePair<string, object[]>> e)
        {
            textBox.AddLine(new ToServerLine(e.Data.Key, e.Data.Value.Select(a => a.ToString()).ToArray()));
        }

		public string PathHead { get { return "server"; } }
    	public bool TryNavigate(params string[] path)
    	{
			return path.Length > 0 && path[0] == PathHead;
    	}
    }
}