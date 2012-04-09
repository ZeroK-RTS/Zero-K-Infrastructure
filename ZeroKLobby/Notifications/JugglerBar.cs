using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LobbyClient;
using PlasmaShared;
using PlasmaShared.ContentService;

namespace ZeroKLobby.Notifications
{
    public partial class JugglerBar : UserControl, INotifyBar
    {
        TasClient client;
        public NotifyBarContainer BarContainer { get; private set; }

        Dictionary<string, InfoItems> Items = new Dictionary<string, InfoItems>();

        public class CbItem {
            public ZkData.GamePreference Value;
            public override string ToString()
            {
                return Value.Description();
            }
        }

        public JugglerBar(TasClient client)
        {
            InitializeComponent();
            this.client = client;
            var cs = new ContentService();

            int cnt = 0;
            
            foreach (ZkData.AutohostMode mode in Enum.GetValues(typeof(ZkData.AutohostMode)).OfType<ZkData.AutohostMode>().Where(x=>x!= ZkData.AutohostMode.None)) {
                var item = new InfoItems();
                Items.Add(mode.ToString(),item);
                int y = 0 + (cnt%3)*24;
                int x = 80 + (cnt/3)*250;
                item.ComboBox = new ComboBox() { Left = x + 125, Width=60, Top = y, DropDownStyle = ComboBoxStyle.DropDownList};
                foreach (ZkData.GamePreference pref in Enum.GetValues(typeof(ZkData.GamePreference)).OfType<ZkData.GamePreference>().OrderByDescending(z=>(int)z)) {
                    item.ComboBox.Items.Add(new CbItem() { Value = pref });
                }

                this.Controls.Add(item.ComboBox);
                item.Label = new Label() { Left = x+ 145, Top = y};
                this.Controls.Add(item.Label);
                this.Controls.Add(new Label() { Left = x+ 0, Width = 120, TextAlign =ContentAlignment.TopRight ,Top = y, Text = mode.Description()});
                cnt++;
            }

            cs.GetJugglerStateCompleted += (sender, args) =>
            {
                if (!args.Cancelled && args.Error == null)
                {
                    var res = args.Result;
                    if (!Program.NotifySection.Bars.Contains(this)) Program.NotifySection.AddBar(this);
                    lbInfo.Text = "Players: " + res.TotalPlayers;
                    foreach (var entry in res.ModeCounts) {
                        Items[entry.Mode.ToString()].Label.Text = entry.Count.ToString();
                    }


                }

            };
            cs.GetJugglerStateAsync();

            cs.GetJugglerConfigCompleted += (sender, args) => {
                if (!args.Cancelled && args.Error == null) {
                    var res = args.Result;
                    cbMatchMake.Checked = res.Active;
                    foreach (var entry in res.Preferences) {
                        var cb = Items[entry.Mode.ToString()].ComboBox;
                        cb.SelectedItem = cb.Items.OfType<CbItem>().FirstOrDefault(x => (int)x.Value == (int)entry.Preference);
                    }


                }

            };
            if (Program.TasClient.MyUser != null) cs.GetJugglerConfig(Program.TasClient.MyUser.LobbyID);
        }

        public void AddedToContainer(NotifyBarContainer container)
        {
            BarContainer = container;
            container.btnDetail.Text = "MatchMaking";
            container.btnDetail.Enabled = false;
        }

        public void CloseClicked(NotifyBarContainer container)
        {
            Program.NotifySection.RemoveBar(this);
        }

        public void DetailClicked(NotifyBarContainer container)
        {
            
        }

        public Control GetControl()
        {
            return this;
        }
    }

    class InfoItems {
        public Label Label;
        public ComboBox ComboBox;
    }
}
