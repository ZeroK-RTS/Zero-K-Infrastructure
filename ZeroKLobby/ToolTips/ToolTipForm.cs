using System;
using System.Diagnostics;
using System.Drawing;
using System.Web.Services.Description;
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

        const int BorderWidth = 20;
        const int BorderHeight = 20;

        protected override CreateParams CreateParams
        {
            get
            {
                var baseParams = base.CreateParams;
                baseParams.ExStyle &= ~WS_EX_APPWINDOW; //hide from taskbar
                baseParams.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                baseParams.ExStyle |= WS_EX_TOPMOST; //always on top
                baseParams.Style |= WS_DISABLED; //prevent focus
                //usefull tool to check ExStyle content: Console.WriteLine("0x{0:x8}", baseParams.ExStyle);
                return baseParams;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        private IToolTipRenderer toolTipRenderer;
        private bool active = false;
        /// <summary>
        /// Indicate whether tooltip have item to draw. If it have no item to draw it will show empty box
        /// </summary>
        public bool IsDrawing { get { return active; } }


        public ToolTipForm(IToolTipRenderer renderer)
        {
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.FromArgb(255, 255, 225);
            toolTipRenderer = renderer;

            //BringToFront();
            Font = Config.GeneralFont;
            ForeColor = Program.Conf.TooltipColor;

            SetStyle(ControlStyles.UserPaint | ControlStyles.UserMouse | ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.Selectable, false);

            if (Environment.OSVersion.Platform == PlatformID.Unix) {
                BackColor = ((SolidBrush)FrameBorderRenderer.Styles[FrameBorderRenderer.StyleType.TechPanel].FillBrush).Color;
            } else {
                AllowTransparency = true;
                BackColor = Color.FromArgb(255, 255, 0, 255);
                TransparencyKey = BackColor;
            }
            
            
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
            if (toolTipRenderer != null) {
                var s = toolTipRenderer.GetSize(Font);
                if (s != null) {
                    return new Size(s.Value.Width+BorderWidth, s.Value.Height + BorderHeight); // add borders
                }
            }
            return null;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            try {
                using (var bgbrush = new SolidBrush(BackColor))
                {
                    e.Graphics.FillRectangle(bgbrush,e.ClipRectangle);
                }
                FrameBorderRenderer.Instance.RenderToGraphics(e.Graphics, DisplayRectangle, FrameBorderRenderer.StyleType.TechPanel);
            } catch (Exception ex) {
                Trace.TraceError("Error rendering tooltip bg: {0}",ex);
            }
        }

        protected override void OnPaint([NotNull] PaintEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");
            if (toolTipRenderer == null || !active) return;
            try
            {
                e.Graphics.TranslateTransform(BorderWidth/2, BorderHeight/2); // border shift
                toolTipRenderer.Draw(e.Graphics, Font, ForeColor);
                e.Graphics.TranslateTransform(-BorderWidth / 2, -BorderHeight / 2); // border shift
            }
            catch (Exception ex)
            {
                Trace.TraceError("Tooltip paint error: {0}", ex.ToString());
            }
        }
    }
}