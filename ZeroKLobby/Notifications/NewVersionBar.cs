using System;
using System.ComponentModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.Windows.Forms;
using Timer = System.Threading.Timer;

namespace ZeroKLobby.Notifications
{
	public partial class NewVersionBar: UserControl, INotifyBar
	{
		NotifyBarContainer container;
		readonly ApplicationDeployment deployment;
		readonly Timer timer;
		
		/// <summary>
		/// Is update bar visible?
		/// </summary>
		static bool barHidden = true;

		public NewVersionBar()
		{
			InitializeComponent();

			if (ApplicationDeployment.IsNetworkDeployed && (deployment = ApplicationDeployment.CurrentDeployment) != null)
			{
        deployment.UpdateCompleted += deployment_UpdateCompleted;
				deployment.UpdateProgressChanged += deployment_UpdateProgressChanged;
				timer = new Timer(timer_Tick, null, TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(20));
			}

			else Trace.TraceError("Zero-K not installed propery - get latest version from http://zero-k.info/lobby");
		}


		void timer_Tick(object token)
		{
			try
			{
				UpdateCheckInfo updateInfo;
				if ((updateInfo = deployment.CheckForDetailedUpdate()) != null && updateInfo.UpdateAvailable)
				{
					if (updateInfo.IsUpdateRequired) barHidden = false;
					if (!barHidden) Program.MainWindow.InvokeFunc(() =>
						{
							lbText.Text = string.Format("Updating to Zero-K lobby {0}", updateInfo.AvailableVersion);
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
			barHidden = true;
			Program.NotifySection.RemoveBar(this);
		}

		public void DetailClicked(NotifyBarContainer container)
		{
      Application.Restart();
      System.Windows.Application.Current.Shutdown();
		}

		public Control GetControl()
		{
			return this;
		}

		void deployment_UpdateCompleted(object sender, AsyncCompletedEventArgs e)
		{
			if (!barHidden) Program.MainWindow.InvokeFunc(() =>
				{
					if (!e.Cancelled && e.Error == null)
					{
						lbText.Text = "Zero-K lobby self-update done, click restart to upgrade";
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
			if (!barHidden) Program.MainWindow.InvokeFunc(() => { progressBar1.Value = e.ProgressPercentage; });
		}
	}
}