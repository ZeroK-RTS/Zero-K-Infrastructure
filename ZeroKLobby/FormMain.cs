using System;
using System.ComponentModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using PlasmaDownloader;
using PlasmaShared;
using ZeroKLobby.MicroLobby;
using ZeroKLobby.Notifications;

namespace ZeroKLobby
{
	public partial class FormMain: Form
	{
		public delegate void Func();

		bool closeForReal;
		FormWindowState lastState = FormWindowState.Normal;


		public ChatTab ChatTab { get { return chatControl1; } }
		public static FormMain Instance { get; set; }


		public NotifySection NotifySection { get { return notifySection1; } }

		public FormMain()
		{
			InitializeComponent();

			var isDesigner = Process.GetCurrentProcess().ProcessName == "devenv"; // workaround for this.DesignMode not working
			if (isDesigner) return;

			Invalidate(true);

			Instance = this;
			systrayIcon.BalloonTipClicked += systrayIcon_BalloonTipClicked;

			if (Program.Downloader != null)
			{
				timer1.Start();
				Program.Downloader.DownloadAdded += TorrentManager_DownloadAdded;
			}

			serverTab1.Visible = Debugger.IsAttached;
			if (!Debugger.IsAttached) tabControl.TabPages.Remove(tabPageServer);

			tabControl.Layout += tabControl_Layout;
		}

		public void DisplayLog()
		{
			if (!FormLog.Instance.Visible)
			{
				FormLog.Instance.Visible = true;
				FormLog.Instance.Focus();
			}
			else FormLog.Instance.Visible = false;
		}


		public void Exit()
		{
			if (closeForReal) return;
			closeForReal = true;
			Program.CloseOnNext = true;
			if (!IsDisposed) InvokeFunc(Close);
		}

		public TabPage GetSelectedTab()
		{
			return tabControl.SelectedTab;
		}

		public void InvokeFunc(Func funcToInvoke)
		{
			try
			{
				Invoke(funcToInvoke);
			}
			catch (Exception ex)
			{
				Trace.TraceError("Error invoking: {0}", ex);
			}
		}

		public void PopupSelf()
		{
			if (!InvokeRequired)
			{
				Visible = true;
				WindowState = FormWindowState.Minimized;
				Visible = true;
				WindowState = lastState;
				Focus();
				BringToFront();
			}
			else InvokeFunc(PopupSelf);
		}


		public void SelectTab(Tab tab)
		{
			SelectTab("tabPage" + tab);
		}


		void SelectTab(string name)
		{
			if (!InvokeRequired)
			{
				if (tabControl.TabPages.ContainsKey(name)) tabControl.SelectTab(name);
			}
			else Invoke((FuncStr)SelectTab, name);
		}


		void UpdateDownloads()
		{
			try
			{
				if (Program.Downloader != null && !Program.CloseOnNext)
				{
					// remove aborted
					foreach (var pane in
						Program.NotifySection.Bars.OfType<DownloadBar>().Where(x => x.Download.IsAborted || x.Download.IsComplete == true)) Program.NotifySection.RemoveBar(pane);

					// update existing
					foreach (var pane in Program.NotifySection.Bars.OfType<DownloadBar>()) pane.UpdateInfo();
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("Error updating transfers: {0}", ex);
			}
		}

		void UpdateSystrayToolTip()
		{
			var sb = new StringBuilder();
			var bat = Program.TasClient.MyBattle;
			if (bat != null)
			{
				// todo display unread PM messages count
				sb.AppendFormat("Players: {0}\n", bat.NonSpectatorCount, bat.SpectatorCount);
				sb.AppendFormat("Battle: {0}\n", bat.Founder);
			}
			else
			{
				var qm = Program.BattleBar.GetQuickMatchInfo();
				if (qm != null && qm.IsEnabled) sb.AppendFormat("{0}\n", qm);
				else sb.AppendFormat("idle");
			}
			var str = sb.ToString();
			systrayIcon.Text = str.Substring(0, Math.Min(str.Length, 64)); // tooltip only allows 64 characters
		}

		void watcher_Changed(object sender, FileSystemEventArgs e)
		{
			if (File.Exists(e.FullPath))
			{
				string text = null;
				try
				{
					text = File.ReadAllText(e.FullPath);
					File.Delete(e.FullPath);
				}
				catch {}
				if (!string.IsNullOrEmpty(text))
				{
					foreach (var s in text.Split('\n'))
					{
						var name = s.Trim();

						if (name != "")
						{
							var args = name.Split('|');
							if (args.Length > 1 && args[1] == "abort")
							{
								var existing = Program.Downloader.Downloads.FirstOrDefault(x => x.Name == args[0]);
								if (existing != null) existing.Abort();
							}
							else Program.Downloader.GetResource(DownloadType.UNKNOWN, name);
						}
					}
				}
			}
		}

		void FormMain_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (e.CloseReason == CloseReason.UserClosing) Program.IsCrash = false;

			Program.CloseOnNext = true;
			if (Program.TasClient != null) Program.TasClient.Disconnect();
			Program.Conf.LastWindowState = WindowState;
			Program.SaveConfig();
		}

		void FormMain_SizeChanged(object sender, EventArgs e)
		{
			if (WindowState != FormWindowState.Minimized) lastState = WindowState;
			else if (Program.Conf.MinimizeToTray)
			{
				Visible = false;
			}
		}

		public void NotifyUser(string message, bool useSound = false, bool useFlashing = false)
		{
			bool isHidden = WindowState == FormWindowState.Minimized || Visible == false || Utils.GetForegroundWindow() != (int)Handle;
			// todo use this when its easy to determine what is user looking at (flash when message not seen)
			if (!string.IsNullOrEmpty(message)) systrayIcon.ShowBalloonTip(5000, "Zero-K", message, ToolTipIcon.Info);
			if (isHidden && useFlashing) FlashWindow();
			if (useSound) SystemSounds.Exclamation.Play();
		}

		/// <summary>
		/// Flashes window if its not foreground - until it is foreground
		/// </summary>
		protected void FlashWindow()
		{
			if (!Focused || !Visible || WindowState == FormWindowState.Minimized)
			{
				Visible = true;
				var info = new Utils.FLASHWINFO();
				info.hwnd = Program.FormMain.Handle;
				info.dwFlags = 0x0000000C | 0x00000003; // flash all until foreground
				info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
				Utils.FlashWindowEx(ref info);
			}
		}


		void MainForm_Load(object sender, EventArgs e)
		{
			;
			if (Debugger.IsAttached) Text = "==== DEBUGGING ===";
			else if (ApplicationDeployment.IsNetworkDeployed) Text = string.Format("Zero-K lobby  (v{0})",ApplicationDeployment.CurrentDeployment.CurrentVersion);
			else Text += " not installed properly - update from http://zero-k.info/lobby";

			Icon = Resources.ZkIcon;
			systrayIcon.Icon = Resources.ZkIcon;

			Program.SpringScanner.Start();

			if (Program.Conf.StartMinimized) WindowState = FormWindowState.Minimized;
			else WindowState = Program.Conf.LastWindowState;
		}

		void TorrentManager_DownloadAdded(object sender, EventArgs<Download> e)
		{
			Invoke(new Action(() => Program.NotifySection.AddBar(new DownloadBar(e.Data))));
		}

		void btnExit_Click(object sender, EventArgs e)
		{
			Program.IsCrash = false;
			Exit();
		}


		void systrayIcon_BalloonTipClicked(object sender, EventArgs e)
		{
			PopupSelf();
		}


		void systrayIcon_Click(object sender, EventArgs e)
		{
			PopupSelf();
		}

		void systrayIcon_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			PopupSelf();
		}


		void systrayIcon_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left) PopupSelf();
		}

		void tabControl_Layout(object sender, LayoutEventArgs e)
		{
			// we want to pre-load all tab controls

			// store the originally selected tab to set the state of the control back to it's original state
			var originallySelectedTabIndex = tabControl.SelectedIndex;

			// iterate through each TagPage in the collection and select it 
			foreach (TabPage tab in tabControl.TabPages) tabControl.SelectedTab = tab;

			// detach the Layout event because we only want it to happen once (could happen later if the form is resized or controls changed etc.
			tabControl.Layout -= tabControl_Layout;

			// restore the selected tabindex
			tabControl.SelectedIndex = originallySelectedTabIndex;
		}

		void tabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (tabControl.SelectedTab == tabPageSettings) settingsTab1.RefreshConfig();
		}


		void timer1_Tick(object sender, EventArgs e)
		{
			UpdateDownloads();
			UpdateSystrayToolTip();
		}

		void trayStrip_Opening(object sender, CancelEventArgs e) {}

		delegate void FuncStr(string data);
	}

	public enum Tab
	{
		Games,
		Chat,
		Battles,
		Widgets,
		Downloader,
		Settings,
		Help
	}
}