using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZeroKLobby.Notifications
{
    public partial class JugglerBar: UserControl, INotifyBar
    {

        readonly Dictionary<AutohostMode, InfoItems> Items = new Dictionary<AutohostMode, InfoItems>();
        readonly TasClient client;
        bool _isActive;
        ProtocolExtension.JugglerState lastState;
        bool suppressChangeEvent;
        Timer tooltipLabelUpdate = new Timer();
        
        public JugglerBar(TasClient client) {
            InitializeComponent();
            this.client = client;

            string info =
                "Set your game type preferences for QuickMatch. System can move you to other room, if you like that type more than type of currently joined room";

            Program.ToolTip.SetText(this, info);
            Program.ToolTip.SetText(lbInfo,"QuickMatch will automatically find you a room of type you like most :)");
            

            int cnt = 0;

            int xOffset = 0, yOffset = 0;
            foreach (AutohostMode mode in Enum.GetValues(typeof(AutohostMode)).OfType<AutohostMode>().Where(x => x != AutohostMode.None)) {
                DpiMeasurement.DpiXYMeasurement(this); //this measurement use cached value. It won't cost anything if another measurement was already done in other control element
                xOffset = DpiMeasurement.ScaleValueX(70 + (cnt / 2) * 190); //DpiMeasurement is a static class stored in ZeroKLobby\Util.cs
                yOffset = DpiMeasurement.ScaleValueY(0 + (cnt % 2) * 21);
                var item = new InfoItems();
                Items.Add(mode, item);

                Controls.Add(new Label { Left = xOffset + 0, Width = DpiMeasurement.ScaleValueX(100), TextAlign = ContentAlignment.TopRight, Top = yOffset, Text = mode.Description() });
                item.ComboBox = new ComboBox { Left = xOffset + DpiMeasurement.ScaleValueX(105), Width = DpiMeasurement.ScaleValueX(50), Top = yOffset, DropDownStyle = ComboBoxStyle.DropDownList };
                foreach (GamePreference pref in Enum.GetValues(typeof(GamePreference)).OfType<GamePreference>().OrderByDescending(z => (int)z)) item.ComboBox.Items.Add(new CbItem { Value = pref });
                item.ComboBox.SelectedValueChanged += (sender, args) => { if (!suppressChangeEvent) SendMyConfig(true); };

                Program.ToolTip.SetText(item.ComboBox, info);

                Controls.Add(item.ComboBox);
                item.Label = new Label { Left = xOffset + DpiMeasurement.ScaleValueX(155), Top = yOffset, Width = DpiMeasurement.ScaleValueX(35) };
                Controls.Add(item.Label);

                Program.ToolTip.SetText(item.Label, "How many waiting people + how many playing");

                cnt++;
            }

            client.BattleJoined += (sender, args) => { };
            client.BattleMyUserStatusChanged += (sender, args) =>
                {
                    if (client.MyBattleStatus != null && client.MyBattleStatus.IsSpectator && IsActive) ActionHandler.ChangeDesiredSpectatorState(false);
                };

            client.Extensions.JugglerStateReceived += (args, state) =>
                {
                    lastState = state;
                    foreach (ProtocolExtension.JugglerState.ModePair entry in state.ModeCounts) {
                        InfoItems item;
                        if (Items.TryGetValue(entry.Mode, out item)) item.Label.Text = string.Format("({0}+{1})", entry.Count, entry.Playing);
                    }
                };

            client.LoginAccepted += (sender, args) =>
                {
                    SendMyConfig(false);
                    //if (!Program.NotifySection.Bars.Contains(this)) Program.NotifySection.AddBar(this);
                };

            client.Extensions.JugglerConfigReceived += (args, config) => { if (args.UserName == GlobalConst.NightwatchName) UpdateMyConfig(config); };

            tooltipLabelUpdate.Tick += (sender, args) =>
                {
                    if (dots.Length == 6) dots = "";
                    else dots += ".";
                    if (IsActive) {
                        if (lastState != null)
                            lbInfo.Text = string.Format("Working{2}\n{0} playing\n{1} waiting",
                                                        lastState.ModeCounts.Sum(x => x.Playing),
                                                        lastState.TotalPlayers,
                                                        dots);
                        else lbInfo.Text = string.Format("Working{0}", dots);
                    }
                };

            tooltipLabelUpdate.Interval = 1000;
            tooltipLabelUpdate.Start();
        }

        string dots = "";

        public NotifyBarContainer BarContainer { get; private set; }


        public bool IsActive {
            get { return _isActive; }
            set {
                _isActive = value;
                if (IsActive) {
                    if (!Program.NotifySection.Bars.Contains(this)) Program.NotifySection.AddBar(this);
                }
                else {
                    Program.NotifySection.RemoveBar(this);
                }
            }
        }

        #region INotifyBar Members

        public void AddedToContainer(NotifyBarContainer container) {
            BarContainer = container;
            BarContainer.btnDetail.Image = Resources.spec;
            Program.ToolTip.SetText(BarContainer.btnDetail, "Force QuickMatch check now");
        }

        public void CloseClicked(NotifyBarContainer container) {
            Deactivate();
        }

        public void DetailClicked(NotifyBarContainer container) {
            SendMyConfig(false, true);
        }

        public Control GetControl() {
            return this;
        }

        #endregion

        public void Activate() {
            if (!IsActive) {
                client.ChangeMyBattleStatus(spectate:false);
                Program.BattleBar.ChangeDesiredSpectatorState(false);
                SendMyConfig(false, true);
            }
        }

        public void SwitchState() {
            if (!IsActive) Activate();
            else Deactivate();
        }


        void UpdateMyConfig(ProtocolExtension.JugglerConfig res) {
            suppressChangeEvent = true;
            foreach (ProtocolExtension.JugglerConfig.PreferencePair entry in res.Preferences) {
                InfoItems item;
                if (Items.TryGetValue(entry.Mode, out item)) {
                    ComboBox cb = item.ComboBox;
                    cb.SelectedItem = cb.Items.OfType<CbItem>().FirstOrDefault(x => x.Value == entry.Preference);
                }
            }
            IsActive = res.Active;
            suppressChangeEvent = false;
        }


        void SendMyConfig(bool sendPreferences, bool? activate = null) {
            var conf = new ProtocolExtension.JugglerConfig();
            conf.Active = activate ?? IsActive;
            if (sendPreferences) {
                var prefs = new List<ProtocolExtension.JugglerConfig.PreferencePair>();
                foreach (var item in Items) {
                    var cb = (CbItem)item.Value.ComboBox.SelectedItem;
                    ;
                    if (cb != null) {
                        GamePreference preference = cb.Value;
                        AutohostMode autohostMode = item.Key;

                        prefs.Add(new ProtocolExtension.JugglerConfig.PreferencePair { Mode = autohostMode, Preference = preference });
                    }
                }
                conf.Preferences = prefs;
            }
            client.Extensions.SendMyJugglerConfig(conf);
        }

        public void Deactivate() {
            if (IsActive) SendMyConfig(false, false);
        }

        #region Nested type: CbItem

        public class CbItem
        {
            public GamePreference Value;

            public override string ToString() {
                return Value.Description();
            }
        }

        #endregion
    }

    class InfoItems
    {
        public ComboBox ComboBox;
        public Label Label;
    }
}