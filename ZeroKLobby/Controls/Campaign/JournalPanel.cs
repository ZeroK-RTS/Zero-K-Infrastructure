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

namespace ZeroKLobby.Controls.Campaign
{
    public partial class JournalPanel : UserControl
    {
        private List<JournalEntry> savedJournals;

        public JournalPanel()
        {
            InitializeComponent();
            journalTitleBox.Font = Config.MenuFont;
            savedJournals = new List<JournalEntry>();
        }

        public void LoadJournalEntry(JournalEntry entry)
        {
            journalTitleBox.Clear();
            journalTextBox.Clear();
            journalTitleBox.AppendText(entry.journal.Name);
            //journalImageBox.BackgroundImage = ;
            journalTextBox.AppendText(entry.savedData.textSnapshot);
        }

        public void LoadJournalEntries(Dictionary<string, Journal> journals, Dictionary<string, CampaignSave.JournalProgressData> journalProgress)
        {
            Dictionary<string, TreeNode> categoryNodes = new Dictionary<string, TreeNode>();

            journalTree.Nodes.Clear();
            savedJournals = new List<JournalEntry>();
            foreach (var bla in journalProgress)
            {
                savedJournals.Add(new JournalEntry { journal = journals[bla.Value.journalID], savedData = bla.Value });
            }

            var categories = savedJournals.Select(x => x.journal.Category).ToList();
            foreach (var category in categories)
            {
                categoryNodes.Add(category, journalTree.Nodes.Add(category));
            }

            foreach (JournalEntry entry in savedJournals)
            {
                TreeNode node = new TreeNode(entry.journal.Name);
                node.Tag = entry.journal.ID;
                categoryNodes[entry.journal.Category].Nodes.Add(node);
            }
        }

        private void journalTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var node = e.Node;
            if (node == null) return;
            JournalEntry entry = savedJournals.FirstOrDefault(x => x.journal.ID == (string)node.Tag);
            if (entry != null) LoadJournalEntry(entry);
            //else journalTitleBox.AppendText(node.Name);
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        public class JournalEntry
        {
            public Journal journal;
            public CampaignSave.JournalProgressData savedData;
        }
    }
}
