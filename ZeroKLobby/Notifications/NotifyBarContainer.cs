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
            BarContent = barContent;
            InitializeComponent();
            btnDetail.Click += detail_Click;
            btnStop.Click += stop_Click;
            btnStop.Image = Resources.Remove;
            var control = barContent.GetControl();
            
            //-- code for countering the 'DPI scaling cause bar to grow infinitely big'. The bar grew larger because each time the bar was created the bar is scaled to high-DPI, but since old height is used again for new bar: the bar get bigger & bigger after each scaling.
            DpiMeasurement.DpiXYMeasurement(this); //this measurement use cached value. It won't cost anything if another measurement was already done in other control element
            double newHeight = DpiMeasurement.ReverseScaleValueY(control.Height); //get old height. Note: DpiMeasurement is a static class stored in ZeroKLobby\Util.cs
            //--
            Height = (int) newHeight + 8; // apply height + margin (the height will again be automatically be scaled up according to Window's DPI) 
            tableLayoutPanel1.Controls.Add(control, 1, 0);
            control.Dock = DockStyle.Fill;
            Dock = DockStyle.Top;
            BarContent.AddedToContainer(this);
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