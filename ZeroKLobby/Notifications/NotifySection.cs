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
        readonly Timer uiUpdate = new Timer();
		ScannerBar scannerBar;
		public IEnumerable<INotifyBar> Bars { get { return Controls.OfType<NotifyBarContainer>().Select(x => x.BarContent); } }

		public NotifySection()
		{
			InitializeComponent();

            uiUpdate.Interval =100; //timer tick to add micro delay to Layout update.
            uiUpdate.Tick += timedUpdate_Tick;
		}

		public void AddBar(INotifyBar bar)
		{
			if (!Bars.Contains(bar))
			{
				Controls.Add(new NotifyBarContainer(bar));
                uiUpdate.Start(); //accumulate update for 50ms because Linux Mono have trouble with multiple add/remove bar spam.
			}
		}

		public void RemoveBar(object bar)
		{
			var container = Controls.OfType<NotifyBarContainer>().FirstOrDefault(x => x.BarContent == bar);
            if (container != null)
            {
                Controls.Remove(container);
                uiUpdate.Start(); //accumulate update for 50ms because Linux Mono have trouble with multiple add/remove bar spam.
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
            uiUpdate.Stop(); //finish size update, stop timer.
            Height = Controls.OfType<Control>().Sum(x => x.Height);
			Program.MainWindow.Invalidate(true);
        }
	}
}