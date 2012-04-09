using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using PlasmaShared;
using PlasmaShared.ContentService;
using AutohostMode = ZkData.AutohostMode;
using GamePreference = ZkData.GamePreference;

namespace ZeroKLobby.Notifications
{
    public partial class JugglerBar: UserControl, INotifyBar
    {
        readonly Dictionary<string, InfoItems> Items = new Dictionary<string, InfoItems>();
        TasClient client;
        public NotifyBarContainer BarContainer { get; private set; }

        private bool suppressChangeEvent = false;

        public JugglerBar(TasClient client)
        {
            InitializeComponent();
            this.client = client;

            
            var cnt = 0;

            foreach (var mode in Enum.GetValues(typeof(AutohostMode)).OfType<AutohostMode>().Where(x => x != AutohostMode.None))
            {
                var item = new InfoItems();
                Items.Add(mode.ToString(), item);
                var y = 0 + (cnt%3)*24;
                var x = 0 + (cnt/3)*230;
                Controls.Add(new Label() { Left = x + 0, Width = 160, TextAlign = ContentAlignment.TopRight, Top = y, Text = mode.Description() });
                item.ComboBox = new ComboBox() { Left = x + 165, Width = 60, Top = y, DropDownStyle = ComboBoxStyle.DropDownList };
                foreach (var pref in Enum.GetValues(typeof(GamePreference)).OfType<GamePreference>().OrderByDescending(z => (int)z)) item.ComboBox.Items.Add(new CbItem() { Value = pref });
                item.ComboBox.SelectedValueChanged += (sender, args) => { if (!suppressChangeEvent) SendMyConfig(true); };

                Controls.Add(item.ComboBox);
                item.Label = new Label() { Left = x + 225, Top = y, Width = 20 };
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

            client.BattleClosed += (sender, args) =>
            {
                if (BarContainer != null) BarContainer.btnStop.Enabled = true;
            };

            this.timer = new Timer();
            timer.Interval = 30000;
            timer.Tick += (sender, args) => GetJugglerState();

        }

        public void Activate() {
            if (!Program.NotifySection.Bars.Contains(this)) Program.NotifySection.AddBar(this);
            SendMyConfig(false);
            timer.Enabled = true;
        }


        JugglerState lastState;
        Timer timer;

        public void GetJugglerState() {
            var cs = new ContentService();
            cs.GetJugglerStateCompleted += (sender, args) =>
            {
                if (!args.Cancelled && args.Error == null)
                {
                    var res = args.Result;
                    lastState = res;
                    if (BarContainer != null) BarContainer.btnDetail.Text = "MatchMaking " + res.TotalPlayers + " players";
                    foreach (var entry in res.ModeCounts)
                    {
                        InfoItems item;
                        if (Items.TryGetValue(entry.Mode.ToString(), out item)) item.Label.Text = entry.Count.ToString();
                    }
                }
            };
            cs.GetJugglerStateAsync();

        }

        public void GetMyConfig() {
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
                    var cb = (CbItem)item.Value.ComboBox.SelectedItem;;
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
            cs.SetJugglerConfig(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword, conf);
        }

        public void AddedToContainer(NotifyBarContainer container)
        {
            BarContainer = container;
            container.btnDetail.Text = "MatchMaking ";
            if (lastState != null) container.btnDetail.Text+= lastState.TotalPlayers +  " players"; ;
        }

        public void CloseClicked(NotifyBarContainer container)
        {
            if (client.MyBattle == null || !client.MyBattle.Founder.IsSpringieManaged)
            {
                Deactivate();
            }
        }

        void Deactivate()
        {
            Program.NotifySection.RemoveBar(this);
            SendMyConfig(false);
            timer.Enabled = false;
        }

        public void DetailClicked(NotifyBarContainer container) {
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