using System;
using System.ComponentModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.Windows.Forms;
using Timer = System.Threading.Timer;

namespace SpringDownloader.Notifications
{
	public partial class NewVersionBar: UserControl, INotifyBar
	{
		NotifyBarContainer container;
		readonly ApplicationDeployment deployment;
		readonly Timer timer;

		public NewVersionBar()
		{
			InitializeComponent();

			if (ApplicationDeployment.IsNetworkDeployed && (deployment = ApplicationDeployment.CurrentDeployment) != null)
			{
				deployment.UpdateCompleted += deployment_UpdateCompleted;
				deployment.UpdateProgressChanged += deployment_UpdateProgressChanged;
				timer = new Timer(timer_Tick, null, TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(20));
			}

			else Trace.TraceError("SpringDownloader not installed propery - get latest version from http://planet-wars.eu/sd/setup.exe");
		}


		void timer_Tick(object token)
		{
			try
			{
				UpdateCheckInfo updateInfo;
				if ((updateInfo = deployment.CheckForDetailedUpdate()) != null && updateInfo.UpdateAvailable)
				{
					Program.FormMain.InvokeFunc(() =>
						{
							lbText.Text = string.Format("Updating to SpringDownloader {0}", updateInfo.AvailableVersion);
							Program.NotifySection.AddBar(this);
						});

					deployment.UpdateAsync();
				}
			}
			catch (Exception ex)
			{
				Trace.TraceWarning("Error checking for update: {0}", ex);
			}
		}

		public void AddedToContainer(NotifyBarContainer container)
		{
			this.container = container;
			container.btnDetail.Text = "Updating";
			container.btnDetail.Enabled = false;
		}

		public void CloseClicked(NotifyBarContainer container)
		{
			Program.NotifySection.RemoveBar(this);
		}

		public void DetailClicked(NotifyBarContainer container)
		{
			Application.Restart();
		}

		public Control GetControl()
		{
			return this;
		}

		void deployment_UpdateCompleted(object sender, AsyncCompletedEventArgs e)
		{
			Program.FormMain.InvokeFunc(() =>
				{
					if (!e.Cancelled && e.Error == null)
					{
						lbText.Text = "SpringDownloader self-update done, click restart to upgrade";
						container.btnDetail.Enabled = true;
						container.btnDetail.Text = "Restart";
					}
					else
					{
						Trace.TraceError("Self updating failed: {0}", e.Error);
						container.btnDetail.Text = "Failed";
					}
				});
		}

		void deployment_UpdateProgressChanged(object sender, DeploymentProgressChangedEventArgs e)
		{
			Program.FormMain.InvokeFunc(() => { progressBar1.Value = e.ProgressPercentage; });
		}
	}
}