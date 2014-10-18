using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ZeroKLobby;
using ZeroKLobby.Notifications;

namespace SpringDownloader.Notifications
{
	public partial class NotifySection: UserControl
	{
        private Timer timedUpdate = new Timer();
		ScannerBar scannerBar;
		public IEnumerable<INotifyBar> Bars { get { return Controls.OfType<NotifyBarContainer>().Select(x => x.BarContent); } }

		public NotifySection()
		{
			InitializeComponent();

            timedUpdate.Interval =100; //timer tick to add micro delay to Layout update.
            timedUpdate.Tick += timedUpdate_Tick;
		}

		public void AddBar(INotifyBar bar)
		{
			if (!Bars.Contains(bar))
			{
				Controls.Add(new NotifyBarContainer(bar));
				//Height = Controls.OfType<Control>().Sum(x => x.Height);
                timedUpdate.Start(); //accumulate update for 50ms because Linux Mono have trouble with multiple add/remove bar spam.
			}
		}

		public void RemoveBar(object bar)
		{
			var container = Controls.OfType<NotifyBarContainer>().Where(x => x.BarContent == bar).SingleOrDefault();
            if (container != null)
            {
                Controls.Remove(container);
                timedUpdate.Start(); //accumulate update for 50ms because Linux Mono have trouble with multiple add/remove bar spam.
            }
		}

		void NotifySection_Load(object sender, EventArgs e)
		{
			scannerBar = new ScannerBar(Program.SpringScanner);
            var scannerBarSize = new System.Drawing.Size(0, scannerBar.Height);
            scannerBar.MinimumSize = scannerBarSize; //fix minimum size forever
            scannerBar.MaximumSize = scannerBarSize; //fix maximum size forever (workaround for DPI bug in Windows)
		}

        private void timedUpdate_Tick(object sender, EventArgs e)
        {
            timedUpdate.Stop(); //finish size update, stop timer.
            Height = Controls.OfType<Control>().Sum(x => x.Height);
			Program.MainWindow.Invalidate(true);
        }
	}
}