using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using PlasmaDownloader;

namespace ZeroKLobby.Notifications
{
    public partial class DownloadBar: UserControl, INotifyBar
    {
        string fileName;
        bool imageLoaded;

        public Download Download { get; set; }


        public DownloadBar()
        {
            InitializeComponent();
        }

        public DownloadBar(Download down): this()
        {
            Dock = DockStyle.Top;
            Download = down;
        }


        public void UpdateInfo()
        {
            if (Download == null) // Probably will never happen.
            {
                label.Text = "Loading...";
                progress.Value = 0;
            }
            else
            {
                LoadMinimapImage(Download.Name);

                var dl = (long)(Download.TotalProgress/100.0*Download.TotalLength);

                if (Download.IsComplete == false) label.Text = Download.Name + " Failed";
                else
                {
                    label.Text = string.Format("{0} - {1} remaining - {2:F1}B of {3:F1}B - {4:F1}B/s",
                                               Download.Name,
                                               Download.TimeRemaining,
                                               Utils.PrintByteLength(dl),
                                               Utils.PrintByteLength(Download.TotalLength),
                                               Utils.PrintByteLength(Download.CurrentSpeed));
                }
                if (Download.TotalProgress == 0) progress.Style = ProgressBarStyle.Marquee;
                else progress.Style = ProgressBarStyle.Continuous;
                progress.Value = (int)Math.Min(Download.TotalProgress*100, 10000);
            }
            toolTip1.SetToolTip(label, label.Text);
        }


        void LoadMinimapImage(string fn)
        {
            if (!imageLoaded && progress.Width > 0)
            {
                imageLoaded = true;
                var wc = new WebClient { Proxy = null };
                wc.DownloadDataCompleted += minimapImageLoadComplete;
            	fileName = fn + ".sd7";
                var uri = new Uri("http://springfiles.com/mini/" + Uri.EscapeUriString(fileName) + "_minimap.jpg");
                wc.DownloadDataAsync(uri);
            }
        }


        public Control GetControl()
        {
            return this;
        }

        public void AddedToContainer(NotifyBarContainer container)
        {
            container.btnDetail.BackgroundImageLayout = ImageLayout.Center;
            container.btnDetail.BackgroundImage = Resources.XferDown;
            container.btnDetail.Enabled = false;
        }

        public void CloseClicked(NotifyBarContainer container)
        {
            container.btnStop.Enabled = false;
            //progress.Value = 0;
            Download.Abort();
        }

        public void DetailClicked(NotifyBarContainer container) {}

        void minimapBox_Click(object sender, EventArgs e)
        {
            Utils.OpenWeb("http://springfiles.com/search_result.php?select_select=select_file_name&search=" + Uri.EscapeDataString(fileName));
        }

        void minimapImageLoadComplete(object sender, DownloadDataCompletedEventArgs e)
        {
            if (e.Error == null && !e.Cancelled && e.Result != null)
            {
                var ms = new MemoryStream(e.Result);
                var minimapImage = Image.FromStream(ms);
                var w = minimapImage.Width;
                var h = minimapImage.Height;
                w = (int)Math.Round(w*((double)(Height)/h));
                h = Height;
                minimapBox.Location = new Point(Width - w, 0);
                minimapBox.Width = w;
                minimapBox.Height = h;

                Image.GetThumbnailImageAbort myCallback = () => false;
                minimapBox.WaitOnLoad = true;
                minimapBox.Image = minimapImage.GetThumbnailImage(w, h, myCallback, IntPtr.Zero);

                progress.Width = progress.Width - w;
                label.Width = label.Width - w;
            }
        }
    }
}