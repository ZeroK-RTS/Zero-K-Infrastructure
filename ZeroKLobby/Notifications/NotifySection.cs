using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ZeroKLobby.Notifications
{
	public partial class NotifySection: UserControl
	{
		ScannerBar scannerBar;
		public IEnumerable<INotifyBar> Bars { get { return Controls.OfType<NotifyBarContainer>().Select(x => x.BarContent); } }

		public NotifySection()
		{
			InitializeComponent();
		}

		public void AddBar(INotifyBar bar)
		{
			if (!Bars.Contains(bar))
			{
				Controls.Add(new NotifyBarContainer(bar));
				Height = Controls.OfType<Control>().Sum(x => x.Height);
				Program.FormMain.Invalidate(true);
			}
		}

		public void RemoveBar(INotifyBar bar)
		{
			var container = Controls.OfType<NotifyBarContainer>().Where(x => x.BarContent == bar).SingleOrDefault();
			if (container != null) Controls.Remove(container);
			Height = Controls.OfType<Control>().Sum(x => x.Height);
			Program.FormMain.Invalidate(true);
		}

		void NotifySection_Load(object sender, EventArgs e)
		{
			scannerBar = new ScannerBar(Program.SpringScanner);
		}
	}
}