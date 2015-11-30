using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LobbyClient;
using ZeroKLobby.MicroLobby;
using ZkData;

namespace ZeroKLobby.MainPages
{
    public partial class JoinQueuePage : UserControl, IMainPage
    {
        public JoinQueuePage()
        {
            InitializeComponent();

            if (this.IsInDesignMode()) return;

            this.JoinQueueButton.Font = Config.MainPageFont;
            this.PartyBoxTitleLabel.Font = Config.MainPageFont;
            this.OneVsOneCheckBox.Font = Config.MainPageFont;
            this.TeamsCheckBox.Font = Config.MainPageFont;
            this.InvitePartyMemberButton.Font = Config.MainPageFont;
            this.LeavePartyButton.Font = Config.MainPageFont;

            Program.TasClient.ClientHasJoinedParty += TasClient_ClientHasJoinedParty;
            Program.TasClient.PartyMemberHasJoined += TasClient_PartyMemberHasJoined;
            Program.TasClient.PartyMemberHasLeft += TasClient_PartyMemberHasLeft;

            TasClient_PartyMemberHasJoined(new object(), new EventArgs<string>("[LCC]quantum[0K]"));

        }

        private void TasClient_PartyMemberHasLeft(object sender, ZkData.EventArgs<string> e)
        {
            throw new NotImplementedException();
        }

        private void TasClient_PartyMemberHasJoined(object sender, ZkData.EventArgs<string> e)
        {
            var NewPlayer = new PlayerListItem
            {
                UserName = e.Data,
            };

            this.PartyPlayerList.AddItemRange(new [] { NewPlayer});
        }

        private void TasClient_ClientHasJoinedParty(object sender, ZkData.EventArgs<List<string>> e)
        {
            throw new NotImplementedException();
        }

        public void GoBack()
        {
            Program.MainWindow.SwitchPage(MainWindow.MainPages.MultiPlayer, false);
        }

        public string Title { get { return "Join Queue"; } }

        public Image MainWindowBgImage { get { return BgImages.blue_galaxy; } }

        private void JoinQueueButton_Click(object sender, EventArgs e)
        {
            Program.TasClient.SendCommand(new JoinQueue { JoinOneVsOne = this.OneVsOneCheckBox.Checked, JoinTeams = this.TeamsCheckBox.Checked });
        }

        private void InvitePartyMemberButton_Click(object sender, EventArgs e)
        {

        }

        private void LeavePartyButton_Click(object sender, EventArgs e)
        {
            Program.TasClient.SendCommand(new LeaveQueue());
        }
    }
}
