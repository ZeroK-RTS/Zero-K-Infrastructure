using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using PlasmaShared;
using ZeroKLobby.MicroLobby;
using ZkData;

namespace ZeroKLobby.Notifications
{
    public partial class JugglerBar: UserControl, INotifyBar
    {
        readonly Dictionary<AutohostMode, InfoItems> Items = new Dictionary<AutohostMode, InfoItems>();
        readonly TasClient client;
        ProtocolExtension.JugglerState lastState;
        bool suppressChangeEvent = false;
        public NotifyBarContainer BarContainer { get; private set; }

        public JugglerBar(TasClient client)
        {
            InitializeComponent();
            this.client = client;

            var info = "Set your game type preferences for QuickMatch. System can move you to other room, if you like that type more than type of currently joined room";

            Program.ToolTip.SetText(this,info);

            var cnt = 0;
            int xOffset = 0, yOffset = 0;
            foreach (var mode in Enum.GetValues(typeof(AutohostMode)).OfType<AutohostMode>().Where(x => x != AutohostMode.None))
            {
                xOffset = 0 + (cnt/2)*210;
                yOffset = 0 + (cnt%2)*21;
                var item = new InfoItems();
                Items.Add(mode, item);

                Controls.Add(new Label()
                             { Left = xOffset + 0, Width = 120, TextAlign = ContentAlignment.TopRight, Top = yOffset, Text = mode.Description() });
                item.ComboBox = new ComboBox() { Left = xOffset + 125, Width = 60, Top = yOffset, DropDownStyle = ComboBoxStyle.DropDownList };
                foreach (var pref in Enum.GetValues(typeof(GamePreference)).OfType<GamePreference>().OrderByDescending(z => (int)z)) item.ComboBox.Items.Add(new CbItem() { Value = pref });
                item.ComboBox.SelectedValueChanged += (sender, args) => { if (!suppressChangeEvent) SendMyConfig(true); };

                Program.ToolTip.SetText(item.ComboBox,info);

                Controls.Add(item.ComboBox);
                item.Label = new Label() { Left = xOffset + 185, Top = yOffset, Width = 25 };
                Controls.Add(item.Label);

                Program.ToolTip.SetText(item.Label,"How many waiting people are OK with that type of game");

                cnt++;
            }
            
            client.BattleJoined += (sender, args) =>
                {
                    if (args.Data.Founder.IsSpringieManaged)
                    {
                        Activate();
                    }
                };

            client.BattleMyUserStatusChanged += (sender, args) =>
            {
                if (client.MyBattleStatus.IsSpectator)
                {
                    // if (Program.NotifySection.Bars.Contains(this)) Deactivate();
                }
                else {
                    if (client.MyBattle.Founder.IsSpringieManaged && !Program.NotifySection.Bars.Contains(this)) Activate();
                }
            };


            client.Extensions.JugglerStateReceived += (args, state) =>
            {
                lastState = state;
                if (BarContainer != null) BarContainer.btnDetail.Text = "QuickMatch " + state.TotalPlayers + " players";
                foreach (var entry in state.ModeCounts)
                {
                    InfoItems item;
                    if (Items.TryGetValue(entry.Mode, out item)) item.Label.Text = "(" + entry.Count.ToString() + ")";
                }
            };

            client.LoginAccepted += (sender, args) =>
            {
                SendMyConfig(false);
            };

            client.Extensions.JugglerConfigReceived += (args, config) =>
            {
                if (args.UserName == GlobalConst.NightwatchName) UpdateMyConfig(config);
            };
        }


        public void Activate()
        {
            if (!Program.NotifySection.Bars.Contains(this)) Program.NotifySection.AddBar(this);
            SendMyConfig(false);
        }


        private void UpdateMyConfig(ProtocolExtension.JugglerConfig res ) {
            suppressChangeEvent = true;
            foreach (var entry in res.Preferences)
            {
                InfoItems item;
                if (Items.TryGetValue(entry.Mode, out item))
                {
                    var cb = item.ComboBox;
                    cb.SelectedItem = cb.Items.OfType<CbItem>().FirstOrDefault(x => x.Value == entry.Preference);
                }
            }
            suppressChangeEvent = false;
            if (res.Active && !Program.NotifySection.Bars.Contains(this)) Program.NotifySection.AddBar(this);
            if (!res.Active && Program.NotifySection.Bars.Contains(this)) Program.NotifySection.RemoveBar(this);
        }


        private void SendMyConfig(bool sendPreferences)
        {
            var conf = new ProtocolExtension.JugglerConfig();
            conf.Active = Program.NotifySection.Bars.Contains(this);
            if (sendPreferences)
            {
                var prefs = new List<ProtocolExtension.JugglerConfig.PreferencePair>();
                foreach (var item in Items)
                {
                    var cb = (CbItem)item.Value.ComboBox.SelectedItem;
                    ;
                    if (cb != null)
                    {
                        var preference = cb.Value;
                        var autohostMode = item.Key;

                        prefs.Add(new ProtocolExtension.JugglerConfig.PreferencePair() { Mode = autohostMode, Preference = preference });
                    }
                }
                conf.Preferences = prefs;
            }
            client.Extensions.SendMyJugglerConfig(conf);
        }

        public void Deactivate()
        {
            Program.NotifySection.RemoveBar(this);
            SendMyConfig(false);
        }

        public void AddedToContainer(NotifyBarContainer container)
        {
            BarContainer = container;
            container.btnDetail.Text = "QuickMatch ";
            if (lastState != null) container.btnDetail.Text += "("+lastState.TotalPlayers + ")";
            container.btnDetail.Enabled = false;
        }

        public void CloseClicked(NotifyBarContainer container)
        {
            Deactivate();
        }

        public void DetailClicked(NotifyBarContainer container)
        {
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