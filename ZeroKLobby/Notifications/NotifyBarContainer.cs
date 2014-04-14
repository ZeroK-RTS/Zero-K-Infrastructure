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
            //Note: control-element is already DPI-scaled and might be DPI-scaled again, and we dont need more scaling
			//Set size to control-element's maximum size (if defined in Program.cs). If not defined then use current height (hopefully the control-element is only used once)
			Height = (control.MaximumSize.Height>0?control.MaximumSize.Height:control.Height) + DpiMeasurement.ScaleValueY(8);
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