using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using PlasmaShared;
using PlasmaShared.ContentService;
using ZeroKLobby.MicroLobby;
using AutohostMode = ZkData.AutohostMode;
using GamePreference = ZkData.GamePreference;

namespace ZeroKLobby.Notifications
{
    public partial class JugglerBar: UserControl, INotifyBar
    {
        readonly Dictionary<string, InfoItems> Items = new Dictionary<string, InfoItems>();
        readonly TasClient client;
        JugglerState lastState;
        readonly ChatBox picoChat;

        bool suppressChangeEvent = false;
        readonly Timer timer;
        public NotifyBarContainer BarContainer { get; private set; }

        public JugglerBar(TasClient client)
        {
            InitializeComponent();
            this.client = client;

            var cnt = 0;
            int xOffset = 0, yOffset = 0;
            foreach (var mode in Enum.GetValues(typeof(AutohostMode)).OfType<AutohostMode>().Where(x => x != AutohostMode.None))
            {
                xOffset = 0 + (cnt/3)*205;
                yOffset = 0 + (cnt%3)*24;
                var item = new InfoItems();
                Items.Add(mode.ToString(), item);

                Controls.Add(new Label()
                             { Left = xOffset + 0, Width = 120, TextAlign = ContentAlignment.TopRight, Top = yOffset, Text = mode.Description() });
                item.ComboBox = new ComboBox() { Left = xOffset + 125, Width = 60, Top = yOffset, DropDownStyle = ComboBoxStyle.DropDownList };
                foreach (var pref in Enum.GetValues(typeof(GamePreference)).OfType<GamePreference>().OrderByDescending(z => (int)z)) item.ComboBox.Items.Add(new CbItem() { Value = pref });
                item.ComboBox.SelectedValueChanged += (sender, args) => { if (!suppressChangeEvent) SendMyConfig(true); };

                Controls.Add(item.ComboBox);
                item.Label = new Label() { Left = xOffset + 185, Top = yOffset, Width = 20 };
                Controls.Add(item.Label);

                cnt++;
            }

            GetJugglerState();
            GetMyConfig();

            client.BattleJoined += (sender, args) =>
                {
                    if (args.Data.Founder.IsSpringieManaged)
                    {
                        Activate();
                        BarContainer.btnStop.Enabled = false;
                    }
                };

            client.BattleClosed += (sender, args) => { if (BarContainer != null) BarContainer.btnStop.Enabled = true; };

            timer = new Timer();
            timer.Interval = 30000;
            timer.Tick += (sender, args) => GetJugglerState();

            picoChat = new ChatBox() { Left = xOffset + 208, Top = 0 };
            picoChat.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            picoChat.Width = Width - picoChat.Left - 3;
            picoChat.Height = Height - picoChat.Top - 10;
            picoChat.BorderStyle = BorderStyle.FixedSingle;

            picoChat.Font = new Font(Program.Conf.ChatFont.FontFamily, Program.Conf.ChatFont.Size*0.8f);
            picoChat.ShowHistory = false;
            picoChat.ShowJoinLeave = false;
            picoChat.HideScroll = false;
            ChatControl.ChannelLineAdded += (sender, args) => { if (args.Channel == "quickmatch") picoChat.AddLine(args.Line); };
            picoChat.MouseClick += (s, e) => NavigationControl.Instance.Path = "chat/channel/quickmatch";

            Controls.Add(picoChat);
        }

        public void Activate()
        {
            if (!Program.NotifySection.Bars.Contains(this)) Program.NotifySection.AddBar(this);
            SendMyConfig(false);
            timer.Enabled = true;
            client.JoinChannel("quickmatch");
        }


        public void GetJugglerState()
        {
            var cs = new ContentService();
            cs.GetJugglerStateCompleted += (sender, args) =>
                {
                    if (!args.Cancelled && args.Error == null)
                    {
                        var res = args.Result;
                        lastState = res;
                        if (BarContainer != null) BarContainer.btnDetail.Text = "QuickMatch " + res.TotalPlayers + " players";
                        foreach (var entry in res.ModeCounts)
                        {
                            InfoItems item;
                            if (Items.TryGetValue(entry.Mode.ToString(), out item)) item.Label.Text = entry.Count.ToString();
                        }
                    }
                };
            cs.GetJugglerStateAsync();
        }

        public void GetMyConfig()
        {
            var cs2 = new ContentService();
            cs2.GetJugglerConfigCompleted += (sender, args) =>
                {
                    if (!args.Cancelled && args.Error == null)
                    {
                        suppressChangeEvent = true;
                        var res = args.Result;
                        foreach (var entry in res.Preferences)
                        {
                            InfoItems item;
                            if (Items.TryGetValue(entry.Mode.ToString(), out item))
                            {
                                var cb = item.ComboBox;
                                cb.SelectedItem = cb.Items.OfType<CbItem>().FirstOrDefault(x => x.Value.ToString() == entry.Preference.ToString());
                            }
                        }
                        suppressChangeEvent = false;
                        if (res.Active) Activate();
                    }
                };

            cs2.GetJugglerConfigAsync(Program.Conf.LobbyPlayerName);
        }


        public void SendMyConfig(bool sendPreferences)
        {
            var conf = new JugglerConfig();
            conf.Active = Program.NotifySection.Bars.Contains(this);
            if (sendPreferences)
            {
                var prefs = new List<PreferencePair>();
                foreach (var item in Items)
                {
                    var cb = (CbItem)item.Value.ComboBox.SelectedItem;
                    ;
                    if (cb != null)
                    {
                        var comboValue = cb.Value;
                        var preference =
                            Enum.GetValues(typeof(PlasmaShared.ContentService.GamePreference)).OfType<PlasmaShared.ContentService.GamePreference>().
                                FirstOrDefault(x => x.ToString() == comboValue.ToString());
                        var autohostMode =
                            Enum.GetValues(typeof(PlasmaShared.ContentService.AutohostMode)).OfType<PlasmaShared.ContentService.AutohostMode>().
                                FirstOrDefault(x => x.ToString() == item.Key);

                        prefs.Add(new PreferencePair() { Mode = autohostMode, Preference = preference });
                    }
                }
                conf.Preferences = prefs.ToArray();
            }

            var cs = new ContentService();
            cs.SetJugglerConfigAsync(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword, conf);
        }

        void Deactivate()
        {
            Program.NotifySection.RemoveBar(this);
            SendMyConfig(false);
            timer.Enabled = false;
            ActionHandler.CloseChannel("quickmatch");
        }

        public void AddedToContainer(NotifyBarContainer container)
        {
            BarContainer = container;
            container.btnDetail.Text = "QuickMatch ";
            if (lastState != null) container.btnDetail.Text += lastState.TotalPlayers + " players";
            ;
        }

        public void CloseClicked(NotifyBarContainer container)
        {
            if (client.MyBattle == null || !client.MyBattle.Founder.IsSpringieManaged) Deactivate();
        }

        public void DetailClicked(NotifyBarContainer container)
        {
            GetMyConfig();
            GetJugglerState();
        }

        public Control GetControl()
        {
            return this;
        }

        public class CbItem
        {
            public GamePreference Value;

            public override string ToString()
            {
                return Value.Description();
            }
        }
    }

    class InfoItems
    {
        public ComboBox ComboBox;
        public Label Label;
    }
}