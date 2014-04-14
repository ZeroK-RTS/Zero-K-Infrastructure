using System;
using System.Diagnostics;
using System.Windows.Forms;
using PlasmaShared;

namespace ZeroKLobby.Notifications
{
	public partial class ScannerBar: UserControl, INotifyBar
	{
		NotifyBarContainer container;
        int ignoreCount = 0;
        int workDone = 0;
        int workTotal = 0;

		public ScannerBar(SpringScanner scanner)
		{
			InitializeComponent();

			var isDesigner = Process.GetCurrentProcess().ProcessName == "devenv"; // workaround for this.DesignMode not working
			if (isDesigner) return;

			scanner.WorkStarted += scanner_WorkProgressChanged;
			scanner.WorkStopped += scanner_WorkStopped;
			scanner.WorkProgressChanged += scanner_WorkProgressChanged;
		}

		public void AddedToContainer(NotifyBarContainer container)
		{
			container.btnStop.Enabled = true;
            Program.ToolTip.SetText(container.btnStop, "Hide bar temporarily");
			container.btnDetail.Enabled = false;
			container.btnDetail.Text = "Scan";
			this.container = container;
		}

		public void CloseClicked(NotifyBarContainer container) {
            Program.MainWindow.InvokeFunc(() => Program.NotifySection.RemoveBar(this));
            ignoreCount = Math.Abs(workTotal - workDone)/2;
        }

		public void DetailClicked(NotifyBarContainer container) {}

		public Control GetControl()
		{
			return this;
		}

		void scanner_WorkProgressChanged(object sender, ProgressEventArgs e)
		{
            if (ignoreCount <= 0) {
                if (e.WorkTotal > 1)
			    {
                    workDone = e.WorkDone;
                    workTotal = e.WorkTotal;
				    Program.MainWindow.InvokeFunc(() =>
					    {
						    Program.NotifySection.AddBar(this);
						    progressBar1.Maximum = e.WorkTotal;
						    progressBar1.Value = Math.Min(e.WorkDone, e.WorkTotal);
						    label1.Text = "Scanning existing maps and games";
						    label2.Text = string.Format("{0}/{1} - {2}", e.WorkDone, e.WorkTotal, e.WorkName);
					    });
			    }
            }
            else if (e.WorkDone > workDone)
            {
                ignoreCount = ignoreCount - 1;
                workDone = e.WorkDone;
            }
		}


		void scanner_WorkStopped(object sender, EventArgs e)
		{
			Program.MainWindow.InvokeFunc(() => Program.NotifySection.RemoveBar(this));
		}
	}
}