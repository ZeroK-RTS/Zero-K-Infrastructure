using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using Newtonsoft.Json;
using ZkData;
using ZeroKLobby.Lines;

namespace ZeroKLobby.MicroLobby
{
    public partial class ServerTab: UserControl, INavigatable
    {
        readonly List<IChatLine> entries = new List<IChatLine>();
        readonly SendBox filterBox;
        bool prevVis = false;
        readonly SendBox sendBox;
        readonly ChatBox textBox;
        const int DisplayLines = 500;

        public ServerTab() {
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
            filterBox.LineEntered += (s, e) =>
                {
                    textBox.ClearTextWindow();
                    
                    textBox.TextFilter = e.Data;
                    var filtered = entries.Where(x => textBox.PassesFilter(x)).ToList();
                    foreach (var chatLine in filtered.Skip(Math.Max(filtered.Count - DisplayLines, 0)).Take(DisplayLines)) textBox.AddLine(chatLine);
                    
                };
            VisibleChanged += (sender, args) =>
                {
                    if (prevVis != Visible && Visible) {
                        textBox.ClearTextWindow();
                        foreach (var chatLine in entries.Skip(Math.Max(entries.Count - DisplayLines, 0)).Take(DisplayLines)) textBox.AddLine(chatLine);
                    }
                    prevVis = Visible;
                };

            textBox.ChatBackgroundColor = TextColor.background; //same as Program.Conf.BgColor but TextWindow.cs need this.
            textBox.IRCForeColor = 14; //mirc grey. Unknown use
        }

        public string PathHead { get { return "server"; } }

        public bool TryNavigate(params string[] path)
        {
            return path.Length > 0 && path[0] == PathHead;
        }

        public bool Hilite(HiliteLevel level, string path) {
            return false;
        }

        public string GetTooltip(params string[] path) {
            return null;
        }

        public void Reload() {
            
        }

        public bool CanReload { get { return false; }}

        public bool IsBusy { get { return false; } }

        void TasClient_Input(object sender, object o) {
            if (o != null) {
                var entry = new FromServerLine(o);
                entries.Add(entry);
                if (prevVis) Program.MainWindow.InvokeFunc(() => textBox.AddLine(entry));
            }
        }

        void TasClient_Output(object sender, object o) {
            if (o != null) {
                var entry = new ToServerLine(o);
                entries.Add(entry);
                if (prevVis) Program.MainWindow.InvokeFunc(() => textBox.AddLine(entry));
            }
        }
    }
}