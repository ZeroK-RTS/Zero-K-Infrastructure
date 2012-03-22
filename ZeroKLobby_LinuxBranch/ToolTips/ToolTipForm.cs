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

        protected override CreateParams CreateParams
        {
            get
            {
                var baseParams = base.CreateParams;
                baseParams.ExStyle &= ~WS_EX_APPWINDOW;
                baseParams.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                return baseParams;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        readonly IToolTipRenderer toolTipRenderer;


        public ToolTipForm(IToolTipRenderer renderer)
        {
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.FromArgb(255, 255, 225);
            toolTipRenderer = renderer;

            BringToFront();

            SetStyle(ControlStyles.UserPaint | ControlStyles.UserMouse | ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.Selectable, false);
        }


        /// <summary>
        /// Creates new tooltip form for displaying given tooltip
        /// </summary>
        public static ToolTipForm CreateToolTipForm(string text)
        {
            IToolTipRenderer renderer = null;
            if (text.StartsWith("#user#")) renderer = new PlayerTooltipRenderer(text.Substring(6));
            else if (text.StartsWith("#battle#")) renderer = new BattleTooltipRenderer(int.Parse(text.Substring(8)));
            else if (text.StartsWith("#map#")) renderer = new MapTooltipRenderer(text.Substring(5));
            else renderer = new TextTooltipRenderer(text);

            var nt = new ToolTipForm(renderer);
            var size = nt.GetTooltipSize();
            if (size != null) nt.Size = size.Value;
            else
            {
                nt.Dispose();
                return null;
            }

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
            e.Graphics.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);
            if (toolTipRenderer != null) toolTipRenderer.Draw(e.Graphics, Font, ForeColor);
        }
    }
}