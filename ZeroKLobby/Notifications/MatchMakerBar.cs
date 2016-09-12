// Contact: Jan Lichovník  licho@licho.eu, tel: +420 604 935 349,  www.itl.cz
// Last change by: licho  03.07.2016

using System;
using System.Collections.Generic;
using System.Linq;
using LobbyClient;

namespace ZeroKLobby.Notifications
{
    public partial class MatchMakerBar: ZklNotifyBar
    {
        private TasClient client;
        public MatchMakerBar(TasClient client)
        {
            this.client = client;
            InitializeComponent();
            lbText.Font = Config.GeneralFont;
            bitmapButton1.Text = "Stop";

            client.MatchMakerStatusUpdated += (sender, status) =>
            {
                if (!status.MatchMakerEnabled) Program.NotifySection.RemoveBar(this);
                else
                {
                    lbText.Text = "In queue: " + string.Join(", ", status.JoinedQueues.Select(x=>$"{x} ({status.QueueCounts[x]} people)"));
                    Program.NotifySection.AddBar(this);
                }
            };

            client.Connected += (sender, welcome) =>
                { Program.NotifySection?.RemoveBar(this); };
        }


        private void bitmapButton1_Click(object sender, EventArgs e)
        {
            client.MatchMakerQueueRequest(null);
        }
    }
}