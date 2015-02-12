using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using ZkData;

namespace ZeroKLobby
{
    public partial class WelcomeForm : Form
    {
        WaveOut waveOut;
        Mp3FileReader audioReader;

        public WelcomeForm()
        {
            InitializeComponent();
            //BackColor = Color.Transparent;
            //SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            //BackColor = Color.FromArgb(0, Color.Empty);
            
            //BackgroundImage = null;
            //btnWindowed_Click(this, EventArgs.Empty);
        }

        
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            this.TopMost = false;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (FormBorderStyle == FormBorderStyle.None) this.TopMost = true;
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Program.ShutDown();
        }


        Image resizeStoredBackground;

        protected override void OnResizeBegin(EventArgs e)
        {
            resizeStoredBackground = BackgroundImage;
            BackgroundImage = null;
            base.OnResizeBegin(e);
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            popPanel.Width = Width / 2 - 20;
            popPanel.Left = Width / 2;
            BackgroundImage = resizeStoredBackground;
        }


        private void btnWindowed_Click(object sender, EventArgs e)
        {
            var image = BackgroundImage;
            BackgroundImage = null;
            if (FormBorderStyle == FormBorderStyle.None)
            {
                TopMost = false;
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.Sizable;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.None;
                TopMost = true;
                WindowState = FormWindowState.Maximized;
            }
            BackgroundImage = image;
        }

        private void WelcomeForm_Load(object sender, EventArgs e)
        {
            BackgroundImage = BgImages.bg_battle.GetResized(Width, Height, InterpolationMode.Default);
            BackgroundImageLayout = ImageLayout.None;

            waveOut = new WaveOut();
            audioReader =  new Mp3FileReader(new MemoryStream(Sounds.menu_music_ROM));
            waveOut.Init(audioReader);
            waveOut.Play();
        }

        private void btnSnd_Click(object sender, EventArgs e)
        {
            if (waveOut.PlaybackState == PlaybackState.Playing) waveOut.Stop();
            else {
                audioReader.Position = 0; 
                waveOut.Play();
            }
        }

        int i;
        private void singleplayerButton_Click(object sender, EventArgs e)
        {
            i++;
            switchPanel1.SwitchContent(new BitmapButton() { Width = 100, Height = 50, Text = "test " + i });
        }
    }
}
