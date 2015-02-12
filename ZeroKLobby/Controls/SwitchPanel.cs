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
        Timer timer = new Timer();

        public SwitchPanel()
        {
            InitializeComponent();
            BackColor = Color.Transparent;
            DoubleBuffered = true;
        }

        Control currentTarget;

        public async Task SwitchContent(Control newTarget)
        {
            var r = ClientRectangle;

            var stepCount = 10;
            var stepDelay = 10;

            if (currentTarget != null) {
                var player = new SoundPlayer(Sounds.panel_move);
                player.Play();
                currentTarget.Width = r.Width;
                currentTarget.Height = r.Height;
                currentTarget.Left = r.Left;
                currentTarget.Top = r.Top;
                currentTarget.Dock = DockStyle.None;
                for (int i = 0; i < stepCount; i++) {
                    currentTarget.Left = 0 - r.Width*i/stepCount;
                    await Task.Delay(stepDelay);
                }
                this.Controls.Remove(currentTarget);
                this.Controls.Clear();

                newTarget.Width = DisplayRectangle.Width;
                newTarget.Height = DisplayRectangle.Height;
                newTarget.Left = -r.Width;
                newTarget.Top = 0;
                newTarget.Dock = DockStyle.None;
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
