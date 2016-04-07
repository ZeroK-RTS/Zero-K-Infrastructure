using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ZeroKLobby;
using ZeroKLobby.Controls;
using ZeroKLobby.Notifications;

namespace SpringDownloader.Notifications
{
	public partial class NotifySection: ZklBaseControl
	{
		public IEnumerable<ZklNotifyBar> Bars { get { return Controls.OfType<ZklNotifyBar>(); } }

	    public NotifySection() {
	        InitializeComponent();
	    }

	    public void AddBar(ZklNotifyBar bar)
		{
			if (!Bars.Contains(bar))
			{
				Controls.Add(bar);
			}
		}

		public void RemoveBar(object bar)
		{
			var container = Controls.OfType<ZklNotifyBar>().FirstOrDefault(x => x == bar);
            if (container != null)
            {
                Controls.Remove(container);
            }
		}


	}
}