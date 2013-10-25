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
            if (!imageLoaded)
            {
                Program.SpringScanner.MetaData.GetMapAsync(fn,
                                                           (map, minimap, heightmap, metalmap) => Program.MainWindow.InvokeFunc(() =>
                                                               {
                                                                   var ms = new MemoryStream(minimap);
                                                                   var minimapImage = Image.FromStream(ms);

                                                                   var w = map.Size.Width;
                                                                   var h = map.Size.Height;
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
                                                               }),
                                                           e => { },
                                                           null);
            }
            imageLoaded = true;
        }



        public Control GetControl()
        {
            return this;
        }

        public void AddedToContainer(NotifyBarContainer container)
        {
            container.btnDetail.BackgroundImageLayout = ImageLayout.Center;
            container.btnDetail.BackgroundImage = ZklResources.WebDown;
            container.btnDetail.Enabled = false;
        }

        public void CloseClicked(NotifyBarContainer container)
        {
            container.btnStop.Enabled = false;
            //progress.Value = 0;
            Download.Abort(); //IsAborted = true; . when IsAborted is TRUE it also cause periodic check in MainWindow.cs to remove DownloadBar
        }

        public void DetailClicked(NotifyBarContainer container) {}

        void minimapBox_Click(object sender, EventArgs e)
        {
            Utils.OpenWeb("http://zero-k.info/Maps/DetailName?name=" + Uri.EscapeDataString(Download.Name), true);
        }
    }
}