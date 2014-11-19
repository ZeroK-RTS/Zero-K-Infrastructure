using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZeroKLobby.Notifications
{
    public partial class NotifyBarContainer : UserControl
    {
        public INotifyBar BarContent { get; protected set; }
        public NotifyBarContainer() { }

        Label lbTitle;

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
            Height = (control.MaximumSize.Height > 0 ? control.MaximumSize.Height : control.Height) + DpiMeasurement.ScaleValueY(28);
            lbTitle = new Label() { Font = new Font("Microsoft Sans Serif", 13.25F, FontStyle.Bold), ForeColor = Color.DarkCyan, AutoSize = true};
            tableLayoutPanel1.Controls.Add(lbTitle,1,0);
            
            tableLayoutPanel1.Controls.Add(control, 1, 1);
            control.Dock = DockStyle.Fill;
            Dock = DockStyle.Top;
            BarContent.AddedToContainer(this);
            ResumeLayout();
        }

        public string Title
        {
            set { lbTitle.Text = value; }
        }

        public string TitleTooltip
        {
            set { Program.ToolTip.SetText(lbTitle, value); }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            using (var brush = new LinearGradientBrush(this.ClientRectangle,
                                                          Color.WhiteSmoke,
                                                          Color.SteelBlue,
                                                          90F))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
            
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