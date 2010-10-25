using System;
using System.ComponentModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;
using PlasmaDownloader;
using PlasmaShared;
using ZeroKLobby.MicroLobby;
using ZeroKLobby.Notifications;
using Size = System.Drawing.Size;

namespace ZeroKLobby
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow: Window
	{
		public delegate void Func();

		readonly ToolStripMenuItem btnExit;

		bool closeForReal;
		readonly WindowInteropHelper interopHelper;
		WindowState lastState = WindowState.Normal;

		readonly NotifySection notifySection1;
		readonly NotifyIcon systrayIcon;
		readonly DispatcherTimer timer1 = new DispatcherTimer();
		readonly ContextMenuStrip trayStrip;
		public ChatTab ChatTab { get { return navigationControl1.ChatTab; } }
		public static MainWindow Instance { get; private set; }

		public NotifySection NotifySection { get { return notifySection1; } }

		public IntPtr Handle
		{
			get { return interopHelper.Handle; }
		}

		public MainWindow()
		{
			InitializeComponent();

			if (Utils.IsDesignTime) return;

			notifySection1 = new NotifySection();

			//Invalidate(true);

			Instance = this;
			//systrayIcon.BalloonTipClicked += systrayIcon_BalloonTipClicked;

			interopHelper = new WindowInteropHelper(this);
			btnExit = new ToolStripMenuItem();
			btnExit.Name = "btnExit";
			btnExit.Size = new Size(92, 22);
			btnExit.Text = "Exit";
			btnExit.Click += new EventHandler(btnExit_Click);

			trayStrip = new ContextMenuStrip();
			trayStrip.Items.AddRange(new ToolStripItem[] { btnExit });
			trayStrip.Name = "trayStrip";
			trayStrip.Size = new Size(93, 26);

			systrayIcon = new NotifyIcon();
			systrayIcon.ContextMenuStrip = trayStrip;
			systrayIcon.Text = "Zero-K";
			systrayIcon.Visible = true;
			systrayIcon.Click += systrayIcon_Click;
			systrayIcon.MouseDoubleClick += systrayIcon_MouseDoubleClick;
			systrayIcon.MouseDown += systrayIcon_MouseDown;
			systrayIcon.BalloonTipClicked += systrayIcon_BalloonTipClicked;

			if (Program.Downloader != null)
			{
				timer1.Interval = TimeSpan.FromMilliseconds(250);
				timer1.Tick += new EventHandler(timer1_Tick);

				Program.Downloader.DownloadAdded += TorrentManager_DownloadAdded;
				timer1.Start();
			}
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
			InvokeFunc(Close);
		}

		public Control GetHoveredControl()
		{
			var host = navigationControl1.GetWindowsFormsHostOfCurrentTab();
			if (host != null)
			{
				var parentControl = host.Child;
				var screenPoint = Control.MousePosition;
				var parentPoint = parentControl.PointToClient(screenPoint);
				Control child;
				while (
					(child =
					 parentControl.GetChildAtPoint(parentPoint, GetChildAtPointSkip.Disabled | GetChildAtPointSkip.Invisible | GetChildAtPointSkip.Transparent)) !=
					null)
				{
					parentControl = child;
					parentPoint = parentControl.PointToClient(screenPoint);
				}
				return parentControl;
			}
			return null;
		}

		public void InvokeFunc(Func funcToInvoke)
		{
			try
			{
				Dispatcher.Invoke(funcToInvoke);
			}
			catch (Exception ex)
			{
				Trace.TraceError("Error invoking: {0}", ex);
			}
		}

		public void NotifyUser(string message, bool useSound = false, bool useFlashing = false)
		{
			var isHidden = WindowState == WindowState.Minimized || IsVisible == false || WindowsApi.GetForegroundWindow() != (int)interopHelper.Handle;
			// todo use this when its easy to determine what is user looking at (flash when message not seen)
			if (!string.IsNullOrEmpty(message)) systrayIcon.ShowBalloonTip(5000, "Zero-K", message, ToolTipIcon.Info);
			if (isHidden && useFlashing) FlashWindow();
			if (useSound) SystemSounds.Exclamation.Play();
		}

		public void PopupSelf()
		{
			if (Dispatcher.CheckAccess())
			{
				Visibility = Visibility.Visible;
				WindowState = WindowState.Minimized;
				Visibility = Visibility.Visible;
				;
				WindowState = lastState;
				Focus();
				Topmost = true;
				Topmost = false;
			}
			else InvokeFunc(PopupSelf);
		}

		/// <summary>
		/// Flashes window if its not foreground - until it is foreground
		/// </summary>
		protected void FlashWindow()
		{
			if (!IsFocused || !IsVisible || WindowState == WindowState.Minimized)
			{
				Visibility = Visibility.Visible;
				var info = new WindowsApi.FLASHWINFO();
				info.hwnd = interopHelper.Handle;
				info.dwFlags = 0x0000000C | 0x00000003; // flash all until foreground
				info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
				WindowsApi.FlashWindowEx(ref info);
			}
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


		void TorrentManager_DownloadAdded(object sender, EventArgs<Download> e)
		{
			Dispatcher.Invoke(new Action(() => Program.NotifySection.AddBar(new DownloadBar(e.Data))));
		}

		void Window_Closing(object sender, CancelEventArgs e)
		{
			//hack ?if (e.CloseReason == CloseReason.UserClosing) Program.IsCrash = false;

			Program.CloseOnNext = true;
			if (Program.TasClient != null) Program.TasClient.Disconnect();
			Program.Conf.LastWindowState = WindowState;
			Program.SaveConfig();
		}

		void Window_Loaded(object sender, RoutedEventArgs e)
		{
			;
			if (Debugger.IsAttached) Title = "==== DEBUGGING ===";
			else if (ApplicationDeployment.IsNetworkDeployed) Title = "Zero-K lobby";
			else Title += " not installed properly - update from http://zero-k.info/lobby";

			// hack Icon = Resources.ZkIcon;
			// hack systrayIcon.Icon = Resources.ZkIcon;

			Program.SpringScanner.Start();

			if (Program.Conf.StartMinimized) WindowState = WindowState.Minimized;
			else WindowState = Program.Conf.LastWindowState;
		}

		void Window_StateChanged(object sender, EventArgs e)
		{
			if (WindowState != WindowState.Minimized) lastState = WindowState;
			else if (Program.Conf.MinimizeToTray) Visibility = Visibility.Hidden;
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


		void timer1_Tick(object sender, EventArgs e)
		{
			UpdateDownloads();
			UpdateSystrayToolTip();
		}
	}
}