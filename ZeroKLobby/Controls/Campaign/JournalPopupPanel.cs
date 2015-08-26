using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CampaignLib;
using ZeroKLobby.MicroLobby.Campaign;

namespace ZeroKLobby.Controls.Campaign
{
    public partial class JournalPopupPanel : UserControl
    {
        public JournalPopupPanel()
        {
            InitializeComponent();
        }

        public void LoadJournalEntry(CampaignManager.JournalViewEntry entry)
        {
            subpanel.LoadJournalEntry(entry);
        }

        public void SetMission(Mission mission)
        {
            playButton.Click += (sender, eventArgs) => CampaignManager.PlayMission(mission);
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}
