using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZeroKLobby.Controls
{
    public partial class MatchQueueControl : UserControl
    {
        public MatchQueueControl()
        {
            InitializeComponent();


            if (this.IsInDesignMode()) return;

            this.DescriptionLabel.Font = Config.ToolbarFontSmall;
            this.QueueNumbersLabel.Font = Config.ToolbarFontSmall;
            this.TimerLabel.Font = Config.ToolbarFont;
            this.LeaveQueueButton.Font = Config.ToolbarFontSmall;

            this.QueueTimer.Tick += QueueTimer_Tick;


            // Just for testing, remove this line when we implement the matchmaker service
            SetPlayerCounts(3, 5, 4);
            StartShowing();

        }

        private void QueueTimer_Tick(object sender, EventArgs e)
        {
            var queueTimeElapsed = DateTime.Now - QueueStartTime;
            var humanTimeElapsedString = new DateTime(queueTimeElapsed.Ticks).ToString("mm:ss");
            this.TimerLabel.Text = humanTimeElapsedString;
        }

        DateTime QueueStartTime;
        Timer QueueTimer = new Timer {Interval = 1000};

        public void SetPlayerCounts(int TeamsPlayerCount, int OneVsOnePlayerCount, int PlanetwarsPlayerCount)
        {
            QueueNumbersLabel.Text = String.Format("Player Counts\r\nTeams: {0} 1v1: {1} Planetwars: {2}",
                TeamsPlayerCount,
                OneVsOnePlayerCount,
                PlanetwarsPlayerCount);
        }

        public void StartShowing()
        {
            this.QueueStartTime = DateTime.Now;
            this.QueueTimer.Start();
            this.Visible = true;
        }

        public void Hide()
        {
            this.QueueTimer.Stop();
            this.Visible = false;
        }
    }
}
