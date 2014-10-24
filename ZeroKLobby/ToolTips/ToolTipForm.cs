using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Annotations;

namespace ZeroKLobby
{
    class ToolTipForm: Form
    {
        const int CS_DROPSHADOW = 0x00020000;
        const int WS_EX_NOACTIVATE = 0x08000000;
        const int WS_EX_TOOLWINDOW = 0x80;
        const int WS_EX_APPWINDOW = 0x00040000;
        const int WS_EX_TOPMOST = 0x00000008;
        const int WS_EX_TRANSPARENT = 0x00000020;
        const int WS_DISABLED = 0x08000000;

        private static PlayerTooltipRenderer playerTooltipRenderer;
        private static BattleTooltipRenderer battleTooltipRenderer;
        private static MapTooltipRenderer mapTooltipRenderer;
        private static TextTooltipRenderer textTooltipRenderer;
        private static ToolTipForm nt;

        protected override CreateParams CreateParams
        {
            get
            {
                var baseParams = base.CreateParams;
                //baseParams.ExStyle &= ~WS_EX_APPWINDOW;
                baseParams.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                baseParams.Style |= WS_DISABLED; //prevent focus
                //usefull tool to check ExStyle content: Console.WriteLine("0x{0:x8}", baseParams.ExStyle);
                return baseParams;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        private IToolTipRenderer toolTipRenderer;
        private bool active = false;
        public bool IsActive { get { return active; } 
            set { 
                this.Visible = value;
                active =  value;
            }
        }


        public ToolTipForm(IToolTipRenderer renderer)
        {
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.FromArgb(255, 255, 225);
            toolTipRenderer = renderer;

            //BringToFront();
            

            this.ShowInTaskbar = false; //hide from taskbar, method 3;
            this.TopMost = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.UserMouse | ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.Selectable, false);
        }


        /// <summary>
        /// Creates new tooltip form for displaying given tooltip
        /// </summary>
        public static ToolTipForm CreateToolTipForm(string text)
        {
            IToolTipRenderer renderer = null;

            if (playerTooltipRenderer == null) 
            {
                playerTooltipRenderer = new PlayerTooltipRenderer();
                battleTooltipRenderer = new BattleTooltipRenderer();
                mapTooltipRenderer = new MapTooltipRenderer();
                textTooltipRenderer = new TextTooltipRenderer();
            }

            if (text.StartsWith ("#user#")) 
            {
                playerTooltipRenderer.SetPlayerTooltipRenderer (text.Substring (6));
                renderer = playerTooltipRenderer;
            } 
            else if (text.StartsWith ("#battle#")) 
            {
                battleTooltipRenderer.SetBattleTooltipRenderer (int.Parse (text.Substring (8)));
                renderer = battleTooltipRenderer;
            } 
            else if (text.StartsWith ("#map#")) 
            {
                mapTooltipRenderer.SetMapTooltipRenderer (text.Substring (5));
                renderer = mapTooltipRenderer;
            } 
            else 
            {
                textTooltipRenderer.SetTextTooltipRenderer (text);
                renderer = textTooltipRenderer;
            }

            if (nt == null)
                nt = new ToolTipForm (renderer);
            else
                nt.toolTipRenderer = renderer;

            var size = nt.GetTooltipSize();
            if (size != null) 
            {
                nt.Size = size.Value;
                nt.active = true;
            }
            else
                nt.active = false;

            return nt;
        }


        public Size? GetTooltipSize()
        {
            if (toolTipRenderer != null) return toolTipRenderer.GetSize(Font);
            else return null;
        }

        protected override void OnPaint([NotNull] PaintEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");
            base.OnPaint(e);
            if (toolTipRenderer == null)
                return;
            e.Graphics.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);
            toolTipRenderer.Draw(e.Graphics, Font, ForeColor);
        }
    }
}