using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZeroKLobby.Controls
{
    public partial class SwitchPanel : Panel
    {
        public SwitchPanel()
        {
            InitializeComponent();

            BackColor = Color.Transparent;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            DoubleBuffered = true;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public override Color BackColor { get; set; }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        protected override bool DoubleBuffered { get; set; }

        Control currentTarget;
        
        public async Task SwitchContent(Control newTarget, bool animate = true)
        {
            var r = ClientRectangle;

            
            if (currentTarget != null && animate) {
                var stepCount = 10;
                var stepDelay = 10;
                SoundPalette.Play(SoundPalette.SoundType.Servo);

                currentTarget.Dock = DockStyle.None;
                currentTarget.Width = r.Width;
                currentTarget.Height = r.Height;
                currentTarget.Left = r.Left;
                currentTarget.Top = r.Top;
                for (int i = 0; i < stepCount; i++) {
                    currentTarget.Left = 0 - r.Width*i/stepCount;
                    await Task.Delay(stepDelay);
                }
                this.Controls.Remove(currentTarget);
                this.Controls.Clear();

                newTarget.Dock = DockStyle.None;
                newTarget.Width = DisplayRectangle.Width;
                newTarget.Height = DisplayRectangle.Height;
                newTarget.Left = -r.Width;
                newTarget.Top = 0;
                this.Controls.Add(newTarget);
                
                for (int i = stepCount; i >=0; i--)
                {
                    newTarget.Left = 0 - r.Width * i / stepCount;
                    await Task.Delay(stepDelay);
                }

                newTarget.Dock = DockStyle.Fill;
                currentTarget = newTarget;
            } else {
                newTarget.Dock = DockStyle.Fill;
                this.Controls.Add(newTarget);
                currentTarget = newTarget;
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }
    }
}
