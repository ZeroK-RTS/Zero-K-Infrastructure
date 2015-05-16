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
    public partial class JournalSubPanel : UserControl
    {
        public JournalSubPanel()
        {
            InitializeComponent();
            journalTitleBox.Font = Config.MenuFont;
        }

        public void LoadJournalEntry(CampaignManager.JournalViewEntry entry)
        {
            journalTitleBox.Clear();
            journalTextBox.Clear();
            journalTitleBox.AppendText(entry.name);
            //journalImageBox.BackgroundImage = ;
            journalTextBox.AppendText(entry.text);
        }
    }
}
