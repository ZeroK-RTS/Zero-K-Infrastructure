using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using LobbyClient;
using ZeroKLobby.MainPages;

namespace ZeroKLobby.BattleRoom
{
    public partial class BattleRoomPage : UserControl, IMainPage
    {
        public BattleRoomPage()
        {
            this.InitializeComponent();
            Program.TasClient.BattleJoined += (s, e) =>
            { Program.MainWindow.SwitchPage(MainWindow.MainPages.BattleRoom, false); };
        }

        public void GoBack()
        {
            Program.MainWindow.SwitchPage(MainWindow.MainPages.MultiPlayer, false);
            Program.TasClient.LeaveBattle();
        }

        public string Title { get { return "Battle"; } }

        public Image MainWindowBgImage { get { return BgImages.blue_galaxy; } }

        public void ChangeDesiredSpectatorState(bool state)
        {
            Program.TasClient.ChangeMyBattleStatus(state);
        }
    }
}
