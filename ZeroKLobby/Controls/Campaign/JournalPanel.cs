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
    public partial class JournalPanel : UserControl
    {
        private List<CampaignManager.JournalViewEntry> journals;

        public JournalPanel()
        {
            InitializeComponent();
            journals = new List<CampaignManager.JournalViewEntry>();
        }

        public void LoadJournalEntry(CampaignManager.JournalViewEntry entry)
        {
            subpanel.LoadJournalEntry(entry);
        }

        public void LoadJournalEntries(List<CampaignManager.JournalViewEntry> journals)
        {
            this.journals = journals;
            Dictionary<string, TreeNode> categoryNodes = new Dictionary<string, TreeNode>();

            journalTree.Nodes.Clear();

            var categories = journals.Select(x => x.category).ToList();
            foreach (var category in categories)
            {
                categoryNodes.Add(category, journalTree.Nodes.Add(category));
            }

            foreach (CampaignManager.JournalViewEntry entry in journals)
            {
                TreeNode node = new TreeNode(entry.name);
                node.Tag = entry.id;
                categoryNodes[entry.category].Nodes.Add(node);
            }
        }

        private void journalTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var node = e.Node;
            if (node == null) return;
            CampaignManager.JournalViewEntry entry = journals.FirstOrDefault(x => x.id == (string)node.Tag);
            if (entry != null) LoadJournalEntry(entry);
            //else journalTitleBox.AppendText(node.Name);
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}
