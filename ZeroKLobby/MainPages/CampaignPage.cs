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
using ZeroKLobby.MainPages;
using ZeroKLobby.Campaign;
using ZeroKLobby.Controls.Campaign;
using Newtonsoft.Json;
using System.IO;

namespace ZeroKLobby.MicroLobby.Campaign
{
    public partial class CampaignPage : UserControl, IMainPage
    {
        protected const string FILE_FILTER = "JSON files (*.json)|*.json|All files (*.*)|*.*";

        CampaignManager manager;
        Dictionary<string, BitmapButton> buttons = new Dictionary<string, BitmapButton>();
        
        JournalPanel journalPanel;
        JournalPopupPanel journalPopupPanel;

        public CampaignPage()
        {
            InitializeComponent();
            manager = new CampaignManager(this);
            manager.LoadCampaign("sunrise");
            //manager.LoadCampaignSave("test", "save1");

            journalButton.Font = Config.MenuFont;
            commButton.Font = Config.MenuFont;
            saveButton.Font = Config.MenuFont;
            loadButton.Font = Config.MenuFont;

            journalButton.Click += (sender, eventArgs) =>
                {
                    if (journalPanel != null)
                    {
                        journalPanel.Dispose();
                    }
                    journalPanel = new JournalPanel();
                    journalPanel.Left = 240;  // FIXME don't hardcode this kind of thing!!
                    journalPanel.LoadJournalEntries(CampaignManager.GetVisibleJournals());
                    journalPanel.Parent = this;
                    journalPanel.BringToFront();
                };

            galControl.BackgroundImage = (Bitmap)GalaxyResources.ResourceManager.GetObject(manager.GetCampaign().Background);
            galControl.BackgroundImageLayout = ImageLayout.Stretch;
            ReloadPlanets();
        }

        public void ReloadPlanets()
        {
            galControl.SetPlanets(CampaignManager.GetVisiblePlanets());
        }

        // TODO: replace with own dialog
        public bool SaveGame()
        {
            var saveFileDialog = new SaveFileDialog { Filter = FILE_FILTER, FilterIndex = 1, RestoreDirectory = true };
            var result = saveFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                string progressJson = JsonConvert.SerializeObject(manager.GetSave(), Formatting.Indented);
                File.WriteAllText(saveFileDialog.FileName, progressJson);
                return true;
            }
            return false;
        }

        public bool LoadGame()
        {
            var openFileDialog = new OpenFileDialog { Filter = FILE_FILTER, FilterIndex = 1, RestoreDirectory = true };
            var result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                manager.LoadCampaignSave(manager.GetCampaign().Name, openFileDialog.FileName, true);
                return true;
            }
            return false;
        }

        private void loadButton_Click(object sender, System.EventArgs e)
        {
            LoadGame();
        }

        private void saveButton_Click(object sender, System.EventArgs e)
        {
            SaveGame();
        }

        public void EnterMission(Mission mission)
        {
            string missionJournal = mission.IntroJournal;
            if (missionJournal == null || !CampaignManager.IsJournalUnlocked(missionJournal))
            {
                // popup journal
                CampaignManager.UnlockJournal(missionJournal);
                if (journalPopupPanel != null)
                {
                    journalPopupPanel.Dispose();
                }
                journalPopupPanel = new JournalPopupPanel();
                journalPopupPanel.Left = 300;  // FIXME don't hardcode this kind of thing!!
                journalPopupPanel.LoadJournalEntry(CampaignManager.GetJournalViewEntry(missionJournal));
                journalPopupPanel.SetMission(mission);
                journalPopupPanel.Parent = this;
                journalPopupPanel.BringToFront();
            }
            else
            {
                CampaignManager.PlayMission(mission);
            }
        }

        public void GoBack()
        {
            Program.MainWindow.SwitchPage(MainWindow.MainPages.SinglePlayer);
        }
        public string Title { get { return "Campaign"; } }
        public Image MainWindowBgImage { get { return BgImages.bg_battle; } }
    }
}
