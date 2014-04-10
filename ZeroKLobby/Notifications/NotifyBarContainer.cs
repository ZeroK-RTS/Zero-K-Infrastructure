using System;
using System.Windows.Forms;

namespace ZeroKLobby.Notifications
{
    public partial class NotifyBarContainer: UserControl
    {
        public INotifyBar BarContent { get; protected set; }
        public NotifyBarContainer() {}

        public NotifyBarContainer(INotifyBar barContent)
        {
            SuspendLayout(); //suspend layout until all bar element is finish set up
            BarContent = barContent;
            InitializeComponent();
            btnDetail.Click += detail_Click;
            btnStop.Click += stop_Click;
            btnStop.Image = ZklResources.Remove;
            var control = barContent.GetControl();
            //Note: "control" is already DPI-scaled so we must downscale it
			if(Environment.OSVersion.Platform != PlatformID.Unix){
            	DpiMeasurement.DpiXYMeasurement(control);
            	Height = DpiMeasurement.ReverseScaleValueY(control.Height) + 8; //inherit height from barContent + 8 for margin (margin is for download bar).
			}
			else { //dont need scaling for Linux, the scaling bug seems to only happen in Windows
				Height = control.Height + 8;
			}
			tableLayoutPanel1.Controls.Add(control, 1, 0);
            control.Dock = DockStyle.Fill;
            Dock = DockStyle.Top;
            BarContent.AddedToContainer(this);
            ResumeLayout();
        }


        protected virtual void detail_Click(object sender, EventArgs e)
        {
            BarContent.DetailClicked(this);
        }


        protected virtual void stop_Click(object sender, EventArgs e)
        {
            BarContent.CloseClicked(this);
        }
    }
}