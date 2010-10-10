using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;

namespace SpringDownloader.Notifications
{
	public partial class NewSpringBar: UserControl, INotifyBar
	{
		public const string DownloadUrl = "http://springrts.com/wiki/Download";
		readonly TasClient client;

		public NewSpringBar(TasClient client)
		{
			this.client = client;
			InitializeComponent();

			client.LoginAccepted += (s, e) => CheckVersion();
			CheckVersion();
		}

		void CheckVersion()
		{
			if (client.IsConnected && Program.SpringPaths.SpringVersion != client.ServerSpringVersion)
			{
				label1.Text = string.Format(label1.Text, client.ServerSpringVersion, Program.SpringPaths.SpringVersion);
				Program.NotifySection.AddBar(this);
			}
		}


		public void AddedToContainer(NotifyBarContainer container)
		{
			container.btnDetail.Text = "Upgrade";
		}

		public void CloseClicked(NotifyBarContainer container)
		{
			Program.NotifySection.RemoveBar(this);
		}

		public void DetailClicked(NotifyBarContainer container)
		{
			Utils.OpenWeb(DownloadUrl);
		}

		public Control GetControl()
		{
			return this;
		}

		void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Utils.OpenWeb(DownloadUrl);
		}
	}
}